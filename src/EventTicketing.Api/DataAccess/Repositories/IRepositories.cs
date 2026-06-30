using EventTicketing.Models.Dtos;
using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;

namespace EventTicketing.DataAccess.Repositories;

// Repositories only query/stage changes; persistence is committed via IUnitOfWork.

public interface IEventRepository
{
    Task<Event?> GetByIdAsync(int id, bool includeSeats = false);
    Task<IReadOnlyList<Event>> GetAllAsync();
    Task<bool> ExistsAsync(int id);
    void Add(Event entity);
    void Remove(Event entity);
}

public interface ISeatRepository
{
    /// <summary>Tracked single seat (for updates).</summary>
    Task<Seat?> GetByIdAsync(int id);
    /// <summary>Tracked seats by id (for batch hold / purchase).</summary>
    Task<List<Seat>> GetByIdsAsync(IReadOnlyCollection<int> ids);
    /// <summary>Tracked seat by grid position — returns null if never held (does not yet exist in DB).</summary>
    Task<Seat?> GetByPositionAsync(int eventId, string rowLabel, int seatNumber);
    Task<IReadOnlyList<Seat>> GetByEventAsync(int eventId, SeatStatus? status = null);
    /// <summary>Count seats that are currently unavailable: active holds + sold. Used to derive available count.</summary>
    Task<int> CountUnavailableByEventAsync(int eventId, DateTime nowUtc);
    /// <summary>InProgress seats whose hold expired — for the background sweeper.</summary>
    Task<List<Seat>> GetExpiredHoldsAsync(DateTime nowUtc);
    void Add(Seat seat);
}

public interface ICustomerRepository
{
    Task<Customer?> GetByIdAsync(int id);
    Task<IReadOnlyList<Customer>> GetAllAsync();
    Task<bool> ExistsAsync(int id);
    Task<bool> EmailExistsAsync(string email, int? excludeId = null);
    void Add(Customer entity);
    void Remove(Customer entity);
}

public interface IOrderRepository
{
    Task<Order?> GetByIdAsync(int id);
    Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId);
    void Add(Order entity);
}

public interface ISeatStatusHistoryRepository
{
    Task<IReadOnlyList<SeatStatusHistory>> GetBySeatAsync(int seatId);
    Task<IReadOnlyList<SeatStatusHistory>> GetByEventAsync(int eventId);
    void Add(SeatStatusHistory entry);
}

/// <summary>Aggregate report queries that use raw SQL (joins, conditional aggregation, window functions).</summary>
public interface IReportRepository
{
    Task<IReadOnlyList<ActiveEventOverviewRow>> GetActiveEventsOverviewAsync(DateTime nowUtc);
    Task<IReadOnlyList<DailySalesRow>> GetEventDailySalesAsync(int eventId, DateTime toDateUtc);
}
