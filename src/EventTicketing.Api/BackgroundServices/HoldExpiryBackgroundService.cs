using EventTicketing.BusinessLogic;
using EventTicketing.BusinessLogic.Services;
using Microsoft.Extensions.Options;

namespace EventTicketing.Api.BackgroundServices;

/// <summary>
/// Periodically releases expired seat holds back to Available (and logs HoldExpired history),
/// so abandoned holds don't lock seats and availability reports stay accurate.
/// </summary>
public class HoldExpiryBackgroundService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<HoldExpiryBackgroundService> _logger;
    private readonly HoldSettings _settings;

    public HoldExpiryBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<HoldExpiryBackgroundService> logger,
        IOptions<HoldSettings> settings)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = settings.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                // Resolve a scoped service per tick (the service depends on the scoped DbContext).
                using var scope = _scopeFactory.CreateScope();
                var reservations = scope.ServiceProvider.GetRequiredService<IReservationService>();
                var released = await reservations.ReleaseExpiredHoldsAsync();
                if (released > 0)
                    _logger.LogInformation("Released {Count} expired seat hold(s).", released);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Hold-expiry sweep failed.");
            }

            await Task.Delay(_settings.SweepInterval, stoppingToken);
        }
    }
}
