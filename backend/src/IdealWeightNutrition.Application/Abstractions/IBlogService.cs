using IdealWeightNutrition.Contracts.Content;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IBlogService
{
    Task<IReadOnlyList<BlogPostSummaryDto>> ListPublishedAsync(CancellationToken cancellationToken = default);
    Task<BlogPostDetailDto?> GetBySlugAsync(string slug, CancellationToken cancellationToken = default);
}
