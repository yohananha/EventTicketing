using EventTicketing.BusinessLogic.Exceptions;
using EventTicketing.BusinessLogic.Mapping;
using EventTicketing.DataAccess;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Entities;

namespace EventTicketing.BusinessLogic.Services;

public class EventService : IEventService
{
    private readonly IEventRepository _events;
    private readonly ISeatRepository _seats;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public EventService(IEventRepository events, ISeatRepository seats, IUnitOfWork uow, IClock clock)
    {
        _events = events;
        _seats = seats;
        _uow = uow;
        _clock = clock;
    }

    /// <summary>Creates an event. Seat rows are NOT inserted here — they are created lazily on first hold.</summary>
    public async Task<EventResponse> CreateAsync(CreateEventRequest request)
    {
        var now = _clock.UtcNow;
        var ev = new Event
        {
            Name = request.Name,
            Type = request.Type,
            Description = request.Description,
            Venue = request.Venue,
            StartsAtUtc = request.StartsAtUtc,
            EndsAtUtc = request.EndsAtUtc,
            RowCount = request.RowCount,
            SeatsPerRow = request.SeatsPerRow,
            BasePrice = request.BasePrice,
            Status = Models.Enums.EventStatus.Scheduled,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        _events.Add(ev);
        await _uow.SaveChangesAsync();

        var total = ev.RowCount * ev.SeatsPerRow;
        return ev.ToResponse(total, total); // all seats start implicitly available
    }

    public async Task<EventResponse?> GetAsync(int id)
    {
        var ev = await _events.GetByIdAsync(id);
        if (ev is null) return null;
        var total = ev.RowCount * ev.SeatsPerRow;
        var unavailable = await _seats.CountUnavailableByEventAsync(id, _clock.UtcNow);
        return ev.ToResponse(total, total - unavailable);
    }

    public async Task<IReadOnlyList<EventResponse>> GetAllAsync()
    {
        var events = await _events.GetAllAsync();
        var now = _clock.UtcNow;
        var result = new List<EventResponse>(events.Count);
        foreach (var ev in events)
        {
            var total = ev.RowCount * ev.SeatsPerRow;
            var unavailable = await _seats.CountUnavailableByEventAsync(ev.Id, now);
            result.Add(ev.ToResponse(total, total - unavailable));
        }
        return result;
    }

    public async Task<EventResponse> UpdateAsync(int id, UpdateEventRequest request)
    {
        var ev = await _events.GetByIdAsync(id)
                 ?? throw new NotFoundException($"Event {id} not found.");

        ev.Name = request.Name;
        ev.Type = request.Type;
        ev.Description = request.Description;
        ev.Venue = request.Venue;
        ev.StartsAtUtc = request.StartsAtUtc;
        ev.EndsAtUtc = request.EndsAtUtc;
        ev.Status = request.Status;
        ev.UpdatedAtUtc = _clock.UtcNow;
        await _uow.SaveChangesAsync();

        var total = ev.RowCount * ev.SeatsPerRow;
        var unavailable = await _seats.CountUnavailableByEventAsync(id, _clock.UtcNow);
        return ev.ToResponse(total, total - unavailable);
    }

    public async Task DeleteAsync(int id)
    {
        var ev = await _events.GetByIdAsync(id)
                 ?? throw new NotFoundException($"Event {id} not found.");
        _events.Remove(ev); // seats cascade-delete
        await _uow.SaveChangesAsync();
    }
}
