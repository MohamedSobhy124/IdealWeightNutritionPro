using System.Security.Claims;

namespace IdealWeightNutrition.Api.Http;

internal static class CartHttp
{
    public const string GuestCartCookie = "iwn_cart_id";

    public static string? GetUserId(ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true
            ? user.FindFirstValue(ClaimTypes.NameIdentifier)
            : null;

    public static string? GetGuestCartId(HttpContext ctx) =>
        ctx.Request.Cookies[GuestCartCookie];
}
