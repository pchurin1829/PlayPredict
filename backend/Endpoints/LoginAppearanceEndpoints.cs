using System.Security.Claims;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.LoginAppearance;

namespace PlayPredict.Api.Endpoints;

public static class LoginAppearanceEndpoints
{
    public static void MapLoginAppearanceEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/public/login-appearance", async (HttpContext context, ILoginAppearanceCompanyResolver resolver,
            LoginAppearanceService service, CancellationToken cancellationToken) =>
        {
            try
            {
                var companyId = await resolver.ResolvePublicCompanyIdAsync(cancellationToken);
                context.Response.Headers.CacheControl = "no-cache, must-revalidate";
                return Results.Ok(await service.GetPublicAsync(companyId, cancellationToken));
            }
            catch (LoginAppearanceConfigurationException)
            {
                return Results.Json(new { code = "LOGIN_APPEARANCE_NOT_CONFIGURED", message = "La apariencia pública del login no está configurada." }, statusCode: 503);
            }
        }).AllowAnonymous().WithTags("Login Appearance");

        var admin = app.MapGroup("/api/admin/login-appearance").RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin)).WithTags("Login Appearance Admin");

        admin.MapGet("", async (ClaimsPrincipal principal, LoginAppearanceService service, CancellationToken cancellationToken) =>
            TryClaims(principal, out var companyId, out _) ? Results.Ok(await service.GetAdminAsync(companyId, cancellationToken)) : Results.Unauthorized());

        admin.MapPost("/{slot}/image", async (string slot, IFormFile file, ClaimsPrincipal principal,
            LoginImageValidator validator, LoginAppearanceService service, CancellationToken cancellationToken) =>
        {
            if (!TryClaims(principal, out var companyId, out var userId)) return Results.Unauthorized();
            if (!TrySlot(slot, out var parsedSlot)) return Results.BadRequest(new { code = "INVALID_SLOT", message = "Slot no reconocido." });
            var validation = await validator.ValidateAsync(file.OpenReadStream(), file.Length, parsedSlot, cancellationToken);
            if (!validation.IsValid) return Results.BadRequest(new { code = validation.ErrorCode, message = validation.ErrorMessage });
            return Results.Ok(await service.ReplaceImageAsync(companyId, userId, parsedSlot, validation, cancellationToken));
        }).DisableAntiforgery();

        admin.MapPut("/{slot}/fit-mode", async (string slot, UpdateLoginImageFitModeRequest request, ClaimsPrincipal principal,
            LoginAppearanceService service, CancellationToken cancellationToken) =>
        {
            if (!TryClaims(principal, out var companyId, out var userId)) return Results.Unauthorized();
            if (!TrySlot(slot, out var parsedSlot)) return Results.BadRequest(new { code = "INVALID_SLOT", message = "Slot no reconocido." });
            if (!Enum.TryParse<LoginImageFitMode>(request.FitMode, true, out var fitMode))
                return Results.BadRequest(new { code = "INVALID_FIT_MODE", message = "FitMode debe ser Contain o Cover." });
            return Results.Ok(await service.UpdateFitModeAsync(companyId, userId, parsedSlot, fitMode, cancellationToken));
        });

        admin.MapDelete("/{slot}", async (string slot, ClaimsPrincipal principal, LoginAppearanceService service, CancellationToken cancellationToken) =>
        {
            if (!TryClaims(principal, out var companyId, out _)) return Results.Unauthorized();
            if (!TrySlot(slot, out var parsedSlot)) return Results.BadRequest(new { code = "INVALID_SLOT", message = "Slot no reconocido." });
            return Results.Ok(await service.RestoreDefaultAsync(companyId, parsedSlot, cancellationToken));
        });
    }

    private static bool TrySlot(string value, out LoginImageSlot slot) => Enum.TryParse(value, true, out slot);
    private static bool TryClaims(ClaimsPrincipal principal, out int companyId, out int userId)
    {
        var company = principal.FindFirstValue("companyId");
        var user = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var hasCompany = int.TryParse(company, out companyId);
        var hasUser = int.TryParse(user, out userId);
        return hasCompany && hasUser;
    }
}
