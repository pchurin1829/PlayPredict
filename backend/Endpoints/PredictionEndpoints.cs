using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

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

            var result = matches.Select(m =>
                ToMatchWithPredictionDto(m, predictions.FirstOrDefault(p => p.MatchId == m.Id)));

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
                .Select(p => ToDto(p))
                .ToListAsync();

            return Results.Ok(predictions);
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

            if (!IsOpenForPrediction(match.Status))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["Este partido no admite pronósticos en su estado actual."]
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
            if (match is null || !IsOpenForPrediction(match.Status))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["Este partido ya no admite modificar el pronóstico."]
                });
            }

            prediction.PredictedHomeScore = dto.PredictedHomeScore;
            prediction.PredictedAwayScore = dto.PredictedAwayScore;
            prediction.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(prediction));
        });
    }

    // Un pronóstico solo puede cargarse o modificarse mientras el partido esté
    // Programado, En juego o Suspendido. Cancelado y Finalizado quedan cerrados.
    private static bool IsOpenForPrediction(MatchStatus status) =>
        status is MatchStatus.Scheduled or MatchStatus.InProgress or MatchStatus.Suspended;

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

    private static PredictionDto ToDto(Prediction p) =>
        new(p.Id, p.MatchId, p.UserId, p.PredictedHomeScore, p.PredictedAwayScore, p.CreatedAtUtc, p.UpdatedAtUtc);

    private static MatchWithPredictionDto ToMatchWithPredictionDto(Match m, Prediction? prediction) =>
        new(m.Id, m.RoundId, m.ParticipantHome, m.ParticipantAway, m.StartsAtUtc, m.Status.ToString(),
            m.HomeGoals, m.AwayGoals, prediction is null ? null : ToDto(prediction));
}
