using System.Text.Json;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Cart;
using IdealWeightNutrition.Domain.Cart;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class GuestCartStore(IDistributedCache cache)
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    private static string Key(string cartId) => $"guest-cart:{cartId}";

    public async Task<GuestCart> GetAsync(string cartId, CancellationToken ct)
    {
        var json = await cache.GetStringAsync(Key(cartId), ct);
        return string.IsNullOrEmpty(json)
            ? new GuestCart()
            : JsonSerializer.Deserialize<GuestCart>(json, JsonOptions) ?? new GuestCart();
    }

    public Task SaveAsync(string cartId, GuestCart cart, CancellationToken ct)
    {
        var json = JsonSerializer.Serialize(cart, JsonOptions);
        return cache.SetStringAsync(
            Key(cartId),
            json,
            new DistributedCacheEntryOptions { SlidingExpiration = TimeSpan.FromDays(14) },
            ct);
    }

    public Task RemoveAsync(string cartId, CancellationToken ct) =>
        cache.RemoveAsync(Key(cartId), ct);
}

internal sealed class CartService : ICartService
{
    private readonly AppDbContext _db;
    private readonly GuestCartStore _guestCarts;
    private readonly AppliedPromoStore _promos;
    private readonly IPromoCodeService _promoCodes;
    private readonly IDateTimeProvider _clock;

    public CartService(
        AppDbContext db,
        GuestCartStore guestCarts,
        AppliedPromoStore promos,
        IPromoCodeService promoCodes,
        IDateTimeProvider clock)
    {
        _db = db;
        _guestCarts = guestCarts;
        _promos = promos;
        _promoCodes = promoCodes;
        _clock = clock;
    }

    public string CreateGuestCartId() => Guid.NewGuid().ToString("N");

    public Task<CartResponse> GetCartAsync(string? userId, string? guestCartId, CancellationToken ct = default) =>
        userId is not null ? GetUserCartAsync(userId, ct) : GetGuestCartAsync(guestCartId, ct);

    public async Task<CartResponse> AddItemAsync(
        string? userId,
        string? guestCartId,
        AddCartItemRequest request,
        CancellationToken ct = default)
    {
        if (request.Quantity < 1)
            throw new InvalidOperationException("Quantity must be at least 1.");

        if (request.ComboOfferId is > 0)
        {
            if (request.FlashSaleItemId is > 0)
                throw new InvalidOperationException("Cannot combine flash sale and combo offer on one line.");
            return await AddComboItemAsync(userId, guestCartId, request.ComboOfferId.Value, request.Quantity, ct);
        }

        var flash = request.FlashSaleItemId is > 0
            ? await ResolveFlashSaleContextAsync(
                request.FlashSaleItemId.Value,
                request.ProductId,
                request.ProductVariantId,
                ct)
            : null;

        var product = await LoadProductAsync(request.ProductId, ct)
            ?? throw new InvalidOperationException("Product not found.");

        var variantId = ResolveVariantId(product, request.ProductVariantId);

        ValidateProductStock(product, variantId, request.Quantity, flash?.MaxQuantity);

        var flashSaleItemId = flash?.FlashSaleItemId;

        if (userId is not null)
        {
            var existing = await _db.ShoppingCartLines.FirstOrDefaultAsync(
                c => c.ApplicationUserId == userId
                    && c.ProductId == request.ProductId
                    && c.ProductVariantId == variantId
                    && c.ComboOfferId == null
                    && c.FlashSaleItemId == flashSaleItemId,
                ct);

            if (existing is not null)
            {
                existing.Count += request.Quantity;
                ValidateProductStock(product, variantId, existing.Count, flash?.MaxQuantity);
            }
            else
            {
                _db.ShoppingCartLines.Add(new ShoppingCartLine
                {
                    ProductId = request.ProductId,
                    Count = request.Quantity,
                    ApplicationUserId = userId,
                    ProductVariantId = variantId,
                    FlashSaleItemId = flash?.FlashSaleItemId,
                    FlashSalePrice = flash?.FlashSalePrice
                });
            }

            await _db.SaveChangesAsync(ct);
            return await GetUserCartAsync(userId, ct);
        }

        guestCartId ??= CreateGuestCartId();
        var guest = await _guestCarts.GetAsync(guestCartId, ct);
        var line = guest.Items.FirstOrDefault(i =>
            i.ProductId == request.ProductId
            && i.ProductVariantId == variantId
            && i.ComboOfferId == null
            && i.FlashSaleItemId == flashSaleItemId);

        if (line is not null)
        {
            line.Quantity += request.Quantity;
            ValidateProductStock(product, variantId, line.Quantity, flash?.MaxQuantity);
        }
        else
        {
            guest.Items.Add(new GuestCartLine
            {
                LineId = Guid.NewGuid(),
                ProductId = request.ProductId,
                ProductVariantId = variantId,
                FlashSaleItemId = flash?.FlashSaleItemId,
                FlashSalePrice = flash?.FlashSalePrice,
                Quantity = request.Quantity
            });
        }

        await _guestCarts.SaveAsync(guestCartId, guest, ct);
        return await BuildGuestResponseAsync(guestCartId, guest, ct);
    }

    private async Task<CartResponse> AddComboItemAsync(
        string? userId,
        string? guestCartId,
        int comboOfferId,
        int quantity,
        CancellationToken ct)
    {
        var (combo, firstItem, maxQty) = await ResolveComboOfferAsync(comboOfferId, ct);

        if (userId is not null)
        {
            var existing = await _db.ShoppingCartLines.FirstOrDefaultAsync(
                c => c.ApplicationUserId == userId && c.ComboOfferId == comboOfferId,
                ct);

            var newQty = (existing?.Count ?? 0) + quantity;
            if (newQty > maxQty)
                throw new InvalidOperationException($"Only {maxQty} combo(s) available.");

            if (existing is not null)
                existing.Count = newQty;
            else
            {
                _db.ShoppingCartLines.Add(new ShoppingCartLine
                {
                    ProductId = firstItem.ProductId,
                    ProductVariantId = firstItem.ProductVariantId,
                    Count = quantity,
                    ApplicationUserId = userId,
                    ComboOfferId = comboOfferId
                });
            }

            await _db.SaveChangesAsync(ct);
            return await GetUserCartAsync(userId, ct);
        }

        guestCartId ??= CreateGuestCartId();
        var guest = await _guestCarts.GetAsync(guestCartId, ct);
        var line = guest.Items.FirstOrDefault(i => i.ComboOfferId == comboOfferId);

        var guestNewQty = (line?.Quantity ?? 0) + quantity;
        if (guestNewQty > maxQty)
            throw new InvalidOperationException($"Only {maxQty} combo(s) available.");

        if (line is not null)
            line.Quantity = guestNewQty;
        else
        {
            guest.Items.Add(new GuestCartLine
            {
                LineId = Guid.NewGuid(),
                ProductId = firstItem.ProductId,
                ProductVariantId = firstItem.ProductVariantId,
                ComboOfferId = comboOfferId,
                Quantity = quantity
            });
        }

        await _guestCarts.SaveAsync(guestCartId, guest, ct);
        return await BuildGuestResponseAsync(guestCartId, guest, ct);
    }

    public async Task<CartResponse> UpdateItemAsync(
        string? userId,
        string? guestCartId,
        string lineId,
        UpdateCartItemRequest request,
        CancellationToken ct = default)
    {
        if (userId is not null)
        {
            if (!int.TryParse(lineId, out var dbId))
                throw new InvalidOperationException("Invalid cart line.");

            var line = await _db.ShoppingCartLines.FirstOrDefaultAsync(
                c => c.Id == dbId && c.ApplicationUserId == userId, ct)
                ?? throw new InvalidOperationException("Cart item not found.");

            if (request.Quantity <= 0)
            {
                _db.ShoppingCartLines.Remove(line);
            }
            else
            {
                if (line.ComboOfferId is > 0)
                {
                    var (_, _, maxQty) = await ResolveComboOfferAsync(line.ComboOfferId.Value, ct);
                    if (request.Quantity > maxQty)
                        throw new InvalidOperationException($"Only {maxQty} combo(s) available.");
                }
                else
                {
                    var product = await LoadProductAsync(line.ProductId, ct)
                        ?? throw new InvalidOperationException("Product not found.");
                    var flashMax = await GetFlashSaleMaxQuantityAsync(line.FlashSaleItemId, ct);
                    ValidateProductStock(product, line.ProductVariantId, request.Quantity, flashMax);
                }

                line.Count = request.Quantity;
            }

            await _db.SaveChangesAsync(ct);
            return await GetUserCartAsync(userId, ct);
        }

        if (string.IsNullOrEmpty(guestCartId))
            throw new InvalidOperationException("Cart not found.");

        var guest = await _guestCarts.GetAsync(guestCartId, ct);
        var guestLine = guest.Items.FirstOrDefault(i => i.LineId.ToString() == lineId)
            ?? throw new InvalidOperationException("Cart item not found.");

        if (request.Quantity <= 0)
            guest.Items.Remove(guestLine);
        else
        {
            if (guestLine.ComboOfferId is > 0)
            {
                var (_, _, maxQty) = await ResolveComboOfferAsync(guestLine.ComboOfferId.Value, ct);
                if (request.Quantity > maxQty)
                    throw new InvalidOperationException($"Only {maxQty} combo(s) available.");
            }
            else
            {
                var product = await LoadProductAsync(guestLine.ProductId, ct)
                    ?? throw new InvalidOperationException("Product not found.");
                var flashMax = await GetFlashSaleMaxQuantityAsync(guestLine.FlashSaleItemId, ct);
                ValidateProductStock(product, guestLine.ProductVariantId, request.Quantity, flashMax);
            }

            guestLine.Quantity = request.Quantity;
        }

        await _guestCarts.SaveAsync(guestCartId, guest, ct);
        return await BuildGuestResponseAsync(guestCartId, guest, ct);
    }

    public async Task<CartResponse> RemoveItemAsync(
        string? userId,
        string? guestCartId,
        string lineId,
        CancellationToken ct = default) =>
        await UpdateItemAsync(userId, guestCartId, lineId, new UpdateCartItemRequest { Quantity = 0 }, ct);

    public async Task<CartResponse> ClearCartAsync(
        string? userId,
        string? guestCartId,
        CancellationToken ct = default)
    {
        if (userId is not null)
        {
            var lines = await _db.ShoppingCartLines.Where(c => c.ApplicationUserId == userId).ToListAsync(ct);
            _db.ShoppingCartLines.RemoveRange(lines);
            await _db.SaveChangesAsync(ct);
            await ClearStoredPromoAsync(userId, guestCartId, ct);
            return EmptyCart();
        }

        if (!string.IsNullOrEmpty(guestCartId))
        {
            await _guestCarts.RemoveAsync(guestCartId, ct);
            await ClearStoredPromoAsync(userId, guestCartId, ct);
        }

        return EmptyCart();
    }

    public async Task<CartResponse> ApplyPromoAsync(
        string? userId,
        string? guestCartId,
        ApplyPromoRequest request,
        CancellationToken ct = default)
    {
        var (lines, products) = await LoadCartSourcesAsync(userId, guestCartId, ct);
        if (lines.Count == 0)
            throw new InvalidOperationException("Your cart is empty.");

        var combos = await LoadCombosForLinesAsync(lines, ct);
        var promoLines = BuildPromoLines(lines, products, combos);
        var validation = await _promoCodes.ValidateAndCalculateAsync(request.Code, promoLines, userId, ct);
        if (!validation.IsValid)
            throw new InvalidOperationException(validation.Message ?? "Invalid promo code.");

        var key = GetPromoKey(userId, guestCartId);
        if (key is null)
            throw new InvalidOperationException("Cart not found.");

        await _promos.SaveAsync(
            key.Value.scope,
            key.Value.id,
            new AppliedPromo { PromoCodeId = validation.Promo!.Id, Code = validation.Promo.Code },
            ct);

        return await BuildCartResponseAsync(lines, products, userId, guestCartId, ct);
    }

    public async Task<CartResponse> RemovePromoAsync(
        string? userId,
        string? guestCartId,
        CancellationToken ct = default)
    {
        await ClearStoredPromoAsync(userId, guestCartId, ct);
        return await GetCartAsync(userId, guestCartId, ct);
    }

    public async Task<CartResponse> MergeGuestCartAsync(
        string userId,
        string? guestCartId,
        CancellationToken ct = default)
    {
        if (string.IsNullOrEmpty(guestCartId))
            return await GetUserCartAsync(userId, ct);

        var guest = await _guestCarts.GetAsync(guestCartId, ct);
        if (guest.Items.Count == 0)
        {
            await _guestCarts.RemoveAsync(guestCartId, ct);
            return await GetUserCartAsync(userId, ct);
        }

        var userLines = await _db.ShoppingCartLines
            .Where(c => c.ApplicationUserId == userId)
            .ToListAsync(ct);

        foreach (var guestItem in guest.Items)
        {
            if (guestItem.ComboOfferId is > 0)
            {
                try
                {
                    var (_, firstItem, maxQty) = await ResolveComboOfferAsync(guestItem.ComboOfferId.Value, ct);
                    var existingCombo = userLines.FirstOrDefault(l => l.ComboOfferId == guestItem.ComboOfferId);
                    var newQty = (existingCombo?.Count ?? 0) + guestItem.Quantity;
                    if (newQty > maxQty)
                        continue;

                    if (existingCombo is not null)
                        existingCombo.Count = newQty;
                    else
                    {
                        _db.ShoppingCartLines.Add(new ShoppingCartLine
                        {
                            ProductId = firstItem.ProductId,
                            ProductVariantId = firstItem.ProductVariantId,
                            Count = guestItem.Quantity,
                            ApplicationUserId = userId,
                            ComboOfferId = guestItem.ComboOfferId
                        });
                    }
                }
                catch (InvalidOperationException)
                {
                    continue;
                }

                continue;
            }

            var exists = userLines.Any(l =>
                l.ProductId == guestItem.ProductId
                && l.ProductVariantId == guestItem.ProductVariantId
                && l.ComboOfferId == null
                && l.FlashSaleItemId == guestItem.FlashSaleItemId);

            if (exists)
                continue;

            var product = await LoadProductAsync(guestItem.ProductId, ct);
            if (product is null)
                continue;

            var variantId = ResolveVariantId(product, guestItem.ProductVariantId);

            try
            {
                var flashMax = await GetFlashSaleMaxQuantityAsync(guestItem.FlashSaleItemId, ct);
                ValidateProductStock(product, variantId, guestItem.Quantity, flashMax);
            }
            catch (InvalidOperationException)
            {
                continue;
            }

            _db.ShoppingCartLines.Add(new ShoppingCartLine
            {
                ProductId = guestItem.ProductId,
                Count = guestItem.Quantity,
                ApplicationUserId = userId,
                ProductVariantId = variantId,
                FlashSaleItemId = guestItem.FlashSaleItemId,
                FlashSalePrice = guestItem.FlashSalePrice
            });
        }

        await _db.SaveChangesAsync(ct);

        var guestPromoKey = GetPromoKey(null, guestCartId);
        if (guestPromoKey is not null)
        {
            var guestPromo = await _promos.GetAsync(guestPromoKey.Value.scope, guestPromoKey.Value.id, ct);
            if (guestPromo is not null)
            {
                var userKey = GetPromoKey(userId, null);
                if (userKey is not null)
                {
                    await _promos.SaveAsync(userKey.Value.scope, userKey.Value.id, guestPromo, ct);
                    await _promos.RemoveAsync(guestPromoKey.Value.scope, guestPromoKey.Value.id, ct);
                }
            }
        }

        await _guestCarts.RemoveAsync(guestCartId, ct);
        return await GetUserCartAsync(userId, ct);
    }

    private async Task<(List<CartLineSource> lines, Dictionary<int, Product> products)> LoadCartSourcesAsync(
        string? userId,
        string? guestCartId,
        CancellationToken ct)
    {
        List<CartLineSource> lines;
        if (userId is not null)
        {
            var dbLines = await _db.ShoppingCartLines
                .AsNoTracking()
                .Where(c => c.ApplicationUserId == userId)
                .ToListAsync(ct);

            lines = dbLines.Select(l => new CartLineSource(
                l.Id.ToString(),
                l.ProductId,
                l.ProductVariantId,
                l.Count,
                l.FlashSalePrice,
                l.FlashSaleItemId,
                l.ComboOfferId)).ToList();
        }
        else if (!string.IsNullOrEmpty(guestCartId))
        {
            var guest = await _guestCarts.GetAsync(guestCartId, ct);
            lines = guest.Items.Select(i => new CartLineSource(
                i.LineId.ToString(),
                i.ProductId,
                i.ProductVariantId,
                i.Quantity,
                i.FlashSalePrice,
                i.FlashSaleItemId,
                i.ComboOfferId)).ToList();
        }
        else
        {
            return ([], new Dictionary<int, Product>());
        }

        if (lines.Count == 0)
            return (lines, new Dictionary<int, Product>());

        var products = await LoadProductsForCartLinesAsync(lines, ct);
        return (lines, products);
    }

    private async Task<Dictionary<int, Product>> LoadProductsForCartLinesAsync(
        List<CartLineSource> lines,
        CancellationToken ct)
    {
        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var comboIds = lines
            .Where(l => l.ComboOfferId is > 0)
            .Select(l => l.ComboOfferId!.Value)
            .Distinct()
            .ToList();

        if (comboIds.Count > 0)
        {
            var comboProductIds = await _db.ComboOfferItems
                .AsNoTracking()
                .Where(i => comboIds.Contains(i.ComboOfferId) && !i.IsDeleted)
                .Select(i => i.ProductId)
                .Distinct()
                .ToListAsync(ct);

            productIds = productIds.Concat(comboProductIds).Distinct().ToList();
        }

        if (productIds.Count == 0)
            return new Dictionary<int, Product>();

        return await _db.Products
            .AsNoTracking()
            .Include(p => p.Images.Where(i => i.ImageInfo == null))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, ct);
    }

    private async Task<CartResponse> GetUserCartAsync(string userId, CancellationToken ct)
    {
        await RepairStoredCartVariantsAsync(userId, ct);
        var (lines, products) = await LoadCartSourcesAsync(userId, null, ct);
        return await BuildCartResponseAsync(lines, products, userId, null, ct);
    }

    private async Task<CartResponse> GetGuestCartAsync(string? guestCartId, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(guestCartId))
            return EmptyCart();

        var (lines, products) = await LoadCartSourcesAsync(null, guestCartId, ct);
        return await BuildCartResponseAsync(lines, products, null, guestCartId, ct);
    }

    private async Task<CartResponse> BuildGuestResponseAsync(
        string guestCartId,
        GuestCart guest,
        CancellationToken ct)
    {
        var lines = guest.Items.Select(i => new CartLineSource(
            i.LineId.ToString(),
            i.ProductId,
            i.ProductVariantId,
            i.Quantity,
            i.FlashSalePrice,
            i.FlashSaleItemId,
            null)).ToList();

        if (lines.Count == 0)
            return EmptyCart();

        var products = await LoadProductsForCartLinesAsync(lines, ct);
        return await BuildCartResponseAsync(lines, products, null, guestCartId, ct);
    }

    private async Task<Dictionary<int, ComboOffer>> LoadCombosForLinesAsync(
        List<CartLineSource> lineList,
        CancellationToken ct)
    {
        var comboIds = lineList
            .Where(l => l.ComboOfferId is > 0)
            .Select(l => l.ComboOfferId!.Value)
            .Distinct()
            .ToList();

        if (comboIds.Count == 0)
            return new Dictionary<int, ComboOffer>();

        return await _db.ComboOffers
            .AsNoTracking()
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .Where(c => comboIds.Contains(c.Id) && !c.IsDeleted)
            .ToDictionaryAsync(c => c.Id, ct);
    }

    private async Task<CartResponse> BuildCartResponseAsync(
        List<CartLineSource> lineList,
        Dictionary<int, Product> products,
        string? userId,
        string? guestCartId,
        CancellationToken ct)
    {
        if (lineList.Count == 0)
            return EmptyCart();

        var flashIds = lineList
            .Where(l => l.FlashSaleItemId is > 0)
            .Select(l => l.FlashSaleItemId!.Value)
            .Distinct()
            .ToList();

        var flashQuantities = flashIds.Count == 0
            ? new Dictionary<int, int>()
            : await _db.FlashSaleItems
                .AsNoTracking()
                .Where(f => flashIds.Contains(f.Id) && !f.IsDeleted)
                .ToDictionaryAsync(f => f.Id, f => f.FlashSaleQuantity, ct);

        var combos = await LoadCombosForLinesAsync(lineList, ct);
        var now = _clock.Now;

        var items = new List<CartItemDto>();
        foreach (var line in lineList)
        {
            if (line.ComboOfferId is > 0
                && combos.TryGetValue(line.ComboOfferId.Value, out var combo))
            {
                if (!combo.IsActive || combo.StartDate > now || combo.EndDate < now)
                    continue;

                var comboMaxQty = ComboOfferService.CalculateMaxQuantity(combo, products);
                var comboInStock = ComboOfferService.IsInStock(combo, products);
                var comboUnitPrice = (double)combo.ComboPrice;
                var comboImage = string.IsNullOrWhiteSpace(combo.ImageUrl)
                    ? products.TryGetValue(line.ProductId, out var displayProduct)
                        ? displayProduct.Images.OrderBy(img => img.DisplayOrder).FirstOrDefault()?.ImageUrl
                            ?? displayProduct.ImageUrl
                        : string.Empty
                    : combo.ImageUrl;

                items.Add(new CartItemDto
                {
                    LineId = line.LineId,
                    ProductId = line.ProductId,
                    ProductVariantId = line.VariantId,
                    ComboOfferId = combo.Id,
                    Title = combo.Name,
                    Slug = combo.Id.ToString(),
                    ImageUrl = comboImage,
                    Quantity = line.Qty,
                    UnitPrice = comboUnitPrice,
                    LineTotal = comboUnitPrice * line.Qty,
                    InStock = comboInStock && line.Qty <= comboMaxQty,
                    MaxQuantity = comboMaxQty
                });
                continue;
            }

            if (!products.TryGetValue(line.ProductId, out var product))
                continue;

            var variantId = line.VariantId;
            if (product.ProductType == ProductType.Variable && variantId is not > 0)
            {
                var resolvedVariantId = ResolveDefaultVariantId(product);
                if (resolvedVariantId is > 0)
                {
                    variantId = resolvedVariantId;
                    await PersistCartVariantAsync(line, resolvedVariantId.Value, userId, guestCartId, ct);
                }
            }

            int? flashMax = null;
            if (line.FlashSaleItemId is > 0 && flashQuantities.TryGetValue(line.FlashSaleItemId.Value, out var fq))
                flashMax = fq;

            var (unitPrice, inStock, maxQty) = ResolveLinePricing(product, variantId, line.FlashPrice, flashMax);
            var image = product.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl ?? product.ImageUrl;

            items.Add(new CartItemDto
            {
                LineId = line.LineId,
                ProductId = line.ProductId,
                ProductVariantId = variantId,
                FlashSaleItemId = line.FlashSaleItemId,
                Title = product.Title,
                Slug = product.GetSlug(),
                ImageUrl = image,
                Quantity = line.Qty,
                UnitPrice = unitPrice,
                LineTotal = unitPrice * line.Qty,
                InStock = inStock,
                MaxQuantity = maxQty
            });
        }

        var response = new CartResponse
        {
            Items = items,
            ItemCount = items.Sum(i => i.Quantity),
            Subtotal = items.Sum(i => i.LineTotal),
            Discount = 0,
            Total = items.Sum(i => i.LineTotal)
        };

        return await EnrichWithPromoAsync(response, lineList, products, combos, userId, guestCartId, ct);
    }

    private async Task<CartResponse> EnrichWithPromoAsync(
        CartResponse cart,
        List<CartLineSource> lines,
        Dictionary<int, Product> products,
        Dictionary<int, ComboOffer> combos,
        string? userId,
        string? guestCartId,
        CancellationToken ct)
    {
        var key = GetPromoKey(userId, guestCartId);
        if (key is null || cart.Items.Count == 0)
            return cart;

        var stored = await _promos.GetAsync(key.Value.scope, key.Value.id, ct);
        if (stored is null)
            return cart;

        var promoLines = BuildPromoLines(lines, products, combos);
        var validation = await _promoCodes.ValidateAndCalculateAsync(stored.Code, promoLines, userId, ct);
        if (!validation.IsValid)
        {
            await _promos.RemoveAsync(key.Value.scope, key.Value.id, ct);
            return cart;
        }

        return new CartResponse
        {
            Items = cart.Items,
            ItemCount = cart.ItemCount,
            Subtotal = cart.Subtotal,
            Discount = validation.DiscountAmount,
            Total = Math.Max(0, cart.Subtotal - validation.DiscountAmount),
            AppliedPromo = validation.Promo
        };
    }

    private static List<PromoCartLine> BuildPromoLines(
        IEnumerable<CartLineSource> lines,
        Dictionary<int, Product> products,
        Dictionary<int, ComboOffer> combos)
    {
        var promoLines = new List<PromoCartLine>();
        foreach (var line in lines)
        {
            if (line.ComboOfferId is > 0 && combos.TryGetValue(line.ComboOfferId.Value, out var combo))
            {
                var comboUnitPrice = (double)combo.ComboPrice;
                var comboListPrice = ComboOfferService.CalculateOriginalPrice(combo, products);
                promoLines.Add(new PromoCartLine
                {
                    ProductId = line.ProductId,
                    ProductVariantId = line.VariantId,
                    FlashSaleItemId = null,
                    ComboOfferId = combo.Id,
                    UnitPrice = comboUnitPrice,
                    Quantity = line.Qty,
                    ListPrice = comboListPrice,
                    ProductListPrice = comboListPrice
                });
                continue;
            }

            if (!products.TryGetValue(line.ProductId, out var product))
                continue;

            var (unitPrice, _, _) = ResolveLinePricing(product, line.VariantId, line.FlashPrice);
            var listPrice = line.VariantId is > 0
                ? product.Variants.FirstOrDefault(v => v.Id == line.VariantId && !v.IsDeleted) is { } variant
                    ? (double)variant.Price
                    : product.ListPrice
                : product.ListPrice;

            promoLines.Add(new PromoCartLine
            {
                ProductId = line.ProductId,
                ProductVariantId = line.VariantId,
                FlashSaleItemId = line.FlashSaleItemId,
                ComboOfferId = line.ComboOfferId,
                UnitPrice = unitPrice,
                Quantity = line.Qty,
                ListPrice = listPrice,
                ProductListPrice = product.ListPrice
            });
        }

        return promoLines;
    }

    private async Task ClearStoredPromoAsync(string? userId, string? guestCartId, CancellationToken ct)
    {
        var key = GetPromoKey(userId, guestCartId);
        if (key is not null)
            await _promos.RemoveAsync(key.Value.scope, key.Value.id, ct);
    }

    private static (string scope, string id)? GetPromoKey(string? userId, string? guestCartId)
    {
        if (userId is not null)
            return ("user", userId);
        if (!string.IsNullOrEmpty(guestCartId))
            return ("guest", guestCartId);
        return null;
    }

    private sealed record CartLineSource(
        string LineId,
        int ProductId,
        int? VariantId,
        int Qty,
        decimal? FlashPrice,
        int? FlashSaleItemId,
        int? ComboOfferId);

    private async Task<Product?> LoadProductAsync(int productId, CancellationToken ct) =>
        await _db.Products
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .FirstOrDefaultAsync(p => p.Id == productId && !p.IsDeleted, ct);

    private async Task<(ComboOffer Combo, ComboOfferItem FirstItem, int MaxQty)> ResolveComboOfferAsync(
        int comboOfferId,
        CancellationToken ct)
    {
        var now = _clock.Now;
        var combo = await _db.ComboOffers
            .Include(c => c.Items.Where(i => !i.IsDeleted))
            .FirstOrDefaultAsync(
                c => c.Id == comboOfferId && !c.IsDeleted && c.IsActive && c.StartDate <= now && c.EndDate >= now,
                ct)
            ?? throw new InvalidOperationException("Combo offer not found or no longer active.");

        var firstItem = combo.Items
            .OrderBy(i => i.DisplayOrder)
            .ThenBy(i => i.Id)
            .FirstOrDefault()
            ?? throw new InvalidOperationException("Combo offer has no products.");

        var productIds = combo.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, ct);

        if (!ComboOfferService.IsInStock(combo, products))
            throw new InvalidOperationException("This combo is out of stock.");

        var maxQty = ComboOfferService.CalculateMaxQuantity(combo, products);
        if (maxQty <= 0)
            throw new InvalidOperationException("This combo is sold out.");

        return (combo, firstItem, maxQty);
    }

    private async Task<FlashSaleCartContext> ResolveFlashSaleContextAsync(
        int flashSaleItemId,
        int productId,
        int? variantId,
        CancellationToken ct)
    {
        var item = await _db.FlashSaleItems
            .AsNoTracking()
            .Include(i => i.FlashSale)
            .FirstOrDefaultAsync(i => i.Id == flashSaleItemId && !i.IsDeleted, ct)
            ?? throw new InvalidOperationException("Flash sale item not found.");

        if (item.ProductId != productId)
            throw new InvalidOperationException("Flash sale item does not match the product.");

        if (item.ProductVariantId != variantId)
            throw new InvalidOperationException("Flash sale item does not match the selected variant.");

        var sale = item.FlashSale
            ?? throw new InvalidOperationException("Flash sale not found.");

        if (sale.IsDeleted || !sale.IsActive)
            throw new InvalidOperationException("This flash sale is not active.");

        var now = _clock.Now;
        if (now < sale.StartDate || now > sale.EndDate)
            throw new InvalidOperationException("This flash sale has ended.");

        if (item.FlashSaleQuantity <= 0)
            throw new InvalidOperationException("This flash sale item is sold out.");

        return new FlashSaleCartContext(item.Id, item.FlashSalePrice, item.FlashSaleQuantity);
    }

    private async Task<int?> GetFlashSaleMaxQuantityAsync(int? flashSaleItemId, CancellationToken ct)
    {
        if (flashSaleItemId is not > 0)
            return null;

        var qty = await _db.FlashSaleItems
            .AsNoTracking()
            .Where(f => f.Id == flashSaleItemId && !f.IsDeleted)
            .Select(f => (int?)f.FlashSaleQuantity)
            .FirstOrDefaultAsync(ct);

        return qty;
    }

    private sealed record FlashSaleCartContext(int FlashSaleItemId, decimal FlashSalePrice, int MaxQuantity);

    private async Task RepairStoredCartVariantsAsync(string userId, CancellationToken ct)
    {
        var lines = await _db.ShoppingCartLines
            .Where(c => c.ApplicationUserId == userId && c.ProductVariantId == null && c.ComboOfferId == null)
            .ToListAsync(ct);

        if (lines.Count == 0)
            return;

        var productIds = lines.Select(l => l.ProductId).Distinct().ToList();
        var products = await _db.Products
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => productIds.Contains(p.Id) && !p.IsDeleted)
            .ToDictionaryAsync(p => p.Id, ct);

        var changed = false;
        foreach (var line in lines)
        {
            if (!products.TryGetValue(line.ProductId, out var product))
                continue;

            var variantId = ResolveVariantId(product, line.ProductVariantId);
            if (variantId is > 0 && line.ProductVariantId != variantId)
            {
                line.ProductVariantId = variantId;
                changed = true;
            }
        }

        if (changed)
            await _db.SaveChangesAsync(ct);
    }

    private static int? ResolveVariantId(Product product, int? requestedVariantId)
    {
        if (requestedVariantId is > 0)
            return requestedVariantId;

        return product.ProductType == ProductType.Variable
            ? ResolveDefaultVariantId(product)
            : null;
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

    private async Task PersistCartVariantAsync(
        CartLineSource line,
        int variantId,
        string? userId,
        string? guestCartId,
        CancellationToken ct)
    {
        if (userId is not null && int.TryParse(line.LineId, out var lineId))
        {
            var dbLine = await _db.ShoppingCartLines
                .FirstOrDefaultAsync(l => l.Id == lineId && l.ApplicationUserId == userId, ct);
            if (dbLine is not null && dbLine.ProductVariantId != variantId)
            {
                dbLine.ProductVariantId = variantId;
                await _db.SaveChangesAsync(ct);
            }

            return;
        }

        if (!string.IsNullOrEmpty(guestCartId) && Guid.TryParse(line.LineId, out var guestLineId))
        {
            var guest = await _guestCarts.GetAsync(guestCartId, ct);
            var guestLine = guest.Items.FirstOrDefault(i => i.LineId == guestLineId);
            if (guestLine is not null && guestLine.ProductVariantId != variantId)
            {
                guestLine.ProductVariantId = variantId;
                await _guestCarts.SaveAsync(guestCartId, guest, ct);
            }
        }
    }

    private static void ValidateProductStock(
        Product product,
        int? variantId,
        int quantity,
        int? flashMaxQuantity)
    {
        var (_, inStock, maxQty) = ResolveLinePricing(product, variantId, null);
        if (flashMaxQuantity is > 0)
            maxQty = Math.Min(maxQty, flashMaxQuantity.Value);

        if (!inStock || quantity > maxQty)
        {
            if (flashMaxQuantity is > 0 && quantity > flashMaxQuantity)
                throw new InvalidOperationException($"Only {flashMaxQuantity} item(s) available for this flash sale.");
            throw new InvalidOperationException($"Only {maxQty} item(s) available in stock.");
        }
    }

    private static void ValidateStock(Product product, int? variantId, int quantity) =>
        ValidateProductStock(product, variantId, quantity, null);

    private static (double unitPrice, bool inStock, int maxQty) ResolveLinePricing(
        Product product,
        int? variantId,
        decimal? flashPrice,
        int? flashMaxQuantity = null)
    {
        if (flashPrice.HasValue)
        {
            var productMax = ResolveProductMaxQuantity(product, variantId);
            var max = flashMaxQuantity is > 0
                ? Math.Min(productMax, flashMaxQuantity.Value)
                : productMax;
            return ((double)flashPrice.Value, max > 0, max);
        }

        if (variantId is > 0)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);
            if (variant is null)
                return (product.Price, false, 0);

            return ((double)variant.Price, variant.StockQuantity > 0, Math.Max(variant.StockQuantity, 0));
        }

        if (product.ProductType == ProductType.Variable)
        {
            var inStock = product.Variants.Where(v => !v.IsDeleted && v.StockQuantity > 0).ToList();
            if (inStock.Count == 0)
                return (product.Price, false, 0);

            var cheapest = inStock.MinBy(v => v.Price)!;
            return ((double)cheapest.Price, true, inStock.Max(v => v.StockQuantity));
        }

        return (product.Price, product.StockQuantity > 0, Math.Max(product.StockQuantity, 0));
    }

    private static int ResolveProductMaxQuantity(Product product, int? variantId)
    {
        if (variantId is > 0)
        {
            var variant = product.Variants.FirstOrDefault(v => v.Id == variantId && !v.IsDeleted);
            return variant is null ? 0 : Math.Max(variant.StockQuantity, 0);
        }

        if (product.ProductType == ProductType.Variable)
        {
            var inStock = product.Variants.Where(v => !v.IsDeleted && v.StockQuantity > 0).ToList();
            return inStock.Count == 0 ? 0 : inStock.Max(v => v.StockQuantity);
        }

        return Math.Max(product.StockQuantity, 0);
    }

    private static CartResponse EmptyCart() => new()
    {
        Items = Array.Empty<CartItemDto>(),
        ItemCount = 0,
        Subtotal = 0,
        Discount = 0,
        Total = 0
    };
}
