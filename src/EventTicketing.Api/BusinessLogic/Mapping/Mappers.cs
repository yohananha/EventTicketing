using EventTicketing.Models.Dtos;
using EventTicketing.Models.Entities;

namespace EventTicketing.BusinessLogic.Mapping;

/// <summary>Manual entity → DTO mapping (kept dependency-free and easy to follow).</summary>
public static class Mappers
{
    public static EventResponse ToResponse(this Event e, int totalSeats, int availableSeats) => new(
        e.Id, e.Name, e.Type, e.Description, e.Venue, e.StartsAtUtc, e.EndsAtUtc,
        e.RowCount, e.SeatsPerRow, e.BasePrice, e.Status, totalSeats, availableSeats);

    public static SeatResponse ToResponse(this Seat s) => new(
        s.Id, s.EventId, s.RowLabel, s.SeatNumber, s.Status, s.Price, s.HoldExpiresAtUtc, s.HeldByCustomerId);

    public static CustomerResponse ToResponse(this Customer c) => new(
        c.Id, c.FullName, c.Email, c.Phone, c.CreatedAtUtc);

    public static OrderResponse ToResponse(this Order o) => new(
        o.Id, o.CustomerId, o.Status, o.TotalAmount, o.CreatedAtUtc,
        o.Items.Select(i => i.ToResponse()).ToList());

    public static OrderItemResponse ToResponse(this OrderItem i) => new(
        i.Id, i.EventId, i.SeatId,
        i.Seat is null ? string.Empty : $"{i.Seat.RowLabel}{i.Seat.SeatNumber}",
        i.UnitPrice);

    public static SeatStatusHistoryResponse ToResponse(this SeatStatusHistory h) => new(
        h.Id, h.SeatId, h.EventId, h.OldStatus, h.NewStatus, h.Reason,
        h.ChangedAtUtc, h.ChangedByCustomerId, h.OrderId);
}
