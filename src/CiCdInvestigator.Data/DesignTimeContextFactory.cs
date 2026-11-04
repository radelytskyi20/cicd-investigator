using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace CiCdInvestigator.Data;

public sealed class DesignTimeContextFactory : IDesignTimeDbContextFactory<InvestigatorContext>
{
    public InvestigatorContext CreateDbContext(string[] args) => new(
        new DbContextOptionsBuilder<InvestigatorContext>()
            .UseSqlServer("Server=localhost;Database=investigator-prototype;Integrated Security=true;TrustServerCertificate=true")
            .Options);
}
