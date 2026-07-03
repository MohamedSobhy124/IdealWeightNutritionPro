using IdealWeightNutrition.Application;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace IdealWeightNutrition.Api;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddModernizedPlatform(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddApplication();
        services.AddInfrastructure(configuration);
        services.AddApiRateLimiting();
        services.AddSignalR();
        services.AddSingleton<INotificationRealtimePublisher, Services.NotificationRealtimePublisher>();
        services.AddHttpContextAccessor();
        return services;
    }
}
