using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class PredictionEndpoints
{
    public static void MapPredictionEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/predictions").WithTags("Predictions").RequireAuthorization();

        group.MapGet("/rounds/{roundId:int}", async (int roundId, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var roundExists = await db.Rounds.AnyAsync(r => r.Id == roundId);
            if (!roundExists)
            {
                return Results.NotFound();
            }

            var matches = await db.Matches
                .Where(m => m.RoundId == roundId)
                .OrderBy(m => m.StartsAtUtc)
                .ToListAsync();

            var matchIds = matches.Select(m => m.Id).ToList();
            var predictions = await db.Predictions
                .Where(p => p.UserId == user.Id && matchIds.Contains(p.MatchId))
                .ToListAsync();

            var evaluations = await GetEvaluationsForPredictionsAsync(db, predictions);

            var result = matches.Select(m =>
            {
                var prediction = predictions.FirstOrDefault(p => p.MatchId == m.Id);
                var evaluation = prediction is null ? null : evaluations.GetValueOrDefault(prediction.Id);
                return ToMatchWithPredictionDto(m, prediction, evaluation);
            });

            return Results.Ok(result);
        });

        group.MapGet("/me", async (ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var predictions = await db.Predictions
                .Where(p => p.UserId == user.Id)
                .OrderByDescending(p => p.UpdatedAtUtc)
                .ToListAsync();

            var evaluations = await GetEvaluationsForPredictionsAsync(db, predictions);

            var dtos = predictions.Select(p => ToDto(p, evaluations.GetValueOrDefault(p.Id)));

            return Results.Ok(dtos);
        });

        group.MapPost("/", async (CreatePredictionDto dto, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var errors = ValidateScores(dto.PredictedHomeScore, dto.PredictedAwayScore);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var match = await db.Matches.FindAsync(dto.MatchId);
            if (match is null)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["matchId"] = ["El partido indicado no existe."]
                });
            }

            if (!CanCreateOrEditPrediction(match))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["Este partido no admite pronósticos: ya comenzó, no está Programado, o su horario de inicio ya pasó."]
                });
            }

            var alreadyExists = await db.Predictions.AnyAsync(p => p.UserId == user.Id && p.MatchId == dto.MatchId);
            if (alreadyExists)
            {
                return Results.Conflict(new { message = "Ya existe un pronóstico para este partido. Modificalo en vez de crear uno nuevo." });
            }

            var now = DateTime.UtcNow;
            var prediction = new Prediction
            {
                MatchId = dto.MatchId,
                UserId = user.Id,
                PredictedHomeScore = dto.PredictedHomeScore,
                PredictedAwayScore = dto.PredictedAwayScore,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            db.Predictions.Add(prediction);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { message = "Ya existe un pronóstico para este partido." });
            }

            return Results.Created($"/api/predictions/{prediction.Id}", ToDto(prediction));
        });

        group.MapPut("/{id:int}", async (int id, UpdatePredictionDto dto, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await UserEndpoints.GetCurrentUserAsync(principal, db);
            if (user is null)
            {
                return Results.Unauthorized();
            }

            var errors = ValidateScores(dto.PredictedHomeScore, dto.PredictedAwayScore);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var prediction = await db.Predictions.FindAsync(id);
            if (prediction is null)
            {
                return Results.NotFound();
            }

            if (prediction.UserId != user.Id)
            {
                return Results.Json(new { message = "No podés modificar el pronóstico de otro usuario." }, statusCode: StatusCodes.Status403Forbidden);
            }

            var match = await db.Matches.FindAsync(prediction.MatchId);
            if (match is null || !CanCreateOrEditPrediction(match))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["Este partido ya no admite modificar el pronóstico: ya comenzó, no está Programado, o su horario de inicio ya pasó."]
                });
            }

            prediction.PredictedHomeScore = dto.PredictedHomeScore;
            prediction.PredictedAwayScore = dto.PredictedAwayScore;
            prediction.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(prediction));
        });
    }

    // Regla definitiva: un pronóstico solo puede crearse o modificarse si el partido
    // está Programado Y su horario de inicio todavía no llegó. Cualquier otro caso
    // (Programado con horario ya pasado, En juego, Suspendido, Finalizado, Cancelado)
    // queda cerrado. El backend es la autoridad final de esta regla.
    private static bool CanCreateOrEditPrediction(Match match) =>
        match.Status == MatchStatus.Scheduled && DateTime.UtcNow < match.StartsAtUtc;

    private static Dictionary<string, string[]> ValidateScores(int homeScore, int awayScore)
    {
        var errors = new Dictionary<string, string[]>();

        if (homeScore < 0)
        {
            errors["predictedHomeScore"] = ["El resultado del local no puede ser negativo."];
        }

        if (awayScore < 0)
        {
            errors["predictedAwayScore"] = ["El resultado del visitante no puede ser negativo."];
        }

        return errors;
    }

    private static async Task<Dictionary<int, PredictionEvaluation>> GetEvaluationsForPredictionsAsync(
        PlayPredictDbContext db, List<Prediction> predictions)
    {
        if (predictions.Count == 0)
        {
            return new Dictionary<int, PredictionEvaluation>();
        }

        var predictionIds = predictions.Select(p => p.Id).ToList();
        var evaluations = await db.PredictionEvaluations
            .Where(e => predictionIds.Contains(e.PredictionId))
            .ToListAsync();

        return evaluations.ToDictionary(e => e.PredictionId);
    }

    private static PredictionDto ToDto(Prediction p, PredictionEvaluation? evaluation = null) =>
        new(p.Id, p.MatchId, p.UserId, p.PredictedHomeScore, p.PredictedAwayScore, p.CreatedAtUtc, p.UpdatedAtUtc,
            evaluation?.Points,
            evaluation?.EvaluationType.ToString(),
            evaluation is null ? null : PredictionEvaluationService.GetLabel(evaluation.EvaluationType),
            evaluation?.OfficialHomeScore,
            evaluation?.OfficialAwayScore);

    private static MatchWithPredictionDto ToMatchWithPredictionDto(Match m, Prediction? prediction, PredictionEvaluation? evaluation) =>
        new(m.Id, m.RoundId, m.ParticipantHome, m.ParticipantAway, m.StartsAtUtc, m.Status.ToString(),
            m.HomeGoals, m.AwayGoals, prediction is null ? null : ToDto(prediction, evaluation), CanCreateOrEditPrediction(m));
}
