using EventTicketing.BusinessLogic.Services;
using EventTicketing.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly IReservationService _reservations;
    public ReservationsController(IReservationService reservations) => _reservations = reservations;

    /// <summary>Click-to-hold / click-to-release. Creates the seat DB row on first hold.</summary>
    [HttpPost("toggle")]
    public async Task<ActionResult<ToggleSeatResponse>> Toggle([FromBody] ToggleSeatRequest request) =>
        Ok(await _reservations.ToggleSeatHoldAsync(request.EventId, request.RowLabel, request.SeatNumber, request.CustomerId));

    /// <summary>Hold a batch of seats for a customer.</summary>
    [HttpPost("hold")]
    public async Task<ActionResult<HoldResultResponse>> Hold(HoldSeatsRequest request) =>
        Ok(await _reservations.HoldSeatsAsync(request.CustomerId, request.SeatIds));

    /// <summary>Release a seat the customer is holding.</summary>
    [HttpDelete("seats/{seatId:int}")]
    public async Task<IActionResult> Release(int seatId, [FromQuery] int customerId)
    {
        await _reservations.ReleaseHoldAsync(seatId, customerId);
        return NoContent();
    }
}
