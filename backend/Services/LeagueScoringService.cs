using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Services;

public sealed record EffectiveLeagueScoring(
    int ExactScorePoints, int CorrectOutcomePoints, int IncorrectPoints,
    bool PreferredPlayerEnabled, int PreferredPlayerPointsPerGoal, PlayerPosition PreferredPlayerPositions);

public sealed class LeagueScoringService
{
    public async Task<EffectiveLeagueScoring?> GetEffectiveAsync(PlayPredictDbContext db, int leagueId)
    {
        var data = await db.Leagues.Where(l => l.Id == leagueId).Select(l => new
        {
            l.UseGeneralScoring, l.ExactScorePoints, l.CorrectOutcomePoints, l.IncorrectPoints,
            l.PreferredPlayerEnabled, l.PreferredPlayerPointsPerGoal, l.PreferredPlayerPositions,
            Company = l.CreatedByUser.Company
        }).FirstOrDefaultAsync();
        if (data is null) return null;
        return data.UseGeneralScoring
            ? new(data.Company.GeneralExactScorePoints, data.Company.GeneralCorrectOutcomePoints, data.Company.GeneralIncorrectPoints,
                data.Company.GeneralPreferredPlayerEnabled, data.Company.GeneralPreferredPlayerPointsPerGoal, data.Company.GeneralPreferredPlayerPositions)
            : new(data.ExactScorePoints, data.CorrectOutcomePoints, data.IncorrectPoints,
                data.PreferredPlayerEnabled, data.PreferredPlayerPointsPerGoal, data.PreferredPlayerPositions);
    }
}
