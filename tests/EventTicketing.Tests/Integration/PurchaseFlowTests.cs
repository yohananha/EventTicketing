using EventTicketing.BusinessLogic.Exceptions;
using EventTicketing.DataAccess;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Enums;
using EventTicketing.Tests.TestSupport;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.Tests.Integration;

public class PurchaseFlowTests
{
    [Fact]
    public async Task Create_event_exposes_full_virtual_seat_map_without_db_rows()
    {
        using var harness = new SqliteHarness();
        var events = harness.Events(harness.Db);

        var request = new CreateEventRequest(
            "Show", EventType.Theater, null, "Hall",
            harness.Clock.UtcNow.AddDays(5), null, RowCount: 4, SeatsPerRow: 6, BasePrice: 50m);
        var created = await events.CreateAsync(request);

        // No seat rows in DB yet — they are created lazily.
        var seatCount = await harness.Db.Seats.CountAsync(s => s.EventId == created.Id);
        seatCount.Should().Be(0);

        // But the response counts are derived from event dimensions.
        created.TotalSeats.Should().Be(24);
        created.AvailableSeats.Should().Be(24);
    }

    [Fact]
    public async Task Hold_then_purchase_marks_seat_sold_and_records_history()
    {
        using var harness = new SqliteHarness();

        // Arrange: event + customer (no seats pre-created).
        var ev = TestData.Event(rows: 1, seatsPerRow: 2, basePrice: 75m);
        var customer = TestData.Customer();
        harness.Db.Events.Add(ev);
        harness.Db.Customers.Add(customer);
        await harness.Db.SaveChangesAsync();

        // Act: hold seat A1 via toggle (creates the row), then purchase it.
        await harness.Reservations(harness.NewContext()).ToggleSeatHoldAsync(ev.Id, "A", 1, customer.Id);

        using var verify1 = harness.NewContext();
        var held = await verify1.Seats.FirstAsync(s => s.EventId == ev.Id && s.RowLabel == "A" && s.SeatNumber == 1);
        held.Status.Should().Be(SeatStatus.InProgress);

        var order = await harness.Orders(harness.NewContext()).PurchaseAsync(customer.Id, new[] { held.Id });

        // Assert persisted state.
        using var verify = harness.NewContext();
        var seat = await verify.Seats.FirstAsync(s => s.Id == held.Id);
        seat.Status.Should().Be(SeatStatus.Occupied);

        var savedOrder = await verify.Orders.Include(o => o.Items).FirstAsync(o => o.Id == order.Id);
        savedOrder.Status.Should().Be(OrderStatus.Paid);
        savedOrder.TotalAmount.Should().Be(75m);
        savedOrder.Items.Should().ContainSingle(i => i.SeatId == held.Id);

        var purchased = await verify.SeatStatusHistory
            .FirstAsync(h => h.SeatId == held.Id && h.Reason == SeatStatusChangeReason.Purchased);
        purchased.OrderId.Should().Be(order.Id);
    }

    [Fact]
    public async Task First_toggle_creates_seat_row_with_InProgress_status()
    {
        using var harness = new SqliteHarness();
        var ev = TestData.Event(rows: 1, seatsPerRow: 3);
        var customer = TestData.Customer();
        harness.Db.Events.Add(ev);
        harness.Db.Customers.Add(customer);
        await harness.Db.SaveChangesAsync();

        await harness.Reservations(harness.NewContext()).ToggleSeatHoldAsync(ev.Id, "A", 2, customer.Id);

        using var verify = harness.NewContext();
        var seat = await verify.Seats.SingleAsync(s => s.EventId == ev.Id);
        seat.RowLabel.Should().Be("A");
        seat.SeatNumber.Should().Be(2);
        seat.Status.Should().Be(SeatStatus.InProgress);
        seat.HeldByCustomerId.Should().Be(customer.Id);
        // History row created in the same SaveChanges as the seat insert.
        var history = await verify.SeatStatusHistory.SingleAsync(h => h.SeatId == seat.Id);
        history.Reason.Should().Be(SeatStatusChangeReason.Reserved);
        history.OldStatus.Should().BeNull();
    }

    [Fact]
    public async Task Purchase_without_an_active_hold_throws_conflict()
    {
        using var harness = new SqliteHarness();
        var ev = TestData.Event(rows: 1, seatsPerRow: 1);
        var customer = TestData.Customer();
        harness.Db.Events.Add(ev);
        harness.Db.Customers.Add(customer);
        await harness.Db.SaveChangesAsync();

        // Hold the seat to create the row, then release it — it's now Available in DB.
        await harness.Reservations(harness.NewContext()).ToggleSeatHoldAsync(ev.Id, "A", 1, customer.Id);
        var seatId = (await harness.Db.Seats.FirstAsync(s => s.EventId == ev.Id)).Id;
        await harness.Reservations(harness.NewContext()).ToggleSeatHoldAsync(ev.Id, "A", 1, customer.Id);

        var orders = harness.Orders(harness.NewContext());
        await orders.Invoking(o => o.PurchaseAsync(customer.Id, new[] { seatId }))
            .Should().ThrowAsync<ConflictException>();
    }

    [Fact]
    public async Task Duplicate_email_violates_unique_index()
    {
        using var harness = new SqliteHarness();
        harness.Db.Customers.Add(TestData.Customer("same@example.com"));
        await harness.Db.SaveChangesAsync();

        using var ctx = harness.NewContext();
        ctx.Customers.Add(TestData.Customer("same@example.com"));
        await ctx.Invoking(c => c.SaveChangesAsync())
            .Should().ThrowAsync<DbUpdateException>();
    }
}
