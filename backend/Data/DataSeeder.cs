using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Data;

public static class DataSeeder
{
    private const string LigaProfesionalName = "Liga Profesional";
    private const string ClausuraEditionName = "Clausura 2026";
    private const string CopaLibertadoresName = "Copa Libertadores";
    private const string FaseDeGruposEditionName = "Fase de Grupos 2026";

    private static readonly string[] RoundNames = { "Fecha 1", "Fecha 2", "Fecha 3", "Fecha 4", "Fecha 5" };
    private static readonly (string Name, string ShortName)[] ArgentineTeams =
    {
        ("Boca Juniors", "Boca"), ("River Plate", "River"), ("Racing Club", "Racing"),
        ("Independiente", "Independiente"), ("Estudiantes de La Plata", "Estudiantes"), ("Gimnasia y Esgrima La Plata", "Gimnasia"),
        ("San Lorenzo", "San Lorenzo"), ("Huracán", "Huracán"), ("Vélez Sarsfield", "Vélez"),
        ("Rosario Central", "Rosario Central"), ("Newell's Old Boys", "Newell's"), ("Talleres", "Talleres"),
        ("Belgrano", "Belgrano"), ("Argentinos Juniors", "Argentinos"), ("Lanús", "Lanús"),
        ("Banfield", "Banfield"), ("Defensa y Justicia", "Defensa"), ("Tigre", "Tigre")
    };
    private const string DefaultCompanyName = "PlayPredict";
    private const string DemoExperienceName = "PlayPredict Demo";
    private const string DemoLeagueName = "Liga General - Liga Profesional (demo)";
    private const string CopaLibertadoresOfficialLeagueName = "Liga General - Copa Libertadores (demo)";

    // Fallback usado únicamente si no hay contraseña configurada (ver SeedAdminUsersAsync).
    // Nunca reutilizar en un entorno real: el método se niega a correr fuera de Development.
    private const string DefaultDevAdminPassword = "admin123";

    private static readonly (string Email, string FirstName, string LastName)[] DevAdminUsers =
    {
        ("admin@playpredict.local", "Administrador", "General"),
        ("admin2@playpredict.local", "Administradora", "Dos"),
        ("admin3@playpredict.local", "Administrador", "Tres"),
    };

    private const string RankingDemoPassword = "demo123";

    private static readonly (string Email, string FirstName, string LastName)[] RankingDemoUsers =
    {
        ("ana.torres@playpredict.local", "Ana", "Torres"),
        ("juan.perez@playpredict.local", "Juan", "Pérez"),
        ("maria.lopez@playpredict.local", "María", "López"),
        ("pedro.gomez@playpredict.local", "Pedro", "Gómez"),
    };

    // Partidos de las 5 Fechas de Clausura 2026.
    // Cada sub-array es una Fecha, cada tupla es (Local, Visitante).
    private static readonly (string Home, string Away)[][] ClausuraMatchups =
    {
        // Fecha 1
        new[] { ("Boca Juniors", "River Plate"), ("Racing Club", "Independiente"), ("Estudiantes", "Gimnasia") },
        // Fecha 2
        new[] { ("River Plate", "Racing Club"), ("Independiente", "Estudiantes"), ("Gimnasia", "Boca Juniors") },
        // Fecha 3
        new[] { ("Boca Juniors", "Independiente"), ("Racing Club", "Gimnasia"), ("Estudiantes", "River Plate") },
        // Fecha 4
        new[] { ("River Plate", "Gimnasia"), ("Independiente", "Boca Juniors"), ("Estudiantes", "Racing Club") },
        // Fecha 5
        new[] { ("Boca Juniors", "Estudiantes"), ("Racing Club", "River Plate"), ("Gimnasia", "Independiente") },
    };

    // Resultados oficiales de las Fechas 1-3 (ya finalizadas).
    // Las Fechas 4-5 no tienen resultado (partidos futuros/pronosticables).
    private static readonly (string Home, string Away, int HomeGoals, int AwayGoals)[] RankingDemoMatches =
    {
        // Fecha 1
        ("Boca Juniors", "River Plate", 2, 1),
        ("Racing Club", "Independiente", 1, 1),
        ("Estudiantes", "Gimnasia", 0, 2),
        // Fecha 2
        ("River Plate", "Racing Club", 3, 0),
        ("Independiente", "Estudiantes", 1, 1),
        ("Gimnasia", "Boca Juniors", 0, 1),
        // Fecha 3
        ("Boca Juniors", "Independiente", 2, 0),
        ("Racing Club", "Gimnasia", 1, 2),
        ("Estudiantes", "River Plate", 2, 2),
    };

    // Pronósticos de demostración por usuario para las Fechas 1-3 (9 partidos evaluados).
    // Las Fechas 4-5 no tienen pronósticos demo (el usuario los carga en vivo).
    private static readonly Dictionary<string, (int Home, int Away)[]> RankingDemoPredictions = new()
    {
        ["ana.torres@playpredict.local"] = new[] { (2, 1), (0, 0), (1, 2), (3, 1), (1, 0), (0, 1), (2, 0), (0, 1), (1, 1) },
        ["juan.perez@playpredict.local"] = new[] { (1, 0), (1, 1), (0, 2), (2, 0), (1, 1), (1, 2), (1, 0), (1, 2), (2, 1) },
        ["maria.lopez@playpredict.local"] = new[] { (1, 2), (1, 1), (0, 1), (1, 1), (0, 1), (2, 0), (0, 1), (2, 1), (1, 2) },
        ["pedro.gomez@playpredict.local"] = new[] { (2, 1), (2, 0), (1, 0), (3, 0), (2, 1), (1, 0), (2, 1), (0, 0), (3, 1) },
    };

    // Corre en todos los entornos: Registro/Login necesitan la Empresa y los Roles ya existentes.
    public static async Task SeedCoreDataAsync(PlayPredictDbContext db)
    {
        if (!await db.Companies.AnyAsync())
        {
            db.Companies.Add(new Company
            {
                Name = DefaultCompanyName,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            });
        }

        foreach (var roleName in new[] { RoleNames.Admin, RoleNames.Player })
        {
            if (!await db.Roles.AnyAsync(r => r.Name == roleName))
            {
                db.Roles.Add(new Role { Name = roleName });
            }
        }

        var existingTeamNames = await db.Teams.Select(t => t.Name).ToListAsync();
        foreach (var (name, shortName) in ArgentineTeams.Where(t => !existingTeamNames.Contains(t.Name)))
        {
            db.Teams.Add(new Team { Name = name, ShortName = shortName, Sport = "Fútbol", Active = true });
        }

        await db.SaveChangesAsync();
    }

    // Corre en todos los entornos: toda Edición debe contar con configuración de puntuación.
    // Crea la configuración inicial (6 / 3 / 0) para cualquier Edición que todavía no la tenga
    // (ediciones creadas antes de este Sprint). Las nuevas Ediciones ya la reciben al crearse
    // desde el endpoint correspondiente.
    public static async Task SeedEditionScoringConfigurationsAsync(PlayPredictDbContext db)
    {
        var editionIdsWithConfig = await db.EditionScoringConfigurations
            .Select(c => c.EditionId)
            .ToListAsync();

        var editionIdsWithoutConfig = await db.Editions
            .Where(e => !editionIdsWithConfig.Contains(e.Id))
            .Select(e => e.Id)
            .ToListAsync();

        if (editionIdsWithoutConfig.Count == 0)
        {
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var editionId in editionIdsWithoutConfig)
        {
            db.EditionScoringConfigurations.Add(new EditionScoringConfiguration
            {
                EditionId = editionId,
                ExactScorePoints = 6,
                CorrectOutcomePoints = 3,
                IncorrectPoints = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        await db.SaveChangesAsync();
    }

    // Catálogo mínimo, explícitamente ficticio, para probar Jugador Preferido sin atribuir
    // planteles reales desactualizados. Es idempotente y sólo completa los equipos del fixture demo.
    public static async Task SeedDemoTeamPlayersAsync(PlayPredictDbContext db)
    {
        var teams = await db.Teams.Where(t => t.Active).ToListAsync();
        foreach (var team in teams)
        {
            for (var number = 1; number <= 3; number++)
            {
                var displayName = $"Jugador Demo {team.ShortName} {number}";
                if (await db.TeamPlayers.AnyAsync(p => p.TeamId == team.Id && p.DisplayName == displayName)) continue;
                db.TeamPlayers.Add(new TeamPlayer
                {
                    TeamId = team.Id,
                    FirstName = "Jugador",
                    LastName = $"Demo {number}",
                    DisplayName = displayName,
                    ShirtNumber = number,
                    Position = number == 1 ? "Delantero" : number == 2 ? "Mediocampista" : "Defensor",
                    Active = true
                });
            }
        }
        await db.SaveChangesAsync();
    }

    // Sólo en Development: 3 usuarios ADMIN de ejemplo para poder entrar al panel sin
    // registro previo (Sprint 8.5, decisión 2: la instalación inicial contiene 3 ADMIN).
    // Contraseña tomada de configuración/variable de entorno (DevSeed:AdminPassword);
    // solo cae al valor por defecto si no hay ninguna configurada. Guarda explícita
    // además del gate por entorno que ya aplica el llamador (Program.cs): este método
    // se niega a ejecutarse si, por error, se lo invocara fuera de Development.
    public static async Task SeedAdminUsersAsync(PlayPredictDbContext db, IConfiguration configuration, IHostEnvironment environment)
    {
        if (!environment.IsDevelopment())
        {
            throw new InvalidOperationException(
                "SeedAdminUsersAsync crea usuarios ADMIN de desarrollo con contraseñas conocidas: nunca debe ejecutarse fuera de Development.");
        }

        var password = configuration["DevSeed:AdminPassword"];
        if (string.IsNullOrWhiteSpace(password))
        {
            password = DefaultDevAdminPassword;
        }

        var company = await db.Companies.OrderBy(c => c.Id).FirstAsync(c => c.IsActive);
        var adminRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Admin);
        var hasher = new PasswordHasher<User>();

        foreach (var (email, firstName, lastName) in DevAdminUsers)
        {
            if (await db.Users.AnyAsync(u => u.Email == email))
            {
                continue;
            }

            var admin = new User
            {
                CompanyId = company.Id,
                FirstName = firstName,
                LastName = lastName,
                Email = email,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            admin.PasswordHash = hasher.HashPassword(admin, password);
            admin.UserRoles.Add(new UserRole { Role = adminRole });

            db.Users.Add(admin);
        }

        await db.SaveChangesAsync();
    }

    // Sólo en Development: reutiliza la Experience "PlayPredict Demo" ya creada por la
    // migración `AddExperiences` (garantizada en todos los entornos para mantener la
    // compatibilidad de las Competencias existentes); la crea de forma defensiva si no
    // existiera. Idempotente por nombre — nunca duplica.
    private static async Task<Experience> GetOrCreateDemoExperienceAsync(PlayPredictDbContext db)
    {
        var experience = await db.Experiences.FirstOrDefaultAsync(e => e.Name == DemoExperienceName);
        if (experience is not null)
        {
            return experience;
        }

        var now = DateTime.UtcNow;
        experience = new Experience
        {
            Name = DemoExperienceName,
            Description = "Experiencia de demostración generada automáticamente para mantener la compatibilidad de las Competencias existentes al incorporar el modelo de Experiencias.",
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

    // Sólo en Development: Liga de demostración para que los Pronósticos sembrados
    // (Sprint 8.5) tengan un LeagueId válido. Reutiliza cualquier Liga que ya exista
    // Oficial para esta Competencia (por ejemplo, la Liga técnica creada por el backfill
    // de `AddLeagues`). Una Liga privada del usuario nunca reemplaza a la Oficial: ambas
    // usan el mismo fixture, pero son ámbitos de participación diferentes.
    private static async Task<League> GetOrCreateDemoLeagueAsync(PlayPredictDbContext db, int competitionId, int ownerUserId)
    {
        var league = await db.Leagues.FirstOrDefaultAsync(l =>
            l.CompetitionId == competitionId && l.LeagueType == LeagueType.Official);
        if (league is not null)
        {
            return league;
        }

        var now = DateTime.UtcNow;
        var editionId = await db.Editions
            .Where(e => e.CompetitionId == competitionId)
            .OrderByDescending(e => e.StartDateUtc)
            .Select(e => e.Id)
            .FirstAsync();
        league = new League
        {
            Name = DemoLeagueName,
            CompetitionId = competitionId,
            EditionId = editionId,
            ScopeType = LeagueScopeType.FullCompetition,
            LeagueType = LeagueType.Official,
            InviteCode = "DEMO-LIGA-01",
            IsActive = true,
            CreatedByUserId = ownerUserId,
            CreatedAtUtc = DateTime.UnixEpoch,
            UpdatedAtUtc = now
        };
        db.Leagues.Add(league);
        await db.SaveChangesAsync();

        return league;
    }

    public static async Task SeedAsync(PlayPredictDbContext db)
    {
        var demoExperience = await GetOrCreateDemoExperienceAsync(db);

        await SeedCompetitionAsync(
            db,
            demoExperience.Id,
            LigaProfesionalName,
            "Competencia de demostración generada por el seed de desarrollo.",
            ClausuraEditionName,
            ClausuraMatchups);

        // Copa Libertadores: 5 Fechas con equipos sudamericanos (no participa en el circuito jugable demo).
        await SeedCompetitionAsync(
            db,
            demoExperience.Id,
            CopaLibertadoresName,
            "Competencia de demostración generada por el seed de desarrollo.",
            FaseDeGruposEditionName,
            new[] {
                new[] { ("River Plate", "Flamengo"), ("Palmeiras", "Boca Juniors"), ("Atlético Nacional", "Peñarol") },
                new[] { ("Flamengo", "Atlético Nacional"), ("Boca Juniors", "River Plate"), ("Peñarol", "Palmeiras") },
                new[] { ("Palmeiras", "Peñarol"), ("River Plate", "Boca Juniors"), ("Atlético Nacional", "Flamengo") },
                new[] { ("Flamengo", "Palmeiras"), ("Boca Juniors", "Atlético Nacional"), ("Peñarol", "River Plate") },
                new[] { ("Palmeiras", "River Plate"), ("Atlético Nacional", "Peñarol"), ("Flamengo", "Boca Juniors") },
            });
    }

    private static async Task SeedCompetitionAsync(
        PlayPredictDbContext db,
        int experienceId,
        string competitionName,
        string description,
        string editionName,
        (string Home, string Away)[][] roundMatchups)
    {
        var matchupNames = roundMatchups.SelectMany(r => r).SelectMany(m => new[] { m.Home, m.Away }).Distinct().ToList();
        var teams = await db.Teams.Where(t => matchupNames.Contains(t.Name)).ToDictionaryAsync(t => t.Name);
        foreach (var name in matchupNames.Where(name => !teams.ContainsKey(name)))
        {
            var team = new Team { Name = name, ShortName = name.Length <= 50 ? name : name[..50], Sport = "Fútbol", Active = true };
            db.Teams.Add(team);
            teams[name] = team;
        }
        await db.SaveChangesAsync();

        var competition = await db.Competitions
            .Include(c => c.Editions)
            .ThenInclude(e => e.Rounds)
            .ThenInclude(r => r.Matches)
            .FirstOrDefaultAsync(c => c.Name == competitionName);

        var now = DateTime.UtcNow;

        if (competition is null)
        {
            // Competencia no existe: crear todo
            competition = new Competition
            {
                ExperienceId = experienceId,
                Name = competitionName,
                Description = description,
                Sport = "Fútbol",
                IsActive = true,
                CreatedAtUtc = now
            };

            var edition = new Edition
            {
                Competition = competition,
                Name = editionName,
                StartDateUtc = now,
                Status = EditionStatus.Active,
                CreatedAtUtc = now
            };

            for (var roundIndex = 0; roundIndex < roundMatchups.Length; roundIndex++)
            {
                var matchups = roundMatchups[roundIndex];
                var round = new Round
                {
                    Edition = edition,
                    Name = RoundNames.Length > roundIndex ? RoundNames[roundIndex] : $"Fecha {roundIndex + 1}",
                    Order = roundIndex + 1,
                    StartDateUtc = now.AddDays(roundIndex * 7)
                };

                for (var i = 0; i < matchups.Length; i++)
                {
                    db.Matches.Add(new Match
                    {
                        Round = round,
                        HomeTeamId = teams[matchups[i].Home].Id,
                        AwayTeamId = teams[matchups[i].Away].Id,
                        ParticipantHome = matchups[i].Home,
                        ParticipantAway = matchups[i].Away,
                        StartsAtUtc = roundIndex < 3
                            ? now.AddDays(-7 * (3 - roundIndex)).AddHours(i * 2)
                            : now.AddDays(7 * (roundIndex - 2) + 1).AddHours(i * 2),
                        Status = MatchStatus.Scheduled,
                        CreatedAtUtc = now
                    });
                }
            }

            db.Competitions.Add(competition);
            db.Editions.Add(edition);
        }
        else
        {
            // Competencia existe: agregar rounds que falten
            var edition = competition.Editions.FirstOrDefault(e => e.Name == editionName);
            if (edition is null)
            {
                edition = new Edition
                {
                    CompetitionId = competition.Id,
                    Name = editionName,
                    StartDateUtc = now,
                    Status = EditionStatus.Active,
                    CreatedAtUtc = now
                };
                db.Editions.Add(edition);
                await db.SaveChangesAsync();
            }

            var existingRounds = await db.Rounds
                .Where(r => r.EditionId == edition.Id)
                .Select(r => r.Order)
                .ToListAsync();

            for (var roundIndex = 0; roundIndex < roundMatchups.Length; roundIndex++)
            {
                if (existingRounds.Contains(roundIndex + 1))
                    continue;

                var matchups = roundMatchups[roundIndex];
                var round = new Round
                {
                    EditionId = edition.Id,
                    Name = RoundNames.Length > roundIndex ? RoundNames[roundIndex] : $"Fecha {roundIndex + 1}",
                    Order = roundIndex + 1,
                    StartDateUtc = now.AddDays(roundIndex * 7)
                };
                db.Rounds.Add(round);
                await db.SaveChangesAsync();

                for (var i = 0; i < matchups.Length; i++)
                {
                    db.Matches.Add(new Match
                    {
                        RoundId = round.Id,
                        HomeTeamId = teams[matchups[i].Home].Id,
                        AwayTeamId = teams[matchups[i].Away].Id,
                        ParticipantHome = matchups[i].Home,
                        ParticipantAway = matchups[i].Away,
                        StartsAtUtc = roundIndex < 3
                            ? now.AddDays(-7 * (3 - roundIndex)).AddHours(i * 2)
                            : now.AddDays(7 * (roundIndex - 2) + 1).AddHours(i * 2),
                        Status = MatchStatus.Scheduled,
                        CreatedAtUtc = now
                    });
                }
            }
        }

        await db.SaveChangesAsync();
    }

    // Sólo en Development: usuarios y pronósticos de demostración para poder mostrar el
    // Ranking (Sprint 6) con datos coherentes de punta a punta sobre Fecha 1 de Clausura 2026.
    // Idempotente: cada paso verifica su propia condición antes de escribir, así que correr
    // este método varias veces (reinicios del contenedor) nunca duplica nada.
    public static async Task SeedRankingDemoAsync(PlayPredictDbContext db, PredictionEvaluationService evaluationService)
    {
        var edition = await db.Editions
            .FirstOrDefaultAsync(e => e.Name == ClausuraEditionName);

        if (edition is null)
        {
            return;
        }

        var rounds = await db.Rounds
            .Include(r => r.Matches)
            .Where(r => r.EditionId == edition.Id)
            .OrderBy(r => r.Order)
            .ToListAsync();

        if (rounds.Count == 0)
        {
            return;
        }

        // Recolectar todos los partidos ordenados por fecha y hora (mismo orden que RankingDemoMatches).
        var allMatches = rounds
            .SelectMany(r => r.Matches)
            .OrderBy(m => m.StartsAtUtc)
            .ToList();

        // Solo sembrar resultados para los partidos que coincidan con RankingDemoMatches.
        // Las Fechas 4-5 (partidos futuros) no reciben resultado.
        var finishedMatches = allMatches.Take(RankingDemoMatches.Length).ToList();

        // Actualizar nombres de equipos para coincidir con RankingDemoMatches.
        for (var i = 0; i < finishedMatches.Count; i++)
        {
            finishedMatches[i].ParticipantHome = RankingDemoMatches[i].Home;
            finishedMatches[i].ParticipantAway = RankingDemoMatches[i].Away;
        }

        await db.SaveChangesAsync();

        var company = await db.Companies.OrderBy(c => c.Id).FirstAsync(c => c.IsActive);
        var playerRole = await db.Roles.FirstAsync(r => r.Name == RoleNames.Player);
        var hasher = new PasswordHasher<User>();

        var users = new Dictionary<string, User>();
        foreach (var (email, firstName, lastName) in RankingDemoUsers)
        {
            var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
            if (user is null)
            {
                user = new User
                {
                    CompanyId = company.Id,
                    FirstName = firstName,
                    LastName = lastName,
                    Email = email,
                    IsActive = true,
                    CreatedAtUtc = DateTime.UtcNow
                };
                user.PasswordHash = hasher.HashPassword(user, RankingDemoPassword);
                user.UserRoles.Add(new UserRole { Role = playerRole });
                db.Users.Add(user);
                await db.SaveChangesAsync();
            }

            users[email] = user;
        }

        // Sprint 8.5: el Pronóstico pertenece a una Liga. Se reutiliza/crea una Liga de
        // demostración sobre la misma Competencia (Liga Profesional) y se incorpora a los
        // 4 usuarios demo como Participantes antes de sembrar sus Pronósticos.
        var competitionId = await db.Editions
            .Where(e => e.Id == edition.Id)
            .Select(e => e.CompetitionId)
            .FirstAsync();
        var demoLeague = await GetOrCreateDemoLeagueAsync(db, competitionId, users[RankingDemoUsers[0].Email].Id);

        foreach (var (email, _, _) in RankingDemoUsers)
        {
            var user = users[email];
            var isParticipant = await db.LeagueParticipants.AnyAsync(lp => lp.LeagueId == demoLeague.Id && lp.UserId == user.Id && lp.LeftAtUtc == null);
            if (!isParticipant)
            {
                db.LeagueParticipants.Add(new LeagueParticipant
                {
                    LeagueId = demoLeague.Id,
                    UserId = user.Id,
                    JoinedAtUtc = DateTime.UnixEpoch
                });
            }
        }

        await db.SaveChangesAsync();

        foreach (var (email, scores) in RankingDemoPredictions)
        {
            var user = users[email];
            for (var i = 0; i < finishedMatches.Count && i < scores.Length; i++)
            {
                var exists = await db.Predictions.AnyAsync(p => p.UserId == user.Id && p.MatchId == finishedMatches[i].Id);
                if (!exists)
                {
                    db.Predictions.Add(new Prediction
                    {
                        MatchId = finishedMatches[i].Id,
                        UserId = user.Id,
                        PredictedHomeScore = scores[i].Home,
                        PredictedAwayScore = scores[i].Away,
                        CreatedAtUtc = finishedMatches[i].StartsAtUtc.AddDays(-1),
                        UpdatedAtUtc = finishedMatches[i].StartsAtUtc.AddDays(-1)
                    });
                }
            }
        }

        await db.SaveChangesAsync();

        // Resultados oficiales + evaluación automática de los pronósticos (misma vía que
        // usaría un Administrador cargando el Resultado Oficial desde el panel).
        // Acotado a finishedMatches.Count (igual que el loop de pronósticos de arriba): si la
        // Competencia ya existía en la base con menos partidos que RankingDemoMatches (idempotencia
        // por nombre en SeedCompetitionAsync), finishedMatches puede ser más chico que RankingDemoMatches.
        for (var i = 0; i < finishedMatches.Count && i < RankingDemoMatches.Length; i++)
        {
            var match = finishedMatches[i];
            if (match.Status != MatchStatus.Finished)
            {
                match.HomeGoals = RankingDemoMatches[i].HomeGoals;
                match.AwayGoals = RankingDemoMatches[i].AwayGoals;
                match.Status = MatchStatus.Finished;
                await evaluationService.PrepareEvaluationsForMatchAsync(db, match);
            }
        }

        await db.SaveChangesAsync();

        // Liga Oficial de Copa Libertadores: disponible para que los jugadores se unan
        // desde Explorar Competencias, pero sin participantes automáticos.
        var copaCompetition = await db.Competitions
            .FirstOrDefaultAsync(c => c.Name == CopaLibertadoresName);
        if (copaCompetition is not null)
        {
            var copaEditionId = await db.Editions
                .Where(e => e.CompetitionId == copaCompetition.Id)
                .OrderByDescending(e => e.StartDateUtc)
                .Select(e => e.Id)
                .FirstAsync();
            var copaLeagueExists = await db.Leagues
                .AnyAsync(l => l.CompetitionId == copaCompetition.Id && l.LeagueType == LeagueType.Official);
            if (!copaLeagueExists)
            {
                var copaLeague = new League
                {
                    Name = CopaLibertadoresOfficialLeagueName,
                    CompetitionId = copaCompetition.Id,
                    EditionId = copaEditionId,
                    ScopeType = LeagueScopeType.FullCompetition,
                    LeagueType = LeagueType.Official,
                    InviteCode = "DEMO-COPA-01",
                    IsActive = true,
                    CreatedByUserId = users[RankingDemoUsers[0].Email].Id,
                    CreatedAtUtc = DateTime.UtcNow,
                    UpdatedAtUtc = DateTime.UtcNow
                };
                db.Leagues.Add(copaLeague);
                await db.SaveChangesAsync();
            }
        }
    }

    // Sólo en Development: Premios de demostración (Sprint 7) sobre Clausura 2026 / Fecha 1.
    // El Premio no calcula nada; solo describe el ámbito y criterio, y el ganador actual se
    // deriva en tiempo real desde el Ranking (Sprint 6). Idempotente por (EditionId, Name).
    private static readonly (
        string Name,
        PrizeType Type,
        string Description,
        string? ReferenceValue,
        string? Sponsor,
        PrizeScopeType Scope,
        PrizeAwardCriteria Criteria,
        int? PositionFrom,
        int? PositionTo,
        PrizeStatus Status)[] PrizeDemoData =
    {
        ("Gran Premio Clausura 2026", PrizeType.Money,
            "Premio para el primer puesto del Ranking General.", "$50.000.000", DefaultCompanyName,
            PrizeScopeType.Edition, PrizeAwardCriteria.Position, 1, 1, PrizeStatus.Published),
        ("Segundo Premio Clausura 2026", PrizeType.Product,
            "Viaje para dos personas.", null, DefaultCompanyName,
            PrizeScopeType.Edition, PrizeAwardCriteria.Position, 2, 2, PrizeStatus.Published),
        ("Premio Fecha 1", PrizeType.Product,
            "Camiseta oficial.", null, DefaultCompanyName,
            PrizeScopeType.Round, PrizeAwardCriteria.RoundWinner, null, null, PrizeStatus.Published),
        ("Rey de los Exactos", PrizeType.Recognition,
            "Reconocimiento para quien consiga más marcadores exactos.", null, null,
            PrizeScopeType.Edition, PrizeAwardCriteria.MostExactScores, null, null, PrizeStatus.Published),
        ("Premio Sorpresa", PrizeType.Other,
            "Premio especial pendiente de publicación.", null, null,
            PrizeScopeType.Edition, PrizeAwardCriteria.Position, 3, 3, PrizeStatus.Draft),
    };

    public static async Task SeedPrizesDemoAsync(PlayPredictDbContext db)
    {
        var edition = await db.Editions.FirstOrDefaultAsync(e => e.Name == ClausuraEditionName);
        if (edition is null)
        {
            // El seed base (SeedAsync) todavía no corrió; nada que hacer todavía.
            return;
        }

        var round = await db.Rounds.FirstOrDefaultAsync(r => r.EditionId == edition.Id && r.Order == 1);

        var now = DateTime.UtcNow;
        foreach (var d in PrizeDemoData)
        {
            var exists = await db.Prizes.AnyAsync(p => p.EditionId == edition.Id && p.Name == d.Name);
            if (exists)
            {
                continue;
            }

            db.Prizes.Add(new Prize
            {
                EditionId = edition.Id,
                RoundId = d.Scope == PrizeScopeType.Round ? round?.Id : null,
                Name = d.Name,
                Description = d.Description,
                PrizeType = d.Type,
                ReferenceValue = d.ReferenceValue,
                SponsorName = d.Sponsor,
                ScopeType = d.Scope,
                AwardCriteria = d.Criteria,
                PositionFrom = d.PositionFrom,
                PositionTo = d.PositionTo,
                Status = d.Status,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });
        }

        await db.SaveChangesAsync();
    }

    /// <summary>
    /// Ajusta las fechas de los partidos demo para que siempre haya partidos
    /// pronosticables (futuros) y partidos finalizados (con resultados) sin importar
    /// cuándo se inicializó la base. Corre en cada arranque del backend en Development.
    /// </summary>
    public static async Task RefreshDemoScheduleAsync(PlayPredictDbContext db)
    {
        var edition = await db.Editions.FirstOrDefaultAsync(e => e.Name == ClausuraEditionName);
        if (edition is null) return;

        var rounds = await db.Rounds
            .Include(r => r.Matches)
            .Where(r => r.EditionId == edition.Id)
            .OrderBy(r => r.Order)
            .ToListAsync();

        if (rounds.Count == 0) return;

        var now = DateTime.UtcNow;
        var changed = false;

        for (var roundIndex = 0; roundIndex < rounds.Count; roundIndex++)
        {
            var round = rounds[roundIndex];
            var baseDate = roundIndex < 3
                ? now.AddDays(-7 * (3 - roundIndex))
                : now.AddDays(7 * (roundIndex - 2) + 1);

            if (round.StartDateUtc?.Date != baseDate.Date)
            {
                round.StartDateUtc = baseDate;
                changed = true;
            }

            for (var i = 0; i < round.Matches.Count; i++)
            {
                var match = round.Matches.OrderBy(m => m.StartsAtUtc).ElementAt(i);
                var newStart = baseDate.AddHours(i * 2);
                if (match.StartsAtUtc != newStart)
                {
                    match.StartsAtUtc = newStart;
                    changed = true;
                }

                if (roundIndex < 3 && match.Status != MatchStatus.Finished)
                {
                    match.Status = MatchStatus.Finished;
                    changed = true;
                }
                else if (roundIndex >= 3 && match.Status == MatchStatus.Finished)
                {
                    match.Status = MatchStatus.Scheduled;
                    match.HomeGoals = null;
                    match.AwayGoals = null;
                    changed = true;
                }
            }
        }

        if (changed)
        {
            await db.SaveChangesAsync();
        }
    }
}
