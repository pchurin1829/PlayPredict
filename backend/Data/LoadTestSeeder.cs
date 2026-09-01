using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Data;

public static class LoadTestSeeder
{
    public const string EnvironmentName = "LoadTest";
    public const string CompanyName = "PLAYPREDICT LOADTEST";
    public const string ExperienceName = "PLAYPREDICT LOADTEST EXPERIENCE";
    public const string CompetitionName = "PLAYPREDICT LOADTEST COMPETITION";
    public const string EditionName = "LOADTEST 2026";
    public const string LeagueName = "PLAYPREDICT LOADTEST OFFICIAL";
    private const string EmailDomain = "playpredict.test";

    private static readonly (string Name, string ShortName)[] TeamDefinitions =
    {
        ("LOADTEST FC 01", "LT01"),
        ("LOADTEST FC 02", "LT02"),
        ("LOADTEST FC 03", "LT03"),
        ("LOADTEST FC 04", "LT04")
    };

    public static void ValidateSafety(
        LoadTestSeedOptions options,
        string environmentName,
        string? connectionString)
    {
        if (!options.Enabled)
            throw new InvalidOperationException("LOADTEST seed is disabled. Set LoadTest__Enabled=true explicitly.");

        if (!string.Equals(environmentName, EnvironmentName, StringComparison.Ordinal))
            throw new InvalidOperationException($"LOADTEST seed requires ASPNETCORE_ENVIRONMENT={EnvironmentName}.");

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("LOADTEST seed requires an explicit database connection string.");

        var database = new NpgsqlConnectionStringBuilder(connectionString).Database;
        if (string.IsNullOrWhiteSpace(database)
            || !database.Contains("loadtest", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("LOADTEST seed refused: database name must contain 'loadtest'.");

        if (options.UserCount is < 1 or > LoadTestSeedOptions.MaximumUserCount)
            throw new InvalidOperationException(
                $"LoadTest__UserCount must be between 1 and {LoadTestSeedOptions.MaximumUserCount}.");

        if (string.IsNullOrWhiteSpace(options.UserPassword) || options.UserPassword.Length < 8)
            throw new InvalidOperationException("LoadTest__UserPassword must contain at least 8 characters.");
    }

    public static async Task<LoadTestSeedResult> SeedAsync(
        PlayPredictDbContext db,
        LoadTestSeedOptions options,
        string environmentName,
        string? connectionString,
        ILogger logger)
    {
        ValidateSafety(options, environmentName, connectionString);
        var database = new NpgsqlConnectionStringBuilder(connectionString).Database;

        logger.LogWarning("========== LOADTEST ENVIRONMENT ==========");
        logger.LogWarning("LOADTEST database: {Database}", database);
        logger.LogWarning("LOADTEST requested users: {UserCount}", options.UserCount);

        await using var transaction = await db.Database.BeginTransactionAsync();
        var now = DateTime.UtcNow;

        await EnsureRolesAsync(db);
        var company = await EnsureCompanyAsync(db, now);
        var experience = await EnsureExperienceAsync(db, now);
        var teams = await EnsureTeamsAndPlayersAsync(db);
        var (edition, rounds) = await EnsureCompetitionAsync(db, experience.Id, now);
        await EnsureScoringAsync(db, edition.Id, now);
        var users = await EnsureUsersAsync(db, company.Id, options, now);
        var league = await EnsureLeagueAsync(db, edition, rounds, users[0].Id, now);
        var matches = await EnsureMatchesAsync(db, rounds, teams, now);
        var finishedMatches = matches.Where(match => match.Status == MatchStatus.Finished).ToList();
        var futureMatches = matches.Where(match => match.Status == MatchStatus.Scheduled).ToList();

        await EnsureHistoricalParticipationAsync(db, league.Id, users, finishedMatches, now);
        var predictions = await EnsureHistoricalPredictionsAsync(db, users, finishedMatches);
        var evaluations = await EnsureEvaluationsAsync(db, league.Id, predictions, finishedMatches, now);

        await transaction.CommitAsync();

        logger.LogWarning("LOADTEST users generated/available: {UserCount}", users.Count);
        logger.LogWarning("LOADTEST league: {LeagueName} (ID {LeagueId})", league.Name, league.Id);
        logger.LogWarning("LOADTEST matches: {Total} ({Finished} finished/evaluated, {Future} future)",
            matches.Count, finishedMatches.Count, futureMatches.Count);
        logger.LogWarning("LOADTEST seed completed successfully.");
        logger.LogWarning("==========================================");

        return new LoadTestSeedResult(users.Count, league.Id, league.Name, finishedMatches.Count,
            futureMatches.Count, predictions.Count, evaluations);
    }

    private static async Task EnsureRolesAsync(PlayPredictDbContext db)
    {
        var names = new[] { RoleNames.Admin, RoleNames.Player };
        var existing = await db.Roles.Where(role => names.Contains(role.Name))
            .Select(role => role.Name).ToListAsync();
        foreach (var name in names.Where(name => !existing.Contains(name)))
            db.Roles.Add(new Role { Name = name });
        await db.SaveChangesAsync();
    }

    private static async Task<Company> EnsureCompanyAsync(PlayPredictDbContext db, DateTime now)
    {
        var company = await db.Companies.FirstOrDefaultAsync(item => item.Name == CompanyName);
        if (company is not null) return company;

        company = new Company
        {
            Name = CompanyName,
            ShortName = "LOADTEST",
            IsActive = true,
            CreatedAtUtc = now,
            GeneralExactScorePoints = 6,
            GeneralCorrectOutcomePoints = 3,
            GeneralIncorrectPoints = 0,
            GeneralPreferredPlayerEnabled = true,
            GeneralPreferredPlayerPointsPerGoal = 2,
            GeneralPreferredPlayerPositions = PlayerPosition.Midfielder | PlayerPosition.Forward
        };
        db.Companies.Add(company);
        await db.SaveChangesAsync();
        return company;
    }

    private static async Task<Experience> EnsureExperienceAsync(PlayPredictDbContext db, DateTime now)
    {
        var experience = await db.Experiences.FirstOrDefaultAsync(item => item.Name == ExperienceName);
        if (experience is not null) return experience;

        experience = new Experience
        {
            Name = ExperienceName,
            Description = "Dataset isolated for PlayPredict load testing.",
            Status = ExperienceStatus.Published,
            IsPublic = false,
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

    private static async Task<Dictionary<string, Team>> EnsureTeamsAndPlayersAsync(PlayPredictDbContext db)
    {
        var names = TeamDefinitions.Select(item => item.Name).ToList();
        var teams = await db.Teams.Where(team => names.Contains(team.Name)).ToDictionaryAsync(team => team.Name);

        foreach (var definition in TeamDefinitions)
        {
            if (teams.ContainsKey(definition.Name)) continue;
            var team = new Team
            {
                Name = definition.Name,
                ShortName = definition.ShortName,
                Sport = "Fútbol",
                Active = true
            };
            db.Teams.Add(team);
            teams[definition.Name] = team;
        }
        await db.SaveChangesAsync();

        foreach (var team in teams.Values)
        {
            var existingNumbers = await db.TeamPlayers.Where(player => player.TeamId == team.Id)
                .Select(player => player.ShirtNumber).ToListAsync();
            for (var number = 1; number <= 4; number++)
            {
                if (existingNumbers.Contains(number)) continue;
                var position = number switch
                {
                    1 => "Arquero",
                    2 => "Defensor",
                    3 => "Mediocampista",
                    _ => "Delantero"
                };
                db.TeamPlayers.Add(new TeamPlayer
                {
                    TeamId = team.Id,
                    FirstName = $"Player{number}",
                    LastName = team.ShortName,
                    DisplayName = $"LT {team.ShortName} {number}",
                    ShirtNumber = number,
                    Position = position,
                    Active = true
                });
            }
        }
        await db.SaveChangesAsync();
        return teams;
    }

    private static async Task<(Edition Edition, List<Round> Rounds)> EnsureCompetitionAsync(
        PlayPredictDbContext db, int experienceId, DateTime now)
    {
        var competition = await db.Competitions.FirstOrDefaultAsync(item => item.Name == CompetitionName);
        if (competition is null)
        {
            competition = new Competition
            {
                ExperienceId = experienceId,
                Name = CompetitionName,
                Description = "Exclusive LOADTEST competition.",
                Sport = "Fútbol",
                IsActive = true,
                CreatedAtUtc = now
            };
            db.Competitions.Add(competition);
            await db.SaveChangesAsync();
        }

        var edition = await db.Editions.FirstOrDefaultAsync(item =>
            item.CompetitionId == competition.Id && item.Name == EditionName);
        if (edition is null)
        {
            edition = new Edition
            {
                CompetitionId = competition.Id,
                Name = EditionName,
                StartDateUtc = now.AddYears(-1),
                EndDateUtc = now.AddYears(1),
                Status = EditionStatus.Active,
                CreatedAtUtc = now
            };
            db.Editions.Add(edition);
            await db.SaveChangesAsync();
        }

        var rounds = await db.Rounds.Where(round => round.EditionId == edition.Id)
            .OrderBy(round => round.Order).ToListAsync();
        for (var order = 1; order <= 3; order++)
        {
            if (rounds.Any(round => round.Order == order)) continue;
            var round = new Round { EditionId = edition.Id, Name = $"LOADTEST Fecha {order}", Order = order };
            db.Rounds.Add(round);
            rounds.Add(round);
        }
        await db.SaveChangesAsync();
        return (edition, rounds.OrderBy(round => round.Order).ToList());
    }

    private static async Task EnsureScoringAsync(PlayPredictDbContext db, int editionId, DateTime now)
    {
        if (await db.EditionScoringConfigurations.AnyAsync(item => item.EditionId == editionId)) return;
        db.EditionScoringConfigurations.Add(new EditionScoringConfiguration
        {
            EditionId = editionId,
            ExactScorePoints = 6,
            CorrectOutcomePoints = 3,
            IncorrectPoints = 0,
            UseExperienceDefaults = false,
            PreferredPlayerEnabled = true,
            PreferredPlayerPointsPerGoal = 2,
            PreferredPlayerPositions = PlayerPosition.Midfielder | PlayerPosition.Forward,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        });
        await db.SaveChangesAsync();
    }

    private static async Task<List<User>> EnsureUsersAsync(
        PlayPredictDbContext db, int companyId, LoadTestSeedOptions options, DateTime now)
    {
        var emails = Enumerable.Range(1, options.UserCount).Select(UserEmail).ToList();
        var usersByEmail = await db.Users.Include(user => user.UserRoles)
            .Where(user => emails.Contains(user.Email)).ToDictionaryAsync(user => user.Email);
        var playerRole = await db.Roles.SingleAsync(role => role.Name == RoleNames.Player);
        var hasher = new PasswordHasher<User>();

        for (var index = 1; index <= options.UserCount; index++)
        {
            var email = UserEmail(index);
            if (!usersByEmail.TryGetValue(email, out var user))
            {
                user = new User
                {
                    CompanyId = companyId,
                    FirstName = "LoadTest",
                    LastName = index.ToString("D5"),
                    Email = email,
                    IsActive = true,
                    CreatedAtUtc = now
                };
                user.UserRoles.Add(new UserRole { RoleId = playerRole.Id });
                db.Users.Add(user);
                usersByEmail[email] = user;
            }
            else if (user.UserRoles.All(role => role.RoleId != playerRole.Id))
            {
                user.UserRoles.Add(new UserRole { UserId = user.Id, RoleId = playerRole.Id });
            }

            user.CompanyId = companyId;
            user.IsActive = true;
            user.PasswordHash = hasher.HashPassword(user, options.UserPassword);
        }
        await db.SaveChangesAsync();
        return emails.Select(email => usersByEmail[email]).ToList();
    }

    private static async Task<League> EnsureLeagueAsync(
        PlayPredictDbContext db, Edition edition, IReadOnlyList<Round> rounds, int ownerId, DateTime now)
    {
        var league = await db.Leagues.FirstOrDefaultAsync(item =>
            item.Name == LeagueName && item.EditionId == edition.Id && item.LeagueType == LeagueType.Official);
        if (league is null)
        {
            league = new League
            {
                Name = LeagueName,
                Description = "Exclusive official league for load testing.",
                CompetitionId = edition.CompetitionId,
                EditionId = edition.Id,
                ScopeType = LeagueScopeType.FullCompetition,
                InviteCode = "LOADTEST-OFFICIAL",
                LeagueType = LeagueType.Official,
                IsActive = true,
                CreatedByUserId = ownerId,
                CreatedAtUtc = now.AddDays(-60),
                UpdatedAtUtc = now,
                UseGeneralScoring = false,
                ExactScorePoints = 6,
                CorrectOutcomePoints = 3,
                IncorrectPoints = 0,
                PreferredPlayerEnabled = true,
                PreferredPlayerPointsPerGoal = 2,
                PreferredPlayerPositions = PlayerPosition.Midfielder | PlayerPosition.Forward
            };
            db.Leagues.Add(league);
            await db.SaveChangesAsync();
        }
        return league;
    }

    private static async Task<List<Match>> EnsureMatchesAsync(
        PlayPredictDbContext db, IReadOnlyList<Round> rounds, IReadOnlyDictionary<string, Team> teams, DateTime now)
    {
        var definitions = new[]
        {
            new MatchDefinition(1, "LOADTEST FC 01", "LOADTEST FC 02", now.AddDays(-14), MatchStatus.Finished, 2, 1),
            new MatchDefinition(1, "LOADTEST FC 03", "LOADTEST FC 04", now.AddDays(-14).AddHours(2), MatchStatus.Finished, 1, 1),
            new MatchDefinition(2, "LOADTEST FC 01", "LOADTEST FC 03", now.AddDays(7), MatchStatus.Scheduled, null, null),
            new MatchDefinition(2, "LOADTEST FC 02", "LOADTEST FC 04", now.AddDays(7).AddHours(2), MatchStatus.Scheduled, null, null),
            new MatchDefinition(3, "LOADTEST FC 04", "LOADTEST FC 01", now.AddDays(14), MatchStatus.Scheduled, null, null),
            new MatchDefinition(3, "LOADTEST FC 02", "LOADTEST FC 03", now.AddDays(14).AddHours(2), MatchStatus.Scheduled, null, null)
        };
        var roundIds = rounds.Select(round => round.Id).ToList();
        var existing = await db.Matches.Where(match => roundIds.Contains(match.RoundId)).ToListAsync();
        var result = new List<Match>();

        foreach (var definition in definitions)
        {
            var round = rounds.Single(item => item.Order == definition.RoundOrder);
            var home = teams[definition.Home];
            var away = teams[definition.Away];
            var match = existing.FirstOrDefault(item => item.RoundId == round.Id
                && item.HomeTeamId == home.Id && item.AwayTeamId == away.Id);
            if (match is null)
            {
                match = new Match
                {
                    RoundId = round.Id,
                    HomeTeamId = home.Id,
                    AwayTeamId = away.Id,
                    ParticipantHome = home.Name,
                    ParticipantAway = away.Name,
                    CreatedAtUtc = now
                };
                db.Matches.Add(match);
            }
            match.StartsAtUtc = definition.StartsAtUtc;
            match.Status = definition.Status;
            match.HomeGoals = definition.HomeGoals;
            match.AwayGoals = definition.AwayGoals;
            result.Add(match);
        }
        await db.SaveChangesAsync();
        return result;
    }

    private static async Task EnsureHistoricalParticipationAsync(
        PlayPredictDbContext db, int leagueId, IReadOnlyList<User> users,
        IReadOnlyList<Match> finishedMatches, DateTime now)
    {
        var userIds = users.Select(user => user.Id).ToList();
        var periods = await db.LeagueParticipants
            .Where(item => item.LeagueId == leagueId && userIds.Contains(item.UserId)).ToListAsync();
        var firstMatch = finishedMatches.Min(match => match.StartsAtUtc);
        var lastMatch = finishedMatches.Max(match => match.StartsAtUtc);

        foreach (var user in users)
        {
            var hasEligibleHistory = periods.Any(item => item.UserId == user.Id
                && item.JoinedAtUtc < firstMatch
                && item.LeftAtUtc.HasValue && item.LeftAtUtc > lastMatch);
            if (hasEligibleHistory) continue;
            db.LeagueParticipants.Add(new LeagueParticipant
            {
                LeagueId = leagueId,
                UserId = user.Id,
                JoinedAtUtc = firstMatch.AddDays(-7),
                LeftAtUtc = now.AddDays(-1)
            });
        }
        await db.SaveChangesAsync();
    }

    private static async Task<List<Prediction>> EnsureHistoricalPredictionsAsync(
        PlayPredictDbContext db, IReadOnlyList<User> users, IReadOnlyList<Match> finishedMatches)
    {
        var userIds = users.Select(user => user.Id).ToList();
        var matchIds = finishedMatches.Select(match => match.Id).ToList();
        var existing = await db.Predictions.Where(item =>
            userIds.Contains(item.UserId) && matchIds.Contains(item.MatchId)).ToListAsync();

        for (var userIndex = 0; userIndex < users.Count; userIndex++)
        {
            for (var matchIndex = 0; matchIndex < finishedMatches.Count; matchIndex++)
            {
                var user = users[userIndex];
                var match = finishedMatches[matchIndex];
                if (existing.Any(item => item.UserId == user.Id && item.MatchId == match.Id)) continue;
                var prediction = new Prediction
                {
                    UserId = user.Id,
                    MatchId = match.Id,
                    PredictedHomeScore = (userIndex + matchIndex) % 3,
                    PredictedAwayScore = (userIndex * 2 + matchIndex) % 3,
                    CreatedAtUtc = match.StartsAtUtc.AddDays(-2),
                    UpdatedAtUtc = match.StartsAtUtc.AddDays(-2)
                };
                db.Predictions.Add(prediction);
                existing.Add(prediction);
            }
        }
        await db.SaveChangesAsync();
        return existing;
    }

    private static async Task<int> EnsureEvaluationsAsync(
        PlayPredictDbContext db, int leagueId, IReadOnlyList<Prediction> predictions,
        IReadOnlyList<Match> matches, DateTime now)
    {
        var predictionIds = predictions.Select(item => item.Id).ToList();
        var existing = await db.PredictionEvaluations.Where(item =>
            item.LeagueId == leagueId && predictionIds.Contains(item.PredictionId)).ToListAsync();
        var matchById = matches.ToDictionary(match => match.Id);

        foreach (var prediction in predictions)
        {
            if (existing.Any(item => item.PredictionId == prediction.Id)) continue;
            var match = matchById[prediction.MatchId];
            var (type, points) = PredictionEvaluationService.Evaluate(
                prediction.PredictedHomeScore, prediction.PredictedAwayScore,
                match.HomeGoals!.Value, match.AwayGoals!.Value, 6, 3, 0);
            var evaluation = new PredictionEvaluation
            {
                PredictionId = prediction.Id,
                LeagueId = leagueId,
                Points = points,
                ResultPoints = points,
                PreferredPlayerPoints = 0,
                EvaluationType = type,
                OfficialHomeScore = match.HomeGoals.Value,
                OfficialAwayScore = match.AwayGoals.Value,
                AppliedRuleValue = points,
                EvaluatedAtUtc = now
            };
            db.PredictionEvaluations.Add(evaluation);
            existing.Add(evaluation);
        }
        await db.SaveChangesAsync();
        return existing.Count;
    }

    internal static string UserEmail(int index) => $"loadtest{index:D5}@{EmailDomain}";

    private sealed record MatchDefinition(
        int RoundOrder, string Home, string Away, DateTime StartsAtUtc,
        MatchStatus Status, int? HomeGoals, int? AwayGoals);
}
