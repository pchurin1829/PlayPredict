using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class EditionEndpoints
{
    public static void MapEditionEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/competitions/{competitionId:int}/editions", async (int competitionId, PlayPredictDbContext db) =>
        {
            var competitionExists = await db.Competitions.AnyAsync(c => c.Id == competitionId);
            if (!competitionExists)
            {
                return Results.NotFound();
            }

            var editions = await db.Editions
                .Where(e => e.CompetitionId == competitionId)
                .OrderByDescending(e => e.StartDateUtc)
                .Select(e => ToDto(e))
                .ToListAsync();

            return Results.Ok(editions);
        }).WithTags("Editions");

        app.MapGet("/api/editions/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var edition = await db.Editions.FindAsync(id);
            return edition is null
                ? Results.NotFound()
                : Results.Ok(ToDto(edition));
        }).WithTags("Editions");

        app.MapPost("/api/competitions/{competitionId:int}/editions", async (int competitionId, CreateEditionDto dto, PlayPredictDbContext db) =>
        {
            var competitionExists = await db.Competitions.AnyAsync(c => c.Id == competitionId);
            if (!competitionExists)
            {
                return Results.NotFound();
            }

            var (errors, status) = ValidateEdition(dto.Name, dto.StartDateUtc, dto.EndDateUtc, dto.Status, EditionStatus.Draft);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var now = DateTime.UtcNow;
            var edition = new Edition
            {
                CompetitionId = competitionId,
                Name = dto.Name.Trim(),
                StartDateUtc = dto.StartDateUtc,
                EndDateUtc = dto.EndDateUtc,
                Status = status,
                CreatedAtUtc = now
            };

            db.Editions.Add(edition);

            // Toda Edición debe contar con configuración de puntuación desde su creación,
            // con los valores iniciales editables (6 / 3 / 0).
            db.EditionScoringConfigurations.Add(new EditionScoringConfiguration
            {
                Edition = edition,
                ExactScorePoints = 6,
                CorrectOutcomePoints = 3,
                IncorrectPoints = 0,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

            await db.SaveChangesAsync();

            return Results.Created($"/api/editions/{edition.Id}", ToDto(edition));
        }).WithTags("Editions");

        app.MapPut("/api/editions/{id:int}", async (int id, UpdateEditionDto dto, PlayPredictDbContext db) =>
        {
            var edition = await db.Editions.FindAsync(id);
            if (edition is null)
            {
                return Results.NotFound();
            }

            var (errors, status) = ValidateEdition(dto.Name, dto.StartDateUtc, dto.EndDateUtc, dto.Status, edition.Status);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            edition.Name = dto.Name.Trim();
            edition.StartDateUtc = dto.StartDateUtc;
            edition.EndDateUtc = dto.EndDateUtc;
            edition.Status = status;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(edition));
        }).WithTags("Editions");
    }

    private static (Dictionary<string, string[]> Errors, EditionStatus Status) ValidateEdition(
        string name, DateTime startDateUtc, DateTime? endDateUtc, string? statusInput, EditionStatus fallbackStatus)
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

        if (endDateUtc.HasValue && endDateUtc.Value < startDateUtc)
        {
            errors["endDateUtc"] = ["La fecha de finalización no puede ser anterior a la fecha de inicio."];
        }

        var status = fallbackStatus;
        if (!string.IsNullOrWhiteSpace(statusInput))
        {
            if (!Enum.TryParse<EditionStatus>(statusInput, ignoreCase: true, out status))
            {
                errors["status"] = [$"Estado inválido. Valores permitidos: {string.Join(", ", Enum.GetNames<EditionStatus>())}."];
            }
        }

        return (errors, status);
    }

    internal static EditionDto ToDto(Edition e) =>
        new(e.Id, e.CompetitionId, e.Name, e.StartDateUtc, e.EndDateUtc, e.Status.ToString(), e.CreatedAtUtc);
}
