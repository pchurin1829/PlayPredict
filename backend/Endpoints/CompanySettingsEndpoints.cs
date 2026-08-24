using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class CompanySettingsEndpoints
{
    public static void MapCompanySettingsEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/company-settings")
            .WithTags("Company Settings")
            .RequireAuthorization();

        group.MapGet("", async (ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var companyId = GetCompanyId(principal);
            var company = companyId is null
                ? null
                : await db.Companies.AsNoTracking().FirstOrDefaultAsync(c => c.Id == companyId);

            return Results.Ok(company is null
                ? new CompanySettingsDto("PlayPredict", "PlayPredict", null)
                : new CompanySettingsDto(company.Name, company.ShortName ?? company.Name, company.LogoUrl));
        });

        group.MapPut("", async (UpdateCompanySettingsDto dto, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            if (!principal.IsInRole(RoleNames.Admin)) return Results.Forbid();

            var errors = Validate(dto);
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var companyId = GetCompanyId(principal);
            var company = companyId is null
                ? null
                : await db.Companies.FirstOrDefaultAsync(c => c.Id == companyId);
            if (company is null) return Results.NotFound();

            company.Name = dto.Name.Trim();
            company.ShortName = dto.ShortName.Trim();
            company.LogoUrl = string.IsNullOrWhiteSpace(dto.LogoUrl) ? null : dto.LogoUrl.Trim();
            await db.SaveChangesAsync();

            return Results.Ok(new CompanySettingsDto(company.Name, company.ShortName, company.LogoUrl));
        });

        group.MapPost("/logo", async (IFormFile file, ClaimsPrincipal principal, PlayPredictDbContext db, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            if (!principal.IsInRole(RoleNames.Admin)) return Results.Forbid();
            var companyId = GetCompanyId(principal);
            var company = companyId is null ? null : await db.Companies.FindAsync(companyId.Value);
            if (company is null) return Results.NotFound();
            var (url, uploadError) = await ManagedImageStorage.SaveAsync(file, "companies", $"company-{company.Id}", configuration, environment);
            if (uploadError is not null) return Results.BadRequest(new { message = uploadError });
            ManagedImageStorage.Delete(company.LogoUrl, "companies", configuration, environment);
            company.LogoUrl = url;
            await db.SaveChangesAsync();
            return Results.Ok(new CompanySettingsDto(company.Name, company.ShortName ?? company.Name, company.LogoUrl));
        }).DisableAntiforgery();

        group.MapDelete("/logo", async (ClaimsPrincipal principal, PlayPredictDbContext db, IWebHostEnvironment environment, IConfiguration configuration) =>
        {
            if (!principal.IsInRole(RoleNames.Admin)) return Results.Forbid();
            var companyId = GetCompanyId(principal);
            var company = companyId is null ? null : await db.Companies.FindAsync(companyId.Value);
            if (company is null) return Results.NotFound();
            ManagedImageStorage.Delete(company.LogoUrl, "companies", configuration, environment);
            company.LogoUrl = null;
            await db.SaveChangesAsync();
            return Results.Ok(new CompanySettingsDto(company.Name, company.ShortName ?? company.Name, null));
        });
    }

    private static int? GetCompanyId(ClaimsPrincipal principal) =>
        int.TryParse(principal.FindFirstValue("companyId"), out var companyId) ? companyId : null;

    private static Dictionary<string, string[]> Validate(UpdateCompanySettingsDto dto)
    {
        var errors = new Dictionary<string, string[]>();
        if (string.IsNullOrWhiteSpace(dto.Name)) errors["name"] = ["El nombre de empresa es obligatorio."];
        else if (dto.Name.Trim().Length > 150) errors["name"] = ["El nombre no puede superar 150 caracteres."];
        if (string.IsNullOrWhiteSpace(dto.ShortName)) errors["shortName"] = ["El nombre corto es obligatorio."];
        else if (dto.ShortName.Trim().Length > 80) errors["shortName"] = ["El nombre corto no puede superar 80 caracteres."];
        if (dto.LogoUrl?.Trim().Length > 500) errors["logoUrl"] = ["La referencia del logo no puede superar 500 caracteres."];
        return errors;
    }
}

public record CompanySettingsDto(string Name, string ShortName, string? LogoUrl);
public record UpdateCompanySettingsDto(string Name, string ShortName, string? LogoUrl);
