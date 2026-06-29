using EventTicketing.Models.Enums;

namespace EventTicketing.Models.Dtos;

public record SeatResponse(
    int Id,
    int EventId,
    string RowLabel,
    int SeatNumber,
    SeatStatus Status,
    decimal Price,
    DateTime? HoldExpiresAtUtc,
    int? HeldByCustomerId);
