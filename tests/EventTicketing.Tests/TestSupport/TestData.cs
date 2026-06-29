using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;

namespace EventTicketing.Tests.TestSupport;

/// <summary>Object-mother helpers that build valid entities with sensible defaults for tests.</summary>
public static class TestData
{
    public static Event Event(int rows = 2, int seatsPerRow = 3, decimal basePrice = 100m) => new()
    {
        Name = "Test Event",
        Type = EventType.Concert,
        Venue = "Test Venue",
        StartsAtUtc = new DateTime(2030, 1, 1, 20, 0, 0, DateTimeKind.Utc),
        RowCount = rows,
        SeatsPerRow = seatsPerRow,
        BasePrice = basePrice,
        Status = EventStatus.OnSale
    };

    public static Customer Customer(string email = "test@example.com") => new()
    {
        FullName = "Test Customer",
        Email = email,
        Phone = "+10000000000"
    };

    public static Seat Seat(
        int id = 1,
        int eventId = 1,
        SeatStatus status = SeatStatus.Available,
        int? heldBy = null,
        DateTime? holdExpiresAtUtc = null,
        decimal price = 100m) => new()
    {
        Id = id,
        EventId = eventId,
        RowLabel = "A",
        SeatNumber = id,
        Status = status,
        HeldByCustomerId = heldBy,
        HoldExpiresAtUtc = holdExpiresAtUtc,
        Price = price
    };
}
