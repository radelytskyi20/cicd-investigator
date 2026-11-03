using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CiCdInvestigator.McpServer;

public sealed class SqlConnectivityCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ConnectivityContext>();
        // No domain schema exists yet. Check the server's master database only.
        var connection = database.Database.GetDbConnection();
        var settings = new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(connection.ConnectionString) { InitialCatalog = "master" };
        connection.ConnectionString = settings.ConnectionString;
        return await database.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy("EF Core SQL connection succeeded.")
            : HealthCheckResult.Unhealthy("SQL Server is unavailable.");
    }
}
