namespace IdealWeightNutrition.Infrastructure.Options;

public sealed class JwtSettings
{
    public const string SectionName = "Jwt";

    public string Issuer { get; set; } = "ideal-weight-nutrition";
    public string Audience { get; set; } = "ideal-weight-nutrition-api";
    public string SigningKey { get; set; } = string.Empty;
    public int AccessTokenMinutes { get; set; } = 60;
    public int RefreshTokenDays { get; set; } = 7;
}
