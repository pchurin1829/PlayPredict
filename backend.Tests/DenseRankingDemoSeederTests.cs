using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Services;
using Xunit;

namespace PlayPredict.Api.Tests;

public class DenseRankingDemoSeederTests
{
    [Fact]
    public async Task Seeder_is_idempotent_and_produces_twenty_real_ranked_participants_for_general_and_round_views()
    {
        await using var db = new PlayPredictDbContext(new DbContextOptionsBuilder<PlayPredictDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString()).Options);
        db.Companies.Add(new Company { Name = "EL NENE", ShortName = "EL NENE", IsActive = true });
        db.Roles.Add(new Role { Name = RoleNames.Player });
        await db.SaveChangesAsync();
        var evaluationService = new PredictionEvaluationService(new LeagueScoringService());

        await DemoDatasetV1Seeder.SeedDenseRankingAsync(db, evaluationService);
        await DemoDatasetV1Seeder.SeedDenseRankingAsync(db, evaluationService);

        var league = await db.Leagues.SingleAsync(item => item.Name == DemoDatasetV1Seeder.DenseRankingLeagueName);
        Assert.Equal(20, await db.LeagueParticipants.CountAsync(item => item.LeagueId == league.Id && item.LeftAtUtc == null));
        Assert.Equal(180, await db.Predictions.CountAsync(item => item.Match.Round.EditionId == league.EditionId));
        Assert.Equal(180, await db.PredictionEvaluations.CountAsync(item => item.LeagueId == league.Id));

        var ranking = await new RankingService().GetLeagueRankingAsync(db, league.Id);
        Assert.Equal(20, ranking.Count);
        Assert.Equal(
            [(54, 3), (42, 2), (36, 4), (27, 3), (24, 1), (18, 3), (12, 2), (6, 2)],
            ranking.GroupBy(item => item.Points).Select(group => (group.Key, group.Count())));
        var rafael = ranking.Single(item => item.FirstName == "Rafael" && item.LastName == "Demo");
        Assert.Equal((3, 36, 3), (rafael.Position, rafael.Points, rafael.SharedCount));

        var inactiveLeague = new League
        {
            Name = "Liga inactiva del usuario", CompetitionId = league.CompetitionId, EditionId = league.EditionId,
            LeagueType = league.LeagueType, ScopeType = league.ScopeType, InviteCode = "INACTIVE-DEMO",
            CreatedByUserId = rafael.UserId, CreatedAtUtc = DateTime.UnixEpoch, UpdatedAtUtc = DateTime.UtcNow
        };
        db.Leagues.Add(inactiveLeague);
        db.LeagueParticipants.Add(new LeagueParticipant { League = inactiveLeague, UserId = rafael.UserId, JoinedAtUtc = DateTime.UnixEpoch, LeftAtUtc = DateTime.UtcNow });
        await db.SaveChangesAsync();
        var summaries = await new RankingService().GetUserLeaguePositionsAsync(db, rafael.UserId);
        var summary = Assert.Single(summaries);
        Assert.Equal((league.Id, DemoDatasetV1Seeder.DenseRankingLeagueName, 3, 3, 36),
            (summary.LeagueId, summary.LeagueName, summary.DensePosition, summary.SharedCount, summary.Points));

        var roundIds = await db.Rounds.Where(round => round.EditionId == league.EditionId).Select(round => round.Id).ToListAsync();
        Assert.Equal(3, roundIds.Count);
        foreach (var roundId in roundIds)
        {
            Assert.Equal(20, (await new RankingService().GetLeagueRoundRankingAsync(db, league.Id, roundId)).Count);
            Assert.Equal(20, (await new RankingService().GetLeagueRoundAwardStandingsAsync(db, league.Id, roundId)).Count);
        }
        Assert.Equal(20, (await new RankingService().GetLeagueAwardStandingsAsync(db, league.Id)).Count);
    }
}
