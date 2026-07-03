using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Promotions;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class ComboOfferService : IComboOfferService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public ComboOfferService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<ComboOfferSummaryDto>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        var combos = await _db.ComboOffers
            .AsNoTracking()
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .Where(c => !c.IsDeleted && c.IsActive && c.StartDate <= now && c.EndDate >= now)
            .OrderBy(c => c.DisplayOrder)
            .ThenBy(c => c.Id)
            .ToListAsync(cancellationToken);

        if (combos.Count == 0)
            return [];

        var products = await LoadProductsForCombosAsync(combos, cancellationToken);
        return combos.Select(c => MapSummary(c, products)).ToList();
    }

    public async Task<ComboOfferDetailDto?> GetActiveAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        var combo = await _db.ComboOffers
            .AsNoTracking()
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(
                c => c.Id == id && !c.IsDeleted && c.IsActive && c.StartDate <= now && c.EndDate >= now,
                cancellationToken);

        if (combo is null)
            return null;

        var products = await LoadProductsForCombosAsync([combo], cancellationToken);
        return MapDetail(combo, products);
    }

    private async Task<Dictionary<int, Product>> LoadProductsForCombosAsync(
        IEnumerable<ComboOffer> combos,
        CancellationToken cancellationToken)
    {
        var productIds = combos
            .SelectMany(c => c.Items)
            .Select(i => i.ProductId)
            .Distinct()
            .ToList();

        if (productIds.Count == 0)
            return new Dictionary<int, Product>();

        return await _db.Products
            .AsNoTracking()
            .Include(p => p.Images.Where(i => i.ImageInfo == null))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }

    private static ComboOfferSummaryDto MapSummary(ComboOffer combo, Dictionary<int, Product> products)
    {
        var original = CalculateOriginalPrice(combo, products);
        var comboPrice = (double)combo.ComboPrice;
        var inStock = IsInStock(combo, products);

        return new ComboOfferSummaryDto
        {
            Id = combo.Id,
            Name = combo.Name,
            NameAr = combo.NameAr,
            Description = combo.Description,
            ImageUrl = combo.ImageUrl,
            ComboPrice = comboPrice,
            OriginalPrice = original,
            SavingsPercent = original > 0 ? Math.Round((original - comboPrice) / original * 100, 1) : 0,
            StartDate = combo.StartDate,
            EndDate = combo.EndDate,
            ProductCount = combo.Items.Count(i => !i.IsDeleted),
            InStock = inStock
        };
    }

    private static ComboOfferDetailDto MapDetail(ComboOffer combo, Dictionary<int, Product> products)
    {
        var original = CalculateOriginalPrice(combo, products);
        var comboPrice = (double)combo.ComboPrice;
        var inStock = IsInStock(combo, products);
        var maxQty = CalculateMaxQuantity(combo, products);

        var items = combo.Items
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.Id)
            .Select(i =>
            {
                products.TryGetValue(i.ProductId, out var product);
                var title = product?.Title ?? $"Product #{i.ProductId}";
                var slug = product?.GetSlug() ?? i.ProductId.ToString();
                return new ComboOfferLineItemDto
                {
                    ProductId = i.ProductId,
                    ProductVariantId = i.ProductVariantId,
                    Title = title,
                    Slug = slug,
                    Quantity = i.Quantity,
                    IsRequired = i.IsRequired,
                    InStock = GetItemStock(i, products) >= i.Quantity
                };
            })
            .ToList();

        return new ComboOfferDetailDto
        {
            Id = combo.Id,
            Name = combo.Name,
            NameAr = combo.NameAr,
            Description = combo.Description,
            ImageUrl = combo.ImageUrl,
            ComboPrice = comboPrice,
            OriginalPrice = original,
            SavingsPercent = original > 0 ? Math.Round((original - comboPrice) / original * 100, 1) : 0,
            StartDate = combo.StartDate,
            EndDate = combo.EndDate,
            MaxQuantity = maxQty,
            InStock = inStock,
            Items = items
        };
    }

    internal static double CalculateOriginalPrice(ComboOffer combo, Dictionary<int, Product> products)
    {
        double total = 0;
        foreach (var item in combo.Items.Where(i => !i.IsDeleted))
        {
            if (!products.TryGetValue(item.ProductId, out var product))
                continue;

            var unit = item.ProductVariantId is > 0
                ? product.Variants.FirstOrDefault(v => v.Id == item.ProductVariantId && !v.IsDeleted) is { } variant
                    ? (double)variant.Price
                    : product.ListPrice
                : product.ListPrice;

            total += unit * item.Quantity;
        }

        return total;
    }

    internal static bool IsInStock(ComboOffer combo, Dictionary<int, Product> products) =>
        combo.Items
            .Where(i => !i.IsDeleted && i.IsRequired)
            .All(i => GetItemStock(i, products) >= i.Quantity);

    internal static int CalculateMaxQuantity(ComboOffer combo, Dictionary<int, Product> products)
    {
        var max = int.MaxValue;
        foreach (var item in combo.Items.Where(i => !i.IsDeleted && i.IsRequired))
        {
            var stock = GetItemStock(item, products);
            max = Math.Min(max, stock / Math.Max(item.Quantity, 1));
        }

        return max == int.MaxValue ? 0 : max;
    }

    internal static int GetItemStock(ComboOfferItem item, Dictionary<int, Product> products)
    {
        if (!products.TryGetValue(item.ProductId, out var product))
            return 0;

        if (item.ProductVariantId is > 0)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == item.ProductVariantId && !v.IsDeleted);
            return variant?.StockQuantity ?? 0;
        }

        return product.StockQuantity;
    }
}
