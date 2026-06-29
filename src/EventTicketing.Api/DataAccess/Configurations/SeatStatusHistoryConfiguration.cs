using EventTicketing.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventTicketing.DataAccess.Configurations;

public class SeatStatusHistoryConfiguration : IEntityTypeConfiguration<SeatStatusHistory>
{
    public void Configure(EntityTypeBuilder<SeatStatusHistory> builder)
    {
        builder.HasKey(h => h.Id);
        builder.Property(h => h.OldStatus).HasConversion<int>();
        builder.Property(h => h.NewStatus).HasConversion<int>();
        builder.Property(h => h.Reason).HasConversion<int>();

        // Report-oriented indexes: per-seat timeline and per-event timeline.
        builder.HasIndex(h => new { h.SeatId, h.ChangedAtUtc });
        builder.HasIndex(h => new { h.EventId, h.ChangedAtUtc });

        builder.HasOne(h => h.Seat)
            .WithMany()
            .HasForeignKey(h => h.SeatId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
