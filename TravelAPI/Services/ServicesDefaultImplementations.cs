using TravelAPI.Services.Abstractions;

namespace TravelAPI.Services;

public static class ServicesDefaultImplementations
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<ITripService, TripService>();
        services.AddScoped<IClientService, ClientService>();
        services.AddScoped<IDateTimeProvider, DateTimeProvider>();

        return services;
    }
}