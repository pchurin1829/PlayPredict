using System.Security.Cryptography;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
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
            // Revocar tokens vigentes: los anteriores no deben revivir al reactivar.
            user.TokenVersion++;
            await db.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name);
            return Results.Ok(AuthEndpoints.ToUserDto(user, roles));
        });

        group.MapPost("/{id:int}/reset-password", async (int id, PlayPredictDbContext db) =>
        {
            var user = await db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Id == id);

            if (user is null)
            {
                return Results.NotFound();
            }

            // Temporal aleatoria 16 caracteres (alfabeto 62^16 ≈ 95 bits).
            // Solo se persiste el hash; el plaintext se devuelve UNA vez y no se loguea.
            const string alphabet = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            var temporary = string.Concat(Enumerable.Range(0, 16).Select(_ => alphabet[RandomNumberGenerator.GetInt32(alphabet.Length)]));

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, temporary);
            user.MustChangePassword = true;
            user.TokenVersion++;
            user.FailedLoginAttempts = 0;
            user.LockoutUntilUtc = null;
            await db.SaveChangesAsync();

            return Results.Ok(new { userId = user.Id, email = user.Email, temporaryPassword = temporary });
        });
    }
}
