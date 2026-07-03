using IdealWeightNutrition.Contracts.Auth;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IAuthService
{
    Task<AuthResult<AuthTokenResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthTokenResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthTokenResponse>> RefreshAsync(RefreshTokenRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<UserProfileResponse>> GetProfileAsync(string userId, CancellationToken cancellationToken = default);
    Task RevokeRefreshTokenAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthResult<MessageResponse>> RequestPasswordResetAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthResult<MessageResponse>> ResetPasswordAsync(ResetPasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<MessageResponse>> ChangePasswordAsync(string userId, ChangePasswordRequest request, CancellationToken cancellationToken = default);
    Task<AuthResult<PersonalDataExportResponse>> ExportPersonalDataAsync(string userId, CancellationToken cancellationToken = default);
    Task<AuthResult<MessageResponse>> DeletePersonalDataAsync(string userId, CancellationToken cancellationToken = default);
    Task<AuthResult<AuthTokenResponse>> LoginWithGoogleAsync(System.Security.Claims.ClaimsPrincipal principal, CancellationToken cancellationToken = default);
    Task<AuthResult<MessageResponse>> SendRegistrationOtpAsync(string email, CancellationToken cancellationToken = default);
    Task<AuthResult<MessageResponse>> VerifyRegistrationOtpAsync(string email, string otp, CancellationToken cancellationToken = default);
}

public sealed class AuthResult<T>
{
    public bool Succeeded { get; init; }
    public T? Value { get; init; }
    public string? Error { get; init; }
    public int StatusCode { get; init; } = 400;

    public static AuthResult<T> Ok(T value) => new() { Succeeded = true, Value = value };
    public static AuthResult<T> Fail(string error, int statusCode = 400) =>
        new() { Succeeded = false, Error = error, StatusCode = statusCode };
}
