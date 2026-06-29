using EventTicketing.Models.Enums;

namespace EventTicketing.Models.Entities;

public class Event
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public EventType Type { get; set; }
    public string? Description { get; set; }
    public string Venue { get; set; } = string.Empty;

    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }

    // Seat-map dimensions; seats are generated from these when the event is created.
    public int RowCount { get; set; }
    public int SeatsPerRow { get; set; }

    public decimal BasePrice { get; set; }
    public EventStatus Status { get; set; } = EventStatus.Scheduled;

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<Seat> Seats { get; set; } = new List<Seat>();
}
