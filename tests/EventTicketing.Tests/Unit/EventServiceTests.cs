using EventTicketing.BusinessLogic.Services;
using EventTicketing.DataAccess;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;
using EventTicketing.Tests.TestSupport;
using FluentAssertions;
using Moq;

namespace EventTicketing.Tests.Unit;

public class EventServiceTests
{
    [Fact]
    public async Task Create_returns_correct_seat_counts_without_inserting_seat_rows()
    {
        var events = new Mock<IEventRepository>();
        var seats = new Mock<ISeatRepository>();
        var uow = new Mock<IUnitOfWork>();
        var clock = new TestClock(new DateTime(2030, 1, 1, 0, 0, 0, DateTimeKind.Utc));

        Event? captured = null;
        events.Setup(e => e.Add(It.IsAny<Event>())).Callback<Event>(e => captured = e);
        uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);

        var sut = new EventService(events.Object, seats.Object, uow.Object, clock);
        var request = new CreateEventRequest(
            "Gig", EventType.Concert, null, "Hall",
            clock.UtcNow.AddDays(10), null, RowCount: 3, SeatsPerRow: 4, BasePrice: 80m);

        var response = await sut.CreateAsync(request);

        // Seat rows are created lazily on first hold — the event row itself only stores dimensions.
        captured.Should().NotBeNull();
        captured!.Seats.Should().BeEmpty();
        response.TotalSeats.Should().Be(12);     // 3 × 4 derived from dimensions
        response.AvailableSeats.Should().Be(12); // all implicitly available
        seats.Verify(s => s.Add(It.IsAny<Seat>()), Times.Never);
    }
}
