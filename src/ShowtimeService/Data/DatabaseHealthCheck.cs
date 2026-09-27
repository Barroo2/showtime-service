using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace ShowtimeService.Data;

public class DatabaseHealthCheck(ShowtimeDbContext db) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await db.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("Datenbank nicht erreichbar");
    }
}
