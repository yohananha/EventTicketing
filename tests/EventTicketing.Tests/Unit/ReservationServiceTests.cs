using EventTicketing.BusinessLogic;
using EventTicketing.BusinessLogic.Exceptions;
using EventTicketing.BusinessLogic.Services;
using EventTicketing.DataAccess;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;
using EventTicketing.Tests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Moq;

namespace EventTicketing.Tests.Unit;

public class ReservationServiceTests
{
    private static readonly DateTime Now = new(2030, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private readonly Mock<ISeatRepository> _seats = new();
    private readonly Mock<IEventRepository> _events = new();
    private readonly Mock<ICustomerRepository> _customers = new();
    private readonly Mock<ISeatStatusHistoryRepository> _history = new();
    private readonly Mock<IUnitOfWork> _uow = new();
    private readonly TestClock _clock = new(Now);

    // Default seat position used across tests (matches the seat created by TestData.Seat).
    private const int EventId = 1;
    private const string Row = "A";
    private const int SeatNum = 1;
    private const int CustomerId = 1;

    private ReservationService CreateSut()
    {
        _customers.Setup(c => c.ExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(1);
        var settings = Options.Create(new HoldSettings { HoldDuration = TimeSpan.FromMinutes(5) });
        return new ReservationService(_seats.Object, _events.Object, _customers.Object, _history.Object, _uow.Object, _clock, settings);
    }

    private void SeatAtPositionReturns(Seat seat) =>
        _seats.Setup(s => s.GetByPositionAsync(EventId, Row, SeatNum)).ReturnsAsync(seat);

    private void NoSeatAtPosition() =>
        _seats.Setup(s => s.GetByPositionAsync(EventId, Row, SeatNum)).ReturnsAsync((Seat?)null);

    private void EventReturns(Event ev) =>
        _events.Setup(e => e.GetByIdAsync(EventId, It.IsAny<bool>())).ReturnsAsync(ev);

    [Fact]
    public async Task Toggle_creates_seat_on_first_hold()
    {
        NoSeatAtPosition();
        EventReturns(TestData.Event(rows: 2, seatsPerRow: 3));
        var sut = CreateSut();

        var result = await sut.ToggleSeatHoldAsync(EventId, Row, SeatNum, CustomerId);

        result.Action.Should().Be("Held");
        result.Status.Should().Be(SeatStatus.InProgress);
        _seats.Verify(s => s.Add(It.Is<Seat>(x => x.RowLabel == Row && x.SeatNumber == SeatNum)), Times.Once);
        _history.Verify(h => h.Add(It.Is<SeatStatusHistory>(x => x.Reason == SeatStatusChangeReason.Reserved && x.OldStatus == null)), Times.Once);
    }

    [Fact]
    public async Task Toggle_available_seat_holds_it()
    {
        var seat = TestData.Seat(status: SeatStatus.Available);
        SeatAtPositionReturns(seat);
        var sut = CreateSut();

        var result = await sut.ToggleSeatHoldAsync(EventId, Row, SeatNum, CustomerId);

        result.Action.Should().Be("Held");
        result.Status.Should().Be(SeatStatus.InProgress);
        seat.HeldByCustomerId.Should().Be(CustomerId);
        seat.HoldExpiresAtUtc.Should().Be(Now.AddMinutes(5));
        _history.Verify(h => h.Add(It.Is<SeatStatusHistory>(x => x.Reason == SeatStatusChangeReason.Reserved)), Times.Once);
        _uow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Toggle_seat_held_by_same_customer_releases_it()
    {
        var seat = TestData.Seat(status: SeatStatus.InProgress, heldBy: CustomerId, holdExpiresAtUtc: Now.AddMinutes(3));
        SeatAtPositionReturns(seat);
        var sut = CreateSut();

        var result = await sut.ToggleSeatHoldAsync(EventId, Row, SeatNum, CustomerId);

        result.Action.Should().Be("Released");
        result.Status.Should().Be(SeatStatus.Available);
        seat.HeldByCustomerId.Should().BeNull();
        _history.Verify(h => h.Add(It.Is<SeatStatusHistory>(x => x.Reason == SeatStatusChangeReason.Released)), Times.Once);
    }

    [Fact]
    public async Task Toggle_seat_held_by_another_customer_throws_conflict()
    {
        var seat = TestData.Seat(status: SeatStatus.InProgress, heldBy: 2, holdExpiresAtUtc: Now.AddMinutes(3));
        SeatAtPositionReturns(seat);
        var sut = CreateSut();

        await sut.Invoking(s => s.ToggleSeatHoldAsync(EventId, Row, SeatNum, CustomerId))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Toggle_occupied_seat_throws_conflict()
    {
        var seat = TestData.Seat(status: SeatStatus.Occupied);
        SeatAtPositionReturns(seat);
        var sut = CreateSut();

        await sut.Invoking(s => s.ToggleSeatHoldAsync(EventId, Row, SeatNum, CustomerId))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Toggle_reuses_an_expired_hold_from_another_customer()
    {
        // Held by customer 2 but the hold already lapsed → customer 1 may take it.
        var seat = TestData.Seat(status: SeatStatus.InProgress, heldBy: 2, holdExpiresAtUtc: Now.AddMinutes(-1));
        SeatAtPositionReturns(seat);
        var sut = CreateSut();

        var result = await sut.ToggleSeatHoldAsync(EventId, Row, SeatNum, CustomerId);

        result.Action.Should().Be("Held");
        seat.HeldByCustomerId.Should().Be(CustomerId);
    }

    [Fact]
    public async Task Toggle_translates_db_concurrency_conflict_to_conflict_exception()
    {
        var seat = TestData.Seat(status: SeatStatus.Available);
        SeatAtPositionReturns(seat);
        _customers.Setup(c => c.ExistsAsync(It.IsAny<int>())).ReturnsAsync(true);
        _uow.Setup(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new DbUpdateConcurrencyException());
        var settings = Options.Create(new HoldSettings());
        var sut = new ReservationService(_seats.Object, _events.Object, _customers.Object, _history.Object, _uow.Object, _clock, settings);

        await sut.Invoking(s => s.ToggleSeatHoldAsync(EventId, Row, SeatNum, CustomerId))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Toggle_invalid_position_throws_validation_exception()
    {
        // Seat A999 doesn't exist in a 2×3 event.
        NoSeatAtPosition();
        EventReturns(TestData.Event(rows: 2, seatsPerRow: 3));
        var sut = CreateSut();

        // Row A, SeatNumber 999 > SeatsPerRow (3)
        await sut.Invoking(s => s.ToggleSeatHoldAsync(EventId, "A", 999, CustomerId))
            .Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Toggle_unknown_customer_throws_not_found()
    {
        _customers.Setup(c => c.ExistsAsync(It.IsAny<int>())).ReturnsAsync(false);
        var settings = Options.Create(new HoldSettings());
        var sut = new ReservationService(_seats.Object, _events.Object, _customers.Object, _history.Object, _uow.Object, _clock, settings);

        await sut.Invoking(s => s.ToggleSeatHoldAsync(EventId, Row, SeatNum, 999))
            .Should().ThrowAsync<NotFoundException>();
    }
}
