using EventTicketing.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventTicketing.DataAccess.Configurations;

public class SeatConfiguration : IEntityTypeConfiguration<Seat>
{
    public void Configure(EntityTypeBuilder<Seat> builder)
    {
        builder.HasKey(s => s.Id);
        builder.Property(s => s.RowLabel).IsRequired().HasMaxLength(10);
        builder.Property(s => s.Status).HasConversion<int>();
        builder.Property(s => s.Price).HasPrecision(18, 2);

        // RowVersion is configured as the SQL Server rowversion concurrency token in
        // AppDbContext (provider-guarded) so integration tests on other providers still work.

        // A physical seat is unique within an event.
        builder.HasIndex(s => new { s.EventId, s.RowLabel, s.SeatNumber }).IsUnique();
        // Fast "available seats for this event" lookups.
        builder.HasIndex(s => new { s.EventId, s.Status });
    }
}
