using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Promotions;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminComboOfferService : IAdminComboOfferService
{
    private readonly AppDbContext _db;
    private readonly LegacyImageStorage _images;
    private readonly IEmailService _email;

    public AdminComboOfferService(AppDbContext db, LegacyImageStorage images, IEmailService email)
    {
        _db = db;
        _images = images;
        _email = email;
    }

    public async Task<IReadOnlyList<AdminComboOfferListItemDto>> ListAsync(
        bool includeDeleted,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ComboOffers.AsNoTracking();
        if (!includeDeleted)
            query = query.Where(c => !c.IsDeleted);

        return await query
            .OrderBy(c => c.DisplayOrder)
            .ThenByDescending(c => c.StartDate)
            .Select(c => new AdminComboOfferListItemDto
            {
                Id = c.Id,
                Name = c.Name,
                NameAr = c.NameAr,
                ImageUrl = c.ImageUrl,
                ComboPrice = c.ComboPrice,
                StartDate = c.StartDate,
                EndDate = c.EndDate,
                IsActive = c.IsActive,
                IsDeleted = c.IsDeleted,
                ItemCount = c.Items.Count(i => !i.IsDeleted),
                DisplayOrder = c.DisplayOrder
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminComboOfferDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var combo = await _db.ComboOffers
            .AsNoTracking()
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken);

        return combo is null ? null : await MapDetailAsync(combo, cancellationToken);
    }

    public async Task<AdminComboOfferDetailDto> CreateAsync(
        UpsertAdminComboOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var combo = new ComboOffer
        {
            Name = request.Name.Trim(),
            NameAr = request.NameAr.Trim(),
            Description = request.Description?.Trim() ?? string.Empty,
            DescriptionAr = request.DescriptionAr?.Trim() ?? string.Empty,
            ImageUrl = request.ImageUrl?.Trim() ?? string.Empty,
            ComboPrice = request.ComboPrice,
            StartDate = request.StartDate,
            EndDate = request.EndDate,
            IsActive = request.IsActive,
            IsDeleted = false,
            MinimumQuantity = request.MinimumQuantity,
            MaximumQuantity = request.MaximumQuantity,
            DisplayOrder = request.DisplayOrder
        };

        _db.ComboOffers.Add(combo);
        await _db.SaveChangesAsync(cancellationToken);
        if (request.NotifySubscribers)
            await NotifySubscribersAsync($"New combo offer: {combo.Name}", $"<p>A new combo offer is live: <strong>{combo.Name}</strong>.</p>", cancellationToken);
        return await MapDetailAsync(combo, cancellationToken);
    }

    public async Task<AdminComboOfferDetailDto> UpdateAsync(
        int id,
        UpsertAdminComboOfferRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var combo = await _db.ComboOffers
            .Include(c => c.Items)
            .FirstOrDefaultAsync(c => c.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Combo offer not found.");

        combo.Name = request.Name.Trim();
        combo.NameAr = request.NameAr.Trim();
        combo.Description = request.Description?.Trim() ?? string.Empty;
        combo.DescriptionAr = request.DescriptionAr?.Trim() ?? string.Empty;
        combo.ImageUrl = request.ImageUrl?.Trim() ?? string.Empty;
        combo.ComboPrice = request.ComboPrice;
        combo.StartDate = request.StartDate;
        combo.EndDate = request.EndDate;
        combo.IsActive = request.IsActive;
        combo.MinimumQuantity = request.MinimumQuantity;
        combo.MaximumQuantity = request.MaximumQuantity;
        combo.DisplayOrder = request.DisplayOrder;

        await _db.SaveChangesAsync(cancellationToken);
        return await MapDetailAsync(combo, cancellationToken);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var combo = await _db.ComboOffers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Combo offer not found.");

        combo.IsActive = !combo.IsActive;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task SoftDeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var combo = await _db.ComboOffers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Combo offer not found.");

        combo.IsDeleted = true;
        combo.IsActive = false;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminComboOfferItemDto> AddItemAsync(
        int comboOfferId,
        AddAdminComboOfferItemRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.ProductId <= 0)
            throw new InvalidOperationException("Product is required.");
        if (request.Quantity <= 0)
            throw new InvalidOperationException("Quantity must be at least 1.");

        var combo = await _db.ComboOffers.FirstOrDefaultAsync(c => c.Id == comboOfferId && !c.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Combo offer not found.");

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

        var duplicate = await _db.ComboOfferItems.AnyAsync(
            i => i.ComboOfferId == comboOfferId &&
                 i.ProductId == request.ProductId &&
                 i.ProductVariantId == request.ProductVariantId &&
                 !i.IsDeleted,
            cancellationToken);
        if (duplicate)
            throw new InvalidOperationException("This product is already in the combo offer.");

        var maxOrder = await _db.ComboOfferItems
            .Where(i => i.ComboOfferId == comboOfferId && !i.IsDeleted)
            .Select(i => (int?)i.DisplayOrder)
            .MaxAsync(cancellationToken) ?? 0;

        var item = new ComboOfferItem
        {
            ComboOfferId = combo.Id,
            ProductId = request.ProductId,
            ProductVariantId = request.ProductVariantId,
            Quantity = request.Quantity,
            IsRequired = request.IsRequired,
            DisplayOrder = maxOrder + 1,
            IsDeleted = false
        };

        _db.ComboOfferItems.Add(item);
        await _db.SaveChangesAsync(cancellationToken);

        var product = await _db.Products.AsNoTracking()
            .FirstAsync(p => p.Id == request.ProductId, cancellationToken);

        return MapItem(item, product.Title);
    }

    public async Task RemoveItemAsync(int itemId, CancellationToken cancellationToken = default)
    {
        var item = await _db.ComboOfferItems.FirstOrDefaultAsync(i => i.Id == itemId && !i.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Combo offer item not found.");

        item.IsDeleted = true;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminImageUploadResultDto> UploadImageAsync(
        int id,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var combo = await _db.ComboOffers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted, cancellationToken)
            ?? throw new InvalidOperationException("Combo offer not found.");

        var previousUrl = combo.ImageUrl;
        var imageUrl = await _images.SaveAsync(LegacyMediaFolder.ComboOffers, fileStream, fileName, cancellationToken);
        combo.ImageUrl = imageUrl;
        await _db.SaveChangesAsync(cancellationToken);

        if (!string.IsNullOrEmpty(previousUrl) && !string.Equals(previousUrl, imageUrl, StringComparison.OrdinalIgnoreCase))
            _images.DeleteIfExists(previousUrl);

        return new AdminImageUploadResultDto { ImageUrl = imageUrl };
    }

    private static void ValidateRequest(UpsertAdminComboOfferRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
            throw new InvalidOperationException("Combo offer name is required.");
        if (string.IsNullOrWhiteSpace(request.NameAr))
            throw new InvalidOperationException("Arabic name is required.");
        if (request.ComboPrice <= 0)
            throw new InvalidOperationException("Combo price must be greater than 0.");
        if (request.EndDate <= request.StartDate)
            throw new InvalidOperationException("End date must be after start date.");
        if (request.MinimumQuantity <= 0)
            throw new InvalidOperationException("Minimum quantity must be at least 1.");
        if (request.MaximumQuantity is <= 0)
            throw new InvalidOperationException("Maximum quantity must be greater than 0 when set.");
    }

    private async Task<AdminComboOfferDetailDto> MapDetailAsync(ComboOffer combo, CancellationToken cancellationToken)
    {
        var productIds = combo.Items.Select(i => i.ProductId).Distinct().ToList();
        var products = productIds.Count == 0
            ? new Dictionary<int, string>()
            : await _db.Products.AsNoTracking()
                .Where(p => productIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Title, cancellationToken);

        return new AdminComboOfferDetailDto
        {
            Id = combo.Id,
            Name = combo.Name,
            NameAr = combo.NameAr,
            Description = combo.Description,
            DescriptionAr = combo.DescriptionAr,
            ImageUrl = combo.ImageUrl,
            ComboPrice = combo.ComboPrice,
            StartDate = combo.StartDate,
            EndDate = combo.EndDate,
            IsActive = combo.IsActive,
            IsDeleted = combo.IsDeleted,
            MinimumQuantity = combo.MinimumQuantity,
            MaximumQuantity = combo.MaximumQuantity,
            DisplayOrder = combo.DisplayOrder,
            Items = combo.Items
                .OrderBy(i => i.DisplayOrder)
                .Select(i => MapItem(i, products.GetValueOrDefault(i.ProductId, $"Product #{i.ProductId}")))
                .ToList()
        };
    }

    private static AdminComboOfferItemDto MapItem(ComboOfferItem item, string productTitle) => new()
    {
        Id = item.Id,
        ProductId = item.ProductId,
        ProductVariantId = item.ProductVariantId,
        ProductTitle = productTitle,
        Quantity = item.Quantity,
        IsRequired = item.IsRequired,
        DisplayOrder = item.DisplayOrder,
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
