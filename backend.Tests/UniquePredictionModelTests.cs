using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Endpoints;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

public sealed class UniquePredictionModelTests
{
    [Fact]
    public void Model_has_one_prediction_per_user_match_and_evaluation_per_league()
    {
        using var db = CreateDb();
        var predictionIndex = db.Model.FindEntityType(typeof(Prediction))!.GetIndexes()
            .Single(index => index.Properties.Select(p => p.Name).SequenceEqual(["UserId", "MatchId"]));
        Assert.True(predictionIndex.IsUnique);
        Assert.Null(typeof(Prediction).GetProperty("LeagueId"));
        var evaluationIndex = db.Model.FindEntityType(typeof(PredictionEvaluation))!.GetIndexes()
            .Single(index => index.Properties.Select(p => p.Name).SequenceEqual(["PredictionId", "LeagueId"]));
        Assert.True(evaluationIndex.IsUnique);
    }

    [Fact]
    public async Task Same_prediction_is_visible_from_official_and_private_and_edit_is_shared()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);
        var prediction = new Prediction { UserId = data.User.Id, MatchId = data.Match.Id,
            PredictedHomeScore = 2, PredictedAwayScore = 1, PreferredPlayerId = 77,
            CreatedAtUtc = data.Match.StartsAtUtc.AddHours(-2), UpdatedAtUtc = data.Match.StartsAtUtc.AddHours(-2) };
        db.Predictions.Add(prediction);
        await db.SaveChangesAsync();

        var officialView = await db.Predictions.SingleAsync(p => p.UserId == data.User.Id && p.MatchId == data.Match.Id);
        var privateView = await db.Predictions.SingleAsync(p => p.UserId == data.User.Id && p.MatchId == data.Match.Id);
        Assert.Equal(officialView.Id, privateView.Id);

        PredictionEndpoints.ApplyValues(privateView, 3, 0, null, updatePreferredPlayer: false);
        await db.SaveChangesAsync();
        var changed = await db.Predictions.AsNoTracking().SingleAsync();
        Assert.Equal((3, 0), (changed.PredictedHomeScore, changed.PredictedAwayScore));
        Assert.Equal(77, changed.PreferredPlayerId); // una Liga que no lo usa no lo borra
    }

    [Fact]
    public async Task Same_prediction_gets_different_scoring_and_disabled_preferred_is_ignored()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);
        data.Match.Status = MatchStatus.Finished;
        data.Match.HomeGoals = 2;
        data.Match.AwayGoals = 1;
        var scorer = new MatchScorer { Match = data.Match, MatchId = data.Match.Id, TeamPlayerId = 77, Goals = 2 };
        db.MatchScorers.Add(scorer);
        db.Predictions.Add(new Prediction { UserId = data.User.Id, MatchId = data.Match.Id,
            PredictedHomeScore = 2, PredictedAwayScore = 1, PreferredPlayerId = 77,
            CreatedAtUtc = data.Match.StartsAtUtc.AddHours(-1), UpdatedAtUtc = data.Match.StartsAtUtc.AddHours(-1) });
        await db.SaveChangesAsync();

        await new PredictionEvaluationService(new LeagueScoringService()).PrepareEvaluationsForMatchAsync(db, data.Match);
        await db.SaveChangesAsync();
        var evaluations = await db.PredictionEvaluations.OrderBy(e => e.LeagueId).ToListAsync();
        Assert.Equal(2, evaluations.Count);
        Assert.Equal(5, evaluations.Single(e => e.LeagueId == data.Official.Id).Points);
        Assert.Equal(14, evaluations.Single(e => e.LeagueId == data.Private.Id).Points);
        Assert.Equal(0, evaluations.Single(e => e.LeagueId == data.Official.Id).PreferredPlayerPoints);
        Assert.Equal(4, evaluations.Single(e => e.LeagueId == data.Private.Id).PreferredPlayerPoints);
        Assert.All(evaluations, evaluation => Assert.Equal((2, 1), (evaluation.OfficialHomeScore, evaluation.OfficialAwayScore)));
    }

    [Theory]
    [InlineData(-60, null, true)]
    [InlineData(1, null, false)]
    [InlineData(-120, -1, false)]
    [InlineData(-120, 1, true)]
    public async Task Eligibility_uses_membership_period_covering_cutoff(int joinedMinutes, int? leftMinutes, bool expected)
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db, addMemberships: false);
        data.Match.Status = MatchStatus.Finished; data.Match.HomeGoals = 1; data.Match.AwayGoals = 0;
        db.LeagueParticipants.Add(new LeagueParticipant { LeagueId = data.Official.Id, UserId = data.User.Id,
            JoinedAtUtc = data.Match.StartsAtUtc.AddMinutes(joinedMinutes),
            LeftAtUtc = leftMinutes.HasValue ? data.Match.StartsAtUtc.AddMinutes(leftMinutes.Value) : null });
        db.Predictions.Add(new Prediction { UserId = data.User.Id, MatchId = data.Match.Id, PredictedHomeScore = 1,
            PredictedAwayScore = 0, CreatedAtUtc = data.Match.StartsAtUtc.AddHours(-3), UpdatedAtUtc = data.Match.StartsAtUtc.AddHours(-3) });
        await db.SaveChangesAsync();
        await new PredictionEvaluationService(new LeagueScoringService()).PrepareEvaluationsForMatchAsync(db, data.Match);
        await db.SaveChangesAsync();
        Assert.Equal(expected, await db.PredictionEvaluations.AnyAsync(e => e.LeagueId == data.Official.Id));
    }

    [Fact]
    public async Task League_created_after_cutoff_does_not_receive_retroactive_points()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);
        data.Match.Status = MatchStatus.Finished; data.Match.HomeGoals = 1; data.Match.AwayGoals = 0;
        data.Private.CreatedAtUtc = data.Match.StartsAtUtc.AddMinutes(1);
        db.Predictions.Add(new Prediction { UserId = data.User.Id, MatchId = data.Match.Id, PredictedHomeScore = 1,
            PredictedAwayScore = 0, CreatedAtUtc = data.Match.StartsAtUtc.AddHours(-1), UpdatedAtUtc = data.Match.StartsAtUtc.AddHours(-1) });
        await db.SaveChangesAsync();
        await new PredictionEvaluationService(new LeagueScoringService()).PrepareEvaluationsForMatchAsync(db, data.Match);
        await db.SaveChangesAsync();
        Assert.True(await db.PredictionEvaluations.AnyAsync(e => e.LeagueId == data.Official.Id));
        Assert.False(await db.PredictionEvaluations.AnyAsync(e => e.LeagueId == data.Private.Id));
    }

    [Fact]
    public async Task Leaving_and_rejoining_opens_a_new_period_without_retroactive_eligibility()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db, addMemberships: false);
        data.Match.Status = MatchStatus.Finished; data.Match.HomeGoals = 1; data.Match.AwayGoals = 0;
        db.LeagueParticipants.AddRange(
            new LeagueParticipant { LeagueId = data.Official.Id, UserId = data.User.Id,
                JoinedAtUtc = data.Match.StartsAtUtc.AddDays(-2), LeftAtUtc = data.Match.StartsAtUtc.AddHours(-2) },
            new LeagueParticipant { LeagueId = data.Official.Id, UserId = data.User.Id,
                JoinedAtUtc = data.Match.StartsAtUtc.AddMinutes(1) });
        db.Predictions.Add(new Prediction { UserId = data.User.Id, MatchId = data.Match.Id, PredictedHomeScore = 1,
            PredictedAwayScore = 0, CreatedAtUtc = data.Match.StartsAtUtc.AddDays(-1), UpdatedAtUtc = data.Match.StartsAtUtc.AddDays(-1) });
        await db.SaveChangesAsync();
        await new PredictionEvaluationService(new LeagueScoringService()).PrepareEvaluationsForMatchAsync(db, data.Match);
        await db.SaveChangesAsync();
        Assert.False(await db.PredictionEvaluations.AnyAsync(e => e.LeagueId == data.Official.Id));
        Assert.Equal(2, await db.LeagueParticipants.CountAsync(p => p.LeagueId == data.Official.Id));
    }

    [Fact]
    public async Task Ranking_is_isolated_by_league_includes_zero_and_keeps_retired_history()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);
        var second = new User { Id = 2, Company = data.Company, FirstName = "Sin", LastName = "Puntos", Email = "zero@test" };
        db.Users.Add(second);
        db.LeagueParticipants.Add(new LeagueParticipant { LeagueId = data.Official.Id, User = second, JoinedAtUtc = DateTime.UnixEpoch });
        var prediction = new Prediction { Id = 10, UserId = data.User.Id, MatchId = data.Match.Id, PredictedHomeScore = 1,
            PredictedAwayScore = 0, CreatedAtUtc = data.Match.StartsAtUtc.AddHours(-1), UpdatedAtUtc = data.Match.StartsAtUtc.AddHours(-1) };
        db.Predictions.Add(prediction);
        db.PredictionEvaluations.AddRange(
            new PredictionEvaluation { Prediction = prediction, LeagueId = data.Official.Id, Points = 5, EvaluationType = EvaluationType.ExactScore },
            new PredictionEvaluation { Prediction = prediction, LeagueId = data.Private.Id, Points = 10, EvaluationType = EvaluationType.ExactScore });
        (await db.LeagueParticipants.FirstAsync(p => p.LeagueId == data.Official.Id && p.UserId == data.User.Id)).LeftAtUtc = DateTime.UtcNow;
        await db.SaveChangesAsync();

        var ranking = await new RankingService().GetLeagueRankingAsync(db, data.Official.Id);
        Assert.Equal(2, ranking.Count);
        Assert.Equal(5, ranking.Single(r => r.UserId == data.User.Id).Points);
        Assert.False(ranking.Single(r => r.UserId == data.User.Id).IsActiveParticipant);
        Assert.Equal(0, ranking.Single(r => r.UserId == second.Id).Points);
        Assert.Equal([1, 2], ranking.Select(r => r.Position));
        var roundRanking = await new RankingService().GetLeagueRoundRankingAsync(db, data.Official.Id, data.Match.RoundId);
        Assert.Equal(5, roundRanking.Single(r => r.UserId == data.User.Id).Points);
        Assert.Equal([1, 2], roundRanking.Select(r => r.Position));
    }

    [Fact]
    public async Task Partial_range_excludes_match_and_deleting_league_keeps_global_prediction()
    {
        await using var db = CreateDb();
        var data = await SeedAsync(db);
        data.Private.ScopeType = LeagueScopeType.RoundRange;
        data.Private.RoundFromId = 999; data.Private.RoundToId = 999;
        var prediction = new Prediction { UserId = data.User.Id, MatchId = data.Match.Id, PredictedHomeScore = 0,
            PredictedAwayScore = 0, CreatedAtUtc = data.Match.StartsAtUtc.AddHours(-1), UpdatedAtUtc = data.Match.StartsAtUtc.AddHours(-1) };
        db.Predictions.Add(prediction);
        data.Match.Status = MatchStatus.Finished; data.Match.HomeGoals = 0; data.Match.AwayGoals = 0;
        await db.SaveChangesAsync();
        await new PredictionEvaluationService(new LeagueScoringService()).PrepareEvaluationsForMatchAsync(db, data.Match);
        await db.SaveChangesAsync();
        Assert.False(await db.PredictionEvaluations.AnyAsync(e => e.LeagueId == data.Private.Id));
        db.LeagueParticipants.RemoveRange(db.LeagueParticipants.Where(p => p.LeagueId == data.Private.Id));
        db.PredictionEvaluations.RemoveRange(db.PredictionEvaluations.Where(e => e.LeagueId == data.Private.Id));
        db.Leagues.Remove(data.Private);
        await db.SaveChangesAsync();
        Assert.True(await db.Predictions.AnyAsync(p => p.Id == prediction.Id));
    }

    private static PlayPredictDbContext CreateDb() => new(new DbContextOptionsBuilder<PlayPredictDbContext>()
        .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);

    private static async Task<TestData> SeedAsync(PlayPredictDbContext db, bool addMemberships = true)
    {
        var company = new Company { Id = 1, Name = "Test", GeneralExactScorePoints = 5 };
        var user = new User { Id = 1, Company = company, FirstName = "Raúl", LastName = "Test", Email = "raul@test" };
        var edition = new Edition { Id = 1, CompetitionId = 1, Name = "2026" };
        var round = new Round { Id = 1, Edition = edition, Name = "Fecha 1", Order = 1 };
        var cutoff = DateTime.UtcNow.AddHours(-1);
        var match = new Match { Id = 1, Round = round, RoundId = 1, HomeTeamId = 1, AwayTeamId = 2,
            ParticipantHome = "River", ParticipantAway = "Boca", StartsAtUtc = cutoff, Status = MatchStatus.Scheduled };
        var official = new League { Id = 1, Name = "NENE 2", CompetitionId = 1, EditionId = 1,
            ScopeType = LeagueScopeType.FullCompetition, LeagueType = LeagueType.Official, CreatedByUser = user,
            CreatedAtUtc = cutoff.AddDays(-2), UseGeneralScoring = false, ExactScorePoints = 5, PreferredPlayerEnabled = false };
        var privateLeague = new League { Id = 2, Name = "Trabajo", CompetitionId = 1, EditionId = 1,
            ScopeType = LeagueScopeType.FullCompetition, LeagueType = LeagueType.Private, CreatedByUser = user,
            CreatedAtUtc = cutoff.AddDays(-1), UseGeneralScoring = false, ExactScorePoints = 10,
            PreferredPlayerEnabled = true, PreferredPlayerPointsPerGoal = 2 };
        db.AddRange(company, user, edition, round, match, official, privateLeague);
        if (addMemberships) db.LeagueParticipants.AddRange(
            new LeagueParticipant { League = official, User = user, JoinedAtUtc = cutoff.AddDays(-1) },
            new LeagueParticipant { League = privateLeague, User = user, JoinedAtUtc = cutoff.AddHours(-2) });
        await db.SaveChangesAsync();
        return new(company, user, match, official, privateLeague);
    }

    private sealed record TestData(Company Company, User User, Match Match, League Official, League Private);
}
