using EventTicketing.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.DataAccess.Repositories;

public class OrderRepository : IOrderRepository
{
    private readonly AppDbContext _db;
    public OrderRepository(AppDbContext db) => _db = db;

    public Task<Order?> GetByIdAsync(int id) =>
        _db.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Seat)
            .FirstOrDefaultAsync(o => o.Id == id);

    public async Task<IReadOnlyList<Order>> GetByCustomerAsync(int customerId) =>
        await _db.Orders
            .AsNoTracking()
            .Include(o => o.Items).ThenInclude(i => i.Seat)
            .Where(o => o.CustomerId == customerId)
            .OrderByDescending(o => o.CreatedAtUtc)
            .ToListAsync();

    public void Add(Order entity) => _db.Orders.Add(entity);
}
