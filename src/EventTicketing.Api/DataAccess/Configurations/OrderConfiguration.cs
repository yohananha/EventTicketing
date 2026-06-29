using EventTicketing.Models.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace EventTicketing.DataAccess.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);
        builder.Property(o => o.Status).HasConversion<int>();
        builder.Property(o => o.TotalAmount).HasPrecision(18, 2);

        builder.HasMany(o => o.Items)
            .WithOne(i => i.Order!)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.HasKey(i => i.Id);
        builder.Property(i => i.UnitPrice).HasPrecision(18, 2);

        // DB-level guarantee that a seat can be sold at most once.
        builder.HasIndex(i => i.SeatId).IsUnique();

        builder.HasOne(i => i.Event)
            .WithMany()
            .HasForeignKey(i => i.EventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(i => i.Seat)
            .WithMany()
            .HasForeignKey(i => i.SeatId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
