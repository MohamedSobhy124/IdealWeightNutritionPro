namespace IdealWeightNutrition.Domain.Identity;

using IdealWeightNutrition.Domain.Time;

public sealed class RefreshToken
{
    public Guid Id { get; set; }
    public required string UserId { get; set; }
    public required string TokenHash { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByTokenHash { get; set; }
    public bool IsActive => RevokedAt is null && UaeDateTime.Now < ExpiresAt;
}
