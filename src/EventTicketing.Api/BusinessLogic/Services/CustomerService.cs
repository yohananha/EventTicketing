using EventTicketing.BusinessLogic.Exceptions;
using EventTicketing.BusinessLogic.Mapping;
using EventTicketing.DataAccess;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Entities;

namespace EventTicketing.BusinessLogic.Services;

public class CustomerService : ICustomerService
{
    private readonly ICustomerRepository _customers;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public CustomerService(ICustomerRepository customers, IUnitOfWork uow, IClock clock)
    {
        _customers = customers;
        _uow = uow;
        _clock = clock;
    }

    public async Task<CustomerResponse> CreateAsync(CreateCustomerRequest request)
    {
        if (await _customers.EmailExistsAsync(request.Email))
            throw new ValidationException($"Email '{request.Email}' is already registered.");

        var customer = new Customer
        {
            FullName = request.FullName,
            Email = request.Email,
            Phone = request.Phone,
            CreatedAtUtc = _clock.UtcNow
        };
        _customers.Add(customer);
        await _uow.SaveChangesAsync();
        return customer.ToResponse();
    }

    public async Task<CustomerResponse?> GetAsync(int id)
    {
        var customer = await _customers.GetByIdAsync(id);
        return customer?.ToResponse();
    }

    public async Task<IReadOnlyList<CustomerResponse>> GetAllAsync()
    {
        var customers = await _customers.GetAllAsync();
        return customers.Select(c => c.ToResponse()).ToList();
    }

    public async Task<CustomerResponse> UpdateAsync(int id, UpdateCustomerRequest request)
    {
        var customer = await _customers.GetByIdAsync(id)
                       ?? throw new NotFoundException($"Customer {id} not found.");
        customer.FullName = request.FullName;
        customer.Phone = request.Phone;
        await _uow.SaveChangesAsync();
        return customer.ToResponse();
    }

    public async Task DeleteAsync(int id)
    {
        var customer = await _customers.GetByIdAsync(id)
                       ?? throw new NotFoundException($"Customer {id} not found.");
        _customers.Remove(customer);
        await _uow.SaveChangesAsync();
    }
}
