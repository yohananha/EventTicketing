using EventTicketing.BusinessLogic.Services;
using EventTicketing.Models.Dtos;
using Microsoft.AspNetCore.Mvc;

namespace EventTicketing.Api.Controllers;

[ApiController]
[Route("api/reports")]
public class ReportsController : ControllerBase
{
    private readonly IReportService _reports;
    public ReportsController(IReportService reports) => _reports = reports;

    /// <summary>Full status-change timeline for a seat (from the history table).</summary>
    [HttpGet("seats/{seatId:int}/history")]
    public async Task<ActionResult<IReadOnlyList<SeatStatusHistoryResponse>>> SeatHistory(int seatId) =>
        Ok(await _reports.GetSeatHistoryAsync(seatId));

    /// <summary>Sales summary for an event: available / held / sold counts and revenue.</summary>
    [HttpGet("events/{eventId:int}/sales")]
    public async Task<ActionResult<EventSalesSummaryResponse>> EventSales(int eventId) =>
        Ok(await _reports.GetEventSalesSummaryAsync(eventId));
}
