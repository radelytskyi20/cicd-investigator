using Microsoft.EntityFrameworkCore;

namespace CiCdInvestigator.McpServer;

public sealed class ConnectivityContext(DbContextOptions<ConnectivityContext> options) : DbContext(options);
