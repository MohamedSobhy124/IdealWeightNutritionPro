using System.Security.Claims;
using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Auth;

namespace IdealWeightNutrition.Api.Features.Auth;

public sealed class LogoutEndpoint : Endpoint<RefreshTokenRequest>
{
    private readonly IAuthService _auth;

    public LogoutEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Post("auth/logout");
        Summary(s => s.Summary = "Revoke the current refresh token.");
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        await _auth.RevokeRefreshTokenAsync(req.RefreshToken, ct);
        await Send.NoContentAsync(ct);
    }
}

public sealed class GetProfileEndpoint : EndpointWithoutRequest<UserProfileResponse>
{
    private readonly IAuthService _auth;

    public GetProfileEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Get("auth/me");
        Summary(s => s.Summary = "Get the authenticated user profile.");
    }

    public override async Task HandleAsync(CancellationToken ct)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userId))
        {
            await Send.UnauthorizedAsync(ct);
            return;
        }

        var result = await _auth.GetProfileAsync(userId, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);

        await Send.OkAsync(result.Value!, ct);
    }
}
