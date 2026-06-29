using EventTicketing.BusinessLogic.Services;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Enums;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/events/{eventId:int}/seats")]
public class SeatsController : ControllerBase
{
    private readonly ISeatService _seats;
    public SeatsController(ISeatService seats) => _seats = seats;

    /// <summary>List an event's seats, optionally filtered by status.</summary>
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<SeatResponse>>> GetByEvent(int eventId, [FromQuery] SeatStatus? status) =>
        Ok(await _seats.GetByEventAsync(eventId, status));

    /// <summary>List seats that can currently be held (includes lapsed holds).</summary>
    [HttpGet("available")]
    public async Task<ActionResult<IReadOnlyList<SeatResponse>>> GetAvailable(int eventId) =>
        Ok(await _seats.GetAvailableAsync(eventId));
}
