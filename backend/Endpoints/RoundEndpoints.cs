using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class RoundEndpoints
{
    public static void MapRoundEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/editions/{editionId:int}/rounds", async (int editionId, PlayPredictDbContext db) =>
        {
            var editionExists = await db.Editions.AnyAsync(e => e.Id == editionId);
            if (!editionExists)
            {
                return Results.NotFound();
            }

            var rounds = await db.Rounds
                .Where(r => r.EditionId == editionId)
                .OrderBy(r => r.Order)
                .Select(r => ToDto(r))
                .ToListAsync();

            return Results.Ok(rounds);
        }).WithTags("Rounds");

        app.MapGet("/api/rounds/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var round = await db.Rounds.FindAsync(id);
            return round is null
                ? Results.NotFound()
                : Results.Ok(ToDto(round));
        }).WithTags("Rounds");

        app.MapPost("/api/editions/{editionId:int}/rounds", async (int editionId, CreateRoundDto dto, PlayPredictDbContext db) =>
        {
            var editionExists = await db.Editions.AnyAsync(e => e.Id == editionId);
            if (!editionExists)
            {
                return Results.NotFound();
            }

            var errors = ValidateRound(dto.Name, dto.Order, dto.StartDateUtc, dto.EndDateUtc);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var round = new Round
            {
                EditionId = editionId,
                Name = dto.Name.Trim(),
                Order = dto.Order,
                StartDateUtc = dto.StartDateUtc,
                EndDateUtc = dto.EndDateUtc
            };

            db.Rounds.Add(round);

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { message = "Ya existe una Fecha con ese orden en la Edición." });
            }

            return Results.Created($"/api/rounds/{round.Id}", ToDto(round));
        }).WithTags("Rounds");

        app.MapPut("/api/rounds/{id:int}", async (int id, UpdateRoundDto dto, PlayPredictDbContext db) =>
        {
            var round = await db.Rounds.FindAsync(id);
            if (round is null)
            {
                return Results.NotFound();
            }

            var errors = ValidateRound(dto.Name, dto.Order, dto.StartDateUtc, dto.EndDateUtc);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            round.Name = dto.Name.Trim();
            round.Order = dto.Order;
            round.StartDateUtc = dto.StartDateUtc;
            round.EndDateUtc = dto.EndDateUtc;

            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                return Results.Conflict(new { message = "Ya existe una Fecha con ese orden en la Edición." });
            }

            return Results.Ok(ToDto(round));
        }).WithTags("Rounds");
    }

    private static Dictionary<string, string[]> ValidateRound(string name, int order, DateTime? startDateUtc, DateTime? endDateUtc)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors["name"] = ["El nombre es obligatorio."];
        }
        else if (name.Trim().Length > 150)
        {
            errors["name"] = ["El nombre no puede superar los 150 caracteres."];
        }

        if (order < 1)
        {
            errors["order"] = ["El orden debe ser mayor o igual a 1."];
        }

        if (startDateUtc.HasValue && endDateUtc.HasValue && endDateUtc.Value < startDateUtc.Value)
        {
            errors["endDateUtc"] = ["La fecha de finalización no puede ser anterior a la fecha de inicio."];
        }

        return errors;
    }

    internal static RoundDto ToDto(Round r) =>
        new(r.Id, r.EditionId, r.Name, r.Order, r.StartDateUtc, r.EndDateUtc);
}
