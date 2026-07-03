using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class GetAdminDashboardEndpoint : EndpointWithoutRequest<AdminDashboardDto>
{
    private readonly IAdminDashboardService _dashboard;

    public GetAdminDashboardEndpoint(IAdminDashboardService dashboard) => _dashboard = dashboard;

    public override void Configure()
    {
        Get("admin/dashboard");
        Policies(AuthPolicies.Employee);
    }

    public override async Task HandleAsync(CancellationToken ct) =>
        await Send.OkAsync(await _dashboard.GetDashboardAsync(ct), ct);
}
