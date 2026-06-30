using EventTicketing.BusinessLogic.Exceptions;
using EventTicketing.BusinessLogic.Mapping;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Enums;

namespace EventTicketing.BusinessLogic.Services;

public class ReportService : IReportService
{
    private readonly ISeatStatusHistoryRepository _history;
    private readonly ISeatRepository _seats;
    private readonly IEventRepository _events;
    private readonly IReportRepository _reports;
    private readonly IClock _clock;

    public ReportService(
        ISeatStatusHistoryRepository history,
        ISeatRepository seats,
        IEventRepository events,
        IReportRepository reports,
        IClock clock)
    {
        _history = history;
        _seats = seats;
        _events = events;
        _reports = reports;
        _clock = clock;
    }

    public async Task<IReadOnlyList<SeatStatusHistoryResponse>> GetSeatHistoryAsync(int seatId)
    {
        var entries = await _history.GetBySeatAsync(seatId);
        return entries.Select(h => h.ToResponse()).ToList();
    }

    public async Task<EventSalesSummaryResponse> GetEventSalesSummaryAsync(int eventId)
    {
        var ev = await _events.GetByIdAsync(eventId)
                 ?? throw new NotFoundException($"Event {eventId} not found.");

        var now = _clock.UtcNow;
        var seats = await _seats.GetByEventAsync(eventId);

        var available = seats.Count(s => SeatRules.IsEffectivelyAvailable(s, now));
        var held = seats.Count(s => s.Status == SeatStatus.InProgress && s.HoldExpiresAtUtc >= now);
        var sold = seats.Count(s => s.Status == SeatStatus.Occupied);
        var revenue = seats.Where(s => s.Status == SeatStatus.Occupied).Sum(s => s.Price);

        return new EventSalesSummaryResponse(ev.Id, ev.Name, seats.Count, available, held, sold, revenue);
    }

    public Task<IReadOnlyList<ActiveEventOverviewRow>> GetActiveEventsOverviewAsync() =>
        _reports.GetActiveEventsOverviewAsync(_clock.UtcNow);

    public async Task<EventDailySalesResponse> GetEventDailySalesAsync(int eventId, DateTime? toDateUtc = null)
    {
        var ev = await _events.GetByIdAsync(eventId)
                 ?? throw new NotFoundException($"Event {eventId} not found.");

        // Default to today (UTC). DateTime.Date strips the time component so the SQL's
        // CAST(@toDate AS date) sees a clean day boundary regardless of caller-passed time.
        var endDate = (toDateUtc ?? _clock.UtcNow).Date;
        var days = await _reports.GetEventDailySalesAsync(eventId, endDate);
        return new EventDailySalesResponse(ev.Id, ev.Name, endDate, days);
    }
}
