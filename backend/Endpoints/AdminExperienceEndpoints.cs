using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class AdminExperienceEndpoints
{
    public static void MapAdminExperienceEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/experiences")
            .WithTags("Admin Experiences")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapGet("", async (PlayPredictDbContext db) =>
        {
            var experiences = await db.Experiences
                .OrderBy(e => e.Name)
                .ToListAsync();

            return Results.Ok(experiences.Select(ToDto));
        });

        group.MapGet("/{id:int}", async (int id, PlayPredictDbContext db) =>
        {
            var experience = await db.Experiences.FindAsync(id);
            return experience is null ? Results.NotFound() : Results.Ok(ToDto(experience));
        });

        group.MapPost("", async (CreateExperienceDto dto, PlayPredictDbContext db) =>
        {
            var errors = Validate(dto.Name, dto.Description, dto.PrimaryColor, dto.SecondaryColor, dto.LogoUrl,
                dto.DefaultExactScorePoints, dto.DefaultCorrectOutcomePoints, dto.DefaultIncorrectPoints);

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var now = DateTime.UtcNow;
            var experience = new Experience
            {
                Name = dto.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                Status = ExperienceStatus.Draft,
                PrimaryColor = string.IsNullOrWhiteSpace(dto.PrimaryColor) ? null : dto.PrimaryColor.Trim(),
                SecondaryColor = string.IsNullOrWhiteSpace(dto.SecondaryColor) ? null : dto.SecondaryColor.Trim(),
                LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim(),
                IsPublic = dto.IsPublic,
                DefaultExactScorePoints = dto.DefaultExactScorePoints,
                DefaultCorrectOutcomePoints = dto.DefaultCorrectOutcomePoints,
                DefaultIncorrectPoints = dto.DefaultIncorrectPoints,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };

            db.Experiences.Add(experience);
            await db.SaveChangesAsync();

            return Results.Created($"/api/admin/experiences/{experience.Id}", ToDto(experience));
        });

        group.MapPut("/{id:int}", async (int id, UpdateExperienceDto dto, PlayPredictDbContext db) =>
        {
            var experience = await db.Experiences.FindAsync(id);
            if (experience is null)
            {
                return Results.NotFound();
            }

            if (experience.Status == ExperienceStatus.Archived)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["No se puede modificar una Experience Archivada."]
                });
            }

            var errors = Validate(dto.Name, dto.Description, dto.PrimaryColor, dto.SecondaryColor, dto.LogoUrl,
                dto.DefaultExactScorePoints, dto.DefaultCorrectOutcomePoints, dto.DefaultIncorrectPoints);

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            experience.Name = dto.Name.Trim();
            experience.Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim();
            experience.PrimaryColor = string.IsNullOrWhiteSpace(dto.PrimaryColor) ? null : dto.PrimaryColor.Trim();
            experience.SecondaryColor = string.IsNullOrWhiteSpace(dto.SecondaryColor) ? null : dto.SecondaryColor.Trim();
            experience.LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim();
            experience.IsPublic = dto.IsPublic;
            experience.DefaultExactScorePoints = dto.DefaultExactScorePoints;
            experience.DefaultCorrectOutcomePoints = dto.DefaultCorrectOutcomePoints;
            experience.DefaultIncorrectPoints = dto.DefaultIncorrectPoints;
            experience.UpdatedAtUtc = DateTime.UtcNow;

            await db.SaveChangesAsync();

            return Results.Ok(ToDto(experience));
        });

        group.MapPut("/{id:int}/publish", async (int id, PlayPredictDbContext db) =>
        {
            var experience = await db.Experiences.FindAsync(id);
            if (experience is null)
            {
                return Results.NotFound();
            }

            if (experience.Status != ExperienceStatus.Draft)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["Solo se puede publicar una Experience en Borrador."]
                });
            }

            experience.Status = ExperienceStatus.Published;
            experience.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(experience));
        });

        group.MapPut("/{id:int}/archive", async (int id, PlayPredictDbContext db) =>
        {
            var experience = await db.Experiences.FindAsync(id);
            if (experience is null)
            {
                return Results.NotFound();
            }

            if (experience.Status == ExperienceStatus.Archived)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["status"] = ["La Experience ya está Archivada."]
                });
            }

            experience.Status = ExperienceStatus.Archived;
            experience.UpdatedAtUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            return Results.Ok(ToDto(experience));
        });
    }

    private static Dictionary<string, string[]> Validate(
        string name, string? description, string? primaryColor, string? secondaryColor, string? logoUrl,
        int exact, int correct, int incorrect)
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

        if (!string.IsNullOrWhiteSpace(description) && description.Trim().Length > 1000)
        {
            errors["description"] = ["La descripción no puede superar los 1000 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(primaryColor) && primaryColor.Trim().Length > 20)
        {
            errors["primaryColor"] = ["El color primario no puede superar los 20 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(secondaryColor) && secondaryColor.Trim().Length > 20)
        {
            errors["secondaryColor"] = ["El color secundario no puede superar los 20 caracteres."];
        }

        if (!string.IsNullOrWhiteSpace(logoUrl) && logoUrl.Trim().Length > 500)
        {
            errors["logoUrl"] = ["La URL del logo no puede superar los 500 caracteres."];
        }

        if (exact < 0)
        {
            errors["defaultExactScorePoints"] = ["Debe ser un valor entero mayor o igual a 0."];
        }

        if (correct < 0)
        {
            errors["defaultCorrectOutcomePoints"] = ["Debe ser un valor entero mayor o igual a 0."];
        }

        if (incorrect < 0)
        {
            errors["defaultIncorrectPoints"] = ["Debe ser un valor entero mayor o igual a 0."];
        }

        return errors;
    }

    internal static ExperienceDto ToDto(Experience e) =>
        new(e.Id, e.Name, e.Description, e.Status.ToString(), StatusLabel(e.Status),
            e.PrimaryColor, e.SecondaryColor, e.LogoUrl, e.IsPublic,
            e.DefaultExactScorePoints, e.DefaultCorrectOutcomePoints, e.DefaultIncorrectPoints,
            e.CreatedAtUtc, e.UpdatedAtUtc);

    internal static string StatusLabel(ExperienceStatus status) => status switch
    {
        ExperienceStatus.Draft => "Borrador",
        ExperienceStatus.Published => "Publicada",
        ExperienceStatus.Archived => "Archivada",
        _ => status.ToString()
    };
}
