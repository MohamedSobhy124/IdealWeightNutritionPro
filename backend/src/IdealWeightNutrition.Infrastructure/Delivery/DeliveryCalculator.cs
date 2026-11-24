namespace IdealWeightNutrition.Infrastructure.Delivery;

/// <summary>Free-delivery settings of one cart line (a product or a combo offer).</summary>
/// <param name="Found">False when the product/combo no longer exists; such a line never allows free delivery.</param>
public sealed record DeliveryLine(bool Found, bool AllowFreeDelivery, double FreeDeliveryMinimumAmount)
{
    public static readonly DeliveryLine Missing = new(false, false, 0);
}

/// <summary>Result of a delivery calculation.</summary>
/// <param name="BaseCharge">City charge, or the remote area's charge when one is selected.</param>
/// <param name="AllLinesAllowFree">Every line allows free delivery (so a threshold applies).</param>
/// <param name="Threshold">Highest minimum among the lines; null when free delivery is not possible.</param>
/// <param name="IsFree">The order ships for free.</param>
/// <param name="FinalCharge">What the customer pays for delivery.</param>
/// <param name="AmountRemainingForFree">How much more subtotal unlocks free delivery; 0 when free or not possible.</param>
public sealed record DeliveryQuote(
    double BaseCharge,
    bool AllLinesAllowFree,
    double? Threshold,
    bool IsFree,
    double FinalCharge,
    double AmountRemainingForFree);

/// <summary>
/// The single delivery-charge rule used by both the checkout quote and order creation.
/// <list type="bullet">
/// <item>Free delivery is possible only when <b>every</b> line allows it.</item>
/// <item>The threshold is the <b>highest</b> minimum among the lines.</item>
/// <item>The threshold is compared with the subtotal <b>before</b> the promo discount.</item>
/// <item>A missing product or combo never allows free delivery.</item>
/// </list>
/// </summary>
public static class DeliveryCalculator
{
    public static DeliveryQuote Calculate(IReadOnlyCollection<DeliveryLine> lines, double subtotal, double baseCharge)
    {
        baseCharge = Math.Max(0, baseCharge);

        var allAllowFree = lines.Count > 0 && lines.All(l => l.Found && l.AllowFreeDelivery);
        if (!allAllowFree)
            return new DeliveryQuote(baseCharge, false, null, false, baseCharge, 0);

        var threshold = Math.Max(0, lines.Max(l => l.FreeDeliveryMinimumAmount));
        var isFree = subtotal >= threshold;
        var remaining = isFree ? 0 : Math.Round(threshold - subtotal, 2);

        return new DeliveryQuote(baseCharge, true, threshold, isFree, isFree ? 0 : baseCharge, remaining);
    }
}
