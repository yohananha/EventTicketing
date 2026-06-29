namespace EventTicketing.Models.Enums;

public enum EventType
{
    Concert = 0,
    Sports = 1,
    Theater = 2,
    Conference = 3,
    Other = 4
}

public enum EventStatus
{
    Scheduled = 0,
    OnSale = 1,
    SoldOut = 2,
    Cancelled = 3,
    Completed = 4
}

/// <summary>Lifecycle of a single seat for an event.</summary>
public enum SeatStatus
{
    Available = 0,
    InProgress = 1, // held by a customer for a limited time (see Seat.HoldExpiresAtUtc)
    Occupied = 2    // sold
}

public enum OrderStatus
{
    Pending = 0,
    Paid = 1,
    Cancelled = 2
}

/// <summary>Why a seat changed status — recorded in SeatStatusHistory for reporting.</summary>
public enum SeatStatusChangeReason
{
    Reserved = 0,
    Released = 1,
    Purchased = 2,
    HoldExpired = 3
}
