using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Security;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class AuthEndpoints
{
    private const string GenericLoginError = "Email o contraseña incorrectos.";

    // Umbral de bloqueo por cuenta (P2.1): 10 fallos consecutivos → 15 minutos.
    private const int MaxFailedAttempts = 10;
    private static readonly TimeSpan LockoutDuration = TimeSpan.FromMinutes(15);

    // Hash sintético precalculado UNA vez por proceso para igualar timing
    // cuando el email no existe (anti-enumeración). Nunca se genera por request.
    private static readonly Lazy<string> DummyHash = new(() =>
        new PasswordHasher<User>().HashPassword(new User { Email = "nonexistent@invalid.local" }, Guid.NewGuid().ToString()));

    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        group.MapPost("/register", async (RegisterDto dto, PlayPredictDbContext db, JwtTokenService jwt) =>
        {
            var errors = ValidateRegister(dto);
            if (errors.Count > 0)
            {
                return Results.ValidationProblem(errors);
            }
            var email = EmailIdentity.Normalize(dto.Email);

            var emailTaken = await db.Users.AnyAsync(u => u.Email.ToLower() == email);
            if (emailTaken)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = ["Ya existe un usuario registrado con este email."]
                });
            }

            var company = await db.Companies.OrderBy(c => c.Id).FirstOrDefaultAsync(c => c.IsActive);
            if (company is null)
            {
                return Results.Problem("No hay una empresa configurada.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var userRole = await db.Roles.FirstOrDefaultAsync(r => r.Name == RoleNames.Player);
            if (userRole is null)
            {
                return Results.Problem("No se encontró el rol PLAYER.", statusCode: StatusCodes.Status500InternalServerError);
            }

            var user = new User
            {
                CompanyId = company.Id,
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Email = email,
                IsActive = true,
                CreatedAtUtc = DateTime.UtcNow
            };

            var hasher = new PasswordHasher<User>();
            user.PasswordHash = hasher.HashPassword(user, dto.Password);
            user.UserRoles.Add(new UserRole { Role = userRole });

            db.Users.Add(user);
            try
            {
                await db.SaveChangesAsync();
            }
            catch (DbUpdateException error) when (EmailIdentity.IsDuplicate(error))
            {
                return Results.ValidationProblem(EmailIdentity.DuplicateError());
            }

            var roles = new[] { RoleNames.Player };
            var token = jwt.GenerateToken(user, roles);

            return Results.Created($"/api/users/me", new AuthResponseDto(token, ToUserDto(user, roles), user.MustChangePassword));
        }).RequireRateLimiting(AuthRateLimiting.RegisterPolicy);

        group.MapPost("/login", async (LoginDto dto, PlayPredictDbContext db, JwtTokenService jwt) =>
        {
            var email = EmailIdentity.Normalize(dto.Email);
            if (!EmailIdentity.IsValid(email) || string.IsNullOrEmpty(dto.Password))
                return InvalidLogin();

            var user = await db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email.ToLower() == email);

            var hasher = new PasswordHasher<User>();
            if (user is null)
            {
                // Timing equivalente sin revelar si el email existe.
                hasher.VerifyHashedPassword(new User(), DummyHash.Value, dto.Password);
                return InvalidLogin();
            }

            if (user.LockoutUntilUtc.HasValue && user.LockoutUntilUtc.Value > DateTime.UtcNow)
                return InvalidLogin();

            var verification = PasswordVerificationResult.Failed;
            if (user.IsActive)
                verification = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            else
                // Inactivo: mismo costo PBKDF2, mismo mensaje. No filtrar estado.
                hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);

            if (!user.IsActive || verification == PasswordVerificationResult.Failed)
            {
                user.FailedLoginAttempts++;
                if (user.FailedLoginAttempts >= MaxFailedAttempts)
                    user.LockoutUntilUtc = DateTime.UtcNow.Add(LockoutDuration);
                await db.SaveChangesAsync();
                return InvalidLogin();
            }

            user.FailedLoginAttempts = 0;
            user.LockoutUntilUtc = null;
            user.LastAccessUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var token = jwt.GenerateToken(user, roles);

            return Results.Ok(new AuthResponseDto(token, ToUserDto(user, roles), user.MustChangePassword));
        }).RequireRateLimiting(AuthRateLimiting.LoginPolicy);
    }

    private static IResult InvalidLogin() =>
        Results.Json(new { message = GenericLoginError }, statusCode: StatusCodes.Status401Unauthorized);

    private static Dictionary<string, string[]> ValidateRegister(RegisterDto dto)
    {
        var errors = EmailIdentity.Validate(dto.Email, dto.ConfirmEmail);

        if (string.IsNullOrWhiteSpace(dto.FirstName))
        {
            errors["firstName"] = ["El nombre es obligatorio."];
        }

        if (string.IsNullOrWhiteSpace(dto.LastName))
        {
            errors["lastName"] = ["El apellido es obligatorio."];
        }

        foreach (var (field, messages) in PasswordPolicy.Validate(dto.Password))
            errors[field] = messages;

        return errors;
    }

    internal static UserDto ToUserDto(User user, IEnumerable<string> roles) =>
        new(
            user.Id,
            user.CompanyId,
            user.FirstName,
            user.LastName,
            user.Email,
            user.IsActive,
            user.CreatedAtUtc,
            user.LastAccessUtc,
            roles.ToList(),
            user.MustChangePassword);
}
