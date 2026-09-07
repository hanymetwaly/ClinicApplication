using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ClinicApp.Infrastructure.Data;

/// <summary>
/// Design-time factory used by EF Core tooling (migrations, scaffolding) to create a <see cref="ClinicDbContext"/>.
/// Reads the connection string from environment variables first, then falls back to sensible development defaults.
/// </summary>
public class ClinicDbContextFactory : IDesignTimeDbContextFactory<ClinicDbContext>
{
    /// <summary>
    /// Creates a new <see cref="ClinicDbContext"/> using the configured connection string.
    /// </summary>
    /// <param name="args">Command-line arguments passed by EF Core tooling.</param>
    /// <returns>An instance of <see cref="ClinicDbContext"/>.</returns>
    public ClinicDbContext CreateDbContext(string[] args)
    {
        var connectionString = BuildConnectionString();
        var options = new DbContextOptionsBuilder<ClinicDbContext>()
            .UseSqlServer(connectionString)
            .Options;
        return new ClinicDbContext(options);
    }

    /// <summary>
    /// Builds a SQL Server connection string from environment variables.
    /// Priority: <c>ConnectionStrings__DefaultConnection</c>, then <c>DB_SERVER</c>, <c>DB_DATABASE</c>, <c>DB_USER</c>, <c>DB_PASSWORD</c>.
    /// </summary>
    /// <returns>A SQL Server connection string.</returns>
    private static string BuildConnectionString()
    {
        var fullConnectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (!string.IsNullOrWhiteSpace(fullConnectionString))
        {
            return fullConnectionString;
        }

        var server = Environment.GetEnvironmentVariable("DB_SERVER") ?? "localhost,1433";
        var database = Environment.GetEnvironmentVariable("DB_DATABASE") ?? "ClinicApp";
        var user = Environment.GetEnvironmentVariable("DB_USER") ?? "sa";
        var password = Environment.GetEnvironmentVariable("DB_PASSWORD") ?? "ClinicApp!2026";

        return $"Server={server};Database={database};User Id={user};Password={password};TrustServerCertificate=True;Encrypt=True";
    }
}
