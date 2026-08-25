using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Data;

public static class DemoGameDataResetter
{
    public static async Task ResetAsync(PlayPredictDbContext db)
    {
        var editionIds = await db.Editions
            .Where(e => e.Competition.Experience.Name.StartsWith("PlayPredict Demo"))
            .Select(e => e.Id).ToListAsync();
        var matchIds = await db.Matches.Where(m => editionIds.Contains(m.Round.EditionId)).Select(m => m.Id).ToListAsync();
        var leagueIds = await db.Leagues.Where(l => editionIds.Contains(l.EditionId)).Select(l => l.Id).ToListAsync();
        var predictionIds = await db.Predictions
            .Where(p => matchIds.Contains(p.MatchId) || leagueIds.Contains(p.LeagueId)).Select(p => p.Id).ToListAsync();

        await using var transaction = await db.Database.BeginTransactionAsync();
        await db.PredictionEvaluations.Where(e => predictionIds.Contains(e.PredictionId)).ExecuteDeleteAsync();
        await db.Predictions.Where(p => predictionIds.Contains(p.Id)).ExecuteDeleteAsync();
        await db.MatchScorers.Where(s => matchIds.Contains(s.MatchId)).ExecuteDeleteAsync();
        await db.Matches.Where(m => matchIds.Contains(m.Id)).ExecuteUpdateAsync(update => update
            .SetProperty(m => m.HomeGoals, (int?)null)
            .SetProperty(m => m.AwayGoals, (int?)null)
            .SetProperty(m => m.Status, MatchStatus.Scheduled));
        await transaction.CommitAsync();
    }
}
