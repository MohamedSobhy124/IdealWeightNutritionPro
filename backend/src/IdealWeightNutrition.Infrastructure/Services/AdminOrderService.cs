using System.Globalization;
using System.Security.Claims;
using System.Text;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Orders;
using IdealWeightNutrition.Contracts.Returns;
using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Infrastructure.Persistence;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Utility;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminOrderService : IAdminOrderService
{
    private const double VatRate = 0.05;

    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IHttpContextAccessor _httpContext;
    private readonly GeideaSettings _geidea;
    private readonly TamaraSettings _tamara;
    private readonly TappySettings _tabby;
    private readonly IPaymentService _payments;
    private readonly IOrderNotificationService _orderNotifications;
    private readonly ILogger<AdminOrderService> _logger;

    public AdminOrderService(
        AppDbContext db,
        IDateTimeProvider clock,
        IHttpContextAccessor httpContext,
        IOptions<GeideaSettings> geidea,
        IOptions<TamaraSettings> tamara,
        IOptions<TappySettings> tabby,
        IPaymentService payments,
        IOrderNotificationService orderNotifications,
        ILogger<AdminOrderService> logger)
    {
        _db = db;
        _clock = clock;
        _httpContext = httpContext;
        _geidea = geidea.Value;
        _tamara = tamara.Value;
        _tabby = tabby.Value;
        _payments = payments;
        _orderNotifications = orderNotifications;
        _logger = logger;
    }

    public async Task<AdminOrderListResponse> ListOrdersAsync(
        AdminOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var filtered = BuildQuery(query);
        var total = await filtered.CountAsync(cancellationToken);

        var items = await filtered
            .OrderByDescending(o => o.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
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

        return new AdminOrderListResponse
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<byte[]> ExportCsvAsync(
        AdminOrderQuery query,
        CancellationToken cancellationToken = default)
    {
        var orders = await BuildQuery(query)
            .OrderByDescending(o => o.Id)
            .ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine(
            "Order ID,Customer Name,Email,Phone,Order Date,Payment Method,Total Without VAT,VAT Amount,Total Inc VAT,Total(include delivery),Order Status,Payment Status");

        foreach (var order in orders)
        {
            var subtotal = order.OrderSubtotal ?? 0;
            var vatAmount = CalculateVat(order.OrderSubtotal, order.OrderTotal);
            var totalWithoutVat = subtotal - vatAmount;

            csv.Append(order.Id).Append(',')
                .Append(Csv(order.Name)).Append(',')
                .Append(Csv(order.Email ?? string.Empty)).Append(',')
                .Append(Csv(order.PhoneNumber)).Append(',')
                .Append(order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append(',')
                .Append(Csv(order.PaymentMethod ?? string.Empty)).Append(',')
                .Append(totalWithoutVat.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(vatAmount.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(subtotal.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(order.OrderTotal.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(Csv(order.OrderStatus ?? string.Empty)).Append(',')
                .Append(Csv(order.PaymentStatus ?? string.Empty))
                .AppendLine();
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public async Task<byte[]> ExportProductProfitsCsvAsync(
        DateTime? dateFrom,
        DateTime? dateTo,
        CancellationToken cancellationToken = default)
    {
        var successfulPaymentStatuses = new[]
        {
            PaymentStatuses.Paid,
            PaymentStatuses.DelayedPayment,
            "Authorized"
        };

        var ordersQuery = _db.OrderHeaders.AsNoTracking()
            .Where(o => successfulPaymentStatuses.Contains(o.PaymentStatus ?? string.Empty));

        if (dateFrom is not null)
            ordersQuery = ordersQuery.Where(o => o.OrderDate >= dateFrom.Value.Date);

        if (dateTo is not null)
            ordersQuery = ordersQuery.Where(o => o.OrderDate < dateTo.Value.Date.AddDays(1));

        var orderIds = await ordersQuery.Select(o => o.Id).ToListAsync(cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine(
            "Product ID,Product Title (EN),Product Title (AR),Store Cost,Total Quantity Sold,Total Revenue,Total Cost,Total Profit,Average Selling Price,Profit Per Unit,Profit %,Number of Orders");

        if (orderIds.Count == 0)
            return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();

        var details = await _db.OrderDetails.AsNoTracking()
            .Where(d => orderIds.Contains(d.OrderHeaderId))
            .Join(
                _db.Products.AsNoTracking().Where(p => p.StoreCost != null && p.StoreCost > 0),
                d => d.ProductId,
                p => p.Id,
                (detail, product) => new
                {
                    detail.OrderHeaderId,
                    detail.ProductId,
                    detail.Count,
                    detail.Price,
                    product.Title,
                    product.TitleAr,
                    StoreCost = product.StoreCost!.Value
                })
            .ToListAsync(cancellationToken);

        var productProfits = details
            .GroupBy(x => new { x.ProductId, x.Title, x.TitleAr, x.StoreCost })
            .Select(g =>
            {
                var totalQty = g.Sum(x => x.Count);
                var totalRevenue = g.Sum(x => x.Price * x.Count);
                var totalCost = g.Sum(x => x.StoreCost * x.Count);
                var totalProfit = g.Sum(x => (x.Price - x.StoreCost) * x.Count);
                var avgSelling = totalQty > 0 ? totalRevenue / totalQty : 0;
                var profitPerUnit = totalQty > 0 ? totalProfit / totalQty : 0;
                var profitPct = totalRevenue > 0 ? totalProfit / totalRevenue * 100 : 0;
                return new
                {
                    g.Key.ProductId,
                    g.Key.Title,
                    g.Key.TitleAr,
                    g.Key.StoreCost,
                    TotalQuantitySold = totalQty,
                    TotalRevenue = totalRevenue,
                    TotalCost = totalCost,
                    TotalProfit = totalProfit,
                    AverageSellingPrice = avgSelling,
                    ProfitPerUnit = profitPerUnit,
                    ProfitPercentage = profitPct,
                    OrderCount = g.Select(x => x.OrderHeaderId).Distinct().Count()
                };
            })
            .OrderByDescending(p => p.TotalProfit)
            .ToList();

        foreach (var profit in productProfits)
        {
            csv.Append(profit.ProductId).Append(',')
                .Append(Csv(profit.Title)).Append(',')
                .Append(Csv(profit.TitleAr)).Append(',')
                .Append(profit.StoreCost.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(profit.TotalQuantitySold).Append(',')
                .Append(profit.TotalRevenue.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(profit.TotalCost.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(profit.TotalProfit.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(profit.AverageSellingPrice.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(profit.ProfitPerUnit.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(profit.ProfitPercentage.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(profit.OrderCount)
                .AppendLine();
        }

        csv.AppendLine();
        csv.AppendLine("SUMMARY");
        csv.AppendLine($"Total Products,{productProfits.Count}");
        csv.AppendLine($"Total Quantity Sold,{productProfits.Sum(p => p.TotalQuantitySold)}");
        csv.AppendLine($"Total Revenue,{productProfits.Sum(p => p.TotalRevenue).ToString("F2", CultureInfo.InvariantCulture)}");
        csv.AppendLine($"Total Cost,{productProfits.Sum(p => p.TotalCost).ToString("F2", CultureInfo.InvariantCulture)}");
        csv.AppendLine($"Total Profit,{productProfits.Sum(p => p.TotalProfit).ToString("F2", CultureInfo.InvariantCulture)}");
        csv.AppendLine($"Total Orders,{orderIds.Count}");

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public async Task<AdminOrderStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var orders = await _db.OrderHeaders.AsNoTracking()
            .Select(o => o.OrderStatus ?? OrderStatuses.Pending)
            .ToListAsync(cancellationToken);

        return new AdminOrderStatisticsDto
        {
            All = orders.Count,
            Pending = orders.Count(s => s == OrderStatuses.Pending),
            Approved = orders.Count(s => s is OrderStatuses.Approved or OrderStatuses.Paid),
            Processing = orders.Count(s => s == OrderStatuses.Processing),
            Shipped = orders.Count(s => s == OrderStatuses.Shipped),
            Delivered = orders.Count(s => s == OrderStatuses.Delivered),
            Cancelled = orders.Count(s => s == OrderStatuses.Cancelled)
        };
    }

    public async Task<IReadOnlyList<AdminOrderAuditLogDto>> GetAuditLogsAsync(
        int orderId,
        CancellationToken cancellationToken = default) =>
        await _db.OrderAuditLogs.AsNoTracking()
            .Where(l => l.OrderHeaderId == orderId && !l.IsDeleted)
            .OrderByDescending(l => l.ActionDate)
            .Select(l => new AdminOrderAuditLogDto
            {
                Id = l.Id,
                OrderHeaderId = l.OrderHeaderId,
                Action = l.Action,
                ActionDetails = l.ActionDetails,
                PerformedByUserId = l.PerformedByUserId,
                PerformedByUserEmail = l.PerformedByUserEmail,
                OldOrderStatus = l.OldOrderStatus,
                NewOrderStatus = l.NewOrderStatus,
                OldPaymentStatus = l.OldPaymentStatus,
                NewPaymentStatus = l.NewPaymentStatus,
                ActionDate = l.ActionDate,
                IpAddress = l.IpAddress,
                UserAgent = l.UserAgent
            })
            .ToListAsync(cancellationToken);

    private IQueryable<Domain.Checkout.OrderHeader> BuildQuery(AdminOrderQuery query)
    {
        var q = _db.OrderHeaders.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.Status) && !string.Equals(query.Status, "all", StringComparison.OrdinalIgnoreCase))
            q = q.Where(o => o.OrderStatus == query.Status);

        if (!string.IsNullOrWhiteSpace(query.PaymentStatus))
            q = q.Where(o => o.PaymentStatus == query.PaymentStatus);

        if (!string.IsNullOrWhiteSpace(query.PaymentMethod))
            q = q.Where(o => o.PaymentMethod == query.PaymentMethod);

        if (query.DateFrom is not null)
            q = q.Where(o => o.OrderDate >= query.DateFrom.Value.Date);

        if (query.DateTo is not null)
        {
            var end = query.DateTo.Value.Date.AddDays(1);
            q = q.Where(o => o.OrderDate < end);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            q = q.Where(o =>
                o.Id.ToString().Contains(term)
                || (o.Name != null && o.Name.ToLower().Contains(term))
                || (o.Email != null && o.Email.ToLower().Contains(term))
                || (o.PhoneNumber != null && o.PhoneNumber.Contains(term))
                || (o.OrderStatus != null && o.OrderStatus.ToLower().Contains(term))
                || (o.PaymentStatus != null && o.PaymentStatus.ToLower().Contains(term))
                || (o.PaymentMethod != null && o.PaymentMethod.ToLower().Contains(term))
                || o.OrderTotal.ToString(CultureInfo.InvariantCulture).Contains(term));
        }

        return q;
    }

    private static double CalculateVat(double? orderSubtotal, double orderTotal)
    {
        if (orderSubtotal is > 0)
            return orderSubtotal.Value * (VatRate / (1 + VatRate));

        return orderTotal * (VatRate / (1 + VatRate));
    }

    private static string Csv(string value) =>
        $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";

    public async Task<AdminOrderDetailDto?> GetOrderAsync(int orderId, CancellationToken cancellationToken = default)
    {
        var order = await _db.OrderHeaders
            .AsNoTracking()
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);

        return order is null ? null : await MapOrderAsync(order, cancellationToken);
    }

    public async Task<AdminOrderActionResponse> StartProcessingAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await RequireOrderAsync(orderId, cancellationToken);

        if (order.OrderStatus == OrderStatuses.Cancelled)
            throw new InvalidOperationException("Cancelled orders cannot be processed.");

        if (order.OrderStatus == OrderStatuses.Delivered)
            throw new InvalidOperationException("Delivered orders cannot be moved back to processing.");

        var oldStatus = order.OrderStatus;
        order.OrderStatus = OrderStatuses.Processing;
        await _db.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(order.Id, "Start Processing", oldOrderStatus: oldStatus, newOrderStatus: order.OrderStatus, cancellationToken: cancellationToken);

        return ActionResponse(order, "Order marked as processing.");
    }

    public async Task<AdminOrderActionResponse> ShipOrderAsync(
        int orderId,
        ShipOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Carrier))
            throw new InvalidOperationException("Carrier is required.");
        if (string.IsNullOrWhiteSpace(request.TrackingNumber))
            throw new InvalidOperationException("Tracking number is required.");

        var order = await RequireOrderAsync(orderId, cancellationToken);

        if (order.OrderStatus == OrderStatuses.Cancelled)
            throw new InvalidOperationException("Cancelled orders cannot be shipped.");
        if (order.OrderStatus == OrderStatuses.Delivered)
            throw new InvalidOperationException("Order is already delivered.");

        var now = _clock.Now;
        var oldStatus = order.OrderStatus;
        order.Carrier = request.Carrier.Trim();
        order.TrackingNumber = request.TrackingNumber.Trim();
        order.OrderStatus = OrderStatuses.Shipped;
        order.ShippingDate = now;

        if (order.PaymentStatus == PaymentStatuses.DelayedPayment)
            order.PaymentDueDate = now.AddDays(30);

        await _db.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(
            order.Id,
            "Ship Order",
            actionDetails: $"Carrier: {order.Carrier}, Tracking: {order.TrackingNumber}",
            oldOrderStatus: oldStatus,
            newOrderStatus: order.OrderStatus,
            cancellationToken: cancellationToken);

        return ActionResponse(order, "Order marked as shipped.");
    }

    public async Task<AdminOrderActionResponse> MarkDeliveredAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await RequireOrderAsync(orderId, cancellationToken);

        if (order.OrderStatus != OrderStatuses.Shipped)
            throw new InvalidOperationException("Only shipped orders can be marked as delivered.");

        var oldStatus = order.OrderStatus;
        order.OrderStatus = OrderStatuses.Delivered;
        await _db.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(order.Id, "Mark Delivered", oldOrderStatus: oldStatus, newOrderStatus: order.OrderStatus, cancellationToken: cancellationToken);

        try
        {
            await _orderNotifications.SendOrderDeliveredAsync(order.Id, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to send delivery notification for order #{OrderId}", order.Id);
        }

        return ActionResponse(order, "Order marked as delivered.");
    }

    public async Task<AdminOrderActionResponse> CancelOrderAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await RequireOrderAsync(orderId, cancellationToken);

        if (order.OrderStatus == OrderStatuses.Cancelled)
            throw new InvalidOperationException("Order is already cancelled.");

        if (order.OrderStatus is OrderStatuses.Shipped or OrderStatuses.Delivered)
            throw new InvalidOperationException("Shipped or delivered orders cannot be cancelled.");

        if (order.PaymentMethod is PaymentMethods.Geidea or PaymentMethods.Tamara or PaymentMethods.Tabby)
        {
            throw new InvalidOperationException(
                "Online payment orders must be cancelled through the payment gateway integration (not yet available in the modern API).");
        }

        var oldOrderStatus = order.OrderStatus;
        var oldPaymentStatus = order.PaymentStatus;
        order.OrderStatus = OrderStatuses.Cancelled;
        order.PaymentStatus = PaymentStatuses.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(
            order.Id,
            "Cancel Order",
            oldOrderStatus: oldOrderStatus,
            newOrderStatus: order.OrderStatus,
            oldPaymentStatus: oldPaymentStatus,
            newPaymentStatus: order.PaymentStatus,
            cancellationToken: cancellationToken);

        return ActionResponse(order, "Order cancelled.");
    }

    public async Task<AdminOrderActionResponse> RecheckPaymentStatusAsync(
        int orderId,
        CancellationToken cancellationToken = default)
    {
        var order = await RequireOrderAsync(orderId, cancellationToken);
        if (order.PaymentMethod is not (PaymentMethods.Geidea or PaymentMethods.Tamara or PaymentMethods.Tabby))
            throw new InvalidOperationException("Payment recheck is only available for online payment orders.");

        var oldOrderStatus = order.OrderStatus;
        var oldPaymentStatus = order.PaymentStatus;
        var completed = await _payments.CompleteAsync(orderId, order.ApplicationUserId, null, cancellationToken);
        await LogAuditAsync(
            order.Id,
            "Recheck Payment Status",
            actionDetails: completed.Message,
            oldOrderStatus: oldOrderStatus,
            newOrderStatus: completed.OrderStatus,
            oldPaymentStatus: oldPaymentStatus,
            newPaymentStatus: completed.PaymentStatus,
            cancellationToken: cancellationToken);

        order = await RequireOrderAsync(orderId, cancellationToken);
        return ActionResponse(order, completed.Message ?? "Payment status rechecked.");
    }

    public async Task<AdminOrderActionResponse> ForceCompleteAsync(
        int orderId,
        ForceOrderActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await RequireOrderAsync(orderId, cancellationToken);
        var oldOrderStatus = order.OrderStatus;
        var oldPaymentStatus = order.PaymentStatus;

        order.OrderStatus = OrderStatuses.Delivered;
        if (order.PaymentStatus is null or PaymentStatuses.Pending or PaymentStatuses.Rejected)
            order.PaymentStatus = PaymentStatuses.Paid;
        order.ShippingDate = order.ShippingDate == DateTime.MinValue ? _clock.Now : order.ShippingDate;

        await _db.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(
            order.Id,
            "Force Complete Order",
            actionDetails: string.IsNullOrWhiteSpace(request.Reason) ? "No reason provided." : $"Reason: {request.Reason.Trim()}",
            oldOrderStatus: oldOrderStatus,
            newOrderStatus: order.OrderStatus,
            oldPaymentStatus: oldPaymentStatus,
            newPaymentStatus: order.PaymentStatus,
            cancellationToken: cancellationToken);

        return ActionResponse(order, "Order force-completed.");
    }

    public async Task<AdminOrderActionResponse> ForceCancelAsync(
        int orderId,
        ForceOrderActionRequest request,
        CancellationToken cancellationToken = default)
    {
        var order = await RequireOrderAsync(orderId, cancellationToken);
        var oldOrderStatus = order.OrderStatus;
        var oldPaymentStatus = order.PaymentStatus;

        order.OrderStatus = OrderStatuses.Cancelled;
        order.PaymentStatus = PaymentStatuses.Cancelled;
        await _db.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(
            order.Id,
            "Force Cancel Order",
            actionDetails: string.IsNullOrWhiteSpace(request.Reason) ? "No reason provided." : $"Reason: {request.Reason.Trim()}",
            oldOrderStatus: oldOrderStatus,
            newOrderStatus: order.OrderStatus,
            oldPaymentStatus: oldPaymentStatus,
            newPaymentStatus: order.PaymentStatus,
            cancellationToken: cancellationToken);

        return ActionResponse(order, "Order force-cancelled.");
    }

    public async Task<AdminOrderActionResponse> UpdateOrderLineAsync(
        int orderId,
        UpdateOrderLineRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.Quantity <= 0)
            throw new InvalidOperationException("Quantity must be greater than zero.");
        if (request.UnitPrice < 0)
            throw new InvalidOperationException("Unit price cannot be negative.");

        var order = await _db.OrderHeaders
            .Include(o => o.Details)
            .FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken)
            ?? throw new InvalidOperationException("Order not found.");

        var line = order.Details.FirstOrDefault(d => d.Id == request.OrderDetailId)
            ?? throw new InvalidOperationException("Order line not found.");

        var oldQuantity = line.Count;
        var oldUnitPrice = line.Price;
        var oldSubtotal = order.OrderSubtotal ?? order.Details.Sum(d => d.Price * d.Count);
        var discount = order.DiscountAmount ?? 0d;
        var shipping = Math.Max(0, order.OrderTotal - oldSubtotal + discount);

        line.Count = request.Quantity;
        line.Price = request.UnitPrice;

        var newSubtotal = order.Details.Sum(d => d.Price * d.Count);
        order.OrderSubtotal = newSubtotal;
        order.OrderTotal = Math.Max(0, newSubtotal - discount + shipping);

        await _db.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(
            order.Id,
            "Update Order Line",
            actionDetails:
            $"Line #{line.Id}: qty {oldQuantity} -> {line.Count}, price {oldUnitPrice:F2} -> {line.Price:F2}.",
            oldOrderStatus: order.OrderStatus,
            newOrderStatus: order.OrderStatus,
            oldPaymentStatus: order.PaymentStatus,
            newPaymentStatus: order.PaymentStatus,
            cancellationToken: cancellationToken);

        return ActionResponse(order, "Order line updated.");
    }

    public async Task<RefundOrderResponse> RefundOrderAsync(
        int orderId,
        RefundOrderRequest request,
        CancellationToken cancellationToken = default)
    {
        if (request.RefundAmount <= 0)
            throw new InvalidOperationException("Refund amount must be greater than 0.");

        var order = await RequireOrderAsync(orderId, cancellationToken);

        if (order.PaymentStatus != PaymentStatuses.Paid
            && order.PaymentStatus != PaymentStatuses.PartiallyRefunded)
            throw new InvalidOperationException("Only paid orders can be refunded.");

        if (order.OrderStatus == OrderStatuses.Cancelled)
            throw new InvalidOperationException("Cancelled orders cannot be refunded.");

        if ((decimal)request.RefundAmount > (decimal)order.OrderTotal)
            throw new InvalidOperationException("Refund amount cannot exceed order total.");

        var isFullRefund = request.RefundAmount >= (decimal)order.OrderTotal;
        string? gatewayRefundId = null;
        var oldOrderStatus = order.OrderStatus;
        var oldPaymentStatus = order.PaymentStatus;

        switch (order.PaymentMethod)
        {
            case PaymentMethods.Tamara when !string.IsNullOrEmpty(order.PaymentIntentId) && _tamara.Enabled:
            {
                var helper = new TamaraHelper(_tamara);
                var refund = await helper.RefundOrderAsync(order.PaymentIntentId, new TamaraRefundRequest
                {
                    TotalAmount = new TamaraAmount
                    {
                        Amount = request.RefundAmount,
                        Currency = _tamara.Currency ?? "AED"
                    },
                    Comment = request.Reason ?? $"Refund for order {order.Id}"
                });
                if (!refund.Success)
                    throw new InvalidOperationException(refund.Message ?? "Tamara refund failed.");
                gatewayRefundId = refund.RefundId;
                break;
            }
            case PaymentMethods.Geidea:
            {
                var helper = new GeideaHelper(_geidea);
                var refund = await helper.RefundPaymentAsync(
                    order.Id.ToString(),
                    request.RefundAmount,
                    reason: request.Reason);
                if (!refund.Success)
                    throw new InvalidOperationException(refund.Message ?? "Geidea refund failed.");
                gatewayRefundId = refund.RefundId;
                break;
            }
            case PaymentMethods.Tabby when !string.IsNullOrEmpty(order.SessionId):
            {
                var helper = new TappyHelper(_tabby);
                var refund = await helper.RefundPaymentAsync(
                    order.SessionId,
                    request.RefundAmount,
                    reason: request.Reason);
                if (!refund.Success)
                    throw new InvalidOperationException(refund.Message ?? "Tabby refund failed.");
                gatewayRefundId = refund.RefundId;
                break;
            }
            case PaymentMethods.Cod:
                break;
            default:
                throw new InvalidOperationException(
                    $"Refunds for payment method '{order.PaymentMethod}' are not supported yet.");
        }

        order.PaymentStatus = isFullRefund
            ? PaymentStatuses.Refunded
            : PaymentStatuses.PartiallyRefunded;
        if (isFullRefund)
            order.OrderStatus = OrderStatuses.Refunded;

        await _db.SaveChangesAsync(cancellationToken);
        await LogAuditAsync(
            order.Id,
            "Refund Order",
            actionDetails: $"Amount: AED {request.RefundAmount:F2}" + (string.IsNullOrWhiteSpace(request.Reason) ? "" : $", Reason: {request.Reason}"),
            oldOrderStatus: oldOrderStatus,
            newOrderStatus: order.OrderStatus,
            oldPaymentStatus: oldPaymentStatus,
            newPaymentStatus: order.PaymentStatus,
            cancellationToken: cancellationToken);

        return new RefundOrderResponse
        {
            OrderId = order.Id,
            OrderStatus = order.OrderStatus ?? OrderStatuses.Pending,
            PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Pending,
            RefundAmount = request.RefundAmount,
            GatewayRefundId = gatewayRefundId,
            Message = "Refund processed successfully."
        };
    }

    private async Task<Domain.Checkout.OrderHeader> RequireOrderAsync(
        int orderId,
        CancellationToken cancellationToken)
    {
        var order = await _db.OrderHeaders.FirstOrDefaultAsync(o => o.Id == orderId, cancellationToken);
        if (order is null)
            throw new InvalidOperationException("Order not found.");
        return order;
    }

    private static AdminOrderActionResponse ActionResponse(
        Domain.Checkout.OrderHeader order,
        string message) =>
        new()
        {
            OrderId = order.Id,
            OrderStatus = order.OrderStatus ?? OrderStatuses.Pending,
            PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Pending,
            Message = message
        };

    private async Task<AdminOrderDetailDto> MapOrderAsync(
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
        var shippingDate = order.ShippingDate == DateTime.MinValue ? (DateTime?)null : order.ShippingDate;

        return new AdminOrderDetailDto
        {
            Id = order.Id,
            OrderDate = order.OrderDate,
            ShippingDate = shippingDate,
            OrderStatus = order.OrderStatus ?? OrderStatuses.Pending,
            PaymentStatus = order.PaymentStatus ?? PaymentStatuses.Pending,
            PaymentMethod = order.PaymentMethod,
            OrderTotal = order.OrderTotal,
            OrderSubtotal = subtotal,
            DiscountAmount = order.DiscountAmount,
            PromoCodeText = order.PromoCodeText,
            Shipping = shipping,
            Name = order.Name,
            Email = order.Email,
            PhoneNumber = order.PhoneNumber,
            StreetAddress = order.StreetAddress,
            City = order.City,
            Area = order.Area,
            TrackingNumber = order.TrackingNumber,
            Carrier = order.Carrier,
            IsGuestOrder = order.IsGuestOrder,
            Items = items
        };
    }

    private async Task LogAuditAsync(
        int orderHeaderId,
        string action,
        string? actionDetails = null,
        string? oldOrderStatus = null,
        string? newOrderStatus = null,
        string? oldPaymentStatus = null,
        string? newPaymentStatus = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var http = _httpContext.HttpContext;
            var user = http?.User;
            var userId = user?.FindFirstValue(ClaimTypes.NameIdentifier);
            var userEmail = user?.FindFirstValue(ClaimTypes.Email);
            var ip = http?.Connection.RemoteIpAddress?.ToString();
            var userAgent = http?.Request.Headers.UserAgent.ToString();
            var now = _clock.Now;

            _db.OrderAuditLogs.Add(new OrderAuditLog
            {
                OrderHeaderId = orderHeaderId,
                Action = action,
                ActionDetails = Truncate(actionDetails, 500),
                PerformedByUserId = userId,
                PerformedByUserEmail = userEmail,
                OldOrderStatus = oldOrderStatus,
                NewOrderStatus = newOrderStatus,
                OldPaymentStatus = oldPaymentStatus,
                NewPaymentStatus = newPaymentStatus,
                ActionDate = now,
                IpAddress = Truncate(ip, 45),
                UserAgent = Truncate(userAgent, 500),
                CreatedBy = userId,
                CreatedDate = now,
                IsDeleted = false
            });
            await _db.SaveChangesAsync(cancellationToken);
        }
        catch
        {
            // Audit logging must not block order operations.
        }
    }

    private static string? Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value) || value.Length <= maxLength)
            return value;

        return value[..maxLength];
    }
}
