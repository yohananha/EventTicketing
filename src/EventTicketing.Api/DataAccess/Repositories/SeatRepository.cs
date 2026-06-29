using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.DataAccess.Repositories;

public class SeatRepository : ISeatRepository
{
    private readonly AppDbContext _db;
    public SeatRepository(AppDbContext db) => _db = db;

    public Task<Seat?> GetByIdAsync(int id) =>
        _db.Seats.FirstOrDefaultAsync(s => s.Id == id);

    public Task<List<Seat>> GetByIdsAsync(IReadOnlyCollection<int> ids) =>
        _db.Seats.Where(s => ids.Contains(s.Id)).ToListAsync();

    public Task<Seat?> GetByPositionAsync(int eventId, string rowLabel, int seatNumber) =>
        _db.Seats.FirstOrDefaultAsync(s =>
            s.EventId == eventId && s.RowLabel == rowLabel && s.SeatNumber == seatNumber);

    public async Task<IReadOnlyList<Seat>> GetByEventAsync(int eventId, SeatStatus? status = null)
    {
        var query = _db.Seats.AsNoTracking().Where(s => s.EventId == eventId);
        if (status is not null) query = query.Where(s => s.Status == status);
        return await query
            .OrderBy(s => s.RowLabel).ThenBy(s => s.SeatNumber)
            .ToListAsync();
    }

    // Seats not yet in DB are implicitly available; only count what's actually locked or sold.
    public Task<int> CountUnavailableByEventAsync(int eventId, DateTime nowUtc) =>
        _db.Seats.CountAsync(s => s.EventId == eventId &&
            (s.Status == SeatStatus.Occupied ||
             (s.Status == SeatStatus.InProgress && s.HoldExpiresAtUtc >= nowUtc)));

    public Task<List<Seat>> GetExpiredHoldsAsync(DateTime nowUtc) =>
        _db.Seats
            .Where(s => s.Status == SeatStatus.InProgress && s.HoldExpiresAtUtc < nowUtc)
            .ToListAsync();

    public void Add(Seat seat) => _db.Seats.Add(seat);
}
