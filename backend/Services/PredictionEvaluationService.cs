using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;

namespace PlayPredict.Api.Services;

// Responsabilidad única: transformar un Pronóstico + Resultado Oficial + configuración
// de la Edición en una PredictionEvaluation. No guarda cambios por sí mismo: deja las
// entidades preparadas en el ChangeTracker para que el llamador las persista junto con
// el resultado oficial en un único SaveChangesAsync (una sola transacción implícita).
public class PredictionEvaluationService
{
    public async Task PrepareEvaluationsForMatchAsync(PlayPredictDbContext db, Match match)
    {
        if (match.Status != MatchStatus.Finished || match.HomeGoals is null || match.AwayGoals is null)
        {
            return;
        }

        var editionId = await db.Rounds
            .Where(r => r.Id == match.RoundId)
            .Select(r => r.EditionId)
            .FirstOrDefaultAsync();

        var config = await db.EditionScoringConfigurations
            .FirstOrDefaultAsync(c => c.EditionId == editionId);

        if (config is null)
        {
            // No debería ocurrir: toda Edición debe tener configuración (seed + alta automática).
            return;
        }

        var predictions = await db.Predictions
            .Where(p => p.MatchId == match.Id)
            .ToListAsync();

        if (predictions.Count == 0)
        {
            return;
        }

        var predictionIds = predictions.Select(p => p.Id).ToList();
        var existingEvaluations = await db.PredictionEvaluations
            .Where(e => predictionIds.Contains(e.PredictionId))
            .ToListAsync();

        var now = DateTime.UtcNow;

        foreach (var prediction in predictions)
        {
            var (type, points) = Evaluate(
                prediction.PredictedHomeScore, prediction.PredictedAwayScore,
                match.HomeGoals.Value, match.AwayGoals.Value,
                config);

            var evaluation = existingEvaluations.FirstOrDefault(e => e.PredictionId == prediction.Id);
            if (evaluation is null)
            {
                evaluation = new PredictionEvaluation { PredictionId = prediction.Id };
                db.PredictionEvaluations.Add(evaluation);
            }

            evaluation.EvaluationType = type;
            evaluation.Points = points;
            evaluation.AppliedRuleValue = points;
            evaluation.OfficialHomeScore = match.HomeGoals.Value;
            evaluation.OfficialAwayScore = match.AwayGoals.Value;
            evaluation.EvaluatedAtUtc = now;
        }
    }

    // Marcador exacto tiene prioridad sobre resultado correcto; solo se aplica una categoría.
    internal static (EvaluationType Type, int Points) Evaluate(
        int predictedHome, int predictedAway, int officialHome, int officialAway, EditionScoringConfiguration config)
    {
        if (predictedHome == officialHome && predictedAway == officialAway)
        {
            return (EvaluationType.ExactScore, config.ExactScorePoints);
        }

        var predictedOutcome = Math.Sign(predictedHome - predictedAway);
        var officialOutcome = Math.Sign(officialHome - officialAway);

        if (predictedOutcome == officialOutcome)
        {
            return (EvaluationType.CorrectOutcome, config.CorrectOutcomePoints);
        }

        return (EvaluationType.Incorrect, config.IncorrectPoints);
    }

    public static string GetLabel(EvaluationType type) => type switch
    {
        EvaluationType.ExactScore => "Marcador exacto",
        EvaluationType.CorrectOutcome => "Resultado correcto",
        EvaluationType.Incorrect => "Incorrecto",
        _ => type.ToString()
    };
}
