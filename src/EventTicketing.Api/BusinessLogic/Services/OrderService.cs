using EventTicketing.BusinessLogic.Exceptions;
using EventTicketing.BusinessLogic.Mapping;
using EventTicketing.DataAccess;
using EventTicketing.DataAccess.Repositories;
using EventTicketing.Models.Dtos;
using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace EventTicketing.BusinessLogic.Services;

public class OrderService : IOrderService
{
    private readonly ISeatRepository _seats;
    private readonly ICustomerRepository _customers;
    private readonly IOrderRepository _orders;
    private readonly ISeatStatusHistoryRepository _history;
    private readonly IUnitOfWork _uow;
    private readonly IClock _clock;

    public OrderService(
        ISeatRepository seats,
        ICustomerRepository customers,
        IOrderRepository orders,
        ISeatStatusHistoryRepository history,
        IUnitOfWork uow,
        IClock clock)
    {
        _seats = seats;
        _customers = customers;
        _orders = orders;
        _history = history;
        _uow = uow;
        _clock = clock;
    }

    /// <summary>
    /// Confirms a purchase: the customer's held seats become Occupied and an Order is created.
    /// Runs in one transaction so seats and order commit together (or not at all).
    /// </summary>
    public async Task<OrderResponse> PurchaseAsync(int customerId, IReadOnlyList<int> seatIds)
    {
        if (!await _customers.ExistsAsync(customerId))
            throw new NotFoundException($"Customer {customerId} not found.");

        Order order = null!;
        try
        {
            await _uow.ExecuteInTransactionAsync(async () =>
            {
                var seats = await _seats.GetByIdsAsync(seatIds);
                if (seats.Count != seatIds.Distinct().Count())
                    throw new NotFoundException("One or more seats were not found.");

                var now = _clock.UtcNow;

                // Every seat must currently be held by this customer and not expired.
                foreach (var seat in seats)
                {
                    if (!SeatRules.IsHeldBy(seat, customerId, now))
                        throw new ConflictException(
                            $"Seat {seat.Id} is not held by customer {customerId} (or the hold expired).");
                }

                order = new Order
                {
                    CustomerId = customerId,
                    Status = OrderStatus.Paid,
                    CreatedAtUtc = now,
                    TotalAmount = seats.Sum(s => s.Price)
                };

                foreach (var seat in seats)
                {
                    seat.Status = SeatStatus.Occupied; // all seats are InProgress here (validated above)
                    seat.HeldByCustomerId = null;
                    seat.HoldExpiresAtUtc = null;
                    seat.LastUpdatedUtc = now;

                    order.Items.Add(new OrderItem
                    {
                        EventId = seat.EventId,
                        SeatId = seat.Id,
                        Seat = seat,
                        UnitPrice = seat.Price
                    });
                }

                _orders.Add(order);
                await _uow.SaveChangesAsync(); // assigns order.Id

                // Record history now that we have the order id (same transaction).
                foreach (var seat in seats)
                {
                    _history.Add(new SeatStatusHistory
                    {
                        SeatId = seat.Id,
                        EventId = seat.EventId,
                        OldStatus = SeatStatus.InProgress,
                        NewStatus = SeatStatus.Occupied,
                        Reason = SeatStatusChangeReason.Purchased,
                        ChangedAtUtc = now,
                        ChangedByCustomerId = customerId,
                        OrderId = order.Id
                    });
                }
                await _uow.SaveChangesAsync();
            });
        }
        catch (DbUpdateConcurrencyException)
        {
            throw new ConflictException("A seat changed concurrently during purchase. Please retry.");
        }

        return order.ToResponse();
    }

    public async Task<OrderResponse?> GetAsync(int id)
    {
        var order = await _orders.GetByIdAsync(id);
        return order?.ToResponse();
    }

    public async Task<IReadOnlyList<OrderResponse>> GetByCustomerAsync(int customerId)
    {
        if (!await _customers.ExistsAsync(customerId))
            throw new NotFoundException($"Customer {customerId} not found.");
        var orders = await _orders.GetByCustomerAsync(customerId);
        return orders.Select(o => o.ToResponse()).ToList();
    }
}
