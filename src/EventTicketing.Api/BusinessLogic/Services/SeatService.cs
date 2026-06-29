using EventTicketing.BusinessLogic.Exceptions;
using EventTicketing.BusinessLogic.Mapping;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Models;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Enums;

namespace EventTicketing.BusinessLogic.Services;

public class SeatService : ISeatService
{
    private readonly ISeatRepository _seats;
    private readonly IEventRepository _events;
    private readonly IClock _clock;

    public SeatService(ISeatRepository seats, IEventRepository events, IClock clock)
    {
        _seats = seats;
        _events = events;
        _clock = clock;
    }

    /// <summary>Returns the full virtual seat grid. Positions never held are shown as Available with Id 0.</summary>
    public async Task<IReadOnlyList<SeatResponse>> GetByEventAsync(int eventId, SeatStatus? status)
    {
        var ev = await _events.GetByIdAsync(eventId)
                 ?? throw new NotFoundException($"Event {eventId} not found.");

        var existing = (await _seats.GetByEventAsync(eventId))
            .ToDictionary(s => (s.RowLabel, s.SeatNumber));

        var result = new List<SeatResponse>();
        foreach (var (rowLabel, seatNumber) in SeatLayout.AllPositions(ev))
        {
            if (existing.TryGetValue((rowLabel, seatNumber), out var seat))
            {
                if (status is null || seat.Status == status)
                    result.Add(seat.ToResponse());
            }
            else if (status is null || status == SeatStatus.Available)
            {
                result.Add(new SeatResponse(0, eventId, rowLabel, seatNumber, SeatStatus.Available, ev.BasePrice, null, null));
            }
        }
        return result;
    }

    public async Task<IReadOnlyList<SeatResponse>> GetAvailableAsync(int eventId)
    {
        var ev = await _events.GetByIdAsync(eventId)
                 ?? throw new NotFoundException($"Event {eventId} not found.");

        var now = _clock.UtcNow;
        // Effective availability includes seats whose hold has lapsed but the sweeper hasn't cleaned yet.
        var existing = (await _seats.GetByEventAsync(eventId))
            .ToDictionary(s => (s.RowLabel, s.SeatNumber));

        var result = new List<SeatResponse>();
        foreach (var (rowLabel, seatNumber) in SeatLayout.AllPositions(ev))
        {
            if (existing.TryGetValue((rowLabel, seatNumber), out var seat))
            {
                if (SeatRules.IsEffectivelyAvailable(seat, now))
                    result.Add(seat.ToResponse());
            }
            else
            {
                // Never touched → implicitly available.
                result.Add(new SeatResponse(0, eventId, rowLabel, seatNumber, SeatStatus.Available, ev.BasePrice, null, null));
            }
        }
        return result;
    }
}
