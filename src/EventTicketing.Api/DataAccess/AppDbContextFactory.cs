using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EventTicketing.DataAccess;

/// <summary>
/// Design-time factory used by `dotnet ef` (migrations) so the tooling can build the
/// context without booting the API. Not used at runtime.
/// </summary>
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("EVENTTICKETING_CONNECTION")
            ?? "Server=localhost,1434;Database=EventTicketing;User Id=sa;Password=Your_strong_Pass123;TrustServerCertificate=True";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new AppDbContext(options);
    }
}
