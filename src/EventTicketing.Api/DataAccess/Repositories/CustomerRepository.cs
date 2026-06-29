using EventTicketing.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.DataAccess.Repositories;

public class CustomerRepository : ICustomerRepository
{
    private readonly AppDbContext _db;
    public CustomerRepository(AppDbContext db) => _db = db;

    public Task<Customer?> GetByIdAsync(int id) =>
        _db.Customers.FirstOrDefaultAsync(c => c.Id == id);

    public async Task<IReadOnlyList<Customer>> GetAllAsync() =>
        await _db.Customers.AsNoTracking().OrderBy(c => c.FullName).ToListAsync();

    public Task<bool> ExistsAsync(int id) => _db.Customers.AnyAsync(c => c.Id == id);

    public Task<bool> EmailExistsAsync(string email, int? excludeId = null) =>
        _db.Customers.AnyAsync(c => c.Email == email && (excludeId == null || c.Id != excludeId));

    public void Add(Customer entity) => _db.Customers.Add(entity);

    public void Remove(Customer entity) => _db.Customers.Remove(entity);
}
