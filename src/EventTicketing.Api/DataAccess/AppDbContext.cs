using EventTicketing.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.DataAccess;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<Event> Events => Set<Event>();
    public DbSet<Seat> Seats => Set<Seat>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<SeatStatusHistory> SeatStatusHistory => Set<SeatStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Picks up every IEntityTypeConfiguration in this assembly (one file per entity).
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // The seat concurrency token is a native SQL Server rowversion. Other providers
        // (e.g. SQLite used in integration tests) have no equivalent, so enable it only for
        // SQL Server; elsewhere RowVersion stays a plain column with no concurrency check.
        if (Database.IsSqlServer())
            modelBuilder.Entity<Seat>().Property(s => s.RowVersion).IsRowVersion();
    }
}
