using EventTicketing.Models.Enums;

namespace EventTicketing.Models.Dtos;

public record SeatStatusHistoryResponse(
    long Id,
    int SeatId,
    int EventId,
    SeatStatus? OldStatus,
    SeatStatus NewStatus,
    SeatStatusChangeReason Reason,
    DateTime ChangedAtUtc,
    int? ChangedByCustomerId,
    int? OrderId);

public record EventSalesSummaryResponse(
    int EventId,
    string EventName,
    int TotalSeats,
    int AvailableSeats,
    int HeldSeats,
    int SoldSeats,
    decimal Revenue);

/// <summary>One row per non-finished event for the operations dashboard.</summary>
public record ActiveEventOverviewRow(
    int EventId,
    string EventName,
    EventStatus Status,
    int PlannedSeats,
    int SoldSeats,
    int HeldSeats,
    int AvailableSeats,
    decimal SoldRatio,   // SoldSeats / PlannedSeats, 0..1
    decimal Revenue);    // sum of Seat.Price for Occupied seats

/// <summary>One row per calendar day on which an event had sales.</summary>
public record DailySalesRow(
    DateTime SaleDate,
    int TicketsSold,
    decimal DailyRevenue,
    int RunningTickets,
    decimal RunningRevenue,
    int RevenueRank);    // DENSE_RANK by DailyRevenue DESC

public record EventDailySalesResponse(
    int EventId,
    string EventName,
    DateTime ToDateUtc,
    IReadOnlyList<DailySalesRow> Days);
