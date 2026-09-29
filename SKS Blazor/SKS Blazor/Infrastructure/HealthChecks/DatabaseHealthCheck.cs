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
            if (!await db.Database.CanConnectAsync(cancellationToken))
                return HealthCheckResult.Unhealthy("Cannot connect to the database.");

            // A wrong or empty file still "connects"; the Users table proves it is Orders.db.
            return await db.Users.AsNoTracking().AnyAsync(cancellationToken)
                ? HealthCheckResult.Healthy("Database connection is healthy.")
                : HealthCheckResult.Unhealthy("Connected, but the Users table is empty.");
        }
        catch (Exception ex)
        {
            return HealthCheckResult.Unhealthy("Database health check threw an exception.", ex);
        }
    }
}
