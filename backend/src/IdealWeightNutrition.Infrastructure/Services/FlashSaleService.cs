using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Promotions;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class FlashSaleService : IFlashSaleService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public FlashSaleService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<IReadOnlyList<FlashSaleSummaryDto>> ListActiveAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        var sales = await _db.FlashSales
            .AsNoTracking()
            .Include(f => f.Items.Where(i => !i.IsDeleted && i.FlashSaleQuantity > 0))
            .Where(f => !f.IsDeleted && f.IsActive && f.StartDate <= now && f.EndDate >= now)
            .OrderBy(f => f.EndDate)
            .ToListAsync(cancellationToken);

        return sales.Select(MapSummary).ToList();
    }

    public async Task<FlashSaleDetailDto?> GetActiveAsync(
        int id,
        CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        var sale = await _db.FlashSales
            .AsNoTracking()
            .Include(f => f.Items.Where(i => !i.IsDeleted && i.FlashSaleQuantity > 0))
            .FirstOrDefaultAsync(
                f => f.Id == id && !f.IsDeleted && f.IsActive && f.StartDate <= now && f.EndDate >= now,
                cancellationToken);

        if (sale is null)
            return null;

        var productIds = sale.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Images.Where(i => i.ImageInfo == null))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var items = sale.Items
            .Select(i => MapItem(i, products))
            .Where(i => i is not null)
            .Cast<FlashSaleItemDto>()
            .ToList();

        return new FlashSaleDetailDto
        {
            Id = sale.Id,
            Name = sale.Name,
            NameAr = sale.NameAr,
            Description = sale.Description,
            ImageUrl = sale.ImageUrl,
            StartDate = sale.StartDate,
            EndDate = sale.EndDate,
            Items = items
        };
    }

    public async Task<IReadOnlyList<FlashSaleProductPriceDto>> ListActiveProductPricesAsync(
        CancellationToken cancellationToken = default)
    {
        var now = _clock.Now;
        return await _db.FlashSaleItems
            .AsNoTracking()
            .Where(i => !i.IsDeleted
                && i.FlashSaleQuantity > 0
                && i.FlashSale != null
                && !i.FlashSale.IsDeleted
                && i.FlashSale.IsActive
                && i.FlashSale.StartDate <= now
                && i.FlashSale.EndDate >= now)
            .Select(i => new FlashSaleProductPriceDto
            {
                ProductId = i.ProductId,
                ProductVariantId = i.ProductVariantId,
                FlashSalePrice = (double)i.FlashSalePrice
            })
            .ToListAsync(cancellationToken);
    }

    private static FlashSaleSummaryDto MapSummary(Domain.Promotions.FlashSale sale) =>
        new()
        {
            Id = sale.Id,
            Name = sale.Name,
            NameAr = sale.NameAr,
            Description = sale.Description,
            ImageUrl = sale.ImageUrl,
            StartDate = sale.StartDate,
            EndDate = sale.EndDate,
            ProductCount = sale.Items.Count(i => !i.IsDeleted && i.FlashSaleQuantity > 0)
        };

    private static FlashSaleItemDto? MapItem(
        Domain.Promotions.FlashSaleItem item,
        Dictionary<int, Product> products)
    {
        if (!products.TryGetValue(item.ProductId, out var product))
            return null;

        var normalPrice = ResolveNormalPrice(product, item.ProductVariantId);
        var flashPrice = (double)item.FlashSalePrice;
        var discount = normalPrice > 0
            ? Math.Round((normalPrice - flashPrice) / normalPrice * 100, 1)
            : 0;

        var image = product.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl ?? product.ImageUrl;

        return new FlashSaleItemDto
        {
            Id = item.Id,
            ProductId = item.ProductId,
            ProductVariantId = item.ProductVariantId,
            Title = product.Title,
            Slug = product.GetSlug(),
            ImageUrl = image,
            NormalPrice = normalPrice,
            FlashSalePrice = flashPrice,
            AvailableQuantity = item.FlashSaleQuantity,
            DiscountPercent = discount,
            ProductType = product.ProductType.ToString()
        };
    }

    private static double ResolveNormalPrice(Product product, int? variantId)
    {
        if (variantId is > 0)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);
            if (variant is not null)
                return (double)variant.Price;
        }

        return product.Price;
    }
}
