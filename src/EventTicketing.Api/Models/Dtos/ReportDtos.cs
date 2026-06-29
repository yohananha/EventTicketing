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
