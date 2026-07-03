using IdealWeightNutrition.Contracts.Reviews;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IReviewService
{
    Task<IReadOnlyList<ProductReviewDto>> ListApprovedProductReviewsAsync(int productId, CancellationToken cancellationToken = default);

    Task<ProductReviewSummaryDto> GetProductReviewSummaryAsync(int productId, CancellationToken cancellationToken = default);

    Task<ProductReviewDto> SubmitProductReviewAsync(
        int productId,
        string userId,
        SubmitProductReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ProductReviewDto>> ListApprovedServiceReviewsAsync(
        int serviceId,
        CancellationToken cancellationToken = default);

    Task<ProductReviewSummaryDto> GetServiceReviewSummaryAsync(
        int serviceId,
        CancellationToken cancellationToken = default);

    Task<ProductReviewDto> SubmitServiceReviewAsync(
        int serviceId,
        string userId,
        SubmitProductReviewRequest request,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<FeaturedReviewDto>> ListFeaturedReviewsAsync(
        int count = 6,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<AdminReviewListItemDto>> ListAdminReviewsAsync(
        string? status,
        CancellationToken cancellationToken = default);

    Task ToggleApprovalAsync(int reviewId, CancellationToken cancellationToken = default);

    Task DeleteReviewAsync(int reviewId, CancellationToken cancellationToken = default);
}
