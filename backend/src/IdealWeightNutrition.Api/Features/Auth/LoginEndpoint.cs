using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Auth;

namespace IdealWeightNutrition.Api.Features.Auth;

public sealed class LoginEndpoint : Endpoint<LoginRequest, AuthTokenResponse>
{
    private readonly IAuthService _auth;
    private readonly ICartService _cart;

    public LoginEndpoint(IAuthService auth, ICartService cart)
    {
        _auth = auth;
        _cart = cart;
    }

    public override void Configure()
    {
        Post("auth/login");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
        Summary(s => s.Summary = "Authenticate with email and password.");
    }

    public override async Task HandleAsync(LoginRequest req, CancellationToken ct)
    {
        var result = await _auth.LoginAsync(req, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);

        await AuthCartMerge.MergeGuestCartIfPresentAsync(HttpContext, _cart, result.Value!.UserId, ct);
        await Send.OkAsync(result.Value!, ct);
    }
}
