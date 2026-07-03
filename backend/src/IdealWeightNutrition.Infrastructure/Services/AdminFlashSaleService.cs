using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminFlashSaleService : IAdminFlashSaleService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly LegacyImageStorage _images;
    private readonly IEmailService _email;

    public AdminFlashSaleService(AppDbContext db, IDateTimeProvider clock, LegacyImageStorage images, IEmailService email)
    {
        _db = db;
        _clock = clock;
        _images = images;
        _email = email;
    }

    public async Task<IReadOnlyList<AdminFlashSaleListItemDto>> ListAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var query = _db.FlashSales.AsNoTracking();
        if (!includeDeleted)
            query = query.Where(f => !f.IsDeleted);

        return await query
            .OrderByDescending(f => f.StartDate)
            .Select(f => new AdminFlashSaleListItemDto
            {
                Id = f.Id,
                Name = f.Name,
                NameAr = f.NameAr,
                ImageUrl = f.ImageUrl,
                StartDate = f.StartDate,
                EndDate = f.EndDate,
                IsActive = f.IsActive,
                IsDeleted = f.IsDeleted,
                ItemCount = f.Items.Count(i => !i.IsDeleted)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminFlashSaleDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var sale = await _db.FlashSales
            .AsNoTracking()
            .Include(f => f.Items)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken);

        return sale is null ? null : await MapDetailAsync(sale, cancellationToken);
    }

    public async Task<AdminFlashSaleDetailDto> CreateAsync(
        UpsertAdminFlashSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var now = _clock.Now;
        var sale = new FlashSale
        {
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            DescriptionAr = request.DescriptionAr?.Trim() ?? string.Empty,
            ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            IsDeleted = false,
            CreatedDate = now
        };

        _db.FlashSales.Add(sale);
        await _db.SaveChangesAsync(cancellationToken);
        if (request.NotifySubscribers)
            await NotifySubscribersAsync($"New flash sale: {sale.Name}", $"<p>A new flash sale is live: <strong>{sale.Name}</strong>.</p>", cancellationToken);
        return await MapDetailAsync(sale, cancellationToken);
    }

    public async Task<AdminFlashSaleDetailDto> UpdateAsync(
        int id,
        UpsertAdminFlashSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var sale = await _db.FlashSales
            .Include(f => f.Items)
            .FirstOrDefaultAsync(f => f.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Flash sale not found.");

        sale.Name = request.Name.Trim();
        sale.NameAr = request.NameAr.Trim();
        sale.Description = request.Description?.Trim() ?? string.Empty;
        sale.DescriptionAr = request.DescriptionAr?.Trim() ?? string.Empty;
        sale.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        sale.StartDate = request.StartDate;
        sale.EndDate = request.EndDate;
        sale.IsActive = request.IsActive;

        await _db.SaveChangesAsync(cancellationToken);
        return await MapDetailAsync(sale, cancellationToken);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var sale = await _db.FlashSales.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Flash sale not found.");

        sale.IsActive = !sale.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var sale = await _db.FlashSales.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Flash sale not found.");

        sale.IsDeleted = true;
        sale.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminFlashSaleItemDto> AddItemAsync(
        int flashSaleId,
        AddAdminFlashSaleItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId <= 0)
            throw new InvalidOperationException("Product is required.");
        if (request.FlashSalePrice <= 0)
            throw new InvalidOperationException("Flash sale price must be greater than 0.");
        if (request.FlashSaleQuantity <= 0)
            throw new InvalidOperationException("Flash sale quantity must be at least 1.");

        var sale = await _db.FlashSales.FirstOrDefaultAsync(f => f.Id == flashSaleId && !f.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Flash sale not found.");

        var productExists = await _db.Products.AnyAsync(p => p.Id == request.ProductId && !p.IsDeleted, cancellationToken);
        if (!productExists)
            throw new InvalidOperationException("Product not found.");

        if (request.ProductVariantId is > 0)
        {
            var variantExists = await _db.Set<ProductVariant>().AnyAsync(
                v => v.Id == request.ProductVariantId && v.ProductId == request.ProductId && !v.IsDeleted,
                cancellationToken);
            if (!variantExists)
                throw new InvalidOperationException("Product variant not found.");
        }

        var duplicate = await _db.FlashSaleItems.AnyAsync(
            i => i.FlashSaleId == flashSaleId &&
                 i.ProductId == request.ProductId &&
                 i.ProductVariantId == request.ProductVariantId &&
                 !i.IsDeleted,
            cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("This product is already in the flash sale.");

        var now = _clock.Now;
        var item = new FlashSaleItem
        {
            FlashSaleId = sale.Id,
            ProductId = request.ProductId,
            ProductVariantId = request.ProductVariantId,
            FlashSalePrice = request.FlashSalePrice,
            FlashSaleQuantity = request.FlashSaleQuantity,
            FlashSaleQuantityCreated = request.FlashSaleQuantity,
            AddedDate = now,
            CreatedDate = now,
            IsDeleted = false
        };

        _db.FlashSaleItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        var product = await _db.Products.AsNoTracking()
            .FirstAsync(p => p.Id == request.ProductId, cancellationToken);

        return MapItem(item, product.Title);
    }

    public async Task RemoveItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.FlashSaleItems.FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Flash sale item not found.");

        item.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminImageUploadResultDto> UploadImageAsync(
        int id,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var sale = await _db.FlashSales.FirstOrDefaultAsync(f => f.Id == id && !f.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Flash sale not found.");

        var previousUrl = sale.ImageUrl;
        var imageUrl = await _images.SaveAsync(LegacyMediaFolder.FlashSales, fileStream, fileName, cancellationToken);
        sale.ImageUrl = imageUrl;
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(previousUrl) && !string.Equals(previousUrl, imageUrl, StringComparison.OrdinalIgnoreCase))
            _images.DeleteIfExists(previousUrl);

        return new AdminImageUploadResultDto { ImageUrl = imageUrl };
    }

    private static void ValidateRequest(UpsertAdminFlashSaleRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Flash sale name is required.");
        if (string.IsNullOrWhiteSpace(request.NameAr))
            throw new InvalidOperationException("Arabic name is required.");
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");
    }

    private async Task<AdminFlashSaleDetailDto> MapDetailAsync(FlashSale sale, CancellationToken cancellationToken)
    {
        var productIds = sale.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Title, cancellationToken);

        return new AdminFlashSaleDetailDto
        {
            Id = sale.Id,
            Name = sale.Name,
            NameAr = sale.NameAr,
            Description = sale.Description,
            DescriptionAr = sale.DescriptionAr,
            ImageUrl = sale.ImageUrl,
            StartDate = sale.StartDate,
            EndDate = sale.EndDate,
            IsActive = sale.IsActive,
            IsDeleted = sale.IsDeleted,
            CreatedDate = sale.CreatedDate,
            Items = sale.Items
                .OrderBy(i => i.AddedDate)
                .Select(i => MapItem(i, products.GetValueOrDefault(i.ProductId, $"Product #{i.ProductId}")))
                .ToList()
        };
    }

    private static AdminFlashSaleItemDto MapItem(FlashSaleItem item, string productTitle) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductVariantId = item.ProductVariantId,
        ProductTitle = productTitle,
        FlashSalePrice = item.FlashSalePrice,
        FlashSaleQuantity = item.FlashSaleQuantity,
        FlashSaleQuantityCreated = item.FlashSaleQuantityCreated,
        AddedDate = item.AddedDate,
        IsDeleted = item.IsDeleted
    };

    private async Task NotifySubscribersAsync(string subject, string body, CancellationToken cancellationToken)
    {
        var emails = await _db.NewsletterSubscriptions.AsNoTracking()
            .Where(s => s.IsActive)
            .Select(s => s.Email)
            .ToListAsync(cancellationToken);
        foreach (var email in emails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            try { await _email.SendAsync(email, subject, body, cancellationToken); } catch { }
        }
    }
}
