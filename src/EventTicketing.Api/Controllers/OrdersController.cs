using EventTicketing.BusinessLogic.Services;
using EventTicketing.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/orders")]
public class OrdersController : ControllerBase
{
    private readonly IOrderService _orders;
    public OrdersController(IOrderService orders) => _orders = orders;

    /// <summary>Confirm a purchase of the customer's held seats; creates a paid order.</summary>
    [HttpPost]
    public async Task<ActionResult<OrderResponse>> Purchase(PurchaseRequest request)
    {
        var order = await _orders.PurchaseAsync(request.CustomerId, request.SeatIds);
        return CreatedAtAction(nameof(Get), new { id = order.Id }, order);
    }

    /// <summary>Get an order by id.</summary>
    [HttpGet("{id:int}")]
    public async Task<ActionResult<OrderResponse>> Get(int id)
    {
        var order = await _orders.GetAsync(id);
        return order is null ? NotFound() : Ok(order);
    }
}
