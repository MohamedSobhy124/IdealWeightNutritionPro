using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Reviews;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Reviews;

public sealed class ListFeaturedReviewsRequest
{
    public int Count { get; init; } = 6;
}

public sealed class ListFeaturedReviewsEndpoint : Endpoint<ListFeaturedReviewsRequest, IReadOnlyList<FeaturedReviewDto>>
{
    private readonly IReviewService _reviews;

    public ListFeaturedReviewsEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Get("reviews/featured");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ListFeaturedReviewsRequest req, CancellationToken ct)
    {
        var items = await _reviews.ListFeaturedReviewsAsync(req.Count, ct);
        await Send.OkAsync(items, ct);
    }
}

public sealed class ListProductReviewsEndpoint : EndpointWithoutRequest<IReadOnlyList<ProductReviewDto>>
{
    private readonly IReviewService _reviews;

    public ListProductReviewsEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Get("products/{productId}/reviews");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
        {
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
            return;
        }

        var items = await _reviews.ListApprovedProductReviewsAsync(productId, ct);
        await Send.OkAsync(items, ct);
    }
}

public sealed class GetProductReviewSummaryEndpoint : EndpointWithoutRequest<ProductReviewSummaryDto>
{
    private readonly IReviewService _reviews;

    public GetProductReviewSummaryEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Get("products/{productId}/reviews/summary");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
        {
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
            return;
        }

        var summary = await _reviews.GetProductReviewSummaryAsync(productId, ct);
        await Send.OkAsync(summary, ct);
    }
}

public sealed class SubmitProductReviewEndpoint : Endpoint<SubmitProductReviewRequest, ProductReviewDto>
{
    private readonly IReviewService _reviews;

    public SubmitProductReviewEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Post("products/{productId}/reviews");
        Policies(AuthPolicies.Customer);
    }

    public override async Task HandleAsync(SubmitProductReviewRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("productId"), out var productId) || productId <= 0)
        {
            ThrowError("Invalid product id.", StatusCodes.Status400BadRequest);
            return;
        }

        var userId = CartHttp.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var review = await _reviews.SubmitProductReviewAsync(productId, userId, req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(review, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ListServiceReviewsEndpoint : EndpointWithoutRequest<IReadOnlyList<ProductReviewDto>>
{
    private readonly IReviewService _reviews;

    public ListServiceReviewsEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Get("services/{serviceId}/reviews");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("serviceId"), out var serviceId) || serviceId <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        await Send.OkAsync(await _reviews.ListApprovedServiceReviewsAsync(serviceId, ct), ct);
    }
}

public sealed class GetServiceReviewSummaryEndpoint : EndpointWithoutRequest<ProductReviewSummaryDto>
{
    private readonly IReviewService _reviews;

    public GetServiceReviewSummaryEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Get("services/{serviceId}/reviews/summary");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("serviceId"), out var serviceId) || serviceId <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        await Send.OkAsync(await _reviews.GetServiceReviewSummaryAsync(serviceId, ct), ct);
    }
}

public sealed class SubmitServiceReviewEndpoint : Endpoint<SubmitProductReviewRequest, ProductReviewDto>
{
    private readonly IReviewService _reviews;

    public SubmitServiceReviewEndpoint(IReviewService reviews) => _reviews = reviews;

    public override void Configure()
    {
        Post("services/{serviceId}/reviews");
        Policies(AuthPolicies.Customer);
    }

    public override async Task HandleAsync(SubmitProductReviewRequest req, CancellationToken ct)
    {
        if (!int.TryParse(Route<string>("serviceId"), out var serviceId) || serviceId <= 0)
        {
            ThrowError("Invalid service id.", StatusCodes.Status400BadRequest);
            return;
        }

        var userId = CartHttp.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            var review = await _reviews.SubmitServiceReviewAsync(serviceId, userId, req, ct);
            HttpContext.Response.StatusCode = StatusCodes.Status201Created;
            await Send.OkAsync(review, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}
