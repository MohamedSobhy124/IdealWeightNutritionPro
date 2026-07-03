using IdealWeightNutrition.Contracts.Orders;

namespace IdealWeightNutrition.Contracts.Admin;

public sealed class AdminDashboardDto
{
    public required int OrdersToday { get; init; }
    public required double RevenueToday { get; init; }
    public required int PendingReturns { get; init; }
    public required int ActiveReturns { get; init; }
    public required int LowStockProducts { get; init; }
    public required int OutOfStockProducts { get; init; }
    public required int ActiveFlashSales { get; init; }
    public required int ActiveComboOffers { get; init; }
    public required int PublishedBlogPosts { get; init; }
    public required int HiddenBlogPosts { get; init; }
    public required int ActiveServices { get; init; }
    public required int ActiveCities { get; init; }
    public required int PendingStockNotifications { get; init; }
    public required int ServicePurchasesTotal { get; init; }
    public required int ServicePurchasesPending { get; init; }
    public required int ServicePurchasesApproved { get; init; }
    public required int ServicePurchasesRejected { get; init; }
    public required int OrdersTotal { get; init; }
    public required int OrdersPending { get; init; }
    public required int OrdersApproved { get; init; }
    public required int OrdersProcessing { get; init; }
    public required int OrdersShipped { get; init; }
    public required int OrdersDelivered { get; init; }
    public required int OrdersCancelled { get; init; }
    public required IReadOnlyList<AdminOrderListItemDto> RecentOrders { get; init; }
}
