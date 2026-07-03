using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Contracts.Orders;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminDashboardService : IAdminDashboardService
{
    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;

    public AdminDashboardService(AppDbContext db, IDateTimeProvider clock)
    {
        _db = db;
        _clock = clock;
    }

    public async Task<AdminDashboardDto> GetDashboardAsync(CancellationToken cancellationToken = default)
    {
        var todayStart = _clock.Now.Date;
        var tomorrowStart = todayStart.AddDays(1);

        var ordersToday = await _db.OrderHeaders
            .AsNoTracking()
            .Where(o => o.OrderDate >= todayStart && o.OrderDate < tomorrowStart)
            .ToListAsync(cancellationToken);

        var pendingReturns = await _db.ReturnRequests
            .AsNoTracking()
            .CountAsync(r => r.Status == ReturnStatuses.Pending, cancellationToken);

        var activeReturns = await _db.ReturnRequests
            .AsNoTracking()
            .CountAsync(
                r => r.Status == ReturnStatuses.Pending
                     || r.Status == ReturnStatuses.Approved
                     || r.Status == ReturnStatuses.Processing,
                cancellationToken);

        var simpleProducts = await _db.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.ProductType == ProductType.Simple)
            .Select(p => new { p.StockQuantity, p.MinimumStockAlert })
            .ToListAsync(cancellationToken);

        var variableVariants = await _db.Set<ProductVariant>()
            .AsNoTracking()
            .Where(v => !v.IsDeleted && _db.Products.Any(p => p.Id == v.ProductId && !p.IsDeleted && p.ProductType == ProductType.Variable))
            .Select(v => new { v.StockQuantity, v.MinimumStockAlert })
            .ToListAsync(cancellationToken);

        var stockSnapshots = simpleProducts
            .Select(p => (p.StockQuantity, p.MinimumStockAlert))
            .Concat(variableVariants.Select(v => (v.StockQuantity, v.MinimumStockAlert)))
            .ToList();

        var outOfStock = stockSnapshots.Count(s => s.StockQuantity == 0);
        var lowStock = stockSnapshots.Count(s => s.StockQuantity > 0 && s.StockQuantity <= s.MinimumStockAlert);

        var recentOrders = await _db.OrderHeaders
            .AsNoTracking()
            .OrderByDescending(o => o.OrderDate)
            .Take(8)
            .Select(o => new AdminOrderListItemDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                CustomerName = o.Name,
                Email = o.Email,
                OrderStatus = o.OrderStatus ?? OrderStatuses.Pending,
                PaymentStatus = o.PaymentStatus ?? PaymentStatuses.Pending,
                OrderTotal = o.OrderTotal,
                City = o.City,
                IsGuestOrder = o.IsGuestOrder
            })
            .ToListAsync(cancellationToken);

        var now = _clock.Now;
        var activeFlashSales = await _db.FlashSales
            .AsNoTracking()
            .CountAsync(
                f => !f.IsDeleted && f.IsActive && f.StartDate <= now && f.EndDate >= now,
                cancellationToken);

        var activeComboOffers = await _db.ComboOffers
            .AsNoTracking()
            .CountAsync(
                c => !c.IsDeleted && c.IsActive && c.StartDate <= now && c.EndDate >= now,
                cancellationToken);

        var publishedBlogPosts = await _db.BlogPosts
            .AsNoTracking()
            .CountAsync(b => !b.IsDeleted, cancellationToken);

        var hiddenBlogPosts = await _db.BlogPosts
            .AsNoTracking()
            .CountAsync(b => b.IsDeleted, cancellationToken);

        var activeServices = await _db.ServiceSubscriptions
            .AsNoTracking()
            .CountAsync(s => s.IsActive, cancellationToken);

        var activeCities = await _db.Cities
            .AsNoTracking()
            .CountAsync(c => c.IsActive, cancellationToken);

        var pendingStockNotifications = await _db.StockNotifications
            .AsNoTracking()
            .CountAsync(s => !s.IsDeleted && s.IsActive && !s.IsNotified, cancellationToken);

        var servicePurchases = await _db.ServicePurchases.AsNoTracking().ToListAsync(cancellationToken);

        var orderStatuses = await _db.OrderHeaders.AsNoTracking()
            .Select(o => o.OrderStatus ?? OrderStatuses.Pending)
            .ToListAsync(cancellationToken);

        return new AdminDashboardDto
        {
            OrdersToday = ordersToday.Count,
            RevenueToday = ordersToday.Sum(o => o.OrderTotal),
            PendingReturns = pendingReturns,
            ActiveReturns = activeReturns,
            LowStockProducts = lowStock,
            OutOfStockProducts = outOfStock,
            ActiveFlashSales = activeFlashSales,
            ActiveComboOffers = activeComboOffers,
            PublishedBlogPosts = publishedBlogPosts,
            HiddenBlogPosts = hiddenBlogPosts,
            ActiveServices = activeServices,
            ActiveCities = activeCities,
            PendingStockNotifications = pendingStockNotifications,
            ServicePurchasesTotal = servicePurchases.Count,
            ServicePurchasesPending = servicePurchases.Count(p => string.Equals(p.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase)),
            ServicePurchasesApproved = servicePurchases.Count(p => string.Equals(p.PaymentStatus, "Approved", StringComparison.OrdinalIgnoreCase)),
            ServicePurchasesRejected = servicePurchases.Count(p => string.Equals(p.PaymentStatus, "Rejected", StringComparison.OrdinalIgnoreCase)),
            OrdersTotal = orderStatuses.Count,
            OrdersPending = orderStatuses.Count(s => s == OrderStatuses.Pending),
            OrdersApproved = orderStatuses.Count(s => s is OrderStatuses.Approved or OrderStatuses.Paid),
            OrdersProcessing = orderStatuses.Count(s => s == OrderStatuses.Processing),
            OrdersShipped = orderStatuses.Count(s => s == OrderStatuses.Shipped),
            OrdersDelivered = orderStatuses.Count(s => s == OrderStatuses.Delivered),
            OrdersCancelled = orderStatuses.Count(s => s == OrderStatuses.Cancelled),
            RecentOrders = recentOrders
        };
    }
}
