using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SKS_Blazor.Infrastructure.Data;

namespace SKS_Blazor.Infrastructure.HealthChecks;

/// <summary>
/// Health check that verifies connectivity to the Upgraded database via the DbContext factory.
/// Resolves IDbContextFactory&lt;UpgradedDbContext&gt; rather than the bare context type, since
/// the context is registered exclusively via AddDbContextFactory.
/// </summary>
public class DatabaseHealthCheck : IHealthCheck
{
    private readonly IDbContextFactory<UpgradedDbContext> _dbFactory;

    public DatabaseHealthCheck(IDbContextFactory<UpgradedDbContext> dbFactory)
    {
        _dbFactory = dbFactory;
    }

    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(cancellationToken);
            var canConnect = await db.Database.CanConnectAsync(cancellationToken);
            return canConnect
                ? HealthCheckResult.Healthy("Database connection is healthy.")
                : HealthCheckResult.Unhealthy("Cannot connect to the database.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check threw an exception.", ex);
        }
    }
}
