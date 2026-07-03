using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Services;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Infrastructure.Storage;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminServiceSubscriptionService : IAdminServiceSubscriptionService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly LegacyImageStorage _images;

    public AdminServiceSubscriptionService(AppDbContext db, IDateTimeProvider clock, LegacyImageStorage images)
    {
        _db = db;
        _clock = clock;
        _images = images;
    }

    public async Task<IReadOnlyList<AdminServiceListItemDto>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ServiceSubscriptions.AsNoTracking();
        if (!includeInactive)
            query = query.Where(s => s.IsActive);

        return await query
            .OrderBy(s => s.DisplayOrder)
            .ThenByDescending(s => s.CreatedDate)
            .Select(s => new AdminServiceListItemDto
            {
                Id = s.Id,
                Title = s.Title,
                TitleAr = s.TitleAr,
                Price = (double)s.Price,
                ServiceType = s.ServiceType.ToString(),
                ImageUrl = s.ImageUrl,
                IsActive = s.IsActive,
                DisplayOrder = s.DisplayOrder,
                ImageCount = s.Images.Count,
                PurchaseCount = _db.ServicePurchases.Count(p => p.ServiceSubscriptionId == s.Id)
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<AdminServiceDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _db.ServiceSubscriptions
            .AsNoTracking()
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        return service is null ? null : MapDetail(service);
    }

    public async Task<AdminServiceDetailDto> CreateAsync(
        UpsertAdminServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var now = _clock.Now;
        var service = new ServiceSubscription
        {
            Title = request.Title.Trim(),
            TitleAr = request.TitleAr?.Trim(),
            Description = request.Description?.Trim(),
            DescriptionAr = request.DescriptionAr?.Trim(),
            Price = (decimal)request.Price,
            ServiceType = ParseServiceType(request.ServiceType),
            OfflinePaymentPercent = ResolveOfflinePercent(request),
            IsActive = request.IsActive,
            DisplayOrder = request.DisplayOrder,
            CreatedDate = now,
            UpdatedDate = now
        };

        _db.ServiceSubscriptions.Add(service);
        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(service);
    }

    public async Task<AdminServiceDetailDto> UpdateAsync(
        int id,
        UpsertAdminServiceRequest request,
        CancellationToken cancellationToken = default)
    {
        ValidateRequest(request);

        var service = await _db.ServiceSubscriptions
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Service not found.");

        service.Title = request.Title.Trim();
        service.TitleAr = request.TitleAr?.Trim();
        service.Description = request.Description?.Trim();
        service.DescriptionAr = request.DescriptionAr?.Trim();
        service.Price = (decimal)request.Price;
        service.ServiceType = ParseServiceType(request.ServiceType);
        service.OfflinePaymentPercent = ResolveOfflinePercent(request);
        service.IsActive = request.IsActive;
        service.DisplayOrder = request.DisplayOrder;
        service.UpdatedDate = _clock.Now;

        await _db.SaveChangesAsync(cancellationToken);
        return MapDetail(service);
    }

    public async Task ToggleActiveAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _db.ServiceSubscriptions.FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Service not found.");

        service.IsActive = !service.IsActive;
        service.UpdatedDate = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var service = await _db.ServiceSubscriptions
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Service not found.");

        var hasPurchases = await _db.ServicePurchases.AnyAsync(p => p.ServiceSubscriptionId == id, cancellationToken);
        if (hasPurchases)
            throw new InvalidOperationException("Cannot delete a service that has purchases. Deactivate it instead.");

        foreach (var image in service.Images)
            _images.DeleteIfExists(image.ImageUrl);

        _images.DeleteIfExists(service.ImageUrl);
        _db.ServiceSubscriptions.Remove(service);
        await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<AdminServiceImageDto> UploadImageAsync(
        int serviceId,
        Stream fileStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        var service = await _db.ServiceSubscriptions
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken)
            ?? throw new InvalidOperationException("Service not found.");

        var imageUrl = await _images.SaveAsync(LegacyMediaFolder.Services, fileStream, fileName, cancellationToken);
        var displayOrder = service.Images.Count == 0
            ? 0
            : service.Images.Max(i => i.DisplayOrder) + 1;

        var image = new ServiceImage
        {
            ServiceSubscriptionId = service.Id,
            ImageUrl = imageUrl,
            DisplayOrder = displayOrder
        };

        if (string.IsNullOrEmpty(service.ImageUrl))
            service.ImageUrl = imageUrl;

        service.UpdatedDate = _clock.Now;
        _db.ServiceImages.Add(image);
        await _db.SaveChangesAsync(cancellationToken);

        return new AdminServiceImageDto
        {
            Id = image.Id,
            ImageUrl = image.ImageUrl,
            DisplayOrder = image.DisplayOrder
        };
    }

    public async Task DeleteImageAsync(int serviceId, int imageId, CancellationToken cancellationToken = default)
    {
        var service = await _db.ServiceSubscriptions
            .Include(s => s.Images)
            .FirstOrDefaultAsync(s => s.Id == serviceId, cancellationToken)
            ?? throw new InvalidOperationException("Service not found.");

        var image = service.Images.FirstOrDefault(i => i.Id == imageId)
            ?? throw new InvalidOperationException("Image not found.");

        _images.DeleteIfExists(image.ImageUrl);
        _db.ServiceImages.Remove(image);
        service.Images.Remove(image);

        if (string.Equals(service.ImageUrl, image.ImageUrl, StringComparison.OrdinalIgnoreCase))
        {
            service.ImageUrl = service.Images
                .Where(i => i.Id != imageId)
                .OrderBy(i => i.DisplayOrder)
                .Select(i => i.ImageUrl)
                .FirstOrDefault();
        }

        service.UpdatedDate = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);
    }

    private static ServiceType ParseServiceType(string value) =>
        Enum.TryParse<ServiceType>(value, true, out var type) ? type : ServiceType.Online;

    private static decimal? ResolveOfflinePercent(UpsertAdminServiceRequest request)
    {
        var type = ParseServiceType(request.ServiceType);
        if (type != ServiceType.Offline)
            return null;

        if (request.OfflinePaymentPercent is null or <= 0 or > 100)
            throw new InvalidOperationException("Offline payment percent must be between 1 and 100.");

        return (decimal)request.OfflinePaymentPercent.Value;
    }

    private static void ValidateRequest(UpsertAdminServiceRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Title))
            throw new InvalidOperationException("Title is required.");
        if (request.Price <= 0)
            throw new InvalidOperationException("Price must be greater than 0.");
    }

    private static AdminServiceDetailDto MapDetail(ServiceSubscription service) => new()
    {
        Id = service.Id,
        Title = service.Title,
        TitleAr = service.TitleAr,
        Description = service.Description,
        DescriptionAr = service.DescriptionAr,
        Price = (double)service.Price,
        ServiceType = service.ServiceType.ToString(),
        OfflinePaymentPercent = service.OfflinePaymentPercent.HasValue
            ? (double)service.OfflinePaymentPercent.Value
            : null,
        ImageUrl = service.ImageUrl,
        IsActive = service.IsActive,
        DisplayOrder = service.DisplayOrder,
        CreatedDate = service.CreatedDate,
        UpdatedDate = service.UpdatedDate,
        Images = service.Images
            .OrderBy(i => i.DisplayOrder)
            .Select(i => new AdminServiceImageDto
            {
                Id = i.Id,
                ImageUrl = i.ImageUrl,
                DisplayOrder = i.DisplayOrder
            })
            .ToList()
    };
}
