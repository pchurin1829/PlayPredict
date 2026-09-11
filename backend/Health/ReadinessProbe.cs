using PlayPredict.Api.Data;

namespace PlayPredict.Api.Health;

/// <summary>
/// Readiness mínima (P2.2): proceso vivo + PostgreSQL alcanzable.
/// Operación mínima (apertura de conexión), sin queries pesadas y sin
/// exigir estado de migraciones (el arranque ya aplica MigrateAsync).
/// </summary>
public static class ReadinessProbe
{
    public static Task<bool> IsReadyAsync(PlayPredictDbContext db) =>
        db.Database.CanConnectAsync();
}
