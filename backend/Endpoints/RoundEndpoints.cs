using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
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

        app.MapPost("/api/editions/{editionId:int}/rounds/generate", async (int editionId, GenerateRoundsDto dto, PlayPredictDbContext db) =>
        {
            if (!await db.Editions.AnyAsync(e => e.Id == editionId)) return Results.NotFound();
            if (dto.Count < 1)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["count"] = ["La cantidad de Fechas debe ser mayor o igual a 1."]
                });
            }

            var existing = await db.Rounds.Where(r => r.EditionId == editionId).OrderBy(r => r.Order).ToListAsync();
            var existingCount = existing.Count;
            if (dto.Count > existingCount)
            {
                var usedOrders = existing.Select(r => r.Order).ToHashSet();
                for (var order = 1; existing.Count < dto.Count; order++)
                {
                    if (usedOrders.Contains(order)) continue;
                    var round = new Round { EditionId = editionId, Name = $"Fecha {order}", Order = order };
                    db.Rounds.Add(round);
                    existing.Add(round);
                    usedOrders.Add(order);
                }
                await db.SaveChangesAsync();
            }

            var createdCount = existing.Count - existingCount;
            var message = dto.Count < existingCount
                ? $"La edición ya tiene {existingCount} fechas. Reducir la cantidad no elimina fechas existentes."
                : createdCount == 0
                    ? $"La edición ya tiene {existingCount} fechas. No se generaron duplicados."
                    : $"Se generaron {createdCount} fechas. La edición ahora tiene {existing.Count}.";

            return Results.Ok(new GenerateRoundsResultDto(existingCount, createdCount, existing.Count, message,
                existing.OrderBy(r => r.Order).Select(ToDto).ToList()));
        }).WithTags("Rounds").RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

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

            var existingRounds = await db.Rounds.Where(r => r.EditionId == editionId).OrderBy(r => r.Order).ToListAsync();
            var occupied = existingRounds.FirstOrDefault(r => r.Order == dto.Order);
            if (occupied is not null)
            {
                var nextOrder = NextAvailableOrder(existingRounds.Select(r => r.Order));
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["order"] = [$"El orden {dto.Order} ya está utilizado por {occupied.Name}. El próximo orden disponible es {nextOrder}."]
                });
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

            var siblingRounds = await db.Rounds.Where(r => r.EditionId == round.EditionId && r.Id != round.Id).OrderBy(r => r.Order).ToListAsync();
            var occupied = siblingRounds.FirstOrDefault(r => r.Order == dto.Order);
            if (occupied is not null)
            {
                var nextOrder = NextAvailableOrder(siblingRounds.Append(round).Select(r => r.Order));
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["order"] = [$"El orden {dto.Order} ya está utilizado por {occupied.Name}. El próximo orden disponible es {nextOrder}."]
                });
            }

            round.Name = dto.Name.Trim();
            round.Order = dto.Order;
            // Los rangos históricos de Round se conservan: la fecha/hora operativa pertenece a Match.
            if (dto.StartDateUtc.HasValue) round.StartDateUtc = dto.StartDateUtc;
            if (dto.EndDateUtc.HasValue) round.EndDateUtc = dto.EndDateUtc;

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

    private static int NextAvailableOrder(IEnumerable<int> orders)
    {
        var used = orders.ToHashSet();
        var next = 1;
        while (used.Contains(next)) next++;
        return next;
    }

    internal static RoundDto ToDto(Round r) =>
        new(r.Id, r.EditionId, r.Name, r.Order, r.StartDateUtc, r.EndDateUtc);
}
