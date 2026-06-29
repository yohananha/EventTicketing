using System.ComponentModel.DataAnnotations;
using EventTicketing.Models.Enums;

namespace EventTicketing.Models.Dtos;

public record PurchaseRequest(
    [Required] int CustomerId,
    [Required, MinLength(1)] IReadOnlyList<int> SeatIds);

public record OrderItemResponse(
    int Id,
    int EventId,
    int SeatId,
    string SeatLabel,
    decimal UnitPrice);

public record OrderResponse(
    int Id,
    int CustomerId,
    OrderStatus Status,
    decimal TotalAmount,
    DateTime CreatedAtUtc,
    IReadOnlyList<OrderItemResponse> Items);
