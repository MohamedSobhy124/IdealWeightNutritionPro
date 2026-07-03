namespace IdealWeightNutrition.Contracts.Cart;

public sealed class CartPromoDto
{
    public required int Id { get; init; }
    public required string Code { get; init; }
    public required string Description { get; init; }
    public required double DiscountAmount { get; init; }
}

public sealed class ApplyPromoRequest
{
    public required string Code { get; init; }
}

public sealed class PromoValidationResult
{
    public required bool IsValid { get; init; }
    public string? Message { get; init; }
    public CartPromoDto? Promo { get; init; }
    public double DiscountAmount { get; init; }
}
