using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Services;

public class PredictionEvaluationService
{
    private readonly LeagueScoringService _scoring;
    public PredictionEvaluationService(LeagueScoringService scoring) => _scoring = scoring;

    public async Task PrepareEvaluationsForMatchAsync(PlayPredictDbContext db, Match match)
    {
        if (match.Status != MatchStatus.Finished || match.HomeGoals is null || match.AwayGoals is null) return;

        var predictions = await db.Predictions.Include(p => p.PreferredPlayer)
            .Where(p => p.MatchId == match.Id && p.CreatedAtUtc < match.StartsAtUtc).ToListAsync();
        if (predictions.Count == 0) return;

        var leagues = await LeaguesContainingMatchAsync(db, match);
        var predictionIds = predictions.Select(p => p.Id).ToList();
        var leagueIds = leagues.Select(l => l.Id).ToList();
        var evaluations = await db.PredictionEvaluations
            .Where(e => predictionIds.Contains(e.PredictionId) && leagueIds.Contains(e.LeagueId)).ToListAsync();

        foreach (var league in leagues)
        {
            var config = await _scoring.GetEffectiveAsync(db, league.Id);
            if (config is null) continue;

            foreach (var prediction in predictions)
            {
                var eligible = await db.LeagueParticipants.AnyAsync(period =>
                    period.LeagueId == league.Id && period.UserId == prediction.UserId
                    && period.JoinedAtUtc < match.StartsAtUtc
                    && (period.LeftAtUtc == null || period.LeftAtUtc > match.StartsAtUtc));
                if (!eligible) continue;

                var (type, resultPoints) = Evaluate(prediction.PredictedHomeScore, prediction.PredictedAwayScore,
                    match.HomeGoals.Value, match.AwayGoals.Value, config.ExactScorePoints, config.CorrectOutcomePoints, config.IncorrectPoints);
                var preferredGoals = config.PreferredPlayerEnabled && prediction.PreferredPlayerId.HasValue
                    ? match.Scorers.Where(s => s.TeamPlayerId == prediction.PreferredPlayerId.Value).Sum(s => s.Goals) : 0;
                var preferredPoints = preferredGoals * config.PreferredPlayerPointsPerGoal;
                var evaluation = evaluations.FirstOrDefault(e => e.PredictionId == prediction.Id && e.LeagueId == league.Id);
                if (evaluation is null)
                {
                    evaluation = new PredictionEvaluation { PredictionId = prediction.Id, LeagueId = league.Id };
                    db.PredictionEvaluations.Add(evaluation);
                    evaluations.Add(evaluation);
                }
                evaluation.EvaluationType = type;
                evaluation.ResultPoints = resultPoints;
                evaluation.PreferredPlayerPoints = preferredPoints;
                evaluation.Points = resultPoints + preferredPoints;
                evaluation.AppliedRuleValue = resultPoints;
                evaluation.OfficialHomeScore = match.HomeGoals.Value;
                evaluation.OfficialAwayScore = match.AwayGoals.Value;
                evaluation.EvaluatedAtUtc = DateTime.UtcNow;
            }
        }
    }

    private static async Task<List<League>> LeaguesContainingMatchAsync(PlayPredictDbContext db, Match match)
    {
        var round = await db.Rounds.FindAsync(match.RoundId);
        if (round is null) return [];
        var leagues = await db.Leagues.Where(l => l.EditionId == round.EditionId && l.CreatedAtUtc < match.StartsAtUtc).ToListAsync();
        var boundaryIds = leagues.SelectMany(l => new[] { l.RoundFromId, l.RoundToId }).Where(id => id.HasValue).Select(id => id!.Value).Distinct().ToList();
        var orders = await db.Rounds.Where(r => boundaryIds.Contains(r.Id)).ToDictionaryAsync(r => r.Id, r => r.Order);
        return leagues.Where(l => l.ScopeType == LeagueScopeType.FullCompetition
            || (l.RoundFromId.HasValue && l.RoundToId.HasValue
                && orders.TryGetValue(l.RoundFromId.Value, out var from)
                && orders.TryGetValue(l.RoundToId.Value, out var to)
                && round.Order >= from && round.Order <= to)).ToList();
    }

    internal static (EvaluationType Type, int Points) Evaluate(int predictedHome, int predictedAway, int officialHome, int officialAway,
        int exactScorePoints, int correctOutcomePoints, int incorrectPoints)
    {
        if (predictedHome == officialHome && predictedAway == officialAway) return (EvaluationType.ExactScore, exactScorePoints);
        return Math.Sign(predictedHome - predictedAway) == Math.Sign(officialHome - officialAway)
            ? (EvaluationType.CorrectOutcome, correctOutcomePoints) : (EvaluationType.Incorrect, incorrectPoints);
    }

    public static string GetLabel(EvaluationType type) => type switch
    {
        EvaluationType.ExactScore => "Marcador exacto",
        EvaluationType.CorrectOutcome => "Resultado correcto",
        EvaluationType.Incorrect => "Incorrecto",
        _ => type.ToString()
    };
}
