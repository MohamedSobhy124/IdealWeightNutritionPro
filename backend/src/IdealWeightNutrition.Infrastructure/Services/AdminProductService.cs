using System.Globalization;
using System.Text;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Infrastructure.Storage;
using IdealWeightNutrition.Utility;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminProductService : IAdminProductService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly ProductStoragePathResolver _storage;
    private readonly IAdminProductVariantService _variants;
    private readonly IStockNotificationFulfillmentService _stockFulfillment;

    public AdminProductService(
        AppDbContext db,
        ProductStoragePathResolver storage,
        IAdminProductVariantService variants,
        IStockNotificationFulfillmentService stockFulfillment)
    {
        _db = db;
        _storage = storage;
        _variants = variants;
        _stockFulfillment = stockFulfillment;
    }

    public async Task<AdminProductListResponse> ListProductsAsync(
        int page = 1,
        int pageSize = 50,
        string? search = null,
        string filter = "active",
        bool includeDeleted = false,
        CancellationToken cancellationToken = default)
    {
        page = Math.Max(1, page);
        pageSize = Math.Clamp(pageSize, 1, 100);

        var effectiveFilter = ResolveListFilter(filter, includeDeleted);
        var query = _db.Products.AsNoTracking();
        query = ApplyProductFilter(query, effectiveFilter);
        query = ApplyProductSearch(query, search);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new
            {
                p.Id,
                p.Title,
                p.SlugEn,
                p.ImageUrl,
                p.Price,
                p.ListPrice,
                p.StockQuantity,
                p.ProductType,
                p.IsDeleted,
                p.IsNew,
                p.IsTrending,
                CategoryName = p.Category != null ? p.Category.Name : null,
                BrandName = p.Brand != null ? p.Brand.Name : null,
                VariantStock = p.Variants.Where(v => !v.IsDeleted).Sum(v => v.StockQuantity)
            })
            .ToListAsync(cancellationToken);

        return new AdminProductListResponse
        {
            Page = page,
            PageSize = pageSize,
            TotalCount = totalCount,
            Items = items.Select(p => new AdminProductListItemDto
            {
                Id = p.Id,
                Title = p.Title,
                Slug = !string.IsNullOrEmpty(p.SlugEn) ? p.SlugEn! : p.Id.ToString(),
                ImageUrl = p.ImageUrl,
                Price = p.Price,
                ListPrice = p.ListPrice,
                StockQuantity = p.ProductType == ProductType.Variable ? p.VariantStock : p.StockQuantity,
                ProductType = p.ProductType == ProductType.Variable ? "Variable" : "Simple",
                CategoryName = p.CategoryName,
                BrandName = p.BrandName,
                IsDeleted = p.IsDeleted,
                InStock = p.ProductType == ProductType.Variable ? p.VariantStock > 0 : p.StockQuantity > 0,
                IsNew = p.IsNew,
                IsTrending = p.IsTrending
            }).ToList()
        };
    }

    private static string ResolveListFilter(string filter, bool includeDeleted)
    {
        if (!string.IsNullOrWhiteSpace(filter) && !filter.Equals("active", StringComparison.OrdinalIgnoreCase))
            return filter.Trim();

        return includeDeleted ? "all" : "active";
    }

    public async Task<AdminProductDetailDto?> GetProductAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken);

        return product is null ? null : await MapDetailAsync(product, cancellationToken);
    }

    public async Task<AdminProductDetailDto> UpdateProductAsync(
        int productId,
        UpdateAdminProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Title is required.");
        if (request.Price < 0 || request.ListPrice < 0)
            throw new InvalidOperationException("Prices cannot be negative.");
        if (request.StoreCost is < 0)
            throw new InvalidOperationException("Store cost cannot be negative.");
        if (request.StockQuantity < 0)
            throw new InvalidOperationException("Stock quantity cannot be negative.");
        if (request.MinimumStockAlert < 0)
            throw new InvalidOperationException("Minimum stock alert cannot be negative.");
        if (request.FreeDeliveryMinimumAmount < 0)
            throw new InvalidOperationException("Free delivery minimum amount cannot be negative.");

        var product = await _db.Products
            .Include(p => p.Category)
            .Include(p => p.Brand)
            .Include(p => p.Variants)
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        product.Title = request.Title.Trim();
        product.Price = request.Price;
        product.ListPrice = request.ListPrice;
        product.StoreCost = request.StoreCost;
        product.IsDeleted = request.IsDeleted;
        product.IsNew = request.IsNew;
        product.IsTrending = request.IsTrending;
        product.AllowFreeDelivery = request.AllowFreeDelivery;
        product.FreeDeliveryMinimumAmount = request.FreeDeliveryMinimumAmount;

        var previousSimpleStock = product.StockQuantity;
        var previousVariantStock = product.Variants
            .Where(v => !v.IsDeleted)
            .ToDictionary(v => v.Id, v => v.StockQuantity);

        if (product.ProductType == ProductType.Simple)
        {
            product.StockQuantity = request.StockQuantity;
            product.MinimumStockAlert = request.MinimumStockAlert;
        }
        else if (request.Variants is { Count: > 0 })
        {
            foreach (var variantUpdate in request.Variants)
            {
                if (variantUpdate.Price < 0)
                    throw new InvalidOperationException("Variant price cannot be negative.");
                if (variantUpdate.StockQuantity < 0)
                    throw new InvalidOperationException("Variant stock cannot be negative.");

                var variant = product.Variants.FirstOrDefault(v => v.Id == variantUpdate.Id && !v.IsDeleted);
                if (variant is null)
                    continue;

                variant.Price = (decimal)variantUpdate.Price;
                variant.StockQuantity = variantUpdate.StockQuantity;
                if (variantUpdate.ListPrice is >= 0)
                    variant.ListPrice = (decimal)variantUpdate.ListPrice.Value;
            }
        }

        await _db.SaveChangesAsync(cancellationToken);

        if (product.ProductType == ProductType.Simple)
        {
            if (previousSimpleStock == 0 && product.StockQuantity > 0)
                await _stockFulfillment.ProcessProductRestockedAsync(productId, null, cancellationToken);
        }
        else
        {
            foreach (var variant in product.Variants.Where(v => !v.IsDeleted))
            {
                previousVariantStock.TryGetValue(variant.Id, out var previousStock);
                if (previousStock == 0 && variant.StockQuantity > 0)
                    await _stockFulfillment.ProcessProductRestockedAsync(productId, variant.Id, cancellationToken);
            }
        }

        return await MapDetailAsync(product, cancellationToken);
    }

    public async Task<AdminProductDetailDto> CreateProductAsync(
        CreateAdminProductRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Title is required.");
        if (request.CategoryId <= 0)
            throw new InvalidOperationException("Category is required.");
        if (request.Price < 0 || request.ListPrice < 0)
            throw new InvalidOperationException("Prices cannot be negative.");
        if (request.StockQuantity < 0)
            throw new InvalidOperationException("Stock quantity cannot be negative.");

        var categoryExists = await _db.Categories
            .AnyAsync(c => c.Id == request.CategoryId && !c.IsDeleted, cancellationToken);
        if (!categoryExists)
            throw new InvalidOperationException("Category not found.");

        if (request.BrandId is > 0)
        {
            var brandExists = await _db.Brands
                .AnyAsync(b => b.Id == request.BrandId && !b.IsDeleted, cancellationToken);
            if (!brandExists)
                throw new InvalidOperationException("Brand not found.");
        }

        var existingSlugs = await _db.Products
            .AsNoTracking()
            .Where(p => p.SlugEn != null && p.SlugEn != "")
            .Select(p => p.SlugEn!)
            .ToListAsync(cancellationToken);

        var slugBase = string.IsNullOrWhiteSpace(request.Slug)
            ? UrlSlugHelper.GenerateSlug(request.Title.Trim())
            : UrlSlugHelper.GenerateSlug(request.Slug.Trim());
        if (string.IsNullOrWhiteSpace(slugBase))
            slugBase = "product";

        var slug = UrlSlugHelper.GenerateUniqueSlug(slugBase, existingSlugs);

        var product = new Product
        {
            Title = request.Title.Trim(),
            TitleAr = string.IsNullOrWhiteSpace(request.TitleAr) ? request.Title.Trim() : request.TitleAr.Trim(),
            SlugEn = slug,
            Description = request.Description?.Trim() ?? string.Empty,
            DescriptionAr = request.DescriptionAr?.Trim() ?? request.Description?.Trim() ?? string.Empty,
            Price = request.Price,
            ListPrice = request.ListPrice > 0 ? request.ListPrice : request.Price,
            StoreCost = request.StoreCost,
            CategryId = request.CategoryId,
            BrandId = request.BrandId is > 0 ? request.BrandId : null,
            ImageUrl = string.Empty,
            StockQuantity = request.StockQuantity,
            MinimumStockAlert = request.MinimumStockAlert,
            ProductType = ProductType.Simple,
            IsDeleted = false,
            IsNew = request.IsNew,
            IsTrending = request.IsTrending,
            AllowFreeDelivery = request.AllowFreeDelivery,
            FreeDeliveryMinimumAmount = request.FreeDeliveryMinimumAmount
        };

        _db.Products.Add(product);
        await _db.SaveChangesAsync(cancellationToken);

        await _db.Entry(product).Reference(p => p.Category).LoadAsync(cancellationToken);
        if (product.BrandId.HasValue)
            await _db.Entry(product).Reference(p => p.Brand).LoadAsync(cancellationToken);

        return await MapDetailAsync(product, cancellationToken);
    }

    public async Task<AdminProductImageDto> UploadProductImageAsync(
        int productId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        return await UploadImageInternalAsync(productId, fileStream, fileName, null, cancellationToken);
    }

    public async Task<AdminProductImageDto> UploadProductInfoImageAsync(
        int productId,
        Stream fileStream,
        string fileName,
        string? imageInfo,
        CancellationToken cancellationToken = default)
    {
        return await UploadImageInternalAsync(productId, fileStream, fileName, imageInfo, cancellationToken);
    }

    private async Task<AdminProductImageDto> UploadImageInternalAsync(
        int productId,
        Stream fileStream,
        string fileName,
        string? imageInfo,
        CancellationToken cancellationToken)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported image type. Use JPG, PNG, WEBP, or GIF.");

        _storage.EnsureProductsDirectoryExists();
        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(_storage.GetProductsDirectory(), storedName);

        await using (var output = File.Create(physicalPath))
        {
            await fileStream.CopyToAsync(output, cancellationToken);
        }

        if (new FileInfo(physicalPath).Length > MaxUploadBytes)
        {
            File.Delete(physicalPath);
            throw new InvalidOperationException("Image must be 5 MB or smaller.");
        }

        var imageUrl = @"\Images\Products\" + storedName;
        var displayOrder = product.Images.Count == 0
            ? 0
            : product.Images.Max(i => i.DisplayOrder) + 1;

        var image = new ProductImage
        {
            ProductId = product.Id,
            ImageUrl = imageUrl,
            DisplayOrder = displayOrder,
            ImageInfo = string.IsNullOrWhiteSpace(imageInfo) ? null : imageInfo.Trim()
        };

        if (string.IsNullOrEmpty(product.ImageUrl))
            product.ImageUrl = imageUrl;

        _db.ProductImages.Add(image);
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminProductImageDto
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl,
            DisplayOrder = image.DisplayOrder,
            ImageInfo = image.ImageInfo
        };
    }

    public async Task DeleteProductImageAsync(
        int productId,
        int imageId,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        var image = product.Images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new InvalidOperationException("Image not found.");

        _db.ProductImages.Remove(image);
        product.Images.Remove(image);

        if (product.ImageUrl == image.ImageUrl)
        {
            product.ImageUrl = product.Images
                .Where(i => i.Id != imageId)
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .FirstOrDefault() ?? string.Empty;
        }

        await _db.SaveChangesAsync(cancellationToken);

        TryDeletePhysicalFile(image.ImageUrl);
    }

    public async Task UpdateProductInfoImageAsync(
        int productId,
        int imageId,
        string? imageInfo,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .Include(p => p.Images)
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        var image = product.Images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new InvalidOperationException("Image not found.");

        image.ImageInfo = string.IsNullOrWhiteSpace(imageInfo) ? null : imageInfo.Trim();
        await _db.SaveChangesAsync(cancellationToken);
    }

    private void TryDeletePhysicalFile(string imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        var fileName = Path.GetFileName(imageUrl.TrimStart('\\', '/'));
        if (string.IsNullOrEmpty(fileName))
            return;

        var path = Path.Combine(_storage.GetProductsDirectory(), fileName);
        if (File.Exists(path))
        {
            try { File.Delete(path); } catch { /* best effort */ }
        }
    }

    private async Task<AdminProductDetailDto> MapDetailAsync(Product product, CancellationToken cancellationToken)
    {
        var options = await _variants.GetOptionsAsync(product.Id, cancellationToken);
        var variantIds = product.Variants.Where(v => !v.IsDeleted).Select(v => v.Id).ToList();
        var variantLinks = variantIds.Count == 0
            ? new Dictionary<int, List<int>>()
            : await _db.ProductVariantOptionValues.AsNoTracking()
                .Where(vov => variantIds.Contains(vov.ProductVariantId))
                .GroupBy(vov => vov.ProductVariantId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(x => x.ProductOptionValueId).ToList(),
                    cancellationToken);

        var valueIds = variantLinks.Values.SelectMany(v => v).Distinct().ToList();
        var valueLabels = valueIds.Count == 0
            ? new Dictionary<int, (string OptionName, string Value)>()
            : await _db.ProductOptionValues.AsNoTracking()
                .Where(v => valueIds.Contains(v.Id))
                .Include(v => v.ProductOption)
                .ToDictionaryAsync(
                    v => v.Id,
                    v => (OptionName: v.ProductOption!.Name, Value: v.Value),
                    cancellationToken);

        return new AdminProductDetailDto
        {
            Id = product.Id,
            Title = product.Title,
            Slug = product.GetSlug(),
            ImageUrl = product.ImageUrl,
            Price = product.Price,
            ListPrice = product.ListPrice,
            StoreCost = product.StoreCost,
            StockQuantity = product.StockQuantity,
            MinimumStockAlert = product.MinimumStockAlert,
            IsNew = product.IsNew,
            IsTrending = product.IsTrending,
            AllowFreeDelivery = product.AllowFreeDelivery,
            FreeDeliveryMinimumAmount = product.FreeDeliveryMinimumAmount,
            ProductType = product.ProductType == ProductType.Variable ? "Variable" : "Simple",
            CategoryName = product.Category?.Name,
            BrandName = product.Brand?.Name,
            IsDeleted = product.IsDeleted,
            Options = options,
            Variants = product.Variants
                .Where(v => !v.IsDeleted)
                .OrderBy(v => v.Id)
                .Select(v =>
                {
                    string? variantName = null;
                    if (variantLinks.TryGetValue(v.Id, out var ids) && ids.Count > 0)
                    {
                        variantName = string.Join(" / ", ids
                            .Where(id => valueLabels.ContainsKey(id))
                            .Select(id => $"{valueLabels[id].OptionName}: {valueLabels[id].Value}"));
                    }

                    return new AdminProductVariantDto
                    {
                        Id = v.Id,
                        Sku = v.Sku,
                        VariantName = variantName,
                        ImageUrl = v.ImageUrl,
                        Price = (double)v.Price,
                        ListPrice = v.ListPrice is null ? null : (double)v.ListPrice,
                        Price50 = v.Price50 is null ? null : (double)v.Price50,
                        Price100 = v.Price100 is null ? null : (double)v.Price100,
                        StockQuantity = v.StockQuantity,
                        MinimumStockAlert = v.MinimumStockAlert,
                        ExpiryDate = v.ExpiryDate,
                        IsDeleted = v.IsDeleted
                    };
                })
                .ToList(),
            Images = product.Images
                .OrderBy(i => i.DisplayOrder)
                .ThenBy(i => i.Id)
                .Select(i => new AdminProductImageDto
                {
                    Id = i.Id,
                    ImageUrl = i.ImageUrl,
                    DisplayOrder = i.DisplayOrder,
                    ImageInfo = i.ImageInfo
                })
                .ToList()
        };
    }

    public async Task<byte[]> ExportProductsCsvAsync(
        string filter = "all",
        string? search = null,
        CancellationToken cancellationToken = default)
    {
        var query = _db.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .AsQueryable();

        query = ApplyProductFilter(query, filter);
        query = ApplyProductSearch(query, search);

        var products = await query
            .OrderByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine(
            "Product ID,Title (EN),Title (AR),Category,Price,List Price,Store Cost,Profit,Profit %,Stock Quantity,ISBN,Author,Is New,Is Trending,Is Deleted,Created Date");

        foreach (var product in products)
        {
            var storeCost = product.StoreCost ?? 0;
            var profit = CalculateExportProfit(product.Price, product.StoreCost);
            var profitPercentage = CalculateExportProfitPercentage(product.Price, product.StoreCost);
            var category = product.Category?.Name ?? string.Empty;

            csv.Append(product.Id).Append(',')
                .Append(Csv(product.Title)).Append(',')
                .Append(Csv(product.TitleAr)).Append(',')
                .Append(Csv(category)).Append(',')
                .Append(product.Price.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(product.ListPrice.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(storeCost.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(profit.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(profitPercentage.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(product.StockQuantity).Append(',')
                .Append(Csv(product.ISBN ?? string.Empty)).Append(',')
                .Append(Csv(product.Author ?? string.Empty)).Append(',')
                .Append(product.IsNew ? "Yes" : "No").Append(',')
                .Append(product.IsTrending ? "Yes" : "No").Append(',')
                .Append(product.IsDeleted ? "Yes" : "No").Append(',')
                .Append(product.CreatedDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture))
                .AppendLine();
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public async Task<RegenerateProductSlugsResponse> RegenerateAllProductSlugsAsync(
        CancellationToken cancellationToken = default)
    {
        var allProducts = await _db.Products.ToListAsync(cancellationToken);
        var updatedCount = 0;

        foreach (var product in allProducts)
        {
            var originalSlug = product.SlugEn;
            if (string.IsNullOrWhiteSpace(product.Title))
                continue;

            var existingSlugs = allProducts
                .Where(p => p.Id != product.Id && !string.IsNullOrEmpty(p.SlugEn))
                .Select(p => p.SlugEn!)
                .ToList();

            var baseSlug = UrlSlugHelper.GenerateSlug(product.Title);
            product.SlugEn = UrlSlugHelper.GenerateUniqueSlug(baseSlug, existingSlugs);

            if (product.SlugEn != originalSlug)
                updatedCount++;
        }

        if (updatedCount > 0)
            await _db.SaveChangesAsync(cancellationToken);

        var total = allProducts.Count;
        return new RegenerateProductSlugsResponse
        {
            Success = true,
            UpdatedCount = updatedCount,
            TotalProducts = total,
            Message = $"Successfully regenerated slugs for {updatedCount} out of {total} products."
        };
    }

    private static IQueryable<Product> ApplyProductFilter(IQueryable<Product> query, string filter)
    {
        if (string.IsNullOrWhiteSpace(filter) || filter.Equals("all", StringComparison.OrdinalIgnoreCase))
            return query;

        return filter.ToLowerInvariant() switch
        {
            "active" => query.Where(p => !p.IsDeleted),
            "deleted" => query.Where(p => p.IsDeleted),
            "lowstock" => query.Where(p => !p.IsDeleted && (
                p.ProductType == ProductType.Simple
                    ? p.StockQuantity > 0 && p.StockQuantity <= p.MinimumStockAlert
                    : p.Variants.Any(v => !v.IsDeleted && v.StockQuantity > 0 && v.StockQuantity <= v.MinimumStockAlert))),
            "outofstock" => query.Where(p => !p.IsDeleted && (
                p.ProductType == ProductType.Simple
                    ? p.StockQuantity <= 0
                    : !p.Variants.Any(v => !v.IsDeleted && v.StockQuantity > 0))),
            "instock" => query.Where(p => !p.IsDeleted && (
                p.ProductType == ProductType.Simple
                    ? p.StockQuantity > 0
                    : p.Variants.Any(v => !v.IsDeleted && v.StockQuantity > 0))),
            "new" => query.Where(p => !p.IsDeleted && p.IsNew),
            "trending" => query.Where(p => !p.IsDeleted && p.IsTrending),
            _ => query
        };
    }

    private static IQueryable<Product> ApplyProductSearch(IQueryable<Product> query, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
            return query;

        var term = search.Trim().ToLowerInvariant();
        return query.Where(p =>
            p.Id.ToString().Contains(term) ||
            (p.Title != null && p.Title.ToLower().Contains(term)) ||
            (p.TitleAr != null && p.TitleAr.ToLower().Contains(term)) ||
            (p.ISBN != null && p.ISBN.ToLower().Contains(term)) ||
            (p.Author != null && p.Author.ToLower().Contains(term)) ||
            (p.Category != null && p.Category.Name != null && p.Category.Name.ToLower().Contains(term)) ||
            p.Price.ToString(CultureInfo.InvariantCulture).Contains(term));
    }

    private static double CalculateExportProfit(double price, double? storeCost)
    {
        if (storeCost is null or <= 0)
            return 0;
        return price - storeCost.Value;
    }

    private static double CalculateExportProfitPercentage(double price, double? storeCost)
    {
        if (storeCost is null or <= 0 || price <= 0)
            return 0;
        var profit = price - storeCost.Value;
        return profit / price * 100;
    }

    private static string Csv(string value) =>
        $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
