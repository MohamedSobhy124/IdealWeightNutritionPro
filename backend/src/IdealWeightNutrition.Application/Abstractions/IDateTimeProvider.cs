namespace IdealWeightNutrition.Application.Abstractions;

public interface IDateTimeProvider
{
    /// <summary>Current UAE local time (Asia/Dubai). Used for business rules and persisted timestamps.</summary>
    DateTime Now { get; }

    /// <summary>Actual UTC time. Use for protocol-level expiry (e.g. JWT).</summary>
    DateTime UtcNow { get; }
}
