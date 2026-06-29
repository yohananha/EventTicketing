using EventTicketing.BusinessLogic;

namespace EventTicketing.Tests.TestSupport;

/// <summary>Controllable clock so tests can drive hold-expiry deterministically.</summary>
public class TestClock : IClock
{
    public TestClock(DateTime utcNow) => UtcNow = utcNow;
    public DateTime UtcNow { get; set; }
}
