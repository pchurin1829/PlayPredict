using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users").WithTags("Users").RequireAuthorization();

        group.MapGet("/me", async (ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var user = await GetCurrentUserAsync(principal, db, includeRoles: true);
            if (user is null)
            {
                return Results.NotFound();
            }

            var roles = user.UserRoles.Select(ur => ur.Role.Name);
            return Results.Ok(AuthEndpoints.ToUserDto(user, roles));
        });

        group.MapPut("/me", async (UpdateProfileDto dto, ClaimsPrincipal principal, PlayPredictDbContext db) =>
        {
            var errors = new Dictionary<string, string[]>();

            if (string.IsNullOrWhiteSpace(dto.FirstName))
            {
                errors["firstName"] = ["El nombre es obligatorio."];
            }

            if (string.IsNullOrWhiteSpace(dto.LastName))
            {
                errors["lastName"] = ["El apellido es obligatorio."];
            }

            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }

            var user = await GetCurrentUserAsync(principal, db, includeRoles: true);
            if (user is null)
            {
                return Results.NotFound();
            }

            user.FirstName = dto.FirstName.Trim();
            user.LastName = dto.LastName.Trim();
            await db.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name);
            return Results.Ok(AuthEndpoints.ToUserDto(user, roles));
        });
    }

    internal static async Task<User?> GetCurrentUserAsync(
        ClaimsPrincipal principal,
        PlayPredictDbContext db,
        bool includeRoles = false)
    {
        var idClaim = principal.FindFirstValue(ClaimTypes.NameIdentifier);
        if (idClaim is null || !int.TryParse(idClaim, out var userId))
        {
            return null;
        }

        IQueryable<User> users = db.Users;
        if (includeRoles)
        {
            users = users.Include(u => u.UserRoles).ThenInclude(ur => ur.Role);
        }

        return await users.FirstOrDefaultAsync(u => u.Id == userId);
    }
}
