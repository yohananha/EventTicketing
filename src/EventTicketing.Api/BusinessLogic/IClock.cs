namespace EventTicketing.BusinessLogic;

/// <summary>Abstracts "now" so hold-expiry logic can be unit-tested deterministically.</summary>
public interface IClock
{
    DateTime UtcNow { get; }
}

public class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
