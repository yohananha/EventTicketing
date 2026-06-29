namespace EventTicketing.BusinessLogic;

/// <summary>Configurable timings for the seat-hold model (bound from appsettings "Hold" section).</summary>
public class HoldSettings
{
    /// <summary>How long a seat stays held before it auto-expires.</summary>
    public TimeSpan HoldDuration { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>How often the background sweeper releases expired holds.</summary>
    public TimeSpan SweepInterval { get; set; } = TimeSpan.FromSeconds(30);
}
