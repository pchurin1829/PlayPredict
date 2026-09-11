using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace PlayPredict.Api.Security;

/// <summary>
/// Rate limiting mínimo para endpoints de autenticación (P2.1).
/// PBKDF2 es CPU-bound: sin throttling, /login es un vector de DoS.
/// Solo se limita auth; el tráfico normal del PLAYER no lleva limitador.
/// La partición es por IP remota (tras ForwardedHeaders, es la IP del proxy directo).
/// El middleware responde 429 con Retry-After automáticamente.
/// </summary>
public static class AuthRateLimiting
{
    public const string LoginPolicy = "auth-login";
    public const string RegisterPolicy = "auth-register";

    public static IServiceCollection AddAuthRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            options.OnRejected = async (context, cancellationToken) =>
            {
                if (!context.HttpContext.Response.Headers.ContainsKey("Retry-After"))
                {
                    if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
                        context.HttpContext.Response.Headers.RetryAfter = ((int)retryAfter.TotalSeconds).ToString();
                    else
                        // Ventana fija de 1 minuto (login): el cliente puede reintentar entonces.
                        context.HttpContext.Response.Headers.RetryAfter = "60";
                }
                await ValueTask.CompletedTask;
            };
            options.AddPolicy(LoginPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 5,
                        Window = TimeSpan.FromMinutes(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
            options.AddPolicy(RegisterPolicy, context =>
                RateLimitPartition.GetFixedWindowLimiter(
                    partitionKey: ClientIp(context),
                    factory: _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = 10,
                        Window = TimeSpan.FromHours(1),
                        QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                        QueueLimit = 0
                    }));
        });
        return services;
    }

    public static string ClientIp(HttpContext context) =>
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
}
