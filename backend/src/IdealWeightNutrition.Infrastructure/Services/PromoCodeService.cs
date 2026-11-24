using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Cart;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class PromoCodeService : IPromoCodeService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public PromoCodeService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<PromoValidationResult> ValidateAndCalculateAsync(
        string code,
        IReadOnlyList<PromoCartLine> lines,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(code))
        {
            return Invalid("Please enter a promo code.");
        }

        var promo = await _db.PromoCodes
            .AsNoTracking()
            .Include(p => p.ExcludedProducts)
            .Include(p => p.ExcludedComboOffers)
            .FirstOrDefaultAsync(p => p.Code.ToLower() == code.Trim().ToLower(), cancellationToken);

        if (promo is null || !promo.IsActive)
            return Invalid("Invalid or inactive promo code.");

        var now = _clock.Now;
        if (now < promo.StartDate)
            return Invalid("This promo code is not valid yet.");
        if (now > promo.EndDate)
            return Invalid("This promo code has expired.");

        if (promo.UsageLimit is > 0 && promo.TimesUsed >= promo.UsageLimit)
            return Invalid("This promo code has reached its usage limit.");

        if (!string.IsNullOrEmpty(userId) && promo.UsageLimitPerUser is > 0)
        {
            var userUsage = await _db.PromoCodeUsages
                .AsNoTracking()
                .CountAsync(
                    u => u.PromoCodeId == promo.Id
                        && u.UserId == userId
                        && _db.OrderHeaders.Any(o => o.Id == u.OrderId && o.OrderStatus != OrderStatuses.Pending),
                    cancellationToken);

            if (userUsage >= promo.UsageLimitPerUser)
                return Invalid("You have already used this promo code the maximum number of times.");
        }

        var discount = CalculateDiscount(promo, lines, out var eligibleSubtotal);
        if (eligibleSubtotal <= 0)
            return Invalid("No items in your cart are eligible for this promo code.");

        if (promo.MinimumOrderAmount is > 0 && (decimal)eligibleSubtotal < promo.MinimumOrderAmount)
        {
            return Invalid($"Minimum order amount of {promo.MinimumOrderAmount:0.##} AED is required for eligible items.");
        }

        if (discount <= 0)
            return Invalid("This promo code does not apply to your cart.");

        return new PromoValidationResult
        {
            IsValid = true,
            Message = "Promo code applied.",
            DiscountAmount = discount,
            Promo = new CartPromoDto
            {
                Id = promo.Id,
                Code = promo.Code,
                Description = promo.Description,
                DiscountAmount = discount
            }
        };
    }

    public async Task RecordUsageAsync(int promoCodeId, string userId, int orderId, CancellationToken cancellationToken = default)
    {
        // Idempotent: an order uses a promo code at most once, however many confirmation
        // paths (return, webhook, background job) reach this.
        var alreadyRecorded = await _db.PromoCodeUsages
            .AnyAsync(u => u.OrderId == orderId && u.PromoCodeId == promoCodeId, cancellationToken);
        if (alreadyRecorded)
            return;

        _db.PromoCodeUsages.Add(new PromoCodeUsage
        {
            PromoCodeId = promoCodeId,
            UserId = userId,
            OrderId = orderId,
            UsedDate = _clock.Now
        });
        await _db.SaveChangesAsync(cancellationToken);

        // Atomic increment so concurrent orders cannot lose an update.
        await _db.PromoCodes
            .Where(p => p.Id == promoCodeId)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.TimesUsed, p => p.TimesUsed + 1), cancellationToken);
    }

    public async Task<int> ReleaseUsageForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var usages = await _db.PromoCodeUsages
            .Where(u => u.OrderId == orderId)
            .ToListAsync(cancellationToken);

        if (usages.Count == 0)
            return 0;

        _db.PromoCodeUsages.RemoveRange(usages);
        await _db.SaveChangesAsync(cancellationToken);

        foreach (var group in usages.GroupBy(u => u.PromoCodeId))
        {
            var count = group.Count();
            await _db.PromoCodes
                .Where(p => p.Id == group.Key)
                .ExecuteUpdateAsync(
                    s => s.SetProperty(p => p.TimesUsed, p => p.TimesUsed > count ? p.TimesUsed - count : 0),
                    cancellationToken);
        }

        return usages.Count;
    }

    private static PromoValidationResult Invalid(string message) =>
        new() { IsValid = false, Message = message };

    private static double CalculateDiscount(PromoCode promo, IReadOnlyList<PromoCartLine> lines, out double eligibleSubtotal)
    {
        var eligible = new List<(PromoCartLine line, double subtotal)>();
        eligibleSubtotal = 0;

        foreach (var line in lines)
        {
            if (!IsEligible(line, promo))
                continue;

            var subtotal = line.UnitPrice * line.Quantity;
            eligible.Add((line, subtotal));
            eligibleSubtotal += subtotal;
        }

        if (eligible.Count == 0 || eligibleSubtotal <= 0)
            return 0;

        if (promo.MinimumOrderAmount is > 0 && (decimal)eligibleSubtotal < promo.MinimumOrderAmount)
            return 0;

        double totalDiscount;

        if (promo.DiscountType == DiscountType.Percentage)
        {
            totalDiscount = eligible.Sum(e => e.subtotal) * (double)promo.DiscountValue / 100;
            if (promo.MaximumDiscountAmount is > 0 && (decimal)totalDiscount > promo.MaximumDiscountAmount)
                totalDiscount = (double)promo.MaximumDiscountAmount;
        }
        else
        {
            totalDiscount = Math.Min((double)promo.DiscountValue, eligibleSubtotal);
        }

        return Math.Round(totalDiscount, 2);
    }

    private static bool IsEligible(PromoCartLine line, PromoCode promo)
    {
        if (promo.ExcludedProducts.Any(e => e.ProductId == line.ProductId))
            return false;

        if (line.ComboOfferId is > 0 && promo.ExcludedComboOffers.Any(e => e.ComboOfferId == line.ComboOfferId))
            return false;

        if (promo.ExcludeDiscountedItems)
        {
            if (line.FlashSaleItemId is > 0 || line.ComboOfferId is > 0)
                return false;

            if (line.ListPrice > line.UnitPrice || line.ProductListPrice > line.UnitPrice)
                return false;
        }

        return true;
    }
}
