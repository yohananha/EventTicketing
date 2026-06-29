using EventTicketing.BusinessLogic.Services;
using Microsoft.Extensions.DependencyInjection;

namespace EventTicketing.BusinessLogic;

public static class DependencyInjection
{
    /// <summary>Registers the clock and all business-logic services.</summary>
    public static IServiceCollection AddBusinessLogic(this IServiceCollection services)
    {
        services.AddSingleton<IClock, SystemClock>();

        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ISeatService, SeatService>();
        services.AddScoped<ICustomerService, CustomerService>();
        services.AddScoped<IReservationService, ReservationService>();
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IReportService, ReportService>();

        return services;
    }
}
