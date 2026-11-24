using IdealWeightNutrition.Contracts.Cart;

namespace IdealWeightNutrition.Application.Abstractions;

public interface IPromoCodeService
{
    Task<PromoValidationResult> ValidateAndCalculateAsync(
        string code,
        IReadOnlyList<PromoCartLine> lines,
        string? userId,
        CancellationToken cancellationToken = default);

    /// <summary>Records that an order used a promo code. Safe to call more than once per order.</summary>
    Task RecordUsageAsync(int promoCodeId, string userId, int orderId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gives back the promo usage of an order that was cancelled before fulfilment, so it no
    /// longer counts towards the code's limits. Returns how many usages were released.
    /// </summary>
    Task<int> ReleaseUsageForOrderAsync(int orderId, CancellationToken cancellationToken = default);
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
