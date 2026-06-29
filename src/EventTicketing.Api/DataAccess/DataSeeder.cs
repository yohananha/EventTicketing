using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.DataAccess;

/// <summary>Idempotent sample-data seeder for local dev and integration tests.</summary>
public static class DataSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Events.AnyAsync(ct)) return; // already seeded

        var now = DateTime.UtcNow;

        // Seat rows are NOT inserted here — they are created lazily on the first hold.
        db.Events.AddRange(
            new Event
            {
                Name = "Coldplay — Live",
                Type = EventType.Concert,
                Venue = "Main Arena",
                Description = "Opening night.",
                StartsAtUtc = now.AddDays(30),
                RowCount = 5,
                SeatsPerRow = 10,
                BasePrice = 250m,
                Status = EventStatus.OnSale,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            },
            new Event
            {
                Name = "Cup Final",
                Type = EventType.Sports,
                Venue = "National Stadium",
                StartsAtUtc = now.AddDays(14),
                RowCount = 3,
                SeatsPerRow = 8,
                BasePrice = 120m,
                Status = EventStatus.OnSale,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            });

        db.Customers.AddRange(
            new Customer { FullName = "Alice Cohen", Email = "alice@example.com", Phone = "+972500000001", CreatedAtUtc = now },
            new Customer { FullName = "Bob Levi", Email = "bob@example.com", Phone = "+972500000002", CreatedAtUtc = now });

        await db.SaveChangesAsync(ct);
    }
}
