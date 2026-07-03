namespace IdealWeightNutrition.Contracts.Auth;

public sealed class LoginRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
}

public sealed class RegisterRequest
{
    public required string Email { get; init; }
    public required string Password { get; init; }
    public required string FullName { get; init; }
    public string? Phone { get; init; }
}

public sealed class VerifyOtpRequest
{
    public required string Email { get; init; }
    public required string Otp { get; init; }
}

public sealed class RefreshTokenRequest
{
    public required string RefreshToken { get; init; }
}

public sealed class ForgotPasswordRequest
{
    public required string Email { get; init; }
}

public sealed class ResetPasswordRequest
{
    public required string Email { get; init; }
    public required string Token { get; init; }
    public required string Password { get; init; }
}

public sealed class MessageResponse
{
    public required string Message { get; init; }
}

public sealed class ChangePasswordRequest
{
    public required string CurrentPassword { get; init; }
    public required string NewPassword { get; init; }
}

public sealed class PersonalDataExportResponse
{
    public required string Json { get; init; }
}

public sealed class AuthTokenResponse
{
    public required string UserId { get; init; }
    public required string AccessToken { get; init; }
    public required string RefreshToken { get; init; }
    public required DateTime ExpiresAt { get; init; }
}
