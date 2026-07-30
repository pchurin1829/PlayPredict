using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Dtos;

namespace PlayPredict.Api.Endpoints;

public static class AdminUserEndpoints
{
    public static void MapAdminUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/users")
            .WithTags("Admin - Users")
            .RequireAuthorization(policy => policy.RequireRole(RoleNames.Admin));

        group.MapGet("", async (PlayPredictDbContext db) =>
        {
            var users = await db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .OrderBy(u => u.Email)
                .ToListAsync();

            var dtos = users.Select(u => AuthEndpoints.ToUserDto(u, u.UserRoles.Select(ur => ur.Role.Name)));
            return Results.Ok(dtos);
        });

        group.MapPut("/{id:int}", async (int id, UpdateUserStatusDto dto, PlayPredictDbContext db) =>
        {
            var user = await db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user is null)
            {
                return Results.NotFound();
            }

            user.IsActive = dto.IsActive;
            await db.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name);
            return Results.Ok(AuthEndpoints.ToUserDto(user, roles));
        });
    }
}
