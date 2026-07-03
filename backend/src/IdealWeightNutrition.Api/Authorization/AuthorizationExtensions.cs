using IdealWeightNutrition.Domain.Constants;
using Microsoft.AspNetCore.Authorization;

namespace IdealWeightNutrition.Api.Authorization;

public static class AuthorizationExtensions
{
    public static IServiceCollection AddApiAuthorization(this IServiceCollection services)
    {
        services.AddAuthorizationBuilder()
            .AddPolicy(Policies.Customer, policy =>
                policy.RequireRole(Roles.Customer, Roles.Company, Roles.Admin))
            .AddPolicy(Policies.Admin, policy =>
                policy.RequireRole(Roles.Admin))
            .AddPolicy(Policies.Employee, policy =>
                policy.RequireRole(Roles.Employee, Roles.Admin));

        return services;
    }
}
