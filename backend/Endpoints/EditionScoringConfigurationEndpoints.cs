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
            var edition = await db.Editions.FindAsync(editionId);
            if (edition is null)
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
                    UseExperienceDefaults = false,
                    PreferredPlayerEnabled = true,
                    PreferredPlayerPointsPerGoal = 2,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                db.EditionScoringConfigurations.Add(config);
                await db.SaveChangesAsync();
            }

            return Results.Ok(await ToDtoAsync(config, edition.CompetitionId, db));
        });

        group.MapPut("", async (int editionId, UpdateEditionScoringConfigurationDto dto, PlayPredictDbContext db) =>
        {
            var edition = await db.Editions.FindAsync(editionId);
            if (edition is null)
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
            config.UseExperienceDefaults = dto.UseExperienceDefaults;
            config.PreferredPlayerEnabled = dto.PreferredPlayerEnabled;
            config.PreferredPlayerPointsPerGoal = dto.PreferredPlayerPointsPerGoal;
            config.UpdatedAtUtc = now;

            await db.SaveChangesAsync();

            return Results.Ok(await ToDtoAsync(config, edition.CompetitionId, db));
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
        if (dto.PreferredPlayerPointsPerGoal < 0)
            errors["preferredPlayerPointsPerGoal"] = ["Debe ser un valor entero mayor o igual a 0."];

        return errors;
    }

    // Los valores "propios" siguen guardados y validados exactamente como antes del Sprint 8
    // (para que "configuración propia" continúe funcionando igual). Los valores "efectivos"
    // son los que realmente aplica el Motor de Puntuación: los de la Experience, completos,
    // cuando UseExperienceDefaults es true.
    private static async Task<EditionScoringConfigurationDto> ToDtoAsync(
        EditionScoringConfiguration config, int competitionId, PlayPredictDbContext db)
    {
        var effectiveExact = config.ExactScorePoints;
        var effectiveCorrect = config.CorrectOutcomePoints;
        var effectiveIncorrect = config.IncorrectPoints;

        if (config.UseExperienceDefaults)
        {
            var defaults = await db.Competitions
                .Where(c => c.Id == competitionId)
                .Select(c => new
                {
                    c.Experience.DefaultExactScorePoints,
                    c.Experience.DefaultCorrectOutcomePoints,
                    c.Experience.DefaultIncorrectPoints
                })
                .FirstOrDefaultAsync();

            if (defaults is not null)
            {
                effectiveExact = defaults.DefaultExactScorePoints;
                effectiveCorrect = defaults.DefaultCorrectOutcomePoints;
                effectiveIncorrect = defaults.DefaultIncorrectPoints;
            }
        }

        return new EditionScoringConfigurationDto(
            config.Id, config.EditionId, config.ExactScorePoints, config.CorrectOutcomePoints, config.IncorrectPoints,
            config.UseExperienceDefaults, effectiveExact, effectiveCorrect, effectiveIncorrect,
            config.PreferredPlayerEnabled, config.PreferredPlayerPointsPerGoal,
            config.CreatedAtUtc, config.UpdatedAtUtc);
    }
}
