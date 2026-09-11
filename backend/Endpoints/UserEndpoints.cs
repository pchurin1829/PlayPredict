using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Security;
using PlayPredict.Api.Services;

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

        group.MapPut("/me/email", async (ChangeEmailDto dto, ClaimsPrincipal principal, PlayPredictDbContext db, JwtTokenService jwt) =>
        {
            var errors = EmailIdentity.Validate(dto.NewEmail, dto.ConfirmEmail, "newEmail");
            if (string.IsNullOrEmpty(dto.CurrentPassword)) errors["currentPassword"] = ["Ingresá tu contraseña actual."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var user = await GetCurrentUserAsync(principal, db, includeRoles: true);
            if (user is null || !user.IsActive) return Results.Unauthorized();
            var hasher = new PasswordHasher<User>();
            var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword);
            if (verification == PasswordVerificationResult.Failed)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["currentPassword"] = ["La contraseña actual es incorrecta."] });

            var email = EmailIdentity.Normalize(dto.NewEmail);
            if (await db.Users.AnyAsync(u => u.Id != user.Id && u.Email.ToLower() == email))
                return Results.ValidationProblem(EmailIdentity.DuplicateError("newEmail"));

            user.Email = email;
            // El email viaja en el JWT: el token anterior queda inválido.
            user.TokenVersion++;
            if (verification == PasswordVerificationResult.SuccessRehashNeeded)
                user.PasswordHash = hasher.HashPassword(user, dto.CurrentPassword);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException error) when (EmailIdentity.IsDuplicate(error))
            {
                return Results.ValidationProblem(EmailIdentity.DuplicateError("newEmail"));
            }
            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            return Results.Ok(new AuthResponseDto(jwt.GenerateToken(user, roles), AuthEndpoints.ToUserDto(user, roles)));
        });

        group.MapPut("/me/password", async (ChangePasswordDto dto, ClaimsPrincipal principal, PlayPredictDbContext db, JwtTokenService jwt) =>
        {
            var errors = new Dictionary<string, string[]>();
            if (string.IsNullOrEmpty(dto.CurrentPassword)) errors["currentPassword"] = ["Ingresá tu contraseña actual."];
            foreach (var (field, messages) in PasswordPolicy.Validate(dto.NewPassword, "newPassword"))
                errors[field] = messages;
            if (dto.NewPassword != dto.ConfirmNewPassword) errors["confirmNewPassword"] = ["La confirmación no coincide con la nueva contraseña."];
            if (errors.Count > 0) return Results.ValidationProblem(errors);

            var user = await GetCurrentUserAsync(principal, db, includeRoles: true);
            if (user is null || !user.IsActive) return Results.Unauthorized();
            var hasher = new PasswordHasher<User>();
            if (hasher.VerifyHashedPassword(user, user.PasswordHash, dto.CurrentPassword) == PasswordVerificationResult.Failed)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["currentPassword"] = ["La contraseña actual es incorrecta."] });
            if (hasher.VerifyHashedPassword(user, user.PasswordHash, dto.NewPassword) != PasswordVerificationResult.Failed)
                return Results.ValidationProblem(new Dictionary<string, string[]> { ["newPassword"] = ["La nueva contraseña debe ser distinta de la actual."] });

            user.PasswordHash = hasher.HashPassword(user, dto.NewPassword);
            user.TokenVersion++;
            user.MustChangePassword = false;
            user.FailedLoginAttempts = 0;
            user.LockoutUntilUtc = null;
            await db.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            return Results.Ok(new AuthResponseDto(jwt.GenerateToken(user, roles), AuthEndpoints.ToUserDto(user, roles)));
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
