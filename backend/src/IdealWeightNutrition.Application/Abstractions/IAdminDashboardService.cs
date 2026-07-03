using IdealWeightNutrition.Contracts.Admin;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAdminDashboardService
{
    Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default);
}
