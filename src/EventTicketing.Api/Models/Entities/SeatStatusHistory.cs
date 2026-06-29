using EventTicketing.Models.Enums;

namespace EventTicketing.Models.Entities;

/// <summary>Append-only audit of every seat status change, kept for future reporting.</summary>
public class SeatStatusHistory
{
    public long Id { get; set; }

    public int SeatId { get; set; }
    public Seat? Seat { get; set; }

    public int EventId { get; set; } // denormalized so event-level reports avoid a join

    public SeatStatus? OldStatus { get; set; }
    public SeatStatus NewStatus { get; set; }
    public SeatStatusChangeReason Reason { get; set; }

    public DateTime ChangedAtUtc { get; set; }
    public int? ChangedByCustomerId { get; set; }
    public int? OrderId { get; set; }
}
