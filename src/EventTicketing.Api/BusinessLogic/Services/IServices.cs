using EventTicketing.Models.Dtos;
using EventTicketing.Models.Enums;

namespace EventTicketing.BusinessLogic.Services;

public interface IEventService
{
    Task<EventResponse> CreateAsync(CreateEventRequest request);
    Task<EventResponse?> GetAsync(int id);
    Task<IReadOnlyList<EventResponse>> GetAllAsync();
    Task<EventResponse> UpdateAsync(int id, UpdateEventRequest request);
    Task DeleteAsync(int id);
}

public interface ISeatService
{
    Task<IReadOnlyList<SeatResponse>> GetByEventAsync(int eventId, SeatStatus? status);
    Task<IReadOnlyList<SeatResponse>> GetAvailableAsync(int eventId);
}

public interface ICustomerService
{
    Task<CustomerResponse> CreateAsync(CreateCustomerRequest request);
    Task<CustomerResponse?> GetAsync(int id);
    Task<IReadOnlyList<CustomerResponse>> GetAllAsync();
    Task<CustomerResponse> UpdateAsync(int id, UpdateCustomerRequest request);
    Task DeleteAsync(int id);
}

public interface IReservationService
{
    /// <summary>Single-seat click-to-hold / click-to-release toggle. Creates the seat row on first hold.</summary>
    Task<ToggleSeatResponse> ToggleSeatHoldAsync(int eventId, string rowLabel, int seatNumber, int customerId);
    /// <summary>Hold a batch of seats by id (seats must already exist from a prior toggle).</summary>
    Task<HoldResultResponse> HoldSeatsAsync(int customerId, IReadOnlyList<int> seatIds);
    /// <summary>Explicitly release a hold the customer owns.</summary>
    Task ReleaseHoldAsync(int seatId, int customerId);
    /// <summary>Release every expired hold (used by the background sweeper). Returns count released.</summary>
    Task<int> ReleaseExpiredHoldsAsync();
}

public interface IOrderService
{
    Task<OrderResponse> PurchaseAsync(int customerId, IReadOnlyList<int> seatIds);
    Task<OrderResponse?> GetAsync(int id);
    Task<IReadOnlyList<OrderResponse>> GetByCustomerAsync(int customerId);
}

public interface IReportService
{
    Task<IReadOnlyList<SeatStatusHistoryResponse>> GetSeatHistoryAsync(int seatId);
    Task<EventSalesSummaryResponse> GetEventSalesSummaryAsync(int eventId);
    /// <summary>Dashboard view of every non-finished event (includes SoldOut to surface data mismatches).</summary>
    Task<IReadOnlyList<ActiveEventOverviewRow>> GetActiveEventsOverviewAsync();
    /// <summary>Per-day sales timeline for an event up to <paramref name="toDateUtc"/> (defaults to today UTC).</summary>
    Task<EventDailySalesResponse> GetEventDailySalesAsync(int eventId, DateTime? toDateUtc = null);
}
