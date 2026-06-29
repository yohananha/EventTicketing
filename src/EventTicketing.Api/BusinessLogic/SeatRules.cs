using EventTicketing.Models.Entities;
using EventTicketing.Models.Enums;

namespace EventTicketing.BusinessLogic;

/// <summary>
/// Pure seat-state rules. Expiry is evaluated lazily here so correctness holds even if the
/// background sweeper is behind: a lapsed hold is treated as available the moment it is touched.
/// </summary>
public static class SeatRules
{
    public static bool IsEffectivelyAvailable(Seat seat, DateTime nowUtc) =>
        seat.Status == SeatStatus.Available ||
        (seat.Status == SeatStatus.InProgress && seat.HoldExpiresAtUtc < nowUtc);

    public static bool IsHeldBy(Seat seat, int customerId, DateTime nowUtc) =>
        seat.Status == SeatStatus.InProgress &&
        seat.HeldByCustomerId == customerId &&
        seat.HoldExpiresAtUtc >= nowUtc;
}
