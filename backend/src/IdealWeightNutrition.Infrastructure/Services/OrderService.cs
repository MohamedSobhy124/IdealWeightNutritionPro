using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Orders;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using IdealWeightNutrition.Domain.Identity;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class OrderService : IOrderService
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _users;

    public OrderService(AppDbContext db, UserManager<ApplicationUser> users)
    {
        _db = db;
        _users = users;
    }

    public async Task<IReadOnlyList<OrderSummaryDto>> ListUserOrdersAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await _db.OrderHeaders
            .AsNoTracking()
            .Where(o => o.ApplicationUserId == userId)
            .OrderByDescending(o => o.OrderDate)
            .Take(50)
            .Select(o => new OrderSummaryDto
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                OrderStatus = o.OrderStatus ?? "Pending",
                PaymentStatus = o.PaymentStatus ?? "Pending",
                OrderTotal = o.OrderTotal
            })
            .ToListAsync(cancellationToken);

    public Task<OrderDto?> GetOrderAsync(
        int orderId,
        string? userId,
        string? guestEmail,
        CancellationToken cancellationToken = default) =>
        LoadOrderIfAuthorizedAsync(orderId, userId, guestEmail, cancellationToken);

    public Task<OrderDto?> TrackOrderAsync(
        TrackOrderRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        return LoadOrderIfAuthorizedAsync(request.OrderId, userId, email, cancellationToken);
    }

    private async Task<OrderDto?> LoadOrderIfAuthorizedAsync(
        int orderId,
        string? userId,
        string? guestEmail,
        CancellationToken cancellationToken)
    {
        var order = await _db.OrderHeaders
            .AsNoTracking()
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        if (order is null)
            return null;

        if (!await CanAccessOrderAsync(order, userId, guestEmail, cancellationToken))
            return null;

        return await MapOrderAsync(order, cancellationToken);
    }

    private async Task<bool> CanAccessOrderAsync(
        Domain.Checkout.OrderHeader order,
        string? userId,
        string? guestEmail,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(userId) && order.ApplicationUserId == userId)
            return true;

        if (string.IsNullOrWhiteSpace(guestEmail))
            return false;

        var normalizedEmail = guestEmail.Trim().ToLowerInvariant();

        if (order.IsGuestOrder)
            return order.Email?.Trim().ToLowerInvariant() == normalizedEmail;

        if (string.IsNullOrEmpty(order.ApplicationUserId))
            return order.Email?.Trim().ToLowerInvariant() == normalizedEmail;

        var user = await _users.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Id == order.ApplicationUserId, cancellationToken);

        return user?.Email?.Trim().ToLowerInvariant() == normalizedEmail;
    }

    private async Task<OrderDto> MapOrderAsync(
        Domain.Checkout.OrderHeader order,
        CancellationToken cancellationToken)
    {
        var productIds = order.Details.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        var items = order.Details.Select(d =>
        {
            products.TryGetValue(d.ProductId, out var product);
            var title = product?.Title ?? $"Product #{d.ProductId}";
            var slug = product?.GetSlug() ?? d.ProductId.ToString();

            return new OrderLineDto
            {
                OrderDetailId = d.Id,
                ProductId = d.ProductId,
                Title = title,
                Slug = slug,
                Quantity = d.Count,
                UnitPrice = d.Price,
                LineTotal = d.Price * d.Count
            };
        }).ToList();

        var subtotal = order.OrderSubtotal ?? items.Sum(i => i.LineTotal);
        var shipping = Math.Max(0, order.OrderTotal - subtotal + (order.DiscountAmount ?? 0));

        return new OrderDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            OrderStatus = order.OrderStatus ?? "Pending",
            PaymentStatus = order.PaymentStatus ?? "Pending",
            PaymentMethod = order.PaymentMethod,
            OrderTotal = order.OrderTotal,
            OrderSubtotal = subtotal,
            Shipping = shipping,
            Name = order.Name,
            Email = order.Email,
            PhoneNumber = order.PhoneNumber,
            StreetAddress = order.StreetAddress,
            City = order.City,
            Area = order.Area,
            Items = items
        };
    }
}
