using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Services;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class ServicePurchaseService : IServicePurchaseService
{
    private static readonly string[] PaidStatuses = ["Approved", PaymentStatuses.Paid];

    private readonly AppDbContext _db;

    public ServicePurchaseService(AppDbContext db) => _db = db;

    public async Task<IReadOnlyList<ServicePurchaseSummaryDto>> ListForUserAsync(
        string userId,
        CancellationToken cancellationToken = default)
    {
        var purchases = await _db.ServicePurchases
            .AsNoTracking()
            .Include(p => p.ServiceSubscription)
            .Where(p =>
                p.ApplicationUserId == userId
                && PaidStatuses.Contains(p.PaymentStatus))
            .OrderByDescending(p => p.PurchaseDate)
            .Take(50)
            .ToListAsync(cancellationToken);

        return purchases.Select(MapSummary).ToList();
    }

    public async Task<ServicePurchaseDetailDto?> GetForUserAsync(
        int purchaseId,
        string userId,
        CancellationToken cancellationToken = default)
    {
        var purchase = await _db.ServicePurchases
            .AsNoTracking()
            .Include(p => p.ServiceSubscription)
            .FirstOrDefaultAsync(
                p => p.Id == purchaseId && p.ApplicationUserId == userId,
                cancellationToken);

        if (purchase is null || !PaidStatuses.Contains(purchase.PaymentStatus))
            return null;

        var summary = MapSummary(purchase);
        var service = purchase.ServiceSubscription;

        return new ServicePurchaseDetailDto
        {
            Id = summary.Id,
            ServiceSubscriptionId = summary.ServiceSubscriptionId,
            ServiceTitle = summary.ServiceTitle,
            ServiceTitleAr = summary.ServiceTitleAr,
            ServiceImageUrl = summary.ServiceImageUrl,
            ServiceType = summary.ServiceType,
            TotalAmount = summary.TotalAmount,
            AmountPaid = summary.AmountPaid,
            DiscountAmount = summary.DiscountAmount,
            PaymentStatus = summary.PaymentStatus,
            Status = summary.Status,
            PurchaseDate = summary.PurchaseDate,
            ServiceDescription = service?.Description,
            ServiceDescriptionAr = service?.DescriptionAr
        };
    }

    private static ServicePurchaseSummaryDto MapSummary(Domain.Services.ServicePurchase purchase)
    {
        var service = purchase.ServiceSubscription;

        return new ServicePurchaseSummaryDto
        {
            Id = purchase.Id,
            ServiceSubscriptionId = purchase.ServiceSubscriptionId,
            ServiceTitle = service?.Title ?? "Service",
            ServiceTitleAr = service?.TitleAr,
            ServiceImageUrl = service?.ImageUrl,
            ServiceType = service?.ServiceType.ToString() ?? "Online",
            TotalAmount = (double)purchase.TotalAmount,
            AmountPaid = (double)purchase.AmountPaid,
            DiscountAmount = (double)purchase.DiscountAmount,
            PaymentStatus = purchase.PaymentStatus,
            Status = purchase.Status,
            PurchaseDate = purchase.PurchaseDate
        };
    }
}
