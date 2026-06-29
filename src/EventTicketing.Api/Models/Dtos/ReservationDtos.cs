using System.ComponentModel.DataAnnotations;
using EventTicketing.Models.Enums;

namespace EventTicketing.Models.Dtos;

/// <summary>Identifies a seat by its grid position (used before the seat row exists in the DB).</summary>
public record ToggleSeatRequest(
    [Required] int EventId,
    [Required] string RowLabel,
    [Required, Range(1, int.MaxValue)] int SeatNumber,
    [Required] int CustomerId);

/// <summary>Result of a toggle: whether the seat was held or released, and its new state.</summary>
public record ToggleSeatResponse(
    int SeatId,
    SeatStatus Status,
    string Action, // "Held" or "Released"
    DateTime? HoldExpiresAtUtc);

public record HoldSeatsRequest(
    [Required] int CustomerId,
    [Required, MinLength(1)] IReadOnlyList<int> SeatIds);

public record HoldResultResponse(IReadOnlyList<SeatResponse> HeldSeats, DateTime HoldExpiresAtUtc);
