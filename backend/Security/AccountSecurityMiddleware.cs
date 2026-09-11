using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using PlayPredict.Api.Data;

namespace PlayPredict.Api.Security;

/// <summary>
/// Validación central de sesión (P2.1). Corre después de UseAuthentication.
/// Garantiza, sin esperar al expiry del JWT:
/// - usuario desactivado o eliminado → token deja de funcionar de inmediato;
/// - cambio/reset de contraseña o de email → tokens anteriores quedan inválidos;
/// - contraseña temporal (MustChangePassword) → solo permite identidad mínima
///   y cambio de contraseña. La restricción vive en backend, no solo en frontend.
/// Acepta el lookup por request autenticada (PK indexado); sin caché en v1.0.
/// </summary>
public static class AccountSecurityMiddleware
{
    public const string TokenVersionClaim = "pp_tv";
    public const string MustChangePasswordClaim = "pp_mcp";

    // Rutas permitidas con contraseña temporal pendiente de cambio.
    private static readonly (string Method, string Path)[] MustChangeAllowed =
    [
        ("GET", "/api/users/me"),
        ("PUT", "/api/users/me/password"),
    ];

    public static IApplicationBuilder UseAccountSecurity(this IApplicationBuilder app) =>
        app.Use(async (context, next) =>
        {
            if (context.User.Identity?.IsAuthenticated != true)
            {
                await next();
                return;
            }

            var idClaim = context.User.FindFirstValue(ClaimTypes.NameIdentifier);
            var versionClaim = context.User.FindFirst(TokenVersionClaim);
            if (idClaim is null || !int.TryParse(idClaim, out var userId) ||
                versionClaim is null || !int.TryParse(versionClaim.Value, out var tokenVersion))
            {
                await Reject(context);
                return;
            }

            var db = context.RequestServices.GetRequiredService<PlayPredictDbContext>();
            var user = await db.Users.AsNoTracking().FirstOrDefaultAsync(u => u.Id == userId);
            if (user is null || !user.IsActive || user.TokenVersion != tokenVersion)
            {
                await Reject(context);
                return;
            }

            if (user.MustChangePassword && !IsMustChangeAllowed(context.Request))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new
                {
                    message = "Debés cambiar tu contraseña temporal antes de continuar."
                });
                return;
            }

            await next();
        });

    private static bool IsMustChangeAllowed(HttpRequest request) =>
        MustChangeAllowed.Any(allowed =>
            string.Equals(request.Method, allowed.Method, StringComparison.OrdinalIgnoreCase) &&
            string.Equals(request.Path.Value, allowed.Path, StringComparison.OrdinalIgnoreCase));

    private static async Task Reject(HttpContext context)
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        await context.Response.WriteAsJsonAsync(new { message = "Sesión inválida. Volvé a ingresar." });
    }
}
