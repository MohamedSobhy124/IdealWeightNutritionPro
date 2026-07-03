using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Reviews;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Admin;

public sealed class ListAdminReviewsRequest
{
    public string? Status { get; init; } = "all";
}

public sealed class ListAdminReviewsEndpoint : Endpoint<ListAdminReviewsRequest, IReadOnlyList<AdminReviewListItemDto>>
{
    private readonly IReviewService _reviews;

    public ListAdminReviewsEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Get("admin/reviews");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(ListAdminReviewsRequest req, CancellationToken ct) =>
        await Send.OkAsync(await _reviews.ListAdminReviewsAsync(req.Status, ct), ct);
}

public sealed class ToggleAdminReviewApprovalEndpoint : EndpointWithoutRequest
{
    private readonly IReviewService _reviews;

    public ToggleAdminReviewApprovalEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Post("admin/reviews/{reviewId}/toggle-approval");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("reviewId"), out var reviewId) || reviewId <= 0)
        {
            ThrowError("Invalid review id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _reviews.ToggleApprovalAsync(reviewId, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}

public sealed class DeleteAdminReviewEndpoint : EndpointWithoutRequest
{
    private readonly IReviewService _reviews;

    public DeleteAdminReviewEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Delete("admin/reviews/{reviewId}");
        Policies(AuthPolicies.Admin);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("reviewId"), out var reviewId) || reviewId <= 0)
        {
            ThrowError("Invalid review id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _reviews.DeleteReviewAsync(reviewId, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}
