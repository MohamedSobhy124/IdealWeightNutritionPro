using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminProductVariantService : IAdminProductVariantService
{
    private static readonly HashSet<string> AllowedExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".jpg", ".jpeg", ".png", ".webp", ".gif"
    };

    private const long MaxUploadBytes = 5 * 1024 * 1024;

    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IStockNotificationFulfillmentService _stockFulfillment;
    private readonly ProductStoragePathResolver _storage;

    public AdminProductVariantService(
        AppDbContext db,
        IDateTimeProvider clock,
        IStockNotificationFulfillmentService stockFulfillment,
        ProductStoragePathResolver storage)
    {
        _db = db;
        _clock = clock;
        _stockFulfillment = stockFulfillment;
        _storage = storage;
    }

    public async Task<IReadOnlyList<AdminProductOptionDto>> GetOptionsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductAsync(productId, cancellationToken);

        var options = await _db.ProductOptions.AsNoTracking()
            .Where(o => o.ProductId == productId && !o.IsDeleted)
            .OrderBy(o => o.DisplayOrder)
            .ThenBy(o => o.Id)
            .ToListAsync(cancellationToken);

        var optionIds = options.Select(o => o.Id).ToList();
        var values = optionIds.Count == 0
            ? new List<ProductOptionValue>()
            : await _db.ProductOptionValues.AsNoTracking()
                .Where(v => optionIds.Contains(v.ProductOptionId) && !v.IsDeleted)
                .OrderBy(v => v.DisplayOrder)
                .ThenBy(v => v.Id)
                .ToListAsync(cancellationToken);

        return options.Select(o => new AdminProductOptionDto
        {
            Id = o.Id,
            Name = o.Name,
            NameAr = o.NameAr,
            DisplayOrder = o.DisplayOrder,
            Values = values
                .Where(v => v.ProductOptionId == o.Id)
                .Select(v => new AdminProductOptionValueDto
                {
                    Id = v.Id,
                    Value = v.Value,
                    ValueAr = v.ValueAr,
                    DisplayOrder = v.DisplayOrder
                })
                .ToList()
        }).ToList();
    }

    public async Task<AdminProductOptionDto> AddOptionAsync(
        int productId,
        AddAdminProductOptionRequest request,
        CancellationToken cancellationToken = default)
    {
        await EnsureProductAsync(productId, cancellationToken);
        if (string.IsNullOrWhiteSpace(request.Name) || string.IsNullOrWhiteSpace(request.NameAr))
            throw new InvalidOperationException("Option name in English and Arabic are required.");

        var option = new ProductOption
        {
            ProductId = productId,
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsDeleted = false
        };
        _db.ProductOptions.Add(option);
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminProductOptionDto
        {
            Id = option.Id,
            Name = option.Name,
            NameAr = option.NameAr,
            DisplayOrder = option.DisplayOrder,
            Values = new List<AdminProductOptionValueDto>()
        };
    }

    public async Task DeleteOptionAsync(int productId, int optionId, CancellationToken cancellationToken = default)
    {
        var option = await _db.ProductOptions
            .FirstOrDefaultAsync(o => o.Id == optionId && o.ProductId == productId && !o.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Option not found.");

        var optionValues = await _db.ProductOptionValues
            .Where(v => v.ProductOptionId == optionId && !v.IsDeleted)
            .ToListAsync(cancellationToken);

        var valueIds = optionValues.Select(v => v.Id).ToList();
        var variantIds = valueIds.Count == 0
            ? new List<int>()
            : await _db.ProductVariantOptionValues.AsNoTracking()
                .Where(vov => valueIds.Contains(vov.ProductOptionValueId))
                .Select(vov => vov.ProductVariantId)
                .Distinct()
                .ToListAsync(cancellationToken);

        if (variantIds.Count > 0)
        {
            var inOrders = await _db.OrderDetails.AnyAsync(
                d => d.ProductVariantId.HasValue && variantIds.Contains(d.ProductVariantId.Value),
                cancellationToken);
            if (inOrders)
                throw new InvalidOperationException("Cannot delete option used in ordered variants.");

            var cartLines = await _db.ShoppingCartLines
                .Where(c => c.ProductVariantId.HasValue && variantIds.Contains(c.ProductVariantId.Value))
                .ToListAsync(cancellationToken);
            foreach (var line in cartLines)
                line.ProductVariantId = null;

            var links = await _db.ProductVariantOptionValues
                .Where(vov => valueIds.Contains(vov.ProductOptionValueId))
                .ToListAsync(cancellationToken);
            _db.ProductVariantOptionValues.RemoveRange(links);

            var variants = await _db.Set<ProductVariant>()
                .Where(v => variantIds.Contains(v.Id))
                .ToListAsync(cancellationToken);
            foreach (var variant in variants)
                variant.IsDeleted = true;
        }

        foreach (var value in optionValues)
            value.IsDeleted = true;

        option.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminProductOptionValueDto> AddOptionValueAsync(
        int productId,
        int optionId,
        AddAdminProductOptionValueRequest request,
        CancellationToken cancellationToken = default)
    {
        var option = await _db.ProductOptions
            .FirstOrDefaultAsync(o => o.Id == optionId && o.ProductId == productId && !o.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Option not found.");

        if (string.IsNullOrWhiteSpace(request.Value) || string.IsNullOrWhiteSpace(request.ValueAr))
            throw new InvalidOperationException("Value in English and Arabic are required.");

        var value = new ProductOptionValue
        {
            ProductOptionId = option.Id,
            Value = request.Value.Trim(),
            ValueAr = request.ValueAr.Trim(),
            DisplayOrder = request.DisplayOrder,
            IsDeleted = false
        };
        _db.ProductOptionValues.Add(value);
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminProductOptionValueDto
        {
            Id = value.Id,
            Value = value.Value,
            ValueAr = value.ValueAr,
            DisplayOrder = value.DisplayOrder
        };
    }

    public async Task DeleteOptionValueAsync(int productId, int valueId, CancellationToken cancellationToken = default)
    {
        var value = await _db.ProductOptionValues
            .Include(v => v.ProductOption)
            .FirstOrDefaultAsync(v => v.Id == valueId && !v.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Option value not found.");

        if (value.ProductOption?.ProductId != productId)
            throw new InvalidOperationException("Option value not found.");

        var variantIds = await _db.ProductVariantOptionValues.AsNoTracking()
            .Where(vov => vov.ProductOptionValueId == valueId)
            .Select(vov => vov.ProductVariantId)
            .Distinct()
            .ToListAsync(cancellationToken);

        if (variantIds.Count > 0)
        {
            var inOrders = await _db.OrderDetails.AnyAsync(
                d => d.ProductVariantId.HasValue && variantIds.Contains(d.ProductVariantId.Value),
                cancellationToken);
            if (inOrders)
                throw new InvalidOperationException("Cannot delete value used in ordered variants.");

            var cartLines = await _db.ShoppingCartLines
                .Where(c => c.ProductVariantId.HasValue && variantIds.Contains(c.ProductVariantId.Value))
                .ToListAsync(cancellationToken);
            foreach (var line in cartLines)
                line.ProductVariantId = null;

            var links = await _db.ProductVariantOptionValues
                .Where(vov => vov.ProductOptionValueId == valueId)
                .ToListAsync(cancellationToken);
            _db.ProductVariantOptionValues.RemoveRange(links);

            var variants = await _db.Set<ProductVariant>()
                .Where(v => variantIds.Contains(v.Id))
                .ToListAsync(cancellationToken);
            foreach (var variant in variants)
                variant.IsDeleted = true;
        }

        value.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<GenerateVariantsResponse> GenerateVariantsAsync(
        int productId,
        CancellationToken cancellationToken = default)
    {
        var product = await _db.Products
            .FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        var options = await _db.ProductOptions.AsNoTracking()
            .Where(o => o.ProductId == productId && !o.IsDeleted)
            .OrderBy(o => o.DisplayOrder)
            .ToListAsync(cancellationToken);

        if (options.Count == 0)
            throw new InvalidOperationException("No options defined for this product.");

        var optionIds = options.Select(o => o.Id).ToList();
        var allValues = await _db.ProductOptionValues.AsNoTracking()
            .Where(v => optionIds.Contains(v.ProductOptionId) && !v.IsDeleted)
            .OrderBy(v => v.DisplayOrder)
            .ToListAsync(cancellationToken);

        var valuesByOption = options.Select(o =>
        {
            var values = allValues.Where(v => v.ProductOptionId == o.Id).ToList();
            if (values.Count == 0)
                throw new InvalidOperationException($"Option '{o.Name}' has no values.");
            return values;
        }).ToList();

        var combinations = Cartesian(valuesByOption);
        var existingVariants = await _db.Set<ProductVariant>()
            .Where(v => v.ProductId == productId && !v.IsDeleted)
            .ToListAsync(cancellationToken);

        var existingVariantIds = existingVariants.Select(v => v.Id).ToList();
        var existingLinks = existingVariantIds.Count == 0
            ? new Dictionary<int, List<int>>()
            : await _db.ProductVariantOptionValues.AsNoTracking()
                .Where(vov => existingVariantIds.Contains(vov.ProductVariantId))
                .GroupBy(vov => vov.ProductVariantId)
                .ToDictionaryAsync(
                    g => g.Key,
                    g => g.Select(x => x.ProductOptionValueId).OrderBy(id => id).ToList(),
                    cancellationToken);

        var existingKeys = existingLinks.Values
            .Select(ids => string.Join(",", ids))
            .ToHashSet(StringComparer.Ordinal);

        product.ProductType = ProductType.Variable;
        var created = 0;
        var skipped = 0;
        var basePrice = (decimal)product.Price;

        foreach (var combo in combinations)
        {
            var key = string.Join(",", combo.Select(v => v.Id).OrderBy(id => id));
            if (existingKeys.Contains(key))
            {
                skipped++;
                continue;
            }

            var variant = new ProductVariant
            {
                ProductId = productId,
                Price = basePrice,
                ListPrice = (decimal?)product.ListPrice,
                StockQuantity = 0,
                MinimumStockAlert = product.MinimumStockAlert,
                IsDeleted = false
            };
            _db.Set<ProductVariant>().Add(variant);
            await _db.SaveChangesAsync(cancellationToken);

            foreach (var value in combo)
            {
                _db.ProductVariantOptionValues.Add(new ProductVariantOptionValue
                {
                    ProductVariantId = variant.Id,
                    ProductOptionValueId = value.Id
                });
            }

            existingKeys.Add(key);
            created++;
        }

        await _db.SaveChangesAsync(cancellationToken);

        return new GenerateVariantsResponse
        {
            Created = created,
            Skipped = skipped,
            Message = created > 0
                ? $"Created {created} variant(s), skipped {skipped} existing."
                : "No new variants created."
        };
    }

    public async Task<AdminProductVariantDto> UpdateVariantAsync(
        int productId,
        int variantId,
        UpdateAdminProductVariantDetailRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Price < 0 || request.StockQuantity < 0)
            throw new InvalidOperationException("Price and stock cannot be negative.");

        var variant = await _db.Set<ProductVariant>()
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId && !v.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Variant not found.");

        var previousStock = variant.StockQuantity;

        variant.Price = (decimal)request.Price;
        variant.StockQuantity = request.StockQuantity;
        variant.MinimumStockAlert = request.MinimumStockAlert;
        variant.Sku = string.IsNullOrWhiteSpace(request.Sku) ? null : request.Sku.Trim();
        variant.ExpiryDate = request.ExpiryDate;
        if (request.ListPrice is >= 0)
            variant.ListPrice = (decimal)request.ListPrice.Value;
        variant.Price50 = request.Price50 is >= 0 ? (decimal)request.Price50.Value : null;
        variant.Price100 = request.Price100 is >= 0 ? (decimal)request.Price100.Value : null;

        await _db.SaveChangesAsync(cancellationToken);

        if (previousStock == 0 && variant.StockQuantity > 0)
            await _stockFulfillment.ProcessProductRestockedAsync(productId, variantId, cancellationToken);

        return await MapVariantAsync(variant, cancellationToken);
    }

    public async Task<AdminImageUploadResultDto> UploadVariantImageAsync(
        int productId,
        int variantId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var variant = await _db.Set<ProductVariant>()
            .FirstOrDefaultAsync(v => v.Id == variantId && v.ProductId == productId && !v.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Variant not found.");

        var extension = Path.GetExtension(fileName);
        if (string.IsNullOrEmpty(extension) || !AllowedExtensions.Contains(extension))
            throw new InvalidOperationException("Unsupported image type. Use JPG, PNG, WEBP, or GIF.");

        _storage.EnsureProductVariantsDirectoryExists();
        TryDeletePhysicalFile(variant.ImageUrl);

        var storedName = $"{Guid.NewGuid():N}{extension.ToLowerInvariant()}";
        var physicalPath = Path.Combine(_storage.GetProductVariantsDirectory(), storedName);

        await using (var output = File.Create(physicalPath))
        {
            await fileStream.CopyToAsync(output, cancellationToken);
        }

        if (new FileInfo(physicalPath).Length > MaxUploadBytes)
        {
            File.Delete(physicalPath);
            throw new InvalidOperationException("Image must be 5 MB or smaller.");
        }

        var imageUrl = @"\Images\Products\Variants\" + storedName;
        variant.ImageUrl = imageUrl;
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminImageUploadResultDto { ImageUrl = imageUrl };
    }

    public async Task SetProductTypeAsync(int productId, string productType, CancellationToken cancellationToken = default)
    {
        var product = await _db.Products.FirstOrDefaultAsync(p => p.Id == productId, cancellationToken)
            ?? throw new InvalidOperationException("Product not found.");

        product.ProductType = string.Equals(productType, "Variable", StringComparison.OrdinalIgnoreCase)
            ? ProductType.Variable
            : ProductType.Simple;

        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task<AdminProductVariantDto> MapVariantAsync(ProductVariant variant, CancellationToken cancellationToken)
    {
        var valueIds = await _db.ProductVariantOptionValues.AsNoTracking()
            .Where(vov => vov.ProductVariantId == variant.Id)
            .Select(vov => vov.ProductOptionValueId)
            .ToListAsync(cancellationToken);

        string? variantName = null;
        if (valueIds.Count > 0)
        {
            var labels = await _db.ProductOptionValues.AsNoTracking()
                .Where(v => valueIds.Contains(v.Id))
                .Include(v => v.ProductOption)
                .OrderBy(v => v.ProductOption!.DisplayOrder)
                .Select(v => $"{v.ProductOption!.Name}: {v.Value}")
                .ToListAsync(cancellationToken);
            variantName = string.Join(" / ", labels);
        }

        return new AdminProductVariantDto
        {
            Id = variant.Id,
            Sku = variant.Sku,
            VariantName = variantName,
            ImageUrl = variant.ImageUrl,
            Price = (double)variant.Price,
            ListPrice = variant.ListPrice is null ? null : (double)variant.ListPrice,
            Price50 = variant.Price50 is null ? null : (double)variant.Price50,
            Price100 = variant.Price100 is null ? null : (double)variant.Price100,
            StockQuantity = variant.StockQuantity,
            MinimumStockAlert = variant.MinimumStockAlert,
            ExpiryDate = variant.ExpiryDate,
            IsDeleted = variant.IsDeleted
        };
    }

    private async Task EnsureProductAsync(int productId, CancellationToken cancellationToken)
    {
        var exists = await _db.Products.AnyAsync(p => p.Id == productId, cancellationToken);
        if (!exists)
            throw new InvalidOperationException("Product not found.");
    }

    private void TryDeletePhysicalFile(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return;

        var fileName = Path.GetFileName(imageUrl.TrimStart('\\', '/'));
        if (string.IsNullOrEmpty(fileName))
            return;

        var path = Path.Combine(_storage.GetProductVariantsDirectory(), fileName);
        if (File.Exists(path))
            File.Delete(path);
    }

    private static List<List<ProductOptionValue>> Cartesian(IReadOnlyList<List<ProductOptionValue>> sets)
    {
        var result = new List<List<ProductOptionValue>> { new() };
        foreach (var set in sets)
        {
            var next = new List<List<ProductOptionValue>>();
            foreach (var partial in result)
            {
                foreach (var item in set)
                {
                    var combo = new List<ProductOptionValue>(partial) { item };
                    next.Add(combo);
                }
            }
            result = next;
        }
        return result;
    }
}
