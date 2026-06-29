using EventTicketing.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.DataAccess.Repositories;

public class SeatStatusHistoryRepository : ISeatStatusHistoryRepository
{
    private readonly AppDbContext _db;
    public SeatStatusHistoryRepository(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<SeatStatusHistory>> GetBySeatAsync(int seatId) =>
        await _db.SeatStatusHistory.AsNoTracking()
            .Where(h => h.SeatId == seatId)
            .OrderBy(h => h.ChangedAtUtc)
            .ToListAsync();

    public async Task<IReadOnlyList<SeatStatusHistory>> GetByEventAsync(int eventId) =>
        await _db.SeatStatusHistory.AsNoTracking()
            .Where(h => h.EventId == eventId)
            .OrderBy(h => h.ChangedAtUtc)
            .ToListAsync();

    public void Add(SeatStatusHistory entry) => _db.SeatStatusHistory.Add(entry);
}
