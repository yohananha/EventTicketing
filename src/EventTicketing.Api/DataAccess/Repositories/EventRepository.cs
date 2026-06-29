using EventTicketing.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.DataAccess.Repositories;

public class EventRepository : IEventRepository
{
    private readonly AppDbContext _db;
    public EventRepository(AppDbContext db) => _db = db;

    public Task<Event?> GetByIdAsync(int id, bool includeSeats = false)
    {
        var query = _db.Events.AsQueryable();
        if (includeSeats) query = query.Include(e => e.Seats);
        return query.FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IReadOnlyList<Event>> GetAllAsync() =>
        await _db.Events.AsNoTracking().OrderBy(e => e.StartsAtUtc).ToListAsync();

    public Task<bool> ExistsAsync(int id) => _db.Events.AnyAsync(e => e.Id == id);

    public void Add(Event entity) => _db.Events.Add(entity);

    public void Remove(Event entity) => _db.Events.Remove(entity);
}
