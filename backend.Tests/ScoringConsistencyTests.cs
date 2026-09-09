using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

// FASE 2 (posiciones por liga), FASE 3 (autogoles) y FASE 4 (historicidad) a nivel
// de motor, con SQLite/InMemory y sin infraestructura HTTP.
public sealed class ScoringConsistencyTests
{
    [Fact]
    public async Task Preferred_position_gating_applies_per_league_and_keeps_global_choice()
    {
        await using var db = await ScoringData.SeedAsync();
        var match = db.Matches.Single(m => m.Id == 1);
        match.Status = MatchStatus.Finished;
        match.HomeGoals = 1;
        match.AwayGoals = 0;
        db.MatchScorers.Add(new MatchScorer { MatchId = 1, TeamPlayerId = 11, TeamId = 1, Goals = 1 });
        await db.SaveChangesAsync();

        var service = new PredictionEvaluationService(new LeagueScoringService());
        await service.PrepareEvaluationsForMatchAsync(db, match);
        await db.SaveChangesAsync();

        var evaluations = await db.PredictionEvaluations.OrderBy(e => e.LeagueId).ToListAsync();
        Assert.Equal(2, evaluations.Count);
        var allowsDefender = evaluations.Single(e => e.LeagueId == 1);
        var forwardsOnly = evaluations.Single(e => e.LeagueId == 2);
        Assert.Equal((6, 2, 8), (allowsDefender.ResultPoints, allowsDefender.PreferredPlayerPoints, allowsDefender.Points));
        Assert.Equal((6, 0, 6), (forwardsOnly.ResultPoints, forwardsOnly.PreferredPlayerPoints, forwardsOnly.Points));
        // La elección global no se toca para adaptarla a una liga.
        Assert.Equal(11, await db.Predictions.Select(p => p.PreferredPlayerId).SingleAsync());

        var rankingAllows = await new RankingService().GetLeagueRankingAsync(db, 1);
        var rankingForwards = await new RankingService().GetLeagueRankingAsync(db, 2);
        Assert.Equal(8, rankingAllows.Single().Points);
        Assert.Equal(6, rankingForwards.Single().Points);
    }

    [Fact]
    public async Task Own_goal_only_decides_match_and_gives_no_preferred_points()
    {
        await using var db = await ScoringData.SeedAsync(seedPrediction: false);
        var match = db.Matches.Single(m => m.Id == 1);
        match.Status = MatchStatus.Finished;
        match.HomeGoals = 1;
        match.AwayGoals = 0;
        db.MatchScorers.Add(new MatchScorer { MatchId = 1, TeamPlayerId = null, TeamId = 1, IsOwnGoal = true, Goals = 1 });
        // El preferido es un delantero local que no convirtió: el autogol no debe darle crédito.
        db.Predictions.Add(ScoringData.Prediction(match, 1, 0, preferredPlayerId: 12));
        await db.SaveChangesAsync();

        await new PredictionEvaluationService(new LeagueScoringService()).PrepareEvaluationsForMatchAsync(db, match);
        await db.SaveChangesAsync();

        var evaluation = await db.PredictionEvaluations.SingleAsync(e => e.LeagueId == 1);
        Assert.Equal(EvaluationType.ExactScore, evaluation.EvaluationType);
        Assert.Equal((6, 0, 6), (evaluation.ResultPoints, evaluation.PreferredPlayerPoints, evaluation.Points));
        Assert.Null(await db.MatchScorers.Select(s => s.TeamPlayerId).SingleAsync());
    }

    [Fact]
    public async Task Mixed_goals_credit_only_normal_goals_to_preferred_player()
    {
        await using var db = await ScoringData.SeedAsync(seedPrediction: false);
        var match = db.Matches.Single(m => m.Id == 1);
        match.Status = MatchStatus.Finished;
        match.HomeGoals = 2;
        match.AwayGoals = 0;
        db.MatchScorers.AddRange(
            new MatchScorer { MatchId = 1, TeamPlayerId = 12, TeamId = 1, Goals = 1 },
            new MatchScorer { MatchId = 1, TeamPlayerId = null, TeamId = 1, IsOwnGoal = true, Goals = 1 });
        db.Predictions.Add(ScoringData.Prediction(match, 2, 0, preferredPlayerId: 12));
        await db.SaveChangesAsync();

        await new PredictionEvaluationService(new LeagueScoringService()).PrepareEvaluationsForMatchAsync(db, match);
        await db.SaveChangesAsync();

        var evaluation = await db.PredictionEvaluations.SingleAsync(e => e.LeagueId == 2);
        Assert.Equal((6, 2, 8), (evaluation.ResultPoints, evaluation.PreferredPlayerPoints, evaluation.Points));
    }

    [Fact]
    public async Task Player_without_parseable_position_scores_no_preferred_points()
    {
        await using var db = await ScoringData.SeedAsync(seedPrediction: false);
        var match = db.Matches.Single(m => m.Id == 1);
        match.Status = MatchStatus.Finished;
        match.HomeGoals = 1;
        match.AwayGoals = 0;
        db.TeamPlayers.Add(new TeamPlayer { Id = 99, TeamId = 1, FirstName = "Sin", LastName = "Posición",
            DisplayName = "Sin Posición", Position = null, Active = true });
        db.MatchScorers.Add(new MatchScorer { MatchId = 1, TeamPlayerId = 99, TeamId = 1, Goals = 1 });
        db.Predictions.Add(ScoringData.Prediction(match, 1, 0, preferredPlayerId: 99));
        await db.SaveChangesAsync();

        await new PredictionEvaluationService(new LeagueScoringService()).PrepareEvaluationsForMatchAsync(db, match);
        await db.SaveChangesAsync();

        Assert.Equal(0, await db.PredictionEvaluations.Where(e => e.LeagueId == 1).Select(e => e.PreferredPlayerPoints).SingleAsync());
    }

    [Fact]
    public async Task Rule_change_applies_forward_and_preserves_history_until_reevaluation()
    {
        await using var db = await ScoringData.SeedAsync(seedPrediction: false);
        var service = new PredictionEvaluationService(new LeagueScoringService());
        var first = db.Matches.Single(m => m.Id == 1);
        first.Status = MatchStatus.Finished;
        first.HomeGoals = 1;
        first.AwayGoals = 0;
        db.MatchScorers.Add(new MatchScorer { MatchId = 1, TeamPlayerId = 11, TeamId = 1, Goals = 1 });
        db.Predictions.Add(ScoringData.Prediction(first, 1, 0, preferredPlayerId: 11));
        await db.SaveChangesAsync();
        await service.PrepareEvaluationsForMatchAsync(db, first);
        await db.SaveChangesAsync();
        var before = await db.PredictionEvaluations.AsNoTracking().SingleAsync(e => e.LeagueId == 1);
        Assert.Equal(8, before.Points);

        // Cambio de reglas: las evaluaciones ya persistidas no se tocan solas.
        var league = db.Leagues.Single(l => l.Id == 1);
        league.ExactScorePoints = 10;
        league.CorrectOutcomePoints = 5;
        league.IncorrectPoints = 7;
        await db.SaveChangesAsync();
        var untouched = await db.PredictionEvaluations.AsNoTracking().SingleAsync(e => e.LeagueId == 1);
        Assert.Equal((before.Points, before.ResultPoints, before.EvaluatedAtUtc),
            (untouched.Points, untouched.ResultPoints, untouched.EvaluatedAtUtc));

        // Un partido evaluado después del cambio usa las reglas nuevas.
        var second = new Match { Id = 2, RoundId = 1, HomeTeamId = 1, AwayTeamId = 2, ParticipantHome = "Local",
            ParticipantAway = "Visita", StartsAtUtc = DateTime.UtcNow.AddHours(1), Status = MatchStatus.Finished,
            HomeGoals = 1, AwayGoals = 0, CreatedAtUtc = DateTime.UtcNow.AddDays(-2) };
        db.Matches.Add(second);
        db.Predictions.Add(ScoringData.Prediction(second, 1, 0, preferredPlayerId: null, createdHoursBefore: 2));
        await db.SaveChangesAsync();
        await service.PrepareEvaluationsForMatchAsync(db, second);
        await db.SaveChangesAsync();
        Assert.Equal(10, await db.PredictionEvaluations
            .Where(e => e.LeagueId == 1 && e.Prediction.MatchId == 2).Select(e => e.Points).SingleAsync());

        // Una corrección del partido viejo re-evalúa con las reglas vigentes a ese momento.
        first.HomeGoals = 0;
        first.AwayGoals = 1;
        db.MatchScorers.RemoveRange(db.MatchScorers.Where(s => s.MatchId == 1));
        first.Scorers.Clear();
        await service.PrepareEvaluationsForMatchAsync(db, first);
        await db.SaveChangesAsync();
        var corrected = await db.PredictionEvaluations.AsNoTracking().SingleAsync(e => e.LeagueId == 1 && e.Prediction.MatchId == 1);
        Assert.Equal(EvaluationType.Incorrect, corrected.EvaluationType);
        Assert.Equal((0, 7), (corrected.PreferredPlayerPoints, corrected.Points));
        Assert.True(corrected.EvaluatedAtUtc >= before.EvaluatedAtUtc);
    }

    [Fact]
    public async Task Exact_correct_incorrect_score_and_rank_in_order()
    {
        await using var db = await ScoringData.SeedAsync(extraUsers: 2, seedPrediction: false);
        var match = db.Matches.Single(m => m.Id == 1);
        match.Status = MatchStatus.Finished;
        match.HomeGoals = 2;
        match.AwayGoals = 1;
        db.Predictions.Add(ScoringData.Prediction(match, 2, 1, userId: 1));
        db.Predictions.Add(ScoringData.Prediction(match, 1, 0, userId: 2));
        db.Predictions.Add(ScoringData.Prediction(match, 0, 3, userId: 3));
        await db.SaveChangesAsync();

        await new PredictionEvaluationService(new LeagueScoringService()).PrepareEvaluationsForMatchAsync(db, match);
        await db.SaveChangesAsync();

        var points = await db.PredictionEvaluations.Where(e => e.LeagueId == 1)
            .ToDictionaryAsync(e => e.Prediction.UserId, e => (e.EvaluationType, e.Points));
        Assert.Equal((EvaluationType.ExactScore, 6), points[1]);
        Assert.Equal((EvaluationType.CorrectOutcome, 3), points[2]);
        Assert.Equal((EvaluationType.Incorrect, 0), points[3]);

        var ranking = await new RankingService().GetLeagueRankingAsync(db, 1);
        Assert.Equal([1, 2, 3], ranking.Select(r => r.UserId));
        Assert.Equal([1, 2, 3], ranking.Select(r => r.Position));
    }

    private static class ScoringData
    {
        public static Prediction Prediction(Match match, int home, int away, int? preferredPlayerId = 11,
            int userId = 1, int createdHoursBefore = 3) => new()
        {
            MatchId = match.Id, UserId = userId, PredictedHomeScore = home, PredictedAwayScore = away,
            PreferredPlayerId = preferredPlayerId,
            CreatedAtUtc = match.StartsAtUtc.AddHours(-createdHoursBefore),
            UpdatedAtUtc = match.StartsAtUtc.AddHours(-createdHoursBefore)
        };

        public static async Task<PlayPredictDbContext> SeedAsync(int extraUsers = 0, bool seedPrediction = true)
        {
            var db = new PlayPredictDbContext(new DbContextOptionsBuilder<PlayPredictDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
            var starts = DateTime.UtcNow.AddHours(-1);
            var company = new Company { Id = 1, Name = "Test" };
            var user = new User { Id = 1, Company = company, FirstName = "Ana", LastName = "Test", Email = "ana@test" };
            db.AddRange(company, user,
                new Experience { Id = 1, Name = "EXP" },
                new Competition { Id = 1, ExperienceId = 1, Name = "COMP", Sport = "Fútbol", IsActive = true },
                new Edition { Id = 1, CompetitionId = 1, Name = "ED1" },
                new Round { Id = 1, EditionId = 1, Name = "Fecha 1", Order = 1 },
                new TeamPlayer { Id = 11, TeamId = 1, FirstName = "Def", LastName = "Uno", DisplayName = "Def Uno", Position = "Defensor", Active = true },
                new TeamPlayer { Id = 12, TeamId = 1, FirstName = "Del", LastName = "Uno", DisplayName = "Del Uno", Position = "Delantero", Active = true },
                new TeamPlayer { Id = 21, TeamId = 2, FirstName = "Del", LastName = "Dos", DisplayName = "Del Dos", Position = "Delantero", Active = true });
            for (var i = 0; i < extraUsers; i++)
                db.Users.Add(new User { Id = 2 + i, Company = company, FirstName = $"Extra{i}", LastName = "Test", Email = $"extra{i}@test" });
            var match = new Match { Id = 1, RoundId = 1, HomeTeamId = 1, AwayTeamId = 2, ParticipantHome = "Local",
                ParticipantAway = "Visita", StartsAtUtc = starts, Status = MatchStatus.Scheduled, CreatedAtUtc = starts.AddDays(-2) };
            db.Matches.Add(match);
            db.Leagues.AddRange(
                new League { Id = 1, Name = "Permite defensor", CompetitionId = 1, EditionId = 1,
                    ScopeType = LeagueScopeType.FullCompetition, LeagueType = LeagueType.Official, InviteCode = "A1",
                    IsActive = true, CreatedByUser = user, CreatedAtUtc = starts.AddDays(-2), UseGeneralScoring = false,
                    ExactScorePoints = 6, CorrectOutcomePoints = 3, IncorrectPoints = 0,
                    PreferredPlayerEnabled = true, PreferredPlayerPointsPerGoal = 2, PreferredPlayerPositions = PlayerPosition.Defender },
                new League { Id = 2, Name = "Solo delanteros", CompetitionId = 1, EditionId = 1,
                    ScopeType = LeagueScopeType.FullCompetition, LeagueType = LeagueType.Official, InviteCode = "B2",
                    IsActive = true, CreatedByUser = user, CreatedAtUtc = starts.AddDays(-2), UseGeneralScoring = false,
                    ExactScorePoints = 6, CorrectOutcomePoints = 3, IncorrectPoints = 0,
                    PreferredPlayerEnabled = true, PreferredPlayerPointsPerGoal = 2, PreferredPlayerPositions = PlayerPosition.Forward });
            for (var leagueId = 1; leagueId <= 2; leagueId++)
                for (var userId = 1; userId <= 1 + extraUsers; userId++)
                    db.LeagueParticipants.Add(new LeagueParticipant { LeagueId = leagueId, UserId = userId, JoinedAtUtc = starts.AddDays(-1) });
            // El seed base trae el pronóstico exacto del defensor para la Liga 1 y 2.
            if (seedPrediction) db.Predictions.Add(Prediction(match, 1, 0));
            await db.SaveChangesAsync();
            return db;
        }
    }
}

// FASE 3 y FASE 4 a nivel HTTP: validación del PUT /result con autogoles, corrección
// de resultados y cambio de reglas vía API de ligas oficiales.
public sealed class ScoringResultEndpointTests
{
    [Fact]
    public async Task Result_with_only_own_goal_evaluates_exact_without_preferred_points()
    {
        await using var api = await ScoringApi.Create();
        await api.AsPlayer();
        await api.CreatePrediction(matchId: 1, leagueId: 1, home: 1, away: 0, preferredPlayerId: 12);

        await api.AsAdmin();
        using var response = await api.Client.PutAsJsonAsync("/api/matches/1/result",
            new { homeGoals = 1, awayGoals = 0, scorers = new[] { new { teamPlayerId = (int?)null, goals = 1, isOwnGoal = true, teamId = (int?)1 } } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var dto = (await response.Content.ReadFromJsonAsync<MatchDto>())!;
        var scorer = Assert.Single(dto.Scorers);
        Assert.True(scorer.IsOwnGoal);
        Assert.Null(scorer.TeamPlayerId);
        Assert.Equal("Autogol", scorer.PlayerName);
        Assert.Equal(1, scorer.TeamId);

        var evaluation = await api.Read(db => db.PredictionEvaluations.SingleAsync());
        Assert.Equal(EvaluationType.ExactScore, evaluation.EvaluationType);
        Assert.Equal((6, 0, 6), (evaluation.ResultPoints, evaluation.PreferredPlayerPoints, evaluation.Points));
    }

    [Fact]
    public async Task Result_rejects_invalid_own_goal_payloads_without_changing_state()
    {
        await using var api = await ScoringApi.Create();
        await api.AsAdmin();
        var badPayloads = new (string Case, object Body)[]
        {
            ("con jugador", new { homeGoals = 1, awayGoals = 0, scorers = new[] { new { teamPlayerId = (int?)12, goals = 1, isOwnGoal = true, teamId = (int?)1 } } }),
            ("equipo desconocido", new { homeGoals = 1, awayGoals = 0, scorers = new[] { new { teamPlayerId = (int?)null, goals = 1, isOwnGoal = true, teamId = (int?)999 } } }),
            ("sumas inconsistentes", new { homeGoals = 0, awayGoals = 1, scorers = new[] { new { teamPlayerId = (int?)null, goals = 1, isOwnGoal = true, teamId = (int?)1 } } }),
        };
        foreach (var (name, body) in badPayloads)
        {
            using var response = await api.Client.PutAsJsonAsync("/api/matches/1/result", body);
            Assert.True(response.StatusCode == HttpStatusCode.BadRequest, name);
            var content = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.True(content.GetProperty("errors").TryGetProperty("scorers", out _), name);
        }

        var match = await api.Read(db => db.Matches.AsNoTracking().SingleAsync(m => m.Id == 1));
        Assert.Equal(MatchStatus.Scheduled, match.Status);
        Assert.Null(match.HomeGoals);
        Assert.Equal(0, await api.Read(db => db.MatchScorers.CountAsync()));
        Assert.Equal(0, await api.Read(db => db.PredictionEvaluations.CountAsync()));
    }

    [Fact]
    public async Task Correction_from_normal_goal_to_own_goal_replaces_scorers()
    {
        await using var api = await ScoringApi.Create();
        await api.AsPlayer();
        await api.CreatePrediction(matchId: 1, leagueId: 1, home: 1, away: 0, preferredPlayerId: 12);

        await api.AsAdmin();
        using var first = await api.Client.PutAsJsonAsync("/api/matches/1/result",
            new { homeGoals = 1, awayGoals = 0, scorers = new[] { new { teamPlayerId = (int?)12, goals = 1, isOwnGoal = false, teamId = (int?)null } } });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        Assert.Equal(2, (await api.Read(db => db.PredictionEvaluations.SingleAsync())).PreferredPlayerPoints);

        using var corrected = await api.Client.PutAsJsonAsync("/api/matches/1/result",
            new { homeGoals = 1, awayGoals = 0, scorers = new[] { new { teamPlayerId = (int?)null, goals = 1, isOwnGoal = true, teamId = (int?)1 } } });
        Assert.Equal(HttpStatusCode.OK, corrected.StatusCode);
        Assert.Equal(1, await api.Read(db => db.MatchScorers.CountAsync()));
        Assert.True(await api.Read(db => db.MatchScorers.Select(s => s.IsOwnGoal).SingleAsync()));
        var evaluation = await api.Read(db => db.PredictionEvaluations.SingleAsync());
        Assert.Equal((6, 0, 6), (evaluation.ResultPoints, evaluation.PreferredPlayerPoints, evaluation.Points));
    }

    [Fact]
    public async Task Positions_gating_end_to_end_two_leagues_share_one_prediction()
    {
        await using var api = await ScoringApi.Create();
        await api.AsAdmin();
        var allowsDefender = await api.CreateOfficialLeague("Permite defensor", ["Defensor"]);
        var forwardsOnly = await api.CreateOfficialLeague("Solo delanteros", ["Delantero"]);
        await api.JoinBoth(allowsDefender, forwardsOnly);

        // Un único pronóstico global (defensor preferido) compartido por ambas ligas.
        await api.AsPlayer();
        await api.CreatePrediction(matchId: 1, leagueId: allowsDefender, home: 1, away: 0, preferredPlayerId: 11);
        Assert.Equal(1, await api.Read(db => db.Predictions.CountAsync()));

        await api.AsAdmin();
        using var response = await api.Client.PutAsJsonAsync("/api/matches/1/result",
            new { homeGoals = 1, awayGoals = 0, scorers = new[] { new { teamPlayerId = (int?)11, goals = 1, isOwnGoal = false, teamId = (int?)null } } });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var evaluations = await api.Read(db => db.PredictionEvaluations
            .Where(e => e.LeagueId == allowsDefender || e.LeagueId == forwardsOnly)
            .OrderBy(e => e.LeagueId).ToListAsync());
        Assert.Equal(2, evaluations.Count);
        Assert.Equal((6, 2, 8), (evaluations[0].ResultPoints, evaluations[0].PreferredPlayerPoints, evaluations[0].Points));
        Assert.Equal(allowsDefender, evaluations[0].LeagueId);
        Assert.Equal((6, 0, 6), (evaluations[1].ResultPoints, evaluations[1].PreferredPlayerPoints, evaluations[1].Points));
        Assert.Equal(forwardsOnly, evaluations[1].LeagueId);
        Assert.Equal(11, await api.Read(db => db.Predictions.Select(p => p.PreferredPlayerId).SingleAsync()));
    }

    [Fact]
    public async Task League_rule_change_preserves_evaluations_and_applies_forward()
    {
        await using var api = await ScoringApi.Create();
        await api.AsAdmin();
        var leagueId = await api.CreateOfficialLeague("Reglas cambiantes", ["Delantero"]);
        await api.Join(leagueId);
        await api.AsPlayer();
        await api.CreatePrediction(matchId: 1, leagueId: leagueId, home: 1, away: 0, preferredPlayerId: 12);

        await api.AsAdmin();
        using var first = await api.Client.PutAsJsonAsync("/api/matches/1/result",
            new { homeGoals = 1, awayGoals = 0, scorers = new[] { new { teamPlayerId = (int?)12, goals = 1, isOwnGoal = false, teamId = (int?)null } } });
        Assert.Equal(HttpStatusCode.OK, first.StatusCode);
        var before = await api.Read(db => db.PredictionEvaluations.AsNoTracking().SingleAsync(e => e.LeagueId == leagueId));

        // Cambiar las reglas no reescribe la historia.
        using var updated = await api.Client.PutAsJsonAsync($"/api/admin/official-leagues/{leagueId}",
            new { name = "Reglas cambiantes", description = (string?)null, competitionId = 1, editionId = 1,
                scopeType = "FullCompetition", roundFromId = (int?)null, roundToId = (int?)null, isActive = true,
                useGeneralScoring = false, exactScorePoints = 10, correctOutcomePoints = 5, incorrectPoints = 1,
                preferredPlayerEnabled = true, preferredPlayerPointsPerGoal = 2, preferredPlayerPositions = new[] { "Delantero" } });
        Assert.Equal(HttpStatusCode.OK, updated.StatusCode);
        var untouched = await api.Read(db => db.PredictionEvaluations.AsNoTracking().SingleAsync(e => e.LeagueId == leagueId));
        Assert.Equal((before.Points, before.ResultPoints, before.EvaluatedAtUtc),
            (untouched.Points, untouched.ResultPoints, untouched.EvaluatedAtUtc));

        // Un partido evaluado después del cambio usa las reglas nuevas.
        var secondMatchId = await api.Read(async db =>
        {
            db.Rounds.Add(new Round { Id = 2, EditionId = 1, Name = "Fecha 2", Order = 2 });
            var starts = DateTime.UtcNow.AddDays(30);
            var match = new Match { RoundId = 2, HomeTeamId = 1, AwayTeamId = 2, ParticipantHome = "Local",
                ParticipantAway = "Visita", StartsAtUtc = starts, Status = MatchStatus.Scheduled, CreatedAtUtc = DateTime.UtcNow };
            db.Matches.Add(match);
            await db.SaveChangesAsync();
            return match.Id;
        });
        await api.AsPlayer();
        await api.CreatePrediction(matchId: secondMatchId, leagueId: leagueId, home: 2, away: 2, preferredPlayerId: null);
        await api.AsAdmin();
        using var second = await api.Client.PutAsJsonAsync($"/api/matches/{secondMatchId}/result",
            new { homeGoals = 2, awayGoals = 2, scorers = new object[] { } });
        Assert.Equal(HttpStatusCode.OK, second.StatusCode);
        Assert.Equal(10, await api.Read(db => db.PredictionEvaluations
            .Where(e => e.LeagueId == leagueId && e.Prediction.MatchId == secondMatchId).Select(e => e.Points).SingleAsync()));
    }

    private sealed class ScoringApi(WebApplication app, SqliteConnection connection) : IAsyncDisposable
    {
        public HttpClient Client { get; } = app.GetTestClient();
        private const string Key = "scoring-consistency-tests-only-signing-key-0123456789";

        public static async Task<ScoringApi> Create()
        {
            var connection = new SqliteConnection("Data Source=:memory:");
            await connection.OpenAsync();
            var builder = WebApplication.CreateBuilder();
            builder.Logging.ClearProviders();
            builder.WebHost.UseTestServer();
            builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:Key"] = Key, ["Jwt:Issuer"] = "scoring-tests", ["Jwt:Audience"] = "scoring-tests"
            });
            builder.Services.AddSingleton<JwtTokenService>();
            builder.Services.AddScoped<LeagueScoringService>();
            builder.Services.AddScoped<PredictionEvaluationService>();
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme).AddJwtBearer(options =>
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true, ValidIssuer = "scoring-tests", ValidateAudience = true, ValidAudience = "scoring-tests",
                    ValidateIssuerSigningKey = true, IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Key)), ValidateLifetime = true
                });
            builder.Services.AddAuthorization();
            builder.Services.AddDbContext<PlayPredictDbContext>(options => options.UseSqlite(connection));
            var app = builder.Build();
            app.UseAuthentication();
            app.UseAuthorization();
            app.MapMatchEndpoints();
            app.MapPredictionEndpoints();
            app.MapAdminOfficialLeagueEndpoints();
            await using (var scope = app.Services.CreateAsyncScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>();
                await db.Database.EnsureCreatedAsync();
                var past = DateTime.UtcNow.AddDays(-2);
                var company = new Company { Id = 1, Name = "Test" };
                db.AddRange(company,
                    new User { Id = 1, Company = company, FirstName = "Admin", LastName = "Test", Email = "admin@test", PasswordHash = "x", CreatedAtUtc = past },
                    new User { Id = 2, Company = company, FirstName = "Jugador", LastName = "Test", Email = "player@test", PasswordHash = "x", CreatedAtUtc = past },
                    new Experience { Id = 1, Name = "EXP TEST" },
                    new Competition { Id = 1, ExperienceId = 1, Name = "COMP", Sport = "Fútbol", IsActive = true, CreatedAtUtc = past },
                    new Edition { Id = 1, CompetitionId = 1, Name = "ED1", StartDateUtc = past },
                    new Round { Id = 1, EditionId = 1, Name = "Fecha 1", Order = 1 },
                    new Team { Id = 1, Name = "Local", ShortName = "LOC", Sport = "Fútbol", Active = true },
                    new Team { Id = 2, Name = "Visita", ShortName = "VIS", Sport = "Fútbol", Active = true },
                    new TeamPlayer { Id = 11, TeamId = 1, FirstName = "Def", LastName = "Uno", DisplayName = "Def Uno", Position = "Defensor", Active = true },
                    new TeamPlayer { Id = 12, TeamId = 1, FirstName = "Del", LastName = "Uno", DisplayName = "Del Uno", Position = "Delantero", Active = true },
                    new TeamPlayer { Id = 21, TeamId = 2, FirstName = "Del", LastName = "Dos", DisplayName = "Del Dos", Position = "Delantero", Active = true });
                var starts = DateTime.UtcNow.AddDays(30);
                db.Matches.Add(new Match { Id = 1, RoundId = 1, HomeTeamId = 1, AwayTeamId = 2, ParticipantHome = "Local",
                    ParticipantAway = "Visita", StartsAtUtc = starts, Status = MatchStatus.Scheduled, CreatedAtUtc = past });
                db.Leagues.Add(new League { Id = 1, Name = "Base", CompetitionId = 1, EditionId = 1,
                    ScopeType = LeagueScopeType.FullCompetition, LeagueType = LeagueType.Official, InviteCode = "BASE1",
                    IsActive = true, CreatedByUserId = 1, CreatedAtUtc = past, UseGeneralScoring = false,
                    ExactScorePoints = 6, CorrectOutcomePoints = 3, IncorrectPoints = 0,
                    PreferredPlayerEnabled = true, PreferredPlayerPointsPerGoal = 2,
                    PreferredPlayerPositions = PlayerPosition.Midfielder | PlayerPosition.Forward });
                db.LeagueParticipants.Add(new LeagueParticipant { LeagueId = 1, UserId = 2, JoinedAtUtc = past });
                await db.SaveChangesAsync();
            }
            await app.StartAsync();
            return new ScoringApi(app, connection);
        }

        public async Task AsAdmin() => await Authenticate("admin@test", RoleNames.Admin);
        public async Task AsPlayer() => await Authenticate("player@test", RoleNames.Player);

        private async Task Authenticate(string email, string role)
        {
            var user = await Read(db => db.Users.AsNoTracking().SingleAsync(u => u.Email == email));
            Client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer",
                app.Services.GetRequiredService<JwtTokenService>().GenerateToken(user, [role]));
        }

        public async Task CreatePrediction(int matchId, int leagueId, int home, int away, int? preferredPlayerId)
        {
            using var response = await Client.PostAsJsonAsync("/api/predictions",
                new { leagueId, matchId, predictedHomeScore = home, predictedAwayScore = away, preferredPlayerId });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        }

        public async Task<int> CreateOfficialLeague(string name, string[] positions)
        {
            using var response = await Client.PostAsJsonAsync("/api/admin/official-leagues",
                new { name, description = (string?)null, competitionId = 1, editionId = 1, scopeType = "FullCompetition",
                    roundFromId = (int?)null, roundToId = (int?)null, isActive = true, useGeneralScoring = false,
                    exactScorePoints = 6, correctOutcomePoints = 3, incorrectPoints = 0, preferredPlayerEnabled = true,
                    preferredPlayerPointsPerGoal = 2, preferredPlayerPositions = positions });
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            return await Read(db => db.Leagues.OrderByDescending(l => l.Id).Select(l => l.Id).FirstAsync());
        }

        public async Task Join(int leagueId)
        {
            var past = DateTime.UtcNow.AddDays(-1);
            await Read(async db =>
            {
                db.LeagueParticipants.Add(new LeagueParticipant { LeagueId = leagueId, UserId = 2, JoinedAtUtc = past });
                await db.SaveChangesAsync();
                return 0;
            });
        }

        public async Task JoinBoth(int firstLeagueId, int secondLeagueId)
        {
            var past = DateTime.UtcNow.AddDays(-1);
            await Read(async db =>
            {
                db.LeagueParticipants.AddRange(
                    new LeagueParticipant { LeagueId = firstLeagueId, UserId = 2, JoinedAtUtc = past },
                    new LeagueParticipant { LeagueId = secondLeagueId, UserId = 2, JoinedAtUtc = past });
                await db.SaveChangesAsync();
                return 0;
            });
        }

        public async Task<T> Read<T>(Func<PlayPredictDbContext, Task<T>> action)
        {
            await using var scope = app.Services.CreateAsyncScope();
            return await action(scope.ServiceProvider.GetRequiredService<PlayPredictDbContext>());
        }

        public async ValueTask DisposeAsync()
        {
            Client.Dispose();
            await app.DisposeAsync();
            await connection.DisposeAsync();
        }
    }
}
