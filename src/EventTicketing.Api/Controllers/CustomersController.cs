using EventTicketing.BusinessLogic.Services;
using EventTicketing.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/customers")]
public class CustomersController : ControllerBase
{
    private readonly ICustomerService _customers;
    private readonly IOrderService _orders;

    public CustomersController(ICustomerService customers, IOrderService orders)
    {
        _customers = customers;
        _orders = orders;
    }

    /// <summary>List all customers.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<CustomerResponse>>> GetAll() =>
        Ok(await _customers.GetAllAsync());

    /// <summary>Get a customer by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> Get(int id)
    {
        var customer = await _customers.GetAsync(id);
        return customer is null ? NotFound() : Ok(customer);
    }

    /// <summary>Register a new customer (email must be unique).</summary>
    [HttpPost]
    public async Task<ActionResult<CustomerResponse>> Create(CreateCustomerRequest request)
    {
        var created = await _customers.CreateAsync(request);
        return CreatedAtAction(nameof(Get), new { id = created.Id }, created);
    }

    /// <summary>Update a customer's name/phone.</summary>
    [HttpPut("{id:int}")]
    public async Task<ActionResult<CustomerResponse>> Update(int id, UpdateCustomerRequest request) =>
        Ok(await _customers.UpdateAsync(id, request));

    /// <summary>Delete a customer.</summary>
    [HttpDelete("{id:int}")]
    public async Task<IActionResult> Delete(int id)
    {
        await _customers.DeleteAsync(id);
        return NoContent();
    }

    /// <summary>List a customer's orders.</summary>
    [HttpGet("{id:int}/orders")]
    public async Task<ActionResult<IReadOnlyList<OrderResponse>>> GetOrders(int id) =>
        Ok(await _orders.GetByCustomerAsync(id));
}
