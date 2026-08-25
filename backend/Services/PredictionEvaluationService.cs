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
        var predictions = await db.Predictions.Include(p => p.PreferredPlayer).Where(p => p.MatchId == match.Id).ToListAsync();
        var ids = predictions.Select(p => p.Id).ToList();
        var evaluations = await db.PredictionEvaluations.Where(e => ids.Contains(e.PredictionId)).ToListAsync();
        foreach (var prediction in predictions)
        {
            var config = await _scoring.GetEffectiveAsync(db, prediction.LeagueId);
            if (config is null) continue;
            var (type, resultPoints) = Evaluate(prediction.PredictedHomeScore, prediction.PredictedAwayScore,
                match.HomeGoals.Value, match.AwayGoals.Value, config.ExactScorePoints, config.CorrectOutcomePoints, config.IncorrectPoints);
            var preferredGoals = config.PreferredPlayerEnabled && prediction.PreferredPlayerId.HasValue
                ? match.Scorers.Where(s => s.TeamPlayerId == prediction.PreferredPlayerId.Value).Sum(s => s.Goals) : 0;
            var preferredPoints = preferredGoals * config.PreferredPlayerPointsPerGoal;
            var evaluation = evaluations.FirstOrDefault(e => e.PredictionId == prediction.Id);
            if (evaluation is null) { evaluation = new PredictionEvaluation { PredictionId = prediction.Id }; db.PredictionEvaluations.Add(evaluation); }
            evaluation.EvaluationType = type; evaluation.ResultPoints = resultPoints; evaluation.PreferredPlayerPoints = preferredPoints;
            evaluation.Points = resultPoints + preferredPoints; evaluation.AppliedRuleValue = resultPoints;
            evaluation.OfficialHomeScore = match.HomeGoals.Value; evaluation.OfficialAwayScore = match.AwayGoals.Value; evaluation.EvaluatedAtUtc = DateTime.UtcNow;
        }
    }

    internal static (EvaluationType Type, int Points) Evaluate(int predictedHome, int predictedAway, int officialHome, int officialAway,
        int exactScorePoints, int correctOutcomePoints, int incorrectPoints)
    {
        if (predictedHome == officialHome && predictedAway == officialAway) return (EvaluationType.ExactScore, exactScorePoints);
        return Math.Sign(predictedHome - predictedAway) == Math.Sign(officialHome - officialAway)
            ? (EvaluationType.CorrectOutcome, correctOutcomePoints) : (EvaluationType.Incorrect, incorrectPoints);
    }

    public static string GetLabel(EvaluationType type) => type switch { EvaluationType.ExactScore => "Marcador exacto", EvaluationType.CorrectOutcome => "Resultado correcto", EvaluationType.Incorrect => "Incorrecto", _ => type.ToString() };
}
