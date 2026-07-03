using FastEndpoints;
using IdealWeightNutrition.Api.Http;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Auth;

namespace IdealWeightNutrition.Api.Features.Auth;

public sealed class RegisterEndpoint : Endpoint<RegisterRequest, AuthTokenResponse>
{
    private readonly IAuthService _auth;
    private readonly ICartService _cart;

    public RegisterEndpoint(IAuthService auth, ICartService cart)
    {
        _auth = auth;
        _cart = cart;
    }

    public override void Configure()
    {
        Post("auth/register");
        AllowAnonymous();
        Options(o => o.RequireRateLimiting(RateLimitPolicies.Auth));
        Summary(s => s.Summary = "Register a new customer account.");
    }

    public override async Task HandleAsync(RegisterRequest req, CancellationToken ct)
    {
        var result = await _auth.RegisterAsync(req, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);

        await AuthCartMerge.MergeGuestCartIfPresentAsync(HttpContext, _cart, result.Value!.UserId, ct);

        HttpContext.Response.StatusCode = StatusCodes.Status201Created;
        await Send.OkAsync(result.Value!, ct);
    }
}
