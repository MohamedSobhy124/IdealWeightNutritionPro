using IdealWeightNutrition.Api.Features.Cart;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;

namespace IdealWeightNutrition.Api.Http;

internal static class AuthCartMerge
{
    public static async Task MergeGuestCartIfPresentAsync(
        HttpContext ctx,
        ICartService cart,
        string userId,
        CancellationToken ct)
    {
        var guestCartId = CartHttp.GetGuestCartId(ctx);
        if (string.IsNullOrEmpty(guestCartId))
            return;

        await cart.MergeGuestCartAsync(userId, guestCartId, ct);
        CartHttpExtensions.ClearGuestCartCookie(ctx);
    }
}
