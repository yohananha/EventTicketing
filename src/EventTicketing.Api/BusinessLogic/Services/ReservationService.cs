using EventTicketing.BusinessLogic.Exceptions;
using EventTicketing.BusinessLogic.Mapping;
using EventTicketing.DataAccess;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Models;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace EventTicketing.BusinessLogic.Services;

public class ReservationService : IReservationService
{
    private readonly ISeatRepository _seats;
    private readonly IEventRepository _events;
    private readonly ICustomerRepository _customers;
    private readonly ISeatStatusHistoryRepository _history;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;
    private readonly HoldSettings _settings;

    public ReservationService(
        ISeatRepository seats,
        IEventRepository events,
        ICustomerRepository customers,
        ISeatStatusHistoryRepository history,
        IUnitOfWork uow,
        IClock clock,
        IOptions<HoldSettings> settings)
    {
        _seats = seats;
        _events = events;
        _customers = customers;
        _history = history;
        _uow = uow;
        _clock = clock;
        _settings = settings.Value;
    }

    public async Task<ToggleSeatResponse> ToggleSeatHoldAsync(int eventId, string rowLabel, int seatNumber, int customerId)
    {
        await EnsureCustomerExists(customerId);
        var now = _clock.UtcNow;
        var label = rowLabel.ToUpperInvariant();

        var seat = await _seats.GetByPositionAsync(eventId, label, seatNumber);

        string action;
        if (seat is null)
        {
            // First time this seat is touched — validate it is within the event's seating plan then create it.
            var ev = await _events.GetByIdAsync(eventId)
                     ?? throw new NotFoundException($"Event {eventId} not found.");
            ValidateSeatPosition(ev, label, seatNumber);

            seat = new Seat
            {
                EventId = ev.Id,
                RowLabel = label,
                SeatNumber = seatNumber,
                Price = ev.BasePrice,
                Status = SeatStatus.InProgress,
                HeldByCustomerId = customerId,
                HoldExpiresAtUtc = now.Add(_settings.HoldDuration),
                LastUpdatedUtc = now
            };
            _seats.Add(seat);
            // Use Seat navigation so EF resolves SeatId after the INSERT (single SaveChanges).
            AddHistory(seat, oldStatus: null, SeatStatusChangeReason.Reserved, customerId);
            action = "Held";
        }
        else if (SeatRules.IsHeldBy(seat, customerId, now))
        {
            Release(seat, customerId, now);
            action = "Released";
        }
        else if (SeatRules.IsEffectivelyAvailable(seat, now))
        {
            Hold(seat, customerId, now);
            action = "Held";
        }
        else if (seat.Status == SeatStatus.Occupied)
        {
            throw new ConflictException($"Seat {label}{seatNumber} in event {eventId} is already sold.");
        }
        else
        {
            throw new ConflictException($"Seat {label}{seatNumber} in event {eventId} is currently held by another customer.");
        }

        await SaveExpectingNoConflict($"{label}{seatNumber}");
        return new ToggleSeatResponse(seat.Id, seat.Status, action, seat.HoldExpiresAtUtc);
    }

    public async Task<HoldResultResponse> HoldSeatsAsync(int customerId, IReadOnlyList<int> seatIds)
    {
        await EnsureCustomerExists(customerId);
        var seats = await _seats.GetByIdsAsync(seatIds);
        if (seats.Count != seatIds.Distinct().Count())
            throw new NotFoundException("One or more seats were not found.");

        var now = _clock.UtcNow;
        foreach (var seat in seats)
        {
            // Re-holding a seat you already hold is allowed (extends the hold); otherwise it must be free.
            if (SeatRules.IsHeldBy(seat, customerId, now) || SeatRules.IsEffectivelyAvailable(seat, now))
                Hold(seat, customerId, now);
            else if (seat.Status == SeatStatus.Occupied)
                throw new ConflictException($"Seat {seat.Id} is already sold.");
            else
                throw new ConflictException($"Seat {seat.Id} is currently held by another customer.");
        }

        await SaveExpectingNoConflict(string.Join(",", seatIds));
        var expiresAt = now.Add(_settings.HoldDuration);
        return new HoldResultResponse(seats.Select(s => s.ToResponse()).ToList(), expiresAt);
    }

    public async Task ReleaseHoldAsync(int seatId, int customerId)
    {
        var seat = await _seats.GetByIdAsync(seatId)
                   ?? throw new NotFoundException($"Seat {seatId} not found.");
        var now = _clock.UtcNow;
        if (!SeatRules.IsHeldBy(seat, customerId, now))
            throw new ConflictException($"Seat {seatId} is not currently held by customer {customerId}.");

        Release(seat, customerId, now);
        await SaveExpectingNoConflict(seatId.ToString());
    }

    public async Task<int> ReleaseExpiredHoldsAsync()
    {
        var now = _clock.UtcNow;
        var expired = await _seats.GetExpiredHoldsAsync(now);
        if (expired.Count == 0) return 0;

        foreach (var seat in expired)
            Release(seat, seat.HeldByCustomerId, now, SeatStatusChangeReason.HoldExpired);

        try
        {
            await _uow.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            // A seat changed under us (e.g. a customer just purchased it). The next sweep
            // will reconcile whatever is left; best-effort cleanup is fine here.
            return 0;
        }
        return expired.Count;
    }

    // --- helpers -------------------------------------------------------------

    private void Hold(Seat seat, int customerId, DateTime now)
    {
        var old = seat.Status;
        seat.Status = SeatStatus.InProgress;
        seat.HeldByCustomerId = customerId;
        seat.HoldExpiresAtUtc = now.Add(_settings.HoldDuration);
        seat.LastUpdatedUtc = now;
        AddHistory(seat, old, SeatStatusChangeReason.Reserved, customerId);
    }

    private void Release(Seat seat, int? customerId, DateTime now,
        SeatStatusChangeReason reason = SeatStatusChangeReason.Released)
    {
        var old = seat.Status;
        seat.Status = SeatStatus.Available;
        seat.HeldByCustomerId = null;
        seat.HoldExpiresAtUtc = null;
        seat.LastUpdatedUtc = now;
        AddHistory(seat, old, reason, customerId);
    }

    private static void ValidateSeatPosition(Event ev, string rowLabel, int seatNumber)
    {
        var rowIndex = SeatLayout.ToRowIndex(rowLabel);
        if (rowIndex < 0 || rowIndex >= ev.RowCount || seatNumber < 1 || seatNumber > ev.SeatsPerRow)
            throw new ValidationException($"Seat {rowLabel}{seatNumber} is outside this event's seating plan.");
    }

    // SeatStatusHistory.Seat navigation lets EF resolve SeatId after an INSERT in the same SaveChanges.
    private void AddHistory(Seat seat, SeatStatus? oldStatus, SeatStatusChangeReason reason, int? customerId)
    {
        _history.Add(new SeatStatusHistory
        {
            Seat = seat,
            EventId = seat.EventId,
            OldStatus = oldStatus,
            NewStatus = seat.Status,
            Reason = reason,
            ChangedAtUtc = seat.LastUpdatedUtc,
            ChangedByCustomerId = customerId
        });
    }

    private async Task EnsureCustomerExists(int customerId)
    {
        if (!await _customers.ExistsAsync(customerId))
            throw new NotFoundException($"Customer {customerId} not found.");
    }

    /// <summary>Commit; a lost concurrency race means another request grabbed the seat first → 409.</summary>
    private async Task SaveExpectingNoConflict(string seatContext)
    {
        try
        {
            await _uow.SaveChangesAsync();
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException($"Seat(s) {seatContext} changed concurrently. Please retry.");
        }
        catch (DbUpdateException)
        {
            // Two concurrent first-holds on the same seat hit the unique index — one wins, one retries.
            throw new ConflictException($"Seat(s) {seatContext} changed concurrently. Please retry.");
        }
    }
}
