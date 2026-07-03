using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace IdealWeightNutrition.Api.Http;

public static class RateLimitingExtensions
{
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddRateLimiter(options =>
        {
            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

            options.AddPolicy(RateLimitPolicies.Auth, context =>
                CreatePartition(context, permitLimit: 12, windowMinutes: 1));

            options.AddPolicy(RateLimitPolicies.PublicForms, context =>
                CreatePartition(context, permitLimit: 8, windowMinutes: 1));

            options.AddPolicy(RateLimitPolicies.Api, context =>
                CreatePartition(context, permitLimit: 120, windowMinutes: 1));

            options.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(context =>
                CreatePartition(context, permitLimit: 300, windowMinutes: 1));
        });

        return services;
    }

    private static RateLimitPartition<string> CreatePartition(
        HttpContext context,
        int permitLimit,
        int windowMinutes)
    {
        var key = context.Connection.RemoteIpAddress?.ToString() ?? "unknown";
        return RateLimitPartition.GetFixedWindowLimiter(
            key,
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = permitLimit,
                Window = TimeSpan.FromMinutes(windowMinutes),
                QueueLimit = 0,
                AutoReplenishment = true
            });
    }
}
