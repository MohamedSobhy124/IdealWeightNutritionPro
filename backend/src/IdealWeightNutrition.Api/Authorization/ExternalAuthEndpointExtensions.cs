using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Api.Authorization;

public static class ExternalAuthEndpointExtensions
{
    public static WebApplication MapExternalAuthEndpoints(this WebApplication app)
    {
        app.MapGet("/api/auth/external/google", (HttpContext ctx, IOptions<AppUrlOptions> urls) =>
        {
            var returnUrl = ctx.Request.Query["returnUrl"].ToString();
            if (string.IsNullOrWhiteSpace(returnUrl))
                returnUrl = $"{urls.Value.FrontendBaseUrl.TrimEnd('/')}/auth/oauth-callback";

            var props = new AuthenticationProperties
            {
                RedirectUri = "/api/auth/external/google/callback",
                Items = { ["returnUrl"] = returnUrl }
            };

            return Results.Challenge(props, [GoogleDefaults.AuthenticationScheme]);
        }).AllowAnonymous();

        app.MapGet("/api/auth/external/google/callback", async (
            HttpContext ctx,
            IAuthService auth,
            ICartService cart,
            IOptions<AppUrlOptions> urls,
            CancellationToken ct) =>
        {
            var authenticate = await ctx.AuthenticateAsync(ExternalAuthSchemes.ExternalCookie);
            if (!authenticate.Succeeded || authenticate.Principal is null)
            {
                var failUrl = $"{urls.Value.FrontendBaseUrl.TrimEnd('/')}/auth/login?error=google";
                return Results.Redirect(failUrl);
            }

            var result = await auth.LoginWithGoogleAsync(authenticate.Principal, ct);
            await ctx.SignOutAsync(ExternalAuthSchemes.ExternalCookie);

            if (!result.Succeeded || result.Value is null)
            {
                var failUrl = $"{urls.Value.FrontendBaseUrl.TrimEnd('/')}/auth/login?error=google";
                return Results.Redirect(failUrl);
            }

            await AuthCartMerge.MergeGuestCartIfPresentAsync(ctx, cart, result.Value.UserId, ct);

            var returnUrl = authenticate.Properties?.Items["returnUrl"]
                ?? $"{urls.Value.FrontendBaseUrl.TrimEnd('/')}/auth/oauth-callback";

            var redirect = QueryHelpers.AddQueryString(returnUrl, new Dictionary<string, string?>
            {
                ["accessToken"] = result.Value.AccessToken,
                ["refreshToken"] = result.Value.RefreshToken,
                ["expiresAt"] = result.Value.ExpiresAt.ToString("O")
            });

            return Results.Redirect(redirect);
        }).AllowAnonymous();

        return app;
    }
}
