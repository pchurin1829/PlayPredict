using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class MatchEndpoints
{
    public static void MapMatchEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/rounds/{roundId:int}/matches", async (int roundId, PlayPredictDbContext db) =>
        {
            var roundExists = await db.Rounds.AnyAsync(r => r.Id == roundId);
            if (!roundExists)
            {
                return Results.NotFound();
            }

            var matches = await db.Matches
                .Where(m => m.RoundId == roundId)
                .OrderBy(m => m.StartsAtUtc)
                .Select(m => ToDto(m))
                .ToListAsync();

            return Results.Ok(matches);
        }).WithTags("Matches");

        app.MapGet("/api/matches/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var match = await db.Matches.FindAsync(id);
            return match is null
                ? Results.NotFound()
                : Results.Ok(ToDto(match));
        }).WithTags("Matches");

        app.MapPost("/api/rounds/{roundId:int}/matches", async (int roundId, CreateMatchDto dto, PlayPredictDbContext db) =>
        {
            var roundExists = await db.Rounds.AnyAsync(r => r.Id == roundId);
            if (!roundExists)
            {
                return Results.NotFound();
            }

            var (errors, status) = ValidateMatch(dto.ParticipantHome, dto.ParticipantAway, dto.Status, MatchStatus.Scheduled);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var match = new Match
            {
                RoundId = roundId,
                ParticipantHome = dto.ParticipantHome.Trim(),
                ParticipantAway = dto.ParticipantAway.Trim(),
                StartsAtUtc = dto.StartsAtUtc,
                Status = status,
                CreatedAtUtc = DateTime.UtcNow
            };

            db.Matches.Add(match);
            await db.SaveChangesAsync();

            return Results.Created($"/api/matches/{match.Id}", ToDto(match));
        }).WithTags("Matches");

        app.MapPut("/api/matches/{id:int}", async (int id, UpdateMatchDto dto, PlayPredictDbContext db) =>
        {
            var match = await db.Matches.FindAsync(id);
            if (match is null)
            {
                return Results.NotFound();
            }

            var (errors, status) = ValidateMatch(dto.ParticipantHome, dto.ParticipantAway, dto.Status, match.Status);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            match.ParticipantHome = dto.ParticipantHome.Trim();
            match.ParticipantAway = dto.ParticipantAway.Trim();
            match.StartsAtUtc = dto.StartsAtUtc;
            match.Status = status;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(match));
        }).WithTags("Matches");

        app.MapPut("/api/matches/{id:int}/result", async (int id, MatchResultDto dto, PlayPredictDbContext db) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (dto.HomeGoals < 0)
            {
                errors["homeGoals"] = ["Los goles del local no pueden ser negativos."];
            }

            if (dto.AwayGoals < 0)
            {
                errors["awayGoals"] = ["Los goles del visitante no pueden ser negativos."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var match = await db.Matches.FindAsync(id);
            if (match is null)
            {
                return Results.NotFound();
            }

            if (match.Status is MatchStatus.Cancelled)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["No se puede cargar un resultado en un Partido Cancelado."]
                });
            }

            match.HomeGoals = dto.HomeGoals;
            match.AwayGoals = dto.AwayGoals;
            match.Status = MatchStatus.Finished;

            await db.SaveChangesAsync();

            // Punto de extensión: en un sprint posterior, aquí se disparará el
            // recálculo de Pronósticos y Rankings a partir del Resultado Oficial.
            // await forecastRecalculationService.RecalculateForMatchAsync(match.Id);

            return Results.Ok(ToDto(match));
        }).WithTags("Matches");
    }

    private static (Dictionary<string, string[]> Errors, MatchStatus Status) ValidateMatch(
        string participantHome, string participantAway, string? statusInput, MatchStatus fallbackStatus)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(participantHome))
        {
            errors["participantHome"] = ["El participante local es obligatorio."];
        }
        else if (participantHome.Trim().Length > 150)
        {
            errors["participantHome"] = ["El participante local no puede superar los 150 caracteres."];
        }

        if (string.IsNullOrWhiteSpace(participantAway))
        {
            errors["participantAway"] = ["El participante visitante es obligatorio."];
        }
        else if (participantAway.Trim().Length > 150)
        {
            errors["participantAway"] = ["El participante visitante no puede superar los 150 caracteres."];
        }

        var status = fallbackStatus;
        if (!string.IsNullOrWhiteSpace(statusInput))
        {
            if (!Enum.TryParse<MatchStatus>(statusInput, ignoreCase: true, out status))
            {
                errors["status"] = [$"Estado inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<MatchStatus>())}."];
            }
            else if (status == MatchStatus.Finished)
            {
                errors["status"] = ["El estado Finalizado solo puede establecerse cargando el Resultado Oficial (PUT /api/matches/{id}/result)."];
            }
        }

        return (errors, status);
    }

    internal static MatchDto ToDto(Match m) =>
        new(m.Id, m.RoundId, m.ParticipantHome, m.ParticipantAway, m.StartsAtUtc, m.Status.ToString(), m.HomeGoals, m.AwayGoals, m.CreatedAtUtc);
}
