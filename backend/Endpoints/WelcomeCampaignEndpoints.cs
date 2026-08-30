using System.Security.Claims;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Enums;
using PlayPredict.Api.WelcomeCampaigns;

namespace PlayPredict.Api.Endpoints;

public static class WelcomeCampaignEndpoints
{
    public static void MapWelcomeCampaignEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/api/welcome-campaign/active", async (ClaimsPrincipal principal, WelcomeCampaignService service, CancellationToken cancellationToken) =>
        {
            if (!TryClaims(principal, out var companyId, out _)) return Results.Unauthorized();
            var active = await service.GetActiveForCompanyAsync(companyId, cancellationToken);
            return active is null ? Results.NoContent() : Results.Ok(active);
        }).RequireAuthorization().WithTags("Welcome Campaign");

        var admin = app.MapGroup("/api/admin/welcome-campaigns")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin))
            .WithTags("Welcome Campaign Admin");

        admin.MapGet("", async (ClaimsPrincipal principal, WelcomeCampaignService service, CancellationToken cancellationToken) =>
            TryClaims(principal, out var companyId, out _) ? Results.Ok(await service.GetAllAsync(companyId, cancellationToken)) : Results.Unauthorized());

        admin.MapGet("/{id:int}", async (int id, ClaimsPrincipal principal, WelcomeCampaignService service, CancellationToken cancellationToken) =>
        {
            if (!TryClaims(principal, out var companyId, out _)) return Results.Unauthorized();
            var campaign = await service.GetAsync(companyId, id, cancellationToken);
            return campaign is null ? Results.NotFound() : Results.Ok(campaign);
        });

        admin.MapPost("", async (CreateWelcomeCampaignRequest request, ClaimsPrincipal principal, WelcomeCampaignService service, CancellationToken cancellationToken) =>
            await Guarded(async () =>
            {
                if (!TryClaims(principal, out var companyId, out var userId)) return Results.Unauthorized();
                var created = await service.CreateAsync(companyId, userId, request.Name, request.ValidFromUtc, request.ValidToUtc, cancellationToken);
                return Results.Created($"/api/admin/welcome-campaigns/{created.Id}", created);
            }));

        admin.MapPut("/{id:int}", async (int id, UpdateWelcomeCampaignRequest request, ClaimsPrincipal principal, WelcomeCampaignService service, CancellationToken cancellationToken) =>
            await Guarded(async () =>
            {
                if (!TryClaims(principal, out var companyId, out var userId)) return Results.Unauthorized();
                var updated = await service.UpdateAsync(companyId, userId, id, request.Name, request.ValidFromUtc, request.ValidToUtc, cancellationToken);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            }));

        admin.MapPost("/{id:int}/activate", async (int id, ClaimsPrincipal principal, WelcomeCampaignService service, CancellationToken cancellationToken) =>
            await Guarded(async () =>
            {
                if (!TryClaims(principal, out var companyId, out var userId)) return Results.Unauthorized();
                var updated = await service.ActivateAsync(companyId, userId, id, cancellationToken);
                return updated is null ? Results.NotFound() : Results.Ok(updated);
            }));

        admin.MapPost("/{id:int}/deactivate", async (int id, ClaimsPrincipal principal, WelcomeCampaignService service, CancellationToken cancellationToken) =>
        {
            if (!TryClaims(principal, out var companyId, out var userId)) return Results.Unauthorized();
            var updated = await service.DeactivateAsync(companyId, userId, id, cancellationToken);
            return updated is null ? Results.NotFound() : Results.Ok(updated);
        });

        admin.MapDelete("/{id:int}", async (int id, ClaimsPrincipal principal, WelcomeCampaignService service, CancellationToken cancellationToken) =>
            await Guarded(async () =>
            {
                if (!TryClaims(principal, out var companyId, out _)) return Results.Unauthorized();
                var result = await service.DeleteAsync(companyId, id, cancellationToken);
                return result is null ? Results.NotFound() : Results.Ok(new { deleted = true });
            }));

        admin.MapPost("/{id:int}/slides", async (int id, IFormFile file, ClaimsPrincipal principal,
            WelcomeCampaignImageValidator validator, WelcomeCampaignService service, CancellationToken cancellationToken) =>
            await Guarded(async () =>
            {
                if (!TryClaims(principal, out var companyId, out _)) return Results.Unauthorized();
                var validation = await validator.ValidateAsync(file.OpenReadStream(), file.Length, cancellationToken);
                if (!validation.IsValid) return Results.BadRequest(new { code = validation.ErrorCode, message = validation.ErrorMessage });
                var slide = await service.AddSlideAsync(companyId, id, validation, cancellationToken);
                return slide is null ? Results.NotFound() : Results.Ok(slide);
            })).DisableAntiforgery();

        admin.MapPost("/{id:int}/slides/{slideId:int}/image", async (int id, int slideId, IFormFile file, ClaimsPrincipal principal,
            WelcomeCampaignImageValidator validator, WelcomeCampaignService service, CancellationToken cancellationToken) =>
            await Guarded(async () =>
            {
                if (!TryClaims(principal, out var companyId, out _)) return Results.Unauthorized();
                var validation = await validator.ValidateAsync(file.OpenReadStream(), file.Length, cancellationToken);
                if (!validation.IsValid) return Results.BadRequest(new { code = validation.ErrorCode, message = validation.ErrorMessage });
                var slide = await service.ReplaceSlideImageAsync(companyId, id, slideId, validation, cancellationToken);
                return slide is null ? Results.NotFound() : Results.Ok(slide);
            })).DisableAntiforgery();

        admin.MapPut("/{id:int}/slides/{slideId:int}", async (int id, int slideId, UpdateWelcomeCampaignSlideRequest request, ClaimsPrincipal principal,
            WelcomeCampaignService service, CancellationToken cancellationToken) =>
            await Guarded(async () =>
            {
                if (!TryClaims(principal, out var companyId, out _)) return Results.Unauthorized();
                if (!Enum.TryParse<WelcomeCampaignFitMode>(request.FitMode, true, out var fitMode))
                    return Results.BadRequest(new { code = "INVALID_FIT_MODE", message = "FitMode debe ser Contain o Cover." });
                var slide = await service.UpdateSlideAsync(companyId, id, slideId, request.DurationSeconds, fitMode, cancellationToken);
                return slide is null ? Results.NotFound() : Results.Ok(slide);
            }));

        admin.MapPut("/{id:int}/slides/{slideId:int}/order", async (int id, int slideId, ReorderWelcomeCampaignSlideRequest request, ClaimsPrincipal principal,
            WelcomeCampaignService service, CancellationToken cancellationToken) =>
        {
            if (!TryClaims(principal, out var companyId, out _)) return Results.Unauthorized();
            var slides = await service.ReorderSlideAsync(companyId, id, slideId, request.SortOrder, cancellationToken);
            return slides is null ? Results.NotFound() : Results.Ok(slides);
        });

        admin.MapDelete("/{id:int}/slides/{slideId:int}", async (int id, int slideId, ClaimsPrincipal principal, WelcomeCampaignService service, CancellationToken cancellationToken) =>
        {
            if (!TryClaims(principal, out var companyId, out _)) return Results.Unauthorized();
            var slides = await service.DeleteSlideAsync(companyId, id, slideId, cancellationToken);
            return slides is null ? Results.NotFound() : Results.Ok(slides);
        });
    }

    private static async Task<IResult> Guarded(Func<Task<IResult>> action)
    {
        try { return await action(); }
        catch (WelcomeCampaignConcurrencyException ex) { return Results.Conflict(new { code = ex.Code, message = ex.Message }); }
        catch (WelcomeCampaignValidationException ex) { return Results.BadRequest(new { code = ex.Code, message = ex.Message }); }
    }

    private static bool TryClaims(ClaimsPrincipal principal, out int companyId, out int userId)
    {
        var company = principal.FindFirstValue("companyId");
        var user = principal.FindFirstValue(ClaimTypes.NameIdentifier) ?? principal.FindFirstValue("sub");
        var hasCompany = int.TryParse(company, out companyId);
        var hasUser = int.TryParse(user, out userId);
        return hasCompany && hasUser;
    }
}
