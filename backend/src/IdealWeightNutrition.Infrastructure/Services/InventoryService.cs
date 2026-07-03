using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Cart;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class InventoryService : IInventoryService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IAdminNotificationService _adminNotifications;
    private readonly IStockNotificationFulfillmentService _stockFulfillment;

    public InventoryService(
        AppDbContext db,
        IDateTimeProvider clock,
        IAdminNotificationService adminNotifications,
        IStockNotificationFulfillmentService stockFulfillment)
    {
        _db = db;
        _clock = clock;
        _adminNotifications = adminNotifications;
        _stockFulfillment = stockFulfillment;
    }

    public async Task EnsureStockAvailableAsync(
        IReadOnlyList<CartItemDto> items,
        CancellationToken cancellationToken = default)
    {
        if (items.Count == 0)
            return;

        var productIds = items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var flashIds = items
            .Where(i => i.FlashSaleItemId is > 0)
            .Select(i => i.FlashSaleItemId!.Value)
            .Distinct()
            .ToList();

        var flashItems = flashIds.Count == 0
            ? new Dictionary<int, Domain.Promotions.FlashSaleItem>()
            : await _db.FlashSaleItems
                .AsNoTracking()
                .Include(f => f.FlashSale)
                .Where(f => flashIds.Contains(f.Id) && !f.IsDeleted)
                .ToDictionaryAsync(f => f.Id, cancellationToken);

        var now = _clock.Now;

        var comboIds = items
            .Where(i => i.ComboOfferId is > 0)
            .Select(i => i.ComboOfferId!.Value)
            .Distinct()
            .ToList();

        var combos = comboIds.Count == 0
            ? new Dictionary<int, ComboOffer>()
            : await _db.ComboOffers
                .AsNoTracking()
                .Include(c => c.Items.Where(i => !i.IsDeleted))
                .Where(c => comboIds.Contains(c.Id) && !c.IsDeleted && c.IsActive)
                .ToDictionaryAsync(c => c.Id, cancellationToken);

        var comboProductIds = combos.Values
            .SelectMany(c => c.Items.Select(i => i.ProductId))
            .Distinct()
            .ToList();

        var comboProducts = comboProductIds.Count == 0
            ? new Dictionary<int, Product>()
            : await _db.Products
                .AsNoTracking()
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .Where(p => comboProductIds.Contains(p.Id) && !p.IsDeleted)
                .ToDictionaryAsync(p => p.Id, cancellationToken);

        foreach (var item in items)
        {
            if (item.ComboOfferId is > 0)
            {
                if (!combos.TryGetValue(item.ComboOfferId.Value, out var combo))
                    throw new InvalidOperationException($"\"{item.Title}\" combo offer is no longer available.");

                if (now < combo.StartDate || now > combo.EndDate)
                    throw new InvalidOperationException($"\"{item.Title}\" combo offer has ended.");

                if (!ComboOfferService.IsInStock(combo, comboProducts))
                    throw new InvalidOperationException($"\"{item.Title}\" is out of stock.");

                var maxQty = ComboOfferService.CalculateMaxQuantity(combo, comboProducts);
                if (item.Quantity > maxQty)
                {
                    throw new InvalidOperationException(
                        $"Only {Math.Max(maxQty, 0)} combo(s) of \"{item.Title}\" are available.");
                }

                continue;
            }

            if (!products.TryGetValue(item.ProductId, out var product))
                throw new InvalidOperationException($"\"{item.Title}\" is no longer available.");

            if (item.FlashSaleItemId is > 0)
            {
                if (!flashItems.TryGetValue(item.FlashSaleItemId.Value, out var flashItem))
                    throw new InvalidOperationException($"\"{item.Title}\" flash sale offer is no longer available.");

                var sale = flashItem.FlashSale;
                if (sale is null || sale.IsDeleted || !sale.IsActive || now < sale.StartDate || now > sale.EndDate)
                    throw new InvalidOperationException($"\"{item.Title}\" flash sale has ended.");

                if (flashItem.FlashSaleQuantity < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Only {Math.Max(flashItem.FlashSaleQuantity, 0)} item(s) of \"{item.Title}\" are available at the flash sale price.");
                }
            }

            if (item.ProductVariantId is > 0)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == item.ProductVariantId);
                if (variant is null)
                    throw new InvalidOperationException($"\"{item.Title}\" variant is no longer available.");

                if (variant.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Only {Math.Max(variant.StockQuantity, 0)} item(s) of \"{item.Title}\" are available.");
                }
            }
            else if (product.ProductType == ProductType.Variable)
            {
                var defaultVariantId = ResolveDefaultVariantId(product);
                if (defaultVariantId is null)
                    throw new InvalidOperationException($"\"{item.Title}\" is out of stock.");

                var variant = product.Variants.FirstOrDefault(v => v.Id == defaultVariantId);
                if (variant is null || variant.StockQuantity < item.Quantity)
                {
                    throw new InvalidOperationException(
                        $"Only {Math.Max(variant?.StockQuantity ?? 0, 0)} item(s) of \"{item.Title}\" are available.");
                }
            }
            else if (product.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException(
                    $"Only {Math.Max(product.StockQuantity, 0)} item(s) of \"{item.Title}\" are available.");
            }

            if (product.StockQuantity < item.Quantity)
            {
                throw new InvalidOperationException(
                    $"Only {Math.Max(product.StockQuantity, 0)} item(s) of \"{item.Title}\" are available.");
            }
        }
    }

    public async Task DeductStockForOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var details = await _db.OrderDetails
            .Where(d => d.OrderHeaderId == orderId)
            .ToListAsync(cancellationToken);

        if (details.Count == 0)
            return;

        var productIds = details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var flashIds = details
            .Where(d => d.FlashSaleItemId is > 0)
            .Select(d => d.FlashSaleItemId!.Value)
            .Distinct()
            .ToList();

        var flashItems = flashIds.Count == 0
            ? new Dictionary<int, FlashSaleItem>()
            : await _db.FlashSaleItems
                .Where(f => flashIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

        var comboIds = details
            .Where(d => d.ComboOfferId is > 0)
            .Select(d => d.ComboOfferId!.Value)
            .Distinct()
            .ToList();

        var combos = comboIds.Count == 0
            ? new Dictionary<int, ComboOffer>()
            : await _db.ComboOffers
                .Include(c => c.Items.Where(i => !i.IsDeleted))
                .Where(c => comboIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);

        var comboProductIds = combos.Values
            .SelectMany(c => c.Items.Select(i => i.ProductId))
            .Where(id => !products.ContainsKey(id))
            .Distinct()
            .ToList();

        if (comboProductIds.Count > 0)
        {
            var extraProducts = await _db.Products
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .Where(p => comboProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            foreach (var pair in extraProducts)
                products[pair.Key] = pair.Value;
        }

        foreach (var detail in details)
        {
            if (detail.ComboOfferId is > 0
                && combos.TryGetValue(detail.ComboOfferId.Value, out var combo))
            {
                foreach (var comboItem in combo.Items.Where(i => !i.IsDeleted))
                {
                    var totalNeeded = comboItem.Quantity * detail.Count;
                    if (!products.TryGetValue(comboItem.ProductId, out var comboProduct))
                        continue;

                    if (comboItem.ProductVariantId is > 0)
                    {
                        var variant = comboProduct.Variants.FirstOrDefault(v => v.Id == comboItem.ProductVariantId);
                        if (variant is not null)
                            variant.StockQuantity = Math.Max(0, variant.StockQuantity - totalNeeded);
                    }
                    else
                    {
                        comboProduct.StockQuantity = Math.Max(0, comboProduct.StockQuantity - totalNeeded);
                    }
                }

                continue;
            }

            if (detail.FlashSaleItemId is > 0
                && flashItems.TryGetValue(detail.FlashSaleItemId.Value, out var flashItem))
            {
                flashItem.FlashSaleQuantity = Math.Max(0, flashItem.FlashSaleQuantity - detail.Count);
            }

            if (!products.TryGetValue(detail.ProductId, out var product))
                continue;

            if (detail.ProductVariantId is > 0)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == detail.ProductVariantId);
                if (variant is not null)
                    variant.StockQuantity = Math.Max(0, variant.StockQuantity - detail.Count);
            }

            product.StockQuantity = Math.Max(0, product.StockQuantity - detail.Count);
        }

        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task RestoreStockForReturnAsync(int returnRequestId, CancellationToken cancellationToken = default)
    {
        var returnRequest = await _db.ReturnRequests
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.Id == returnRequestId, cancellationToken);

        if (returnRequest?.Items is null || returnRequest.Items.Count == 0)
            return;

        var detailIds = returnRequest.Items.Select(i => i.OrderDetailId).ToList();
        var orderDetails = await _db.OrderDetails
            .Where(d => detailIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        if (orderDetails.Count == 0)
            return;

        var productIds = orderDetails.Values.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var flashIds = orderDetails.Values
            .Where(d => d.FlashSaleItemId is > 0)
            .Select(d => d.FlashSaleItemId!.Value)
            .Distinct()
            .ToList();

        var flashItems = flashIds.Count == 0
            ? new Dictionary<int, FlashSaleItem>()
            : await _db.FlashSaleItems
                .Where(f => flashIds.Contains(f.Id))
                .ToDictionaryAsync(f => f.Id, cancellationToken);

        var comboIds = orderDetails.Values
            .Where(d => d.ComboOfferId is > 0)
            .Select(d => d.ComboOfferId!.Value)
            .Distinct()
            .ToList();

        var combos = comboIds.Count == 0
            ? new Dictionary<int, ComboOffer>()
            : await _db.ComboOffers
                .Include(c => c.Items.Where(i => !i.IsDeleted))
                .Where(c => comboIds.Contains(c.Id))
                .ToDictionaryAsync(c => c.Id, cancellationToken);

        var comboProductIds = combos.Values
            .SelectMany(c => c.Items.Select(i => i.ProductId))
            .Where(id => !products.ContainsKey(id))
            .Distinct()
            .ToList();

        if (comboProductIds.Count > 0)
        {
            var extraProducts = await _db.Products
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .Where(p => comboProductIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, cancellationToken);

            foreach (var pair in extraProducts)
                products[pair.Key] = pair.Value;
        }

        var productsToCheck = new HashSet<int>();
        var variantsToCheck = new HashSet<int>();

        foreach (var returnItem in returnRequest.Items)
        {
            if (!orderDetails.TryGetValue(returnItem.OrderDetailId, out var detail))
                continue;

            var qty = returnItem.Quantity;
            if (qty <= 0)
                continue;

            if (detail.ComboOfferId is > 0
                && combos.TryGetValue(detail.ComboOfferId.Value, out var combo))
            {
                foreach (var comboItem in combo.Items.Where(i => !i.IsDeleted))
                {
                    var totalRestore = comboItem.Quantity * qty;
                    if (!products.TryGetValue(comboItem.ProductId, out var comboProduct))
                        continue;

                    if (comboItem.ProductVariantId is > 0)
                    {
                        var variant = comboProduct.Variants.FirstOrDefault(v => v.Id == comboItem.ProductVariantId);
                        if (variant is not null)
                        {
                            variant.StockQuantity += totalRestore;
                            variantsToCheck.Add(variant.Id);
                        }
                    }
                    else
                    {
                        comboProduct.StockQuantity += totalRestore;
                    }

                    productsToCheck.Add(comboProduct.Id);
                }

                continue;
            }

            if (detail.FlashSaleItemId is > 0
                && flashItems.TryGetValue(detail.FlashSaleItemId.Value, out var flashItem))
            {
                flashItem.FlashSaleQuantity += qty;
            }

            if (!products.TryGetValue(detail.ProductId, out var product))
                continue;

            if (detail.ProductVariantId is > 0)
            {
                var variant = product.Variants.FirstOrDefault(v => v.Id == detail.ProductVariantId);
                if (variant is not null)
                {
                    variant.StockQuantity += qty;
                    variantsToCheck.Add(variant.Id);
                }
            }

            product.StockQuantity += qty;
            productsToCheck.Add(product.Id);
        }

        await _db.SaveChangesAsync(cancellationToken);

        foreach (var productId in productsToCheck)
        {
            await _adminNotifications.CheckProductStockLevelsAsync(productId, cancellationToken);
            await _stockFulfillment.ProcessProductRestockedAsync(productId, null, cancellationToken);
        }

        foreach (var variantId in variantsToCheck)
        {
            await _adminNotifications.CheckVariantStockLevelsAsync(variantId, cancellationToken);
            var variant = await _db.Set<ProductVariant>().AsNoTracking()
                .FirstOrDefaultAsync(v => v.Id == variantId, cancellationToken);
            if (variant is not null)
                await _stockFulfillment.ProcessProductRestockedAsync(variant.ProductId, variantId, cancellationToken);
        }
    }

    private static int? ResolveDefaultVariantId(Product product)
    {
        if (product.ProductType != ProductType.Variable)
            return null;

        var inStock = product.Variants
            .Where(v => !v.IsDeleted && v.StockQuantity > 0)
            .ToList();

        return inStock.Count == 0 ? null : inStock.MinBy(v => v.Price)!.Id;
    }
}
