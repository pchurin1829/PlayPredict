using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;
using PlayPredict.Api.Domain.Constants;
using PlayPredict.Api.Domain.Entities;
using PlayPredict.Api.Dtos;
using PlayPredict.Api.Services;

namespace PlayPredict.Api.Endpoints;

public static class AuthEndpoints
{
    private const string DefaultCompanyName = "PlayPredict";

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

            var email = dto.Email.Trim().ToLowerInvariant();

            var emailTaken = await db.Users.AnyAsync(u => u.Email == email);
            if (emailTaken)
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["email"] = ["Ya existe un usuario registrado con este email."]
                });
            }

            var company = await db.Companies.FirstOrDefaultAsync(c => c.Name == DefaultCompanyName);
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
            await db.SaveChangesAsync();

            var roles = new[] { RoleNames.Player };
            var token = jwt.GenerateToken(user, roles);

            return Results.Created($"/api/users/me", new AuthResponseDto(token, ToUserDto(user, roles)));
        });

        group.MapPost("/login", async (LoginDto dto, PlayPredictDbContext db, JwtTokenService jwt) =>
        {
            var email = dto.Email.Trim().ToLowerInvariant();

            var user = await db.Users
                .Include(u => u.UserRoles).ThenInclude(ur => ur.Role)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user is null || !user.IsActive)
            {
                return Results.Json(new { message = "Email o contraseña incorrectos." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            var hasher = new PasswordHasher<User>();
            var verification = hasher.VerifyHashedPassword(user, user.PasswordHash, dto.Password);
            if (verification == PasswordVerificationResult.Failed)
            {
                return Results.Json(new { message = "Email o contraseña incorrectos." }, statusCode: StatusCodes.Status401Unauthorized);
            }

            user.LastAccessUtc = DateTime.UtcNow;
            await db.SaveChangesAsync();

            var roles = user.UserRoles.Select(ur => ur.Role.Name).ToList();
            var token = jwt.GenerateToken(user, roles);

            return Results.Ok(new AuthResponseDto(token, ToUserDto(user, roles)));
        });
    }

    private static Dictionary<string, string[]> ValidateRegister(RegisterDto dto)
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

        if (string.IsNullOrWhiteSpace(dto.Email) || !dto.Email.Contains('@'))
        {
            errors["email"] = ["El email es obligatorio y debe ser válido."];
        }

        if (string.IsNullOrWhiteSpace(dto.Password) || dto.Password.Length < 6)
        {
            errors["password"] = ["La contraseña debe tener al menos 6 caracteres."];
        }

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
            roles.ToList());
}
