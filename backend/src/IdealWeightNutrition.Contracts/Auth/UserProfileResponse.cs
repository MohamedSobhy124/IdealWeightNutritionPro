namespace IdealWeightNutrition.Contracts.Auth;

public sealed class UserProfileResponse
{
    public required string Id { get; init; }
    public required string Email { get; init; }
    public string? FullName { get; init; }
    public required IReadOnlyList<string> Roles { get; init; }
}
