using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class EditionScoringConfigurationEndpoints
{
    public static void MapEditionScoringConfigurationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/editions/{editionId:int}/scoring-configuration")
            .WithTags("Edition Scoring Configuration")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapGet("", async (int editionId, PlayPredictDbContext db) =>
        {
            var editionExists = await db.Editions.AnyAsync(e => e.Id == editionId);
            if (!editionExists)
            {
                return Results.NotFound();
            }

            var config = await db.EditionScoringConfigurations
                .FirstOrDefaultAsync(c => c.EditionId == editionId);

            if (config is null)
            {
                // No debería ocurrir (toda Edición debe tener configuración), pero se crea
                // de forma defensiva con los valores iniciales si faltara.
                var now = DateTime.UtcNow;
                config = new EditionScoringConfiguration
                {
                    EditionId = editionId,
                    ExactScorePoints = 6,
                    CorrectOutcomePoints = 3,
                    IncorrectPoints = 0,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                db.EditionScoringConfigurations.Add(config);
                await db.SaveChangesAsync();
            }

            return Results.Ok(ToDto(config));
        });

        group.MapPut("", async (int editionId, UpdateEditionScoringConfigurationDto dto, PlayPredictDbContext db) =>
        {
            var editionExists = await db.Editions.AnyAsync(e => e.Id == editionId);
            if (!editionExists)
            {
                return Results.NotFound();
            }

            var errors = Validate(dto);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var config = await db.EditionScoringConfigurations
                .FirstOrDefaultAsync(c => c.EditionId == editionId);

            var now = DateTime.UtcNow;
            if (config is null)
            {
                config = new EditionScoringConfiguration
                {
                    EditionId = editionId,
                    CreatedAtUtc = now
                };
                db.EditionScoringConfigurations.Add(config);
            }

            config.ExactScorePoints = dto.ExactScorePoints;
            config.CorrectOutcomePoints = dto.CorrectOutcomePoints;
            config.IncorrectPoints = dto.IncorrectPoints;
            config.UpdatedAtUtc = now;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(config));
        });
    }

    private static Dictionary<string, string[]> Validate(UpdateEditionScoringConfigurationDto dto)
    {
        var errors = new Dictionary<string, string[]>();

        if (dto.ExactScorePoints < 0)
        {
            errors["exactScorePoints"] = ["Debe ser un valor entero mayor o igual a 0."];
        }

        if (dto.CorrectOutcomePoints < 0)
        {
            errors["correctOutcomePoints"] = ["Debe ser un valor entero mayor o igual a 0."];
        }

        if (dto.IncorrectPoints < 0)
        {
            errors["incorrectPoints"] = ["Debe ser un valor entero mayor o igual a 0."];
        }

        return errors;
    }

    private static EditionScoringConfigurationDto ToDto(EditionScoringConfiguration c) =>
        new(c.Id, c.EditionId, c.ExactScorePoints, c.CorrectOutcomePoints, c.IncorrectPoints, c.CreatedAtUtc, c.UpdatedAtUtc);
}
