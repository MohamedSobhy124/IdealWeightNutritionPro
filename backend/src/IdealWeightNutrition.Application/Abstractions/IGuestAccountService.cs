namespace IdealWeightNutrition.Application.Abstractions;

public sealed class GuestAccountResult
{
    public string? UserId { get; init; }
    public bool LinkedExistingAccount { get; init; }
    public bool CreatedNewAccount { get; init; }
}

public interface IGuestAccountService
{
    Task<GuestAccountResult> ResolveOrCreateAsync(
        string email,
        string fullName,
        string? phoneNumber,
        string? streetAddress,
        string? city,
        string? state,
        string? postalCode,
        bool createAccountIfMissing,
        CancellationToken cancellationToken = default);
}
