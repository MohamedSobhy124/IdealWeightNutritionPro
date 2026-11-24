using IdealWeightNutrition.Infrastructure.Delivery;

namespace IdealWeightNutrition.Application.Tests.Delivery;

public class DeliveryCalculatorTests
{
    private static DeliveryLine Free(double minimum = 0) => new(true, true, minimum);
    private static DeliveryLine NotFree() => new(true, false, 0);

    [Fact]
    public void All_lines_free_and_subtotal_above_threshold_ships_free()
    {
        var quote = DeliveryCalculator.Calculate([Free(100), Free(50)], subtotal: 150, baseCharge: 25);

        Assert.True(quote.IsFree);
        Assert.Equal(0, quote.FinalCharge);
        Assert.Equal(25, quote.BaseCharge);
        Assert.Equal(100, quote.Threshold);
        Assert.Equal(0, quote.AmountRemainingForFree);
    }

    [Fact]
    public void Subtotal_exactly_at_threshold_ships_free()
    {
        var quote = DeliveryCalculator.Calculate([Free(200)], subtotal: 200, baseCharge: 25);
        Assert.True(quote.IsFree);
    }

    [Fact]
    public void All_lines_free_but_below_threshold_pays_and_reports_the_gap()
    {
        var quote = DeliveryCalculator.Calculate([Free(200)], subtotal: 149.5, baseCharge: 25);

        Assert.False(quote.IsFree);
        Assert.True(quote.AllLinesAllowFree);
        Assert.Equal(25, quote.FinalCharge);
        Assert.Equal(50.5, quote.AmountRemainingForFree);
    }

    [Fact]
    public void One_line_not_allowing_free_delivery_blocks_it_for_the_whole_cart()
    {
        var quote = DeliveryCalculator.Calculate([Free(0), NotFree()], subtotal: 10_000, baseCharge: 25);

        Assert.False(quote.IsFree);
        Assert.False(quote.AllLinesAllowFree);
        Assert.Null(quote.Threshold);
        Assert.Equal(25, quote.FinalCharge);
        Assert.Equal(0, quote.AmountRemainingForFree);
    }

    [Fact]
    public void Combo_and_product_with_different_minimums_use_the_highest()
    {
        var combo = Free(300);
        var product = Free(100);

        Assert.False(DeliveryCalculator.Calculate([combo, product], subtotal: 250, baseCharge: 20).IsFree);
        Assert.True(DeliveryCalculator.Calculate([combo, product], subtotal: 300, baseCharge: 20).IsFree);
        Assert.Equal(300, DeliveryCalculator.Calculate([combo, product], subtotal: 250, baseCharge: 20).Threshold);
    }

    [Fact]
    public void Missing_product_or_combo_never_allows_free_delivery()
    {
        var quote = DeliveryCalculator.Calculate([Free(0), DeliveryLine.Missing], subtotal: 500, baseCharge: 15);

        Assert.False(quote.IsFree);
        Assert.Equal(15, quote.FinalCharge);
    }

    [Fact]
    public void Zero_minimum_means_always_free_when_allowed()
    {
        var quote = DeliveryCalculator.Calculate([Free(0)], subtotal: 1, baseCharge: 30);
        Assert.True(quote.IsFree);
        Assert.Equal(0, quote.FinalCharge);
    }

    [Fact]
    public void Empty_cart_is_never_free()
    {
        var quote = DeliveryCalculator.Calculate([], subtotal: 0, baseCharge: 20);
        Assert.False(quote.IsFree);
        Assert.Equal(20, quote.FinalCharge);
    }

    [Fact]
    public void Negative_values_are_clamped()
    {
        var quote = DeliveryCalculator.Calculate([Free(-10)], subtotal: 0, baseCharge: -5);
        Assert.Equal(0, quote.BaseCharge);
        Assert.Equal(0, quote.Threshold);
        Assert.True(quote.IsFree);
    }
}
