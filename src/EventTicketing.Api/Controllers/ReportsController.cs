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

    /// <summary>Dashboard view: one row per non-finished event with sold/held/available counts and revenue.</summary>
    [HttpGet("events/active-overview")]
    public async Task<ActionResult<IReadOnlyList<ActiveEventOverviewRow>>> ActiveEventsOverview() =>
        Ok(await _reports.GetActiveEventsOverviewAsync());

    /// <summary>Per-day sales timeline for an event (running totals + DENSE_RANK by revenue). Defaults to today UTC.</summary>
    [HttpGet("events/{eventId:int}/daily-sales")]
    public async Task<ActionResult<EventDailySalesResponse>> EventDailySales(int eventId, [FromQuery] DateTime? toDate) =>
        Ok(await _reports.GetEventDailySalesAsync(eventId, toDate));
}
