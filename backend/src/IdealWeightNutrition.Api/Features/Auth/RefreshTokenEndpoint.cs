using FastEndpoints;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Auth;

namespace IdealWeightNutrition.Api.Features.Auth;

public sealed class RefreshTokenEndpoint : Endpoint<RefreshTokenRequest, AuthTokenResponse>
{
    private readonly IAuthService _auth;

    public RefreshTokenEndpoint(IAuthService auth) => _auth = auth;

    public override void Configure()
    {
        Post("auth/refresh");
        AllowAnonymous();
        Summary(s => s.Summary = "Rotate refresh token and issue a new access token.");
    }

    public override async Task HandleAsync(RefreshTokenRequest req, CancellationToken ct)
    {
        var result = await _auth.RefreshAsync(req, ct);
        if (!result.Succeeded)
            ThrowError(result.Error!, result.StatusCode);

        await Send.OkAsync(result.Value!, ct);
    }
}
