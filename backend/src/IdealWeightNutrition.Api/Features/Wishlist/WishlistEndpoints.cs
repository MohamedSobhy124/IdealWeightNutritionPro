using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Wishlist;
using AuthPolicies = IdealWeightNutrition.Domain.Constants.Policies;

namespace IdealWeightNutrition.Api.Features.Wishlist;

public sealed class GetWishlistEndpoint : EndpointWithoutRequest<WishlistResponse>
{
    private readonly IWishlistService _wishlist;

    public GetWishlistEndpoint(IWishlistService wishlist) => _wishlist = wishlist;

    public override void Configure()
    {
        Get("wishlist");
        Policies(AuthPolicies.Customer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = CartHttp.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(await _wishlist.GetWishlistAsync(userId, ct), ct);
    }
}

public sealed class GetWishlistProductIdsEndpoint : EndpointWithoutRequest<WishlistProductIdsResponse>
{
    private readonly IWishlistService _wishlist;

    public GetWishlistProductIdsEndpoint(IWishlistService wishlist) => _wishlist = wishlist;

    public override void Configure()
    {
        Get("wishlist/product-ids");
        Policies(AuthPolicies.Customer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = CartHttp.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        await Send.OkAsync(await _wishlist.GetProductIdsAsync(userId, ct), ct);
    }
}

public sealed class ToggleWishlistEndpoint : Endpoint<WishlistToggleRequest, WishlistToggleResponse>
{
    private readonly IWishlistService _wishlist;

    public ToggleWishlistEndpoint(IWishlistService wishlist) => _wishlist = wishlist;

    public override void Configure()
    {
        Post("wishlist/toggle");
        Policies(AuthPolicies.Customer);
    }

    public override async Task HandleAsync(WishlistToggleRequest req, CancellationToken ct)
    {
        var userId = CartHttp.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        try
        {
            await Send.OkAsync(await _wishlist.ToggleAsync(userId, req.ProductId, ct), ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RemoveWishlistItemEndpoint : EndpointWithoutRequest
{
    private readonly IWishlistService _wishlist;

    public RemoveWishlistItemEndpoint(IWishlistService wishlist) => _wishlist = wishlist;

    public override void Configure()
    {
        Delete("wishlist/{wishlistItemId}");
        Policies(AuthPolicies.Customer);
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = CartHttp.GetUserId(User);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        if (!int.TryParse(Route<string>("wishlistItemId"), out var wishlistItemId) || wishlistItemId <= 0)
        {
            ThrowError("Invalid wishlist item id.", StatusCodes.Status400BadRequest);
            return;
        }

        try
        {
            await _wishlist.RemoveAsync(userId, wishlistItemId, ct);
            await Send.NoContentAsync(ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status404NotFound);
        }
    }
}
