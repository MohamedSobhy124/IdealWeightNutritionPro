using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Cart;
using IdealWeightNutrition.Api.Http;

namespace IdealWeightNutrition.Api.Features.Cart;

internal static class CartHttpExtensions
{
    public static void SetGuestCartCookie(HttpContext ctx, string cartId)
    {
        var options = GuestCartCookieOptions(ctx);
        options.MaxAge = TimeSpan.FromDays(14);
        ctx.Response.Cookies.Append(CartHttp.GuestCartCookie, cartId, options);
    }

    public static void ClearGuestCartCookie(HttpContext ctx)
    {
        ctx.Response.Cookies.Delete(CartHttp.GuestCartCookie, GuestCartCookieOptions(ctx));
    }

    /// <summary>
    /// Local dev reaches the API through the storefront proxy, so Lax stays first-party.
    /// Production calls the API on another site, so the cookie has to be cross-site.
    /// </summary>
    private static CookieOptions GuestCartCookieOptions(HttpContext ctx)
    {
        var isDevelopment = ctx.RequestServices.GetRequiredService<IWebHostEnvironment>().IsDevelopment();
        return new CookieOptions
        {
            HttpOnly = true,
            Secure = !isDevelopment,
            SameSite = isDevelopment ? SameSiteMode.Lax : SameSiteMode.None,
            Path = "/",
        };
    }
}

public sealed class GetCartEndpoint : EndpointWithoutRequest<CartResponse>
{
    private readonly ICartService _cart;

    public GetCartEndpoint(ICartService cart) => _cart = cart;

    public override void Configure()
    {
        Get("cart");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _cart.GetCartAsync(
            CartHttp.GetUserId(User),
            CartHttp.GetGuestCartId(HttpContext),
            ct);
        await Send.OkAsync(result, ct);
    }
}

public sealed class AddCartItemEndpoint : Endpoint<AddCartItemRequest, CartResponse>
{
    private readonly ICartService _cart;

    public AddCartItemEndpoint(ICartService cart) => _cart = cart;

    public override void Configure()
    {
        Post("cart/items");
        AllowAnonymous();
    }

    public override async Task HandleAsync(AddCartItemRequest req, CancellationToken ct)
    {
        var userId = CartHttp.GetUserId(User);
        var guestCartId = CartHttp.GetGuestCartId(HttpContext);

        if (userId is null && string.IsNullOrEmpty(guestCartId))
        {
            guestCartId = _cart.CreateGuestCartId();
            CartHttpExtensions.SetGuestCartCookie(HttpContext, guestCartId);
        }

        try
        {
            var result = await _cart.AddItemAsync(userId, guestCartId, req, ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class UpdateCartItemEndpoint : Endpoint<UpdateCartItemRequest, CartResponse>
{
    private readonly ICartService _cart;

    public UpdateCartItemEndpoint(ICartService cart) => _cart = cart;

    public override void Configure()
    {
        Put("cart/items/{lineId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(UpdateCartItemRequest req, CancellationToken ct)
    {
        var lineId = Route<string>("lineId") ?? string.Empty;
        try
        {
            var result = await _cart.UpdateItemAsync(
                CartHttp.GetUserId(User),
                CartHttp.GetGuestCartId(HttpContext),
                lineId,
                req,
                ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RemoveCartItemEndpoint : EndpointWithoutRequest<CartResponse>
{
    private readonly ICartService _cart;

    public RemoveCartItemEndpoint(ICartService cart) => _cart = cart;

    public override void Configure()
    {
        Delete("cart/items/{lineId}");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var lineId = Route<string>("lineId") ?? string.Empty;
        try
        {
            var result = await _cart.RemoveItemAsync(
                CartHttp.GetUserId(User),
                CartHttp.GetGuestCartId(HttpContext),
                lineId,
                ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class ClearCartEndpoint : EndpointWithoutRequest<CartResponse>
{
    private readonly ICartService _cart;

    public ClearCartEndpoint(ICartService cart) => _cart = cart;

    public override void Configure()
    {
        Delete("cart");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _cart.ClearCartAsync(
            CartHttp.GetUserId(User),
            CartHttp.GetGuestCartId(HttpContext),
            ct);
        await Send.OkAsync(result, ct);
    }
}

public sealed class ApplyCartPromoEndpoint : Endpoint<ApplyPromoRequest, CartResponse>
{
    private readonly ICartService _cart;

    public ApplyCartPromoEndpoint(ICartService cart) => _cart = cart;

    public override void Configure()
    {
        Post("cart/promo");
        AllowAnonymous();
    }

    public override async Task HandleAsync(ApplyPromoRequest req, CancellationToken ct)
    {
        try
        {
            var result = await _cart.ApplyPromoAsync(
                CartHttp.GetUserId(User),
                CartHttp.GetGuestCartId(HttpContext),
                req,
                ct);
            await Send.OkAsync(result, ct);
        }
        catch (InvalidOperationException ex)
        {
            ThrowError(ex.Message, StatusCodes.Status400BadRequest);
        }
    }
}

public sealed class RemoveCartPromoEndpoint : EndpointWithoutRequest<CartResponse>
{
    private readonly ICartService _cart;

    public RemoveCartPromoEndpoint(ICartService cart) => _cart = cart;

    public override void Configure()
    {
        Delete("cart/promo");
        AllowAnonymous();
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var result = await _cart.RemovePromoAsync(
            CartHttp.GetUserId(User),
            CartHttp.GetGuestCartId(HttpContext),
            ct);
        await Send.OkAsync(result, ct);
    }
}
