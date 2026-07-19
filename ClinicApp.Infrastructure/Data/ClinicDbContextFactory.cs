using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClinicApp.Infrastructure.Data;

public class ClinicDbContextFactory : IDesignTimeDbContextFactory<ClinicDbContext>
{
    public ClinicDbContext CreateDbContext(string[] args)
    {
        var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=localhost,1433;Database=ClinicApp;User Id=sa;Password=ClinicApp!2026;TrustServerCertificate=True;Encrypt=True";
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ClinicDbContext(options);
    }
}
