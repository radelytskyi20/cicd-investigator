using CiCdInvestigator.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace CiCdInvestigator.McpServer;

public sealed class SqlConnectivityCheck(IServiceScopeFactory scopeFactory) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<InvestigatorContext>();
        return await database.Runs.AnyAsync(x => x.Id == "run-102", cancellationToken)
            ? HealthCheckResult.Healthy("Initial dataset is ready.")
            : HealthCheckResult.Unhealthy("Initial dataset is missing.");
    }
}
