using EventTicketing.Models.Enums;

namespace EventTicketing.Models.Entities;

public class Seat
{
    public int Id { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    public string RowLabel { get; set; } = string.Empty; // e.g. "A"
    public int SeatNumber { get; set; }

    public SeatStatus Status { get; set; } = SeatStatus.Available;
    public decimal Price { get; set; }

    // Hold metadata — only meaningful while Status == InProgress.
    public DateTime? HoldExpiresAtUtc { get; set; }
    public int? HeldByCustomerId { get; set; }

    public DateTime LastUpdatedUtc { get; set; }

    // Optimistic-concurrency token: SQL Server stamps this on every UPDATE, so two
    // racing transactions on the same seat cannot both win — the loser gets a conflict.
    public byte[] RowVersion { get; set; } = Array.Empty<byte>();
}
