using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Returns;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Checkout;
using IdealWeightNutrition.Domain.Constants;
using IdealWeightNutrition.Domain.Returns;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class ReturnService : IReturnService
{
    private const int ReturnWindowDaysDelivered = 14;
    private const int ReturnWindowDaysShipped = 30;

    private readonly AppDbContext _db;
    private readonly IDateTimeProvider _clock;
    private readonly IInventoryService _inventory;
    private readonly IEmailService _email;
    private readonly IAdminNotificationService _adminNotifications;

    public ReturnService(
        AppDbContext db,
        IDateTimeProvider clock,
        IInventoryService inventory,
        IEmailService email,
        IAdminNotificationService adminNotifications)
    {
        _db = db;
        _clock = clock;
        _inventory = inventory;
        _email = email;
        _adminNotifications = adminNotifications;
    }

    public async Task<ReturnRequestDto> CreateReturnAsync(
        CreateReturnRequest request,
        string? userId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.Reason))
            throw new InvalidOperationException("Return reason is required.");
        if (request.Items.Count == 0)
            throw new InvalidOperationException("At least one item must be selected for return.");

        var order = await ResolveOrderForReturnAsync(request.OrderId, userId, request.Email, cancellationToken);

        await EnsureReturnEligibleAsync(order, cancellationToken);

        var orderDetails = await _db.OrderDetails
            .Where(d => d.OrderHeaderId == order.Id)
            .ToListAsync(cancellationToken);

        var productIds = orderDetails.Select(d => d.ProductId).Distinct().ToList();
        var products = await _db.Products
            .AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);

        decimal refundTotal = 0;
        var returnItems = new List<ReturnRequestItem>();

        foreach (var item in request.Items.Where(i => i.Quantity > 0))
        {
            var detail = orderDetails.FirstOrDefault(d => d.Id == item.OrderDetailId)
                ?? throw new InvalidOperationException($"Order line {item.OrderDetailId} not found.");

            if (item.Quantity > detail.Count)
                throw new InvalidOperationException("Return quantity cannot exceed ordered quantity.");

            var returnItem = new ReturnRequestItem
            {
                OrderDetailId = detail.Id,
                Quantity = item.Quantity,
                ReturnPrice = (decimal)detail.Price,
                ItemReason = item.ItemReason,
                ItemCondition = item.ItemCondition ?? "New"
            };
            returnItems.Add(returnItem);
            refundTotal += (decimal)detail.Price * item.Quantity;
        }

        if (returnItems.Count == 0)
            throw new InvalidOperationException("At least one item must be selected for return.");

        var isGuest = order.IsGuestOrder;
        var returnRequest = new ReturnRequest
        {
            OrderHeaderId = order.Id,
            ApplicationUserId = isGuest ? null : userId,
            Email = isGuest ? request.Email?.Trim() : order.Email,
            PhoneNumber = isGuest ? order.PhoneNumber : null,
            Reason = request.Reason.Trim(),
            AdditionalNotes = request.AdditionalNotes?.Trim(),
            Status = ReturnStatuses.Pending,
            RequestDate = _clock.Now,
            RefundAmount = refundTotal,
            RefundStatus = RefundStatuses.Pending,
            Items = returnItems
        };

        _db.ReturnRequests.Add(returnRequest);
        order.OrderStatus = OrderStatuses.ReturnRequested;
        await _db.SaveChangesAsync(cancellationToken);

        await TryNotifyCustomerAsync(returnRequest.Id, cancellationToken);
        await _adminNotifications.NotifyNewReturnRequestAsync(returnRequest.Id, cancellationToken);

        return await MapReturnAsync(returnRequest.Id, products, cancellationToken);
    }

    public async Task<IReadOnlyList<ReturnListItemDto>> ListUserReturnsAsync(
        string userId,
        CancellationToken cancellationToken = default) =>
        await _db.ReturnRequests
            .AsNoTracking()
            .Where(r => r.ApplicationUserId == userId)
            .OrderByDescending(r => r.RequestDate)
            .Select(r => new ReturnListItemDto
            {
                Id = r.Id,
                OrderId = r.OrderHeaderId,
                Status = r.Status,
                RequestDate = r.RequestDate,
                CustomerEmail = r.Email,
                RefundAmount = r.RefundAmount
            })
            .ToListAsync(cancellationToken);

    public async Task<ReturnRequestDto?> GetReturnAsync(
        int returnId,
        string? userId,
        string? guestEmail,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ReturnRequests.AsNoTracking().Where(r => r.Id == returnId);
        if (!string.IsNullOrEmpty(userId))
            query = query.Where(r => r.ApplicationUserId == userId);
        else if (!string.IsNullOrWhiteSpace(guestEmail))
        {
            var email = guestEmail.Trim().ToLowerInvariant();
            query = query.Where(r => r.Email != null && r.Email.ToLower() == email);
        }
        else
            return null;

        if (!await query.AnyAsync(cancellationToken))
            return null;

        return await MapReturnAsync(returnId, null, cancellationToken);
    }

    public async Task<IReadOnlyList<ReturnListItemDto>> ListAdminReturnsAsync(
        string? status,
        CancellationToken cancellationToken = default)
    {
        var query = _db.ReturnRequests.AsNoTracking();
        if (!string.IsNullOrWhiteSpace(status) && !status.Equals("all", StringComparison.OrdinalIgnoreCase))
            query = query.Where(r => r.Status == status);

        return await query
            .OrderByDescending(r => r.RequestDate)
            .Join(
                _db.OrderHeaders.AsNoTracking(),
                r => r.OrderHeaderId,
                o => o.Id,
                (r, o) => new ReturnListItemDto
                {
                    Id = r.Id,
                    OrderId = r.OrderHeaderId,
                    Status = r.Status,
                    RequestDate = r.RequestDate,
                    CustomerEmail = r.Email ?? o.Email,
                    RefundAmount = r.RefundAmount
                })
            .ToListAsync(cancellationToken);
    }

    public async Task<ReturnRequestDto?> GetAdminReturnAsync(int returnId, CancellationToken cancellationToken = default)
    {
        if (!await _db.ReturnRequests.AsNoTracking().AnyAsync(r => r.Id == returnId, cancellationToken))
            return null;

        return await MapReturnAsync(returnId, null, cancellationToken);
    }

    public async Task<ReturnActionResponse> ApproveReturnAsync(
        int returnId,
        ApproveReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        var returnRequest = await RequireReturnAsync(returnId, cancellationToken);
        if (returnRequest.Status != ReturnStatuses.Pending)
            throw new InvalidOperationException("Only pending return requests can be approved.");

        returnRequest.Status = ReturnStatuses.Approved;
        returnRequest.ApprovedDate = _clock.Now;
        returnRequest.AdminNotes = request.AdminNotes?.Trim();

        if (!string.IsNullOrWhiteSpace(request.ReturnTrackingNumber))
        {
            returnRequest.ReturnTrackingNumber = request.ReturnTrackingNumber.Trim();
            returnRequest.ReturnCarrier = request.ReturnCarrier?.Trim();
            returnRequest.ReturnShippedDate = _clock.Now;
        }

        var order = await _db.OrderHeaders.FirstAsync(o => o.Id == returnRequest.OrderHeaderId, cancellationToken);
        order.OrderStatus = OrderStatuses.ReturnApproved;
        await _db.SaveChangesAsync(cancellationToken);

        await TryNotifyCustomerAsync(returnRequest.Id, cancellationToken);

        return new ReturnActionResponse
        {
            ReturnId = returnRequest.Id,
            Status = returnRequest.Status,
            Message = "Return request approved."
        };
    }

    public async Task<ReturnActionResponse> RejectReturnAsync(
        int returnId,
        RejectReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(request.RejectionReason))
            throw new InvalidOperationException("Rejection reason is required.");

        var returnRequest = await RequireReturnAsync(returnId, cancellationToken);
        if (returnRequest.Status != ReturnStatuses.Pending)
            throw new InvalidOperationException("Only pending return requests can be rejected.");

        returnRequest.Status = ReturnStatuses.Rejected;
        returnRequest.RejectedDate = _clock.Now;
        returnRequest.RejectionReason = request.RejectionReason.Trim();
        returnRequest.AdminNotes = request.AdminNotes?.Trim();
        await _db.SaveChangesAsync(cancellationToken);

        await TryNotifyCustomerAsync(returnRequest.Id, cancellationToken);

        return new ReturnActionResponse
        {
            ReturnId = returnRequest.Id,
            Status = returnRequest.Status,
            Message = "Return request rejected."
        };
    }

    public async Task<ReturnActionResponse> MarkReturnReceivedAsync(
        int returnId,
        CancellationToken cancellationToken = default)
    {
        var returnRequest = await RequireReturnAsync(returnId, cancellationToken);
        if (returnRequest.Status is not ReturnStatuses.Approved and not ReturnStatuses.Processing)
            throw new InvalidOperationException("Only approved return requests can be marked as received.");

        returnRequest.Status = ReturnStatuses.Processing;
        returnRequest.ReturnReceivedDate = _clock.Now;
        await _db.SaveChangesAsync(cancellationToken);

        await TryNotifyCustomerAsync(returnRequest.Id, cancellationToken);

        return new ReturnActionResponse
        {
            ReturnId = returnRequest.Id,
            Status = returnRequest.Status,
            Message = "Return marked as received."
        };
    }

    public async Task<ReturnActionResponse> CompleteReturnAsync(
        int returnId,
        CompleteReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        var returnRequest = await RequireReturnAsync(returnId, cancellationToken);
        if (returnRequest.Status != ReturnStatuses.Processing)
            throw new InvalidOperationException("Only processing return requests can be completed.");

        returnRequest.Status = ReturnStatuses.Completed;
        returnRequest.CompletedDate = _clock.Now;
        returnRequest.RefundStatus = RefundStatuses.Processed;
        returnRequest.RefundProcessedDate = _clock.Now;
        returnRequest.RefundTransactionId = request.RefundTransactionId?.Trim();

        var order = await _db.OrderHeaders.FirstAsync(o => o.Id == returnRequest.OrderHeaderId, cancellationToken);
        order.OrderStatus = OrderStatuses.Returned;
        order.PaymentStatus = PaymentStatuses.Refunded;

        await _db.SaveChangesAsync(cancellationToken);

        try
        {
            await _inventory.RestoreStockForReturnAsync(returnId, cancellationToken);
        }
        catch
        {
            // Stock restoration must not block return completion (legacy parity).
        }

        await TryNotifyCustomerAsync(returnRequest.Id, cancellationToken);

        return new ReturnActionResponse
        {
            ReturnId = returnRequest.Id,
            Status = returnRequest.Status,
            Message = "Return completed and refund recorded."
        };
    }

    public async Task<ReturnActionResponse> CancelReturnAsync(
        int returnId,
        CancelReturnRequest request,
        CancellationToken cancellationToken = default)
    {
        var returnRequest = await RequireReturnAsync(returnId, cancellationToken);
        if (returnRequest.Status is ReturnStatuses.Completed or ReturnStatuses.Rejected or ReturnStatuses.Cancelled)
            throw new InvalidOperationException("This return request cannot be cancelled.");

        returnRequest.Status = ReturnStatuses.Cancelled;
        returnRequest.AdminNotes = request.AdminNotes?.Trim() ?? returnRequest.AdminNotes;
        if (!string.IsNullOrWhiteSpace(request.Reason))
            returnRequest.RejectionReason = request.Reason.Trim();

        var order = await _db.OrderHeaders.FirstAsync(o => o.Id == returnRequest.OrderHeaderId, cancellationToken);
        if (order.OrderStatus == OrderStatuses.ReturnRequested
            || order.OrderStatus == OrderStatuses.ReturnApproved
            || order.OrderStatus == OrderStatuses.Returned)
        {
            order.OrderStatus = OrderStatuses.Delivered;
        }

        await _db.SaveChangesAsync(cancellationToken);
        await TryNotifyCustomerAsync(returnRequest.Id, cancellationToken);

        return new ReturnActionResponse
        {
            ReturnId = returnRequest.Id,
            Status = returnRequest.Status,
            Message = "Return request cancelled."
        };
    }

    private async Task<OrderHeader> ResolveOrderForReturnAsync(
        int orderId,
        string? userId,
        string? email,
        CancellationToken cancellationToken)
    {
        OrderHeader? order;

        if (!string.IsNullOrEmpty(userId))
        {
            order = await _db.OrderHeaders.FirstOrDefaultAsync(
                o => o.Id == orderId && o.ApplicationUserId == userId && !o.IsGuestOrder,
                cancellationToken);
        }
        else
        {
            if (string.IsNullOrWhiteSpace(email))
                throw new InvalidOperationException("Email is required for guest return requests.");

            var normalized = email.Trim().ToLowerInvariant();
            order = await _db.OrderHeaders.FirstOrDefaultAsync(
                o => o.Id == orderId
                     && o.IsGuestOrder
                     && o.Email != null
                     && o.Email.ToLower() == normalized,
                cancellationToken);
        }

        return order ?? throw new InvalidOperationException("Order not found or access denied.");
    }

    private async Task EnsureReturnEligibleAsync(OrderHeader order, CancellationToken cancellationToken)
    {
        if (order.OrderStatus is not OrderStatuses.Delivered and not OrderStatuses.Shipped)
            throw new InvalidOperationException("Only shipped or delivered orders can be returned.");

        var deadline = order.OrderStatus == OrderStatuses.Delivered
            ? order.ShippingDate.AddDays(ReturnWindowDaysDelivered)
            : order.ShippingDate.AddDays(ReturnWindowDaysShipped);

        if (_clock.Now > deadline)
            throw new InvalidOperationException("The return window for this order has passed.");

        var hasActive = await _db.ReturnRequests.AnyAsync(
            r => r.OrderHeaderId == order.Id
                 && (r.Status == ReturnStatuses.Pending
                     || r.Status == ReturnStatuses.Approved
                     || r.Status == ReturnStatuses.Processing),
            cancellationToken);

        if (hasActive)
            throw new InvalidOperationException("A return request already exists for this order.");
    }

    private async Task<ReturnRequest> RequireReturnAsync(int returnId, CancellationToken cancellationToken) =>
        await _db.ReturnRequests.FirstOrDefaultAsync(r => r.Id == returnId, cancellationToken)
        ?? throw new InvalidOperationException("Return request not found.");

    private async Task<ReturnRequestDto> MapReturnAsync(
        int returnId,
        Dictionary<int, Product>? products,
        CancellationToken cancellationToken)
    {
        var returnRequest = await _db.ReturnRequests
            .AsNoTracking()
            .Include(r => r.Items)
            .FirstAsync(r => r.Id == returnId, cancellationToken);

        products ??= await LoadProductsForReturnItemsAsync(returnRequest.Items, cancellationToken);

        var detailIds = returnRequest.Items.Select(i => i.OrderDetailId).ToList();
        var details = await _db.OrderDetails.AsNoTracking()
            .Where(d => detailIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, cancellationToken);

        var items = returnRequest.Items.Select(i =>
        {
            details.TryGetValue(i.OrderDetailId, out var detail);
            var productId = detail?.ProductId ?? 0;
            products.TryGetValue(productId, out var product);

            return new ReturnItemDto
            {
                Id = i.Id,
                OrderDetailId = i.OrderDetailId,
                ProductId = productId,
                ProductTitle = product?.Title ?? $"Product #{productId}",
                Quantity = i.Quantity,
                ReturnPrice = i.ReturnPrice,
                ItemReason = i.ItemReason,
                ItemCondition = i.ItemCondition
            };
        }).ToList();

        return new ReturnRequestDto
        {
            Id = returnRequest.Id,
            OrderId = returnRequest.OrderHeaderId,
            Status = returnRequest.Status,
            RequestDate = returnRequest.RequestDate,
            Reason = returnRequest.Reason,
            AdditionalNotes = returnRequest.AdditionalNotes,
            RejectionReason = returnRequest.RejectionReason,
            RefundAmount = returnRequest.RefundAmount,
            RefundStatus = returnRequest.RefundStatus,
            Items = items
        };
    }

    private async Task<Dictionary<int, Product>> LoadProductsForReturnItemsAsync(
        IEnumerable<ReturnRequestItem> items,
        CancellationToken cancellationToken)
    {
        var detailIds = items.Select(i => i.OrderDetailId).ToList();
        var details = await _db.OrderDetails.AsNoTracking()
            .Where(d => detailIds.Contains(d.Id))
            .ToListAsync(cancellationToken);
        var productIds = details.Select(d => d.ProductId).Distinct().ToList();
        return await _db.Products.AsNoTracking()
            .Where(p => productIds.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id, cancellationToken);
    }

    private async Task TryNotifyCustomerAsync(int returnId, CancellationToken cancellationToken)
    {
        try
        {
            var returnRequest = await _db.ReturnRequests
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.Id == returnId, cancellationToken);

            if (returnRequest is null)
                return;

            var customerEmail = returnRequest.Email;
            if (string.IsNullOrWhiteSpace(customerEmail) && !string.IsNullOrEmpty(returnRequest.ApplicationUserId))
            {
                customerEmail = await _db.Users
                    .AsNoTracking()
                    .Where(u => u.Id == returnRequest.ApplicationUserId)
                    .Select(u => u.Email)
                    .FirstOrDefaultAsync(cancellationToken);
            }

            if (string.IsNullOrWhiteSpace(customerEmail))
                return;

            var subject = returnRequest.Status switch
            {
                ReturnStatuses.Approved => $"Return request #{returnRequest.Id} approved",
                ReturnStatuses.Rejected => $"Return request #{returnRequest.Id} rejected",
                ReturnStatuses.Processing => $"Return request #{returnRequest.Id} received",
                ReturnStatuses.Completed => $"Return request #{returnRequest.Id} completed",
                _ => $"Return request #{returnRequest.Id} submitted"
            };

            var body = $"""
                <p>Hello,</p>
                <p>Your return request <strong>#{returnRequest.Id}</strong> for order <strong>#{returnRequest.OrderHeaderId}</strong> is now <strong>{returnRequest.Status}</strong>.</p>
                {(returnRequest.RejectionReason is not null ? $"<p>Reason: {System.Net.WebUtility.HtmlEncode(returnRequest.RejectionReason)}</p>" : "")}
                {(returnRequest.RefundAmount is not null && returnRequest.Status == ReturnStatuses.Completed
                    ? $"<p>Refund amount: AED {returnRequest.RefundAmount:N2}</p>"
                    : "")}
                <p>Thank you,<br/>Ideal Weight Nutrition</p>
                """;

            await _email.SendAsync(customerEmail, subject, body, cancellationToken);
        }
        catch
        {
            // Email failures must not block return workflow.
        }
    }
}
