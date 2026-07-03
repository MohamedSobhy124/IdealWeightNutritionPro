using IdealWeightNutrition.Contracts.Cart;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IPromoCodeService
{
    Task<PromoValidationResult> ValidateAndCalculateAsync(
        string code,
        IReadOnlyList<PromoCartLine> lines,
        string? userId,
        CancellationToken cancellationToken = default);

    Task RecordUsageAsync(int promoCodeId, string userId, int orderId, CancellationToken cancellationToken = default);
}

public sealed class PromoCartLine
{
    public required int ProductId { get; init; }
    public int? ProductVariantId { get; init; }
    public int? FlashSaleItemId { get; init; }
    public int? ComboOfferId { get; init; }
    public required double UnitPrice { get; init; }
    public required int Quantity { get; init; }
    public required double ListPrice { get; init; }
    public required double ProductListPrice { get; init; }
}
