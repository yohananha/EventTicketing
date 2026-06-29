namespace EventTicketing.Models.Entities;

/// <summary>One purchased seat for one event. Unique on SeatId so a seat can be sold only once.</summary>
public class OrderItem
{
    public int Id { get; set; }

    public int OrderId { get; set; }
    public Order? Order { get; set; }

    public int EventId { get; set; }
    public Event? Event { get; set; }

    public int SeatId { get; set; }
    public Seat? Seat { get; set; }

    public decimal UnitPrice { get; set; }
}
