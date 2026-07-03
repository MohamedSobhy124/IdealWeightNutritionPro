using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Catalogue;
using IdealWeightNutrition.Contracts.Common;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Infrastructure.Caching;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class CatalogueService : ICatalogueService
{
    private const string CategoriesCacheKey = "catalogue:categories";
    private const string BrandsCacheKey = "catalogue:brands";

    private readonly AppDbContext _db;
    private readonly IDistributedCache _cache;
    private readonly CacheOptions _cacheOptions;

    public CatalogueService(
        AppDbContext db,
        IDistributedCache cache,
        IOptions<CacheOptions> cacheOptions)
    {
        _db = db;
        _cache = cache;
        _cacheOptions = cacheOptions.Value;
    }

    public async Task<PagedResult<ProductListItemDto>> ListProductsAsync(
        ProductQueryRequest query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var productsQuery = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images.Where(i => i.ImageInfo == null))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => !p.IsDeleted);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim();
            productsQuery = productsQuery.Where(p =>
                p.Title.Contains(term) || p.TitleAr.Contains(term));
        }

        if (query.CategoryId is > 0)
            productsQuery = productsQuery.Where(p => p.CategryId == query.CategoryId);

        if (query.BrandId is > 0)
            productsQuery = productsQuery.Where(p => p.BrandId == query.BrandId);

        if (!string.IsNullOrWhiteSpace(query.Availability))
        {
            productsQuery = query.Availability.ToLowerInvariant() switch
            {
                "instock" => productsQuery.Where(p =>
                    p.ProductType == ProductType.Simple
                        ? p.StockQuantity > 0
                        : p.Variants.Any(v => !v.IsDeleted && v.StockQuantity > 0)),
                "outofstock" => productsQuery.Where(p =>
                    p.ProductType == ProductType.Simple
                        ? p.StockQuantity == 0
                        : !p.Variants.Any(v => !v.IsDeleted && v.StockQuantity > 0)),
                _ => productsQuery
            };
        }

        productsQuery = query.SortBy?.ToLowerInvariant() switch
        {
            "price_low" => productsQuery.OrderBy(p => p.Price),
            "price_high" => productsQuery.OrderByDescending(p => p.Price),
            "newest" => productsQuery.OrderByDescending(p => p.Id),
            "new" => productsQuery.OrderByDescending(p => p.IsNew).ThenByDescending(p => p.Id),
            "trending" => productsQuery.OrderByDescending(p => p.IsTrending).ThenByDescending(p => p.Id),
            "name" => productsQuery.OrderBy(p => p.Title),
            _ => productsQuery
                .OrderByDescending(p => p.IsTrending)
                .ThenByDescending(p => p.IsNew)
                .ThenBy(p => p.Title)
        };

        var total = await productsQuery.CountAsync(cancellationToken);
        var items = await productsQuery
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductListItemDto>
        {
            Items = items.Select(MapListItem).ToList(),
            Page = page,
            PageSize = pageSize,
            TotalCount = total
        };
    }

    public async Task<ProductDetailDto?> GetProductBySlugAsync(
        string slug,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slug))
            return null;

        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images.Where(i => i.ImageInfo == null).OrderBy(i => i.DisplayOrder))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => !p.IsDeleted);

        Product? product = await query.FirstOrDefaultAsync(p => p.SlugEn == slug, cancellationToken);

        if (product is null && int.TryParse(slug, out var id))
            product = await query.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (product is null)
            return null;

        IReadOnlyList<StorefrontProductOptionDto> options = [];
        IReadOnlyList<StorefrontProductVariantDto> variants = [];

        if (product.ProductType == ProductType.Variable)
        {
            options = await _db.ProductOptions.AsNoTracking()
                .Where(o => o.ProductId == product.Id && !o.IsDeleted)
                .OrderBy(o => o.DisplayOrder)
                .Select(o => new StorefrontProductOptionDto
                {
                    Id = o.Id,
                    Name = o.Name,
                    NameAr = o.NameAr,
                    Values = o.OptionValues
                        .Where(v => !v.IsDeleted)
                        .OrderBy(v => v.DisplayOrder)
                        .Select(v => new StorefrontProductOptionValueDto
                        {
                            Id = v.Id,
                            Value = v.Value,
                            ValueAr = v.ValueAr
                        })
                        .ToList()
                })
                .ToListAsync(cancellationToken);

            var variantIds = product.Variants.Select(v => v.Id).ToList();
            var optionValueMap = await _db.ProductVariantOptionValues.AsNoTracking()
                .Where(vov => variantIds.Contains(vov.ProductVariantId))
                .GroupBy(vov => vov.ProductVariantId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => (IReadOnlyList<int>)g.Select(x => x.ProductOptionValueId).ToList(),
                    cancellationToken);

            variants = product.Variants
                .Where(v => !v.IsDeleted)
                .Select(v => new StorefrontProductVariantDto
                {
                    Id = v.Id,
                    Sku = v.Sku,
                    Price = (double)v.Price,
                    ListPrice = v.ListPrice is null ? null : (double)v.ListPrice,
                    StockQuantity = v.StockQuantity,
                    ImageUrl = v.ImageUrl,
                    OptionValueIds = optionValueMap.TryGetValue(v.Id, out var ids) ? ids : []
                })
                .ToList();
        }

        return MapDetail(product, options, variants);
    }

    public async Task<IReadOnlyList<CategoryDto>> ListCategoriesAsync(CancellationToken cancellationToken = default)
    {
        var ttl = TimeSpan.FromMinutes(_cacheOptions.ReferenceDataMinutes);
        if (ttl > TimeSpan.Zero)
        {
            var cached = await DistributedCacheJson.GetAsync<List<CategoryDto>>(
                _cache, CategoriesCacheKey, cancellationToken);
            if (cached is not null)
                return cached;
        }

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                NameAr = c.NameAr,
                ImageUrl = c.ImageUrl
            })
            .ToListAsync(cancellationToken);

        if (ttl > TimeSpan.Zero)
        {
            await DistributedCacheJson.SetAsync(
                _cache, CategoriesCacheKey, categories, ttl, cancellationToken);
        }

        return categories;
    }

    public async Task<IReadOnlyList<BrandDto>> ListBrandsAsync(CancellationToken cancellationToken = default)
    {
        var ttl = TimeSpan.FromMinutes(_cacheOptions.ReferenceDataMinutes);
        if (ttl > TimeSpan.Zero)
        {
            var cached = await DistributedCacheJson.GetAsync<List<BrandDto>>(
                _cache, BrandsCacheKey, cancellationToken);
            if (cached is not null)
                return cached;
        }

        var brands = await _db.Brands
            .AsNoTracking()
            .Where(b => !b.IsDeleted)
            .OrderBy(b => b.Name)
            .Select(b => new BrandDto
            {
                Id = b.Id,
                Name = b.Name,
                NameAr = b.NameAr,
                ImageUrl = b.ImageUrl
            })
            .ToListAsync(cancellationToken);

        if (ttl > TimeSpan.Zero)
        {
            await DistributedCacheJson.SetAsync(
                _cache, BrandsCacheKey, brands, ttl, cancellationToken);
        }

        return brands;
    }

    public async Task<IReadOnlyList<ProductListItemDto>> ListDiscountedProductsAsync(
        int limit = 20,
        CancellationToken cancellationToken = default)
    {
        limit = Math.Clamp(limit, 1, 50);

        var mainDiscounts = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images.Where(i => i.ImageInfo == null))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => !p.IsDeleted
                && p.StockQuantity > 0
                && p.ListPrice > p.Price
                && p.ListPrice > 0)
            .OrderByDescending(p => (p.ListPrice - p.Price) / p.ListPrice)
            .Take(limit)
            .ToListAsync(cancellationToken);

        var results = mainDiscounts
            .Select(p => (Product: p, Discount: (p.ListPrice - p.Price) / p.ListPrice))
            .ToList();

        if (results.Count < limit)
        {
            var existingIds = results.Select(r => r.Product.Id).ToHashSet();
            var variantCandidates = await _db.Products
                .AsNoTracking()
                .Include(p => p.Category)
                .Include(p => p.Brand)
                .Include(p => p.Images.Where(i => i.ImageInfo == null))
                .Include(p => p.Variants.Where(v => !v.IsDeleted))
                .Where(p => !p.IsDeleted
                    && p.ProductType == ProductType.Variable
                    && p.StockQuantity > 0
                    && !existingIds.Contains(p.Id))
                .Take(50)
                .ToListAsync(cancellationToken);

            foreach (var product in variantCandidates)
            {
                var maxDiscount = product.Variants
                    .Where(v => !v.IsDeleted
                        && v.StockQuantity > 0
                        && v.ListPrice is > 0
                        && v.ListPrice > v.Price)
                    .Select(v => (double)((v.ListPrice!.Value - v.Price) / v.ListPrice.Value))
                    .DefaultIfEmpty(0)
                    .Max();

                if (maxDiscount > 0)
                    results.Add((product, maxDiscount));
            }

            results = results
                .OrderByDescending(r => r.Discount)
                .Take(limit)
                .ToList();
        }

        return results.Select(r => MapListItem(r.Product)).ToList();
    }

    public async Task<IReadOnlyList<CategoryProductSectionDto>> GetCategoryProductSectionsAsync(
        int maxCategories = 6,
        int productsPerCategory = 4,
        CancellationToken cancellationToken = default)
    {
        maxCategories = Math.Clamp(maxCategories, 1, 12);
        productsPerCategory = Math.Clamp(productsPerCategory, 1, 12);

        var categories = await _db.Categories
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .OrderBy(c => c.Name)
            .Take(maxCategories)
            .ToListAsync(cancellationToken);

        if (categories.Count == 0)
            return [];

        var categoryIds = categories.Select(c => c.Id).ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Images.Where(i => i.ImageInfo == null))
            .Include(p => p.Variants.Where(v => !v.IsDeleted))
            .Where(p => !p.IsDeleted
                && categoryIds.Contains(p.CategryId)
                && (p.ProductType == ProductType.Simple
                    ? p.StockQuantity > 0
                    : p.Variants.Any(v => !v.IsDeleted && v.StockQuantity > 0)))
            .OrderByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        var grouped = products
            .GroupBy(p => p.CategryId)
            .ToDictionary(g => g.Key, g => g.Take(productsPerCategory).ToList());

        var sections = new List<CategoryProductSectionDto>();
        foreach (var category in categories)
        {
            if (!grouped.TryGetValue(category.Id, out var categoryProducts) || categoryProducts.Count == 0)
                continue;

            sections.Add(new CategoryProductSectionDto
            {
                Category = new CategoryDto
                {
                    Id = category.Id,
                    Name = category.Name,
                    NameAr = category.NameAr,
                    ImageUrl = category.ImageUrl
                },
                Products = categoryProducts.Select(MapListItem).ToList()
            });
        }

        return sections;
    }

    private static ProductListItemDto MapListItem(Product p)
    {
        var (price, listPrice, inStock, displayVariantId) = ResolveDisplayPricing(p);
        var image = p.Images.OrderBy(i => i.DisplayOrder).FirstOrDefault()?.ImageUrl ?? p.ImageUrl;

        return new ProductListItemDto
        {
            Id = p.Id,
            Slug = p.GetSlug(),
            Title = p.Title,
            TitleAr = p.TitleAr,
            Price = price,
            ListPrice = listPrice,
            InStock = inStock,
            ImageUrl = image,
            CategoryId = p.CategryId,
            CategoryName = p.Category?.Name,
            BrandId = p.BrandId,
            BrandName = p.Brand?.Name,
            IsNew = p.IsNew,
            IsTrending = p.IsTrending,
            DisplayVariantId = displayVariantId,
            ProductType = p.ProductType.ToString()
        };
    }

    private static ProductDetailDto MapDetail(
        Product p,
        IReadOnlyList<StorefrontProductOptionDto> options,
        IReadOnlyList<StorefrontProductVariantDto> variants)
    {
        var (price, inStock) = ResolvePriceAndStock(p);
        var images = p.Images
            .Where(i => i.ImageInfo == null)
            .OrderBy(i => i.DisplayOrder)
            .Select(i => i.ImageUrl)
            .ToList();

        if (images.Count == 0 && !string.IsNullOrEmpty(p.ImageUrl))
            images.Add(p.ImageUrl);

        return new ProductDetailDto
        {
            Id = p.Id,
            Slug = p.GetSlug(),
            Title = p.Title,
            TitleAr = p.TitleAr,
            Description = p.Description,
            DescriptionAr = p.DescriptionAr,
            SuggestedUse = p.SuggestedUse,
            SuggestedUseAr = p.SuggestedUseAr,
            HealthNotes = p.HealthNotes,
            HealthNotesAr = p.HealthNotesAr,
            Specification = p.Specification,
            SpecificationAr = p.SpecificationAr,
            Price = price,
            ListPrice = p.ListPrice,
            InStock = inStock,
            StockQuantity = p.ProductType == ProductType.Simple
                ? p.StockQuantity
                : p.Variants.Where(v => !v.IsDeleted).Sum(v => v.StockQuantity),
            ProductType = p.ProductType.ToString(),
            ImageUrls = images,
            Category = p.Category is null
                ? null
                : new CategoryDto
                {
                    Id = p.Category.Id,
                    Name = p.Category.Name,
                    NameAr = p.Category.NameAr,
                    ImageUrl = p.Category.ImageUrl
                },
            Brand = p.Brand is null
                ? null
                : new BrandDto
                {
                    Id = p.Brand.Id,
                    Name = p.Brand.Name,
                    NameAr = p.Brand.NameAr,
                    ImageUrl = p.Brand.ImageUrl
                },
            IsNew = p.IsNew,
            IsTrending = p.IsTrending,
            Options = options,
            Variants = variants
        };
    }

    private static (double price, bool inStock) ResolvePriceAndStock(Product p)
    {
        var (price, _, inStock, _) = ResolveDisplayPricing(p);
        return (price, inStock);
    }

    private static (double price, double listPrice, bool inStock, int? displayVariantId) ResolveDisplayPricing(Product p)
    {
        if (p.ProductType == ProductType.Variable)
        {
            var inStockVariants = p.Variants.Where(v => !v.IsDeleted && v.StockQuantity > 0).ToList();
            if (inStockVariants.Count == 0)
                return (p.Price, p.ListPrice, false, null);

            var bestVariant = inStockVariants.MinBy(v => v.Price)!;
            var listPrice = bestVariant.ListPrice is > 0
                ? (double)bestVariant.ListPrice
                : p.ListPrice;
            return ((double)bestVariant.Price, listPrice, true, bestVariant.Id);
        }

        return (p.Price, p.ListPrice, p.StockQuantity > 0, null);
    }
}
