using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Wishlist;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Engagement;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class WishlistService : IWishlistService
{
    private readonly AppDbContext _db;

    public WishlistService(AppDbContext db) => _db = db;

    public async Task<WishlistResponse> GetWishlistAsync(string userId, CancellationToken cancellationToken = default)
    {
        var items = await LoadWishlistItemsAsync(userId, cancellationToken);
        return new WishlistResponse { Items = items, Count = items.Count };
    }

    public async Task<WishlistProductIdsResponse> GetProductIdsAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var ids = await _db.WishlistItems
            .AsNoTracking()
            .Where(w => w.ApplicationUserId == userId)
            .Select(w => w.ProductId)
            .ToListAsync(cancellationToken);

        return new WishlistProductIdsResponse { ProductIds = ids };
    }

    public async Task<WishlistToggleResponse> ToggleAsync(
        string userId,
        int productId,
        CancellationToken cancellationToken = default)
    {
        var productExists = await _db.Products.AnyAsync(p => p.Id == productId && !p.IsDeleted, cancellationToken);
        if (!productExists)
            throw new InvalidOperationException("Product not found.");

        var existing = await _db.WishlistItems
            .FirstOrDefaultAsync(w => w.ApplicationUserId == userId && w.ProductId == productId, cancellationToken);

        bool isInWishlist;
        string message;

        if (existing is not null)
        {
            _db.WishlistItems.Remove(existing);
            isInWishlist = false;
            message = "Removed from wishlist.";
        }
        else
        {
            _db.WishlistItems.Add(new WishlistItem
            {
                ProductId = productId,
                ApplicationUserId = userId
            });
            isInWishlist = true;
            message = "Added to wishlist.";
        }

        await _db.SaveChangesAsync(cancellationToken);

        var count = await _db.WishlistItems.CountAsync(w => w.ApplicationUserId == userId, cancellationToken);
        return new WishlistToggleResponse
        {
            IsInWishlist = isInWishlist,
            WishlistCount = count,
            Message = message
        };
    }

    public async Task RemoveAsync(string userId, int wishlistItemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.WishlistItems.FirstOrDefaultAsync(
            w => w.Id == wishlistItemId && w.ApplicationUserId == userId,
            cancellationToken) ?? throw new InvalidOperationException("Wishlist item not found.");

        _db.WishlistItems.Remove(item);
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<IReadOnlyList<WishlistItemDto>> LoadWishlistItemsAsync(
        string userId,
        CancellationToken cancellationToken)
    {
        var rows = await _db.WishlistItems
            .AsNoTracking()
            .Where(w => w.ApplicationUserId == userId)
            .Join(
                _db.Products.AsNoTracking().Where(p => !p.IsDeleted),
                w => w.ProductId,
                p => p.Id,
                (w, p) => new { w.Id, p })
            .OrderByDescending(x => x.Id)
            .ToListAsync(cancellationToken);

        var variableProductIds = rows
            .Where(x => x.p.ProductType == ProductType.Variable)
            .Select(x => x.p.Id)
            .Distinct()
            .ToList();

        var variantStock = variableProductIds.Count == 0
            ? new Dictionary<int, int>()
            : await _db.Set<ProductVariant>()
                .AsNoTracking()
                .Where(v => variableProductIds.Contains(v.ProductId) && !v.IsDeleted)
                .GroupBy(v => v.ProductId)
                .Select(g => new { ProductId = g.Key, Total = g.Sum(v => v.StockQuantity) })
                .ToDictionaryAsync(x => x.ProductId, x => x.Total, cancellationToken);

        return rows.Select(x =>
        {
            var inStock = x.p.ProductType == ProductType.Variable
                ? variantStock.GetValueOrDefault(x.p.Id) > 0
                : x.p.StockQuantity > 0;

            return new WishlistItemDto
            {
                Id = x.Id,
                ProductId = x.p.Id,
                Title = x.p.Title,
                Slug = x.p.GetSlug(),
                ImageUrl = x.p.ImageUrl,
                Price = x.p.Price,
                ListPrice = x.p.ListPrice,
                InStock = inStock,
                ProductType = x.p.ProductType.ToString()
            };
        }).ToList();
    }
}
