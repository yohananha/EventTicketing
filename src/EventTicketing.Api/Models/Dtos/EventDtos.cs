using System.ComponentModel.DataAnnotations;
using EventTicketing.Models.Enums;

namespace EventTicketing.Models.Dtos;

public record CreateEventRequest(
    [Required, StringLength(200)] string Name,
    EventType Type,
    [StringLength(1000)] string? Description,
    [Required, StringLength(200)] string Venue,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    [Range(1, 200)] int RowCount,
    [Range(1, 200)] int SeatsPerRow,
    [Range(0, 100000)] decimal BasePrice);

public record UpdateEventRequest(
    [Required, StringLength(200)] string Name,
    EventType Type,
    [StringLength(1000)] string? Description,
    [Required, StringLength(200)] string Venue,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    EventStatus Status);

public record EventResponse(
    int Id,
    string Name,
    EventType Type,
    string? Description,
    string Venue,
    DateTime StartsAtUtc,
    DateTime? EndsAtUtc,
    int RowCount,
    int SeatsPerRow,
    decimal BasePrice,
    EventStatus Status,
    int TotalSeats,
    int AvailableSeats);
