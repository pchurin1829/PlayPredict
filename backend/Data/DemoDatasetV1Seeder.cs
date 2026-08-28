using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Data;

/// <summary>
/// Dataset comercial reproducible para demostraciones. Todas sus identidades se resuelven
/// por claves naturales estables y cada inserción comprueba existencia, por lo que puede
/// ejecutarse varias veces sin duplicar registros.
/// </summary>
public static class DemoDatasetV1Seeder
{
    public const string Version = "PlayPredict Demo v1";
    public const string PlayerPassword = "demo123";

    private static readonly (string Name, string ShortName)[] CanonicalTeams =
    {
        ("Boca Juniors", "Boca"), ("River Plate", "River"), ("Racing Club", "Racing"),
        ("Independiente", "Independiente"), ("San Lorenzo", "San Lorenzo"),
        ("Estudiantes de La Plata", "Estudiantes"), ("Gimnasia y Esgrima La Plata", "Gimnasia"),
        ("Argentinos Juniors", "Argentinos"), ("Vélez Sarsfield", "Vélez"),
        ("Rosario Central", "Rosario Central"), ("Newell's Old Boys", "Newell's"),
        ("Huracán", "Huracán"), ("Lanús", "Lanús"), ("Banfield", "Banfield"),
        ("Belgrano", "Belgrano"), ("Talleres", "Talleres"),
        ("Defensa y Justicia", "Defensa"), ("Barracas Central", "Barracas"),
        ("Unión", "Unión"), ("Platense", "Platense"),
        ("Flamengo", "Flamengo"), ("Palmeiras", "Palmeiras"),
        ("Atlético Nacional", "Atlético Nacional"), ("Peñarol", "Peñarol")
    };

    private static readonly (string Email, string FirstName, string LastName)[] DemoPlayers =
    {
        ("rafael.demo@playpredict.local", "Rafael", "Demo"),
        ("ana.torres@playpredict.local", "Ana", "Torres"),
        ("juan.perez@playpredict.local", "Juan", "Pérez"),
        ("maria.lopez@playpredict.local", "María", "López"),
        ("pedro.gomez@playpredict.local", "Pedro", "Gómez")
    };

    private static readonly string[] FullRosterTeams =
        ["Boca Juniors", "River Plate", "Flamengo", "Palmeiras", "Atlético Nacional"];

    private static readonly (string Home, string Away)[][] LibertadoresFixture =
    {
        [("River Plate", "Flamengo"), ("Palmeiras", "Boca Juniors"), ("Atlético Nacional", "Peñarol")],
        [("Flamengo", "Atlético Nacional"), ("Boca Juniors", "River Plate"), ("Peñarol", "Palmeiras")],
        [("Palmeiras", "Peñarol"), ("River Plate", "Boca Juniors"), ("Atlético Nacional", "Flamengo")],
        [("Flamengo", "Palmeiras"), ("Boca Juniors", "Atlético Nacional"), ("Peñarol", "River Plate")],
        [("Palmeiras", "River Plate"), ("Atlético Nacional", "Peñarol"), ("Flamengo", "Boca Juniors")]
    };

    private static readonly (string Home, string Away)[][] LigaProfesionalFixture =
    {
        [("Boca Juniors", "River Plate"), ("Racing Club", "Independiente"), ("Estudiantes de La Plata", "Gimnasia y Esgrima La Plata")],
        [("River Plate", "Racing Club"), ("Independiente", "Estudiantes de La Plata"), ("Gimnasia y Esgrima La Plata", "Boca Juniors")],
        [("Boca Juniors", "Independiente"), ("Racing Club", "Gimnasia y Esgrima La Plata"), ("Estudiantes de La Plata", "River Plate")],
        [("River Plate", "Gimnasia y Esgrima La Plata"), ("Independiente", "Boca Juniors"), ("Estudiantes de La Plata", "Racing Club")],
        [("Boca Juniors", "Estudiantes de La Plata"), ("Racing Club", "River Plate"), ("Gimnasia y Esgrima La Plata", "Independiente")]
    };

    private static readonly (int Home, int Away)[] OfficialResults =
    {
        (2, 1), (1, 1), (0, 2), (3, 0), (1, 1), (2, 1), (1, 0), (2, 2), (0, 1)
    };

    public static async Task SeedAsync(PlayPredictDbContext db, PredictionEvaluationService evaluationService)
    {
        await EnsureDemoCompanyAsync(db);
        var experience = await EnsureExperienceAsync(db);
        var teams = await EnsureTeamsAsync(db);
        await NormalizeDuplicateTeamsAsync(db, teams);
        var ligaEdition = await EnsureCompetitionAsync(db, experience.Id, "Liga Profesional de Fútbol", "2026", LigaProfesionalFixture, teams);
        var copaEdition = await EnsureCompetitionAsync(db, experience.Id, "Copa Libertadores", "2026", LibertadoresFixture, teams);
        await EnsureReferenceCompetitionAsync(db, experience.Id, "Copa Argentina", "2026");
        await EnsureScoringAsync(db, ligaEdition.Id, preferredPlayerEnabled: true);
        await EnsureScoringAsync(db, copaEdition.Id, preferredPlayerEnabled: true);
        await EnsureRostersAsync(db, teams);

        await EnsureUsersAsync(db);

        // Los datos de juego se dejan vacíos para que el circuito ADMIN + PLAYER pueda
        // probarse desde cero. Resultados, pronósticos y evaluaciones se generan manualmente.
    }

    private static async Task EnsureDemoCompanyAsync(PlayPredictDbContext db)
    {
        var company = await db.Companies.OrderBy(c => c.Id).FirstAsync(c => c.IsActive);
        if (company.Name == "PlayPredict" && string.IsNullOrWhiteSpace(company.ShortName))
        {
            company.Name = "EL NENE";
            company.ShortName = "EL NENE";
        }
        else if (company.Name == "EL NENE" && string.IsNullOrWhiteSpace(company.ShortName))
        {
            company.ShortName = "EL NENE";
        }
        await db.SaveChangesAsync();
    }

    private static async Task<Experience> EnsureExperienceAsync(PlayPredictDbContext db)
    {
        var experience = await db.Experiences.FirstOrDefaultAsync(x => x.Name == Version);
        if (experience is not null) return experience;
        var now = DateTime.UtcNow;
        experience = new Experience
        {
            Name = Version,
            Description = "Dataset reproducible para demostrar el circuito ADMIN + PLAYER.",
            Status = ExperienceStatus.Published,
            IsPublic = true,
            DefaultExactScorePoints = 6,
            DefaultCorrectOutcomePoints = 3,
            DefaultIncorrectPoints = 0,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };
        db.Experiences.Add(experience);
        await db.SaveChangesAsync();
        return experience;
    }

    private static async Task EnsureReferenceCompetitionAsync(PlayPredictDbContext db, int experienceId, string competitionName, string editionName)
    {
        var now = DateTime.UtcNow;
        var competition = await db.Competitions.FirstOrDefaultAsync(c => c.Name == competitionName);
        if (competition is null)
        {
            competition = new Competition
            {
                ExperienceId = experienceId, Name = competitionName,
                Description = $"Fuente deportiva canónica de {Version}.", Sport = "Fútbol", IsActive = true, CreatedAtUtc = now
            };
            db.Competitions.Add(competition);
            await db.SaveChangesAsync();
        }

        if (!await db.Editions.AnyAsync(e => e.CompetitionId == competition.Id && e.Name == editionName))
        {
            db.Editions.Add(new Edition
            {
                CompetitionId = competition.Id, Name = editionName,
                StartDateUtc = now.Date, Status = EditionStatus.Active, CreatedAtUtc = now
            });
            await db.SaveChangesAsync();
        }
    }

    private static async Task<Dictionary<string, Team>> EnsureTeamsAsync(PlayPredictDbContext db)
    {
        var result = new Dictionary<string, Team>(StringComparer.Ordinal);
        foreach (var (name, shortName) in CanonicalTeams)
        {
            var team = await db.Teams.FirstOrDefaultAsync(t => t.Name == name);
            if (team is null)
            {
                team = new Team { Name = name, ShortName = shortName, Sport = "Fútbol", Active = true };
                db.Teams.Add(team);
                await db.SaveChangesAsync();
            }
            else
            {
                team.ShortName = shortName;
                team.Sport = "Fútbol";
                team.Active = true;
            }
            result[name] = team;
        }
        await db.SaveChangesAsync();
        return result;
    }

    private static async Task<Edition> EnsureCompetitionAsync(PlayPredictDbContext db, int experienceId,
        string competitionName, string editionName, (string Home, string Away)[][] fixture, Dictionary<string, Team> teams)
    {
        var now = DateTime.UtcNow;
        var competition = await db.Competitions.FirstOrDefaultAsync(c => c.Name == competitionName);
        if (competition is null)
        {
            competition = new Competition
            {
                ExperienceId = experienceId, Name = competitionName,
                Description = $"Fuente deportiva canónica de {Version}.", Sport = "Fútbol", IsActive = true, CreatedAtUtc = now
            };
            db.Competitions.Add(competition);
            await db.SaveChangesAsync();
        }

        var edition = await db.Editions.FirstOrDefaultAsync(e => e.CompetitionId == competition.Id && e.Name == editionName);
        if (edition is null)
        {
            edition = new Edition { CompetitionId = competition.Id, Name = editionName, StartDateUtc = now.Date, Status = EditionStatus.Active, CreatedAtUtc = now };
            db.Editions.Add(edition);
            await db.SaveChangesAsync();
        }

        for (var roundIndex = 0; roundIndex < fixture.Length; roundIndex++)
        {
            var order = roundIndex + 1;
            var baseDate = order <= 3 ? now.Date.AddDays(-7 * (4 - order)).AddHours(18) : now.Date.AddDays(order == 4 ? 2 : 9).AddHours(18);
            var round = await db.Rounds.FirstOrDefaultAsync(r => r.EditionId == edition.Id && r.Order == order);
            if (round is null)
            {
                round = new Round { EditionId = edition.Id, Name = $"Fecha {order}", Order = order, StartDateUtc = baseDate };
                db.Rounds.Add(round);
                await db.SaveChangesAsync();
            }
            else if (order >= 4)
            {
                round.StartDateUtc = baseDate;
            }

            for (var matchIndex = 0; matchIndex < fixture[roundIndex].Length; matchIndex++)
            {
                var matchup = fixture[roundIndex][matchIndex];
                var home = teams[matchup.Home];
                var away = teams[matchup.Away];
                var match = await db.Matches.FirstOrDefaultAsync(m => m.RoundId == round.Id && m.HomeTeamId == home.Id && m.AwayTeamId == away.Id);
                if (match is null)
                {
                    db.Matches.Add(new Match
                    {
                        RoundId = round.Id, HomeTeamId = home.Id, AwayTeamId = away.Id,
                        ParticipantHome = home.Name, ParticipantAway = away.Name,
                        StartsAtUtc = baseDate.AddHours(matchIndex * 2), Status = MatchStatus.Scheduled, CreatedAtUtc = now
                    });
                }
                else if (order >= 4 && match.Status != MatchStatus.Finished)
                {
                    match.StartsAtUtc = baseDate.AddHours(matchIndex * 2);
                    match.Status = MatchStatus.Scheduled;
                }
            }
            await db.SaveChangesAsync();
        }
        return edition;
    }

    private static async Task EnsureScoringAsync(PlayPredictDbContext db, int editionId, bool preferredPlayerEnabled)
    {
        var config = await db.EditionScoringConfigurations.FirstOrDefaultAsync(c => c.EditionId == editionId);
        var now = DateTime.UtcNow;
        if (config is null)
        {
            config = new EditionScoringConfiguration { EditionId = editionId, CreatedAtUtc = now };
            db.EditionScoringConfigurations.Add(config);
        }
        config.ExactScorePoints = 6;
        config.CorrectOutcomePoints = 3;
        config.IncorrectPoints = 0;
        config.UseExperienceDefaults = false;
        config.PreferredPlayerEnabled = preferredPlayerEnabled;
        config.PreferredPlayerPointsPerGoal = 2;
        config.UpdatedAtUtc = now;
        await db.SaveChangesAsync();
    }

    private static async Task EnsureRostersAsync(PlayPredictDbContext db, Dictionary<string, Team> teams)
    {
        foreach (var teamName in teams.Keys)
        {
            var team = teams[teamName];
            var activeCount = await db.TeamPlayers.CountAsync(p => p.TeamId == team.Id && p.Active);
            if (activeCount >= 4) continue;
            for (var number = 1; number <= 4; number++)
            {
                if (activeCount >= 4) break;
                var displayName = $"Demo {team.ShortName} {number}";
                if (await db.TeamPlayers.AnyAsync(p => p.TeamId == team.Id && p.DisplayName == displayName)) continue;
                db.TeamPlayers.Add(new TeamPlayer
                {
                    TeamId = team.Id, FirstName = "Jugador", LastName = $"Demo {number}", DisplayName = displayName,
                    ShirtNumber = number, Position = number switch { 1 => "Arquero", 2 => "Defensor", 3 => "Mediocampista", _ => "Delantero" }, Active = true
                });
                activeCount++;
            }
        }

        foreach (var teamName in FullRosterTeams)
        {
            var team = teams[teamName];
            var additions = new[] { ("Arquero", 2), ("Defensor", 6), ("Mediocampista", 6), ("Delantero", 3) };
            var shirt = 20;
            foreach (var (position, count) in additions)
            for (var index = 1; index <= count; index++)
            {
                var displayName = $"{team.ShortName} {position} {index}";
                if (await db.TeamPlayers.AnyAsync(p => p.TeamId == team.Id && p.DisplayName == displayName)) continue;
                db.TeamPlayers.Add(new TeamPlayer
                {
                    TeamId = team.Id,
                    FirstName = position,
                    LastName = $"{team.ShortName} {index}",
                    DisplayName = displayName,
                    ShirtNumber = shirt++,
                    Position = position,
                    Active = true
                });
            }
        }
        await db.SaveChangesAsync();
    }

    private static async Task NormalizeDuplicateTeamsAsync(PlayPredictDbContext db, IReadOnlyDictionary<string, Team> teams)
    {
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["Argentinos Jrs"] = "Argentinos Juniors",
            ["Belgrano (Córdoba)"] = "Belgrano",
            ["Estudiantes"] = "Estudiantes de La Plata",
            ["Gimnasia"] = "Gimnasia y Esgrima La Plata",
            ["Newell's"] = "Newell's Old Boys",
            ["Vélez"] = "Vélez Sarsfield"
        };

        foreach (var (alias, canonicalName) in aliases)
        {
            var duplicate = await db.Teams.FirstOrDefaultAsync(t => t.Name == alias);
            if (duplicate is null) continue;
            var canonical = teams[canonicalName];
            var homeMatches = await db.Matches.Where(m => m.HomeTeamId == duplicate.Id).ToListAsync();
            var awayMatches = await db.Matches.Where(m => m.AwayTeamId == duplicate.Id).ToListAsync();
            foreach (var match in homeMatches) { match.HomeTeamId = canonical.Id; match.ParticipantHome = canonical.Name; }
            foreach (var match in awayMatches) { match.AwayTeamId = canonical.Id; match.ParticipantAway = canonical.Name; }
            foreach (var player in await db.TeamPlayers.Where(p => p.TeamId == duplicate.Id).ToListAsync()) player.TeamId = canonical.Id;
            db.Teams.Remove(duplicate);
            await db.SaveChangesAsync();
        }
    }

    private static async Task<List<User>> EnsureUsersAsync(PlayPredictDbContext db)
    {
        var company = await db.Companies.OrderBy(c => c.Id).FirstAsync(c => c.IsActive);
        var role = await db.Roles.FirstAsync(r => r.Name == RoleNames.Player);
        var hasher = new PasswordHasher<User>();
        var users = new List<User>();
        foreach (var (email, firstName, lastName) in DemoPlayers)
        {
            var user = await db.Users.Include(u => u.UserRoles).FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                user = new User { CompanyId = company.Id, Email = email, FirstName = firstName, LastName = lastName, IsActive = true, CreatedAtUtc = DateTime.UtcNow };
                user.PasswordHash = hasher.HashPassword(user, PlayerPassword);
                user.UserRoles.Add(new UserRole { RoleId = role.Id });
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }
            users.Add(user);
        }
        return users;
    }

    private static async Task<League> EnsureLeagueAsync(PlayPredictDbContext db, string name, Edition edition,
        LeagueType type, int ownerId, IReadOnlyList<Round> rounds)
    {
        var league = await db.Leagues.FirstOrDefaultAsync(l => l.Name == name && l.EditionId == edition.Id && l.LeagueType == type);
        var now = DateTime.UtcNow;
        if (league is null)
        {
            league = new League
            {
                Name = name, Description = $"Liga de {Version}.", CompetitionId = edition.CompetitionId, EditionId = edition.Id,
                ScopeType = LeagueScopeType.RoundRange, RoundFromId = rounds.First().Id, RoundToId = rounds.Last().Id,
                LeagueType = type, InviteCode = type == LeagueType.Official ? $"OFF-{Guid.NewGuid():N}"[..16].ToUpperInvariant() : $"V1-{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
                IsActive = true, CreatedByUserId = ownerId, CreatedAtUtc = DateTime.UnixEpoch, UpdatedAtUtc = now
            };
            db.Leagues.Add(league);
            await db.SaveChangesAsync();
        }
        return league;
    }

    private static async Task EnsureParticipantsAsync(PlayPredictDbContext db, int leagueId, IEnumerable<User> users)
    {
        foreach (var user in users)
            if (!await db.LeagueParticipants.AnyAsync(p => p.LeagueId == leagueId && p.UserId == user.Id && p.LeftAtUtc == null))
                db.LeagueParticipants.Add(new LeagueParticipant { LeagueId = leagueId, UserId = user.Id, JoinedAtUtc = DateTime.UnixEpoch });
        await db.SaveChangesAsync();
    }

    private static async Task SeedPlayedRoundsAsync(PlayPredictDbContext db, int editionId, IReadOnlyList<League> leagues,
        IReadOnlyList<User> users, PredictionEvaluationService evaluationService)
    {
        var matches = await db.Matches.Include(m => m.Scorers)
            .Where(m => m.Round.EditionId == editionId && m.Round.Order <= 3)
            .OrderBy(m => m.Round.Order).ThenBy(m => m.StartsAtUtc).ToListAsync();

        for (var matchIndex = 0; matchIndex < matches.Count; matchIndex++)
        {
            var match = matches[matchIndex];
            var result = OfficialResults[matchIndex % OfficialResults.Length];
            match.HomeGoals = result.Home; match.AwayGoals = result.Away; match.Status = MatchStatus.Finished;
            var homePlayer = await db.TeamPlayers.Where(p => p.TeamId == match.HomeTeamId && p.Active).OrderBy(p => p.Id).FirstAsync();
            var awayPlayer = await db.TeamPlayers.Where(p => p.TeamId == match.AwayTeamId && p.Active).OrderBy(p => p.Id).FirstAsync();
            db.MatchScorers.RemoveRange(match.Scorers);
            match.Scorers = [];
            if (result.Home > 0) match.Scorers.Add(new MatchScorer { MatchId = match.Id, TeamPlayerId = homePlayer.Id, Goals = result.Home });
            if (result.Away > 0) match.Scorers.Add(new MatchScorer { MatchId = match.Id, TeamPlayerId = awayPlayer.Id, Goals = result.Away });

            foreach (var league in leagues)
            for (var userIndex = 0; userIndex < users.Count; userIndex++)
            {
                var user = users[userIndex];
                if (await db.Predictions.AnyAsync(p => p.UserId == user.Id && p.MatchId == match.Id)) continue;
                var (home, away) = userIndex switch
                {
                    0 => result,
                    1 => result.Home > result.Away ? (result.Home + 1, result.Away) : result.Home < result.Away ? (result.Home, result.Away + 1) : (2, 2),
                    2 => result.Home > result.Away ? (0, 1) : result.Home < result.Away ? (1, 0) : (1, 0),
                    3 => matchIndex % 2 == 0 ? result : (result.Away, result.Home),
                    _ => (Math.Max(0, result.Home - 1), result.Away)
                };
                db.Predictions.Add(new Prediction
                {
                    MatchId = match.Id, UserId = user.Id,
                    PredictedHomeScore = home, PredictedAwayScore = away,
                    PreferredPlayerId = userIndex is 0 or 4 ? homePlayer.Id : null,
                    CreatedAtUtc = match.StartsAtUtc.AddDays(-1), UpdatedAtUtc = match.StartsAtUtc.AddDays(-1)
                });
            }
            await db.SaveChangesAsync();
            await evaluationService.PrepareEvaluationsForMatchAsync(db, match);
            await db.SaveChangesAsync();
        }
    }
}
