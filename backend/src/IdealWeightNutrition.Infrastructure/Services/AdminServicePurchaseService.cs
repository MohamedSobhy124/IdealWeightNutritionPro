using System.Globalization;
using System.Text;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Contracts.Admin;
using IdealWeightNutrition.Domain.Identity;
using IdealWeightNutrition.Domain.Services;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace IdealWeightNutrition.Infrastructure.Services;

internal sealed class AdminServicePurchaseService : IAdminServicePurchaseService
{
    private const decimal VatRate = 0.05m;

    private readonly AppDbContext _db;

    public AdminServicePurchaseService(AppDbContext db) => _db = db;

    public async Task<AdminServicePurchaseListResponse> ListAsync(
        AdminServicePurchaseQuery query,
        CancellationToken cancellationToken = default)
    {
        var page = Math.Max(1, query.Page);
        var pageSize = Math.Clamp(query.PageSize, 1, 100);

        var filtered = await BuildQuery(query)
            .Include(p => p.ServiceSubscription)
            .ToListAsync(cancellationToken);
        var total = filtered.Count;
        var pageItems = filtered
            .OrderByDescending(p => p.PurchaseDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToList();

        var items = await MapListAsync(pageItems, cancellationToken);

        return new AdminServicePurchaseListResponse
        {
            Items = items,
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<AdminServicePurchaseDetailDto?> GetAsync(int id, CancellationToken cancellationToken = default)
    {
        var purchase = await _db.ServicePurchases.AsNoTracking()
            .Include(p => p.ServiceSubscription)
            .FirstOrDefaultAsync(p => p.Id == id, cancellationToken);

        if (purchase is null)
            return null;

        var list = await MapListAsync([purchase], cancellationToken);
        var summary = list[0];
        string? offerSummary = null;

        if (purchase.ServiceOfferId is > 0)
        {
            var offer = await _db.Set<ServiceOffer>().AsNoTracking()
                .FirstOrDefaultAsync(o => o.Id == purchase.ServiceOfferId, cancellationToken);
            if (offer is not null)
            {
                offerSummary = offer.DiscountType == ServiceDiscountType.Percentage
                    ? $"{offer.DiscountValue:0.##}% off"
                    : $"AED {offer.DiscountValue:0.##} off";
            }
        }

        return new AdminServicePurchaseDetailDto
        {
            Id = summary.Id,
            ServiceSubscriptionId = summary.ServiceSubscriptionId,
            ServiceTitle = summary.ServiceTitle,
            CustomerName = summary.CustomerName,
            Email = summary.Email,
            Phone = summary.Phone,
            TotalAmount = summary.TotalAmount,
            AmountPaid = summary.AmountPaid,
            DiscountAmount = summary.DiscountAmount,
            VatAmount = summary.VatAmount,
            PaymentStatus = summary.PaymentStatus,
            ServiceStatus = summary.ServiceStatus,
            PurchaseDate = summary.PurchaseDate,
            ServiceOfferId = purchase.ServiceOfferId,
            PaymentIntentId = purchase.PaymentIntentId,
            SessionId = purchase.SessionId,
            OfferSummary = offerSummary,
            IsGuest = string.IsNullOrEmpty(purchase.ApplicationUserId)
        };
    }

    public async Task<byte[]> ExportCsvAsync(
        AdminServicePurchaseQuery query,
        CancellationToken cancellationToken = default)
    {
        var purchases = await BuildQuery(query)
            .Include(p => p.ServiceSubscription)
            .OrderByDescending(p => p.Id)
            .ToListAsync(cancellationToken);

        var rows = await MapListAsync(purchases, cancellationToken);

        var csv = new StringBuilder();
        csv.AppendLine(
            "Purchase ID,Service Title,Customer Name,Email,Phone,Purchase Date,Total Without VAT,VAT Amount,Total Inc VAT,Discount Amount,Amount Paid,Remaining Amount,Payment Status,Service Status");

        foreach (var row in rows)
        {
            var totalIncVat = row.TotalAmount - row.DiscountAmount;
            var totalWithoutVat = totalIncVat - row.VatAmount;
            var remaining = row.TotalAmount - row.AmountPaid;

            csv.Append(row.Id).Append(',')
                .Append(Csv(row.ServiceTitle)).Append(',')
                .Append(Csv(row.CustomerName)).Append(',')
                .Append(Csv(row.Email ?? string.Empty)).Append(',')
                .Append(Csv(row.Phone ?? string.Empty)).Append(',')
                .Append(row.PurchaseDate.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture)).Append(',')
                .Append(totalWithoutVat.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.VatAmount.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(totalIncVat.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.DiscountAmount.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.AmountPaid.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(remaining.ToString("F2", CultureInfo.InvariantCulture)).Append(',')
                .Append(Csv(row.PaymentStatus)).Append(',')
                .Append(Csv(row.ServiceStatus))
                .AppendLine();
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
    }

    public async Task<AdminServicePurchaseStatisticsDto> GetStatisticsAsync(CancellationToken cancellationToken = default)
    {
        var purchases = await _db.ServicePurchases.AsNoTracking().ToListAsync(cancellationToken);
        return new AdminServicePurchaseStatisticsDto
        {
            All = purchases.Count,
            Pending = purchases.Count(p => string.Equals(p.PaymentStatus, "Pending", StringComparison.OrdinalIgnoreCase)),
            Approved = purchases.Count(p => string.Equals(p.PaymentStatus, "Approved", StringComparison.OrdinalIgnoreCase)),
            Rejected = purchases.Count(p => string.Equals(p.PaymentStatus, "Rejected", StringComparison.OrdinalIgnoreCase))
        };
    }

    public async Task<AdminServicePurchaseActionResponse> UpdateAsync(
        int id,
        UpdateAdminServicePurchaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var purchase = await _db.ServicePurchases.FirstOrDefaultAsync(p => p.Id == id, cancellationToken)
            ?? throw new InvalidOperationException("Service purchase not found.");

        if (request.AmountPaid is < 0)
            throw new InvalidOperationException("Amount paid cannot be negative.");
        if (request.AmountPaid is > 0 && request.AmountPaid > purchase.TotalAmount)
            throw new InvalidOperationException("Amount paid cannot exceed total amount.");

        if (!string.IsNullOrWhiteSpace(request.PaymentStatus))
            purchase.PaymentStatus = request.PaymentStatus.Trim();
        if (!string.IsNullOrWhiteSpace(request.ServiceStatus))
            purchase.Status = request.ServiceStatus.Trim();
        if (request.AmountPaid is not null)
            purchase.AmountPaid = request.AmountPaid.Value;

        await _db.SaveChangesAsync(cancellationToken);

        return new AdminServicePurchaseActionResponse
        {
            PurchaseId = purchase.Id,
            PaymentStatus = purchase.PaymentStatus,
            ServiceStatus = purchase.Status,
            AmountPaid = (double)purchase.AmountPaid,
            Message = "Service purchase updated."
        };
    }

    private IQueryable<ServicePurchase> BuildQuery(AdminServicePurchaseQuery query)
    {
        var q = _db.ServicePurchases.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.PaymentStatus))
            q = q.Where(p => p.PaymentStatus == query.PaymentStatus);

        if (!string.IsNullOrWhiteSpace(query.ServiceStatus))
            q = q.Where(p => p.Status == query.ServiceStatus);

        if (query.DateFrom is not null)
            q = q.Where(p => p.PurchaseDate >= query.DateFrom.Value.Date);

        if (query.DateTo is not null)
        {
            var end = query.DateTo.Value.Date.AddDays(1);
            q = q.Where(p => p.PurchaseDate < end);
        }

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var term = query.Search.Trim().ToLowerInvariant();
            var matchingUserIds = _db.Users.AsNoTracking()
                .Where(u =>
                    (u.Name != null && u.Name.ToLower().Contains(term))
                    || (u.Email != null && u.Email.ToLower().Contains(term))
                    || (u.PhoneNumber != null && u.PhoneNumber.Contains(term)))
                .Select(u => u.Id);

            q = q.Where(p =>
                p.Id.ToString().Contains(term)
                || (p.ServiceSubscription != null && p.ServiceSubscription.Title.ToLower().Contains(term))
                || (p.GuestName != null && p.GuestName.ToLower().Contains(term))
                || (p.GuestEmail != null && p.GuestEmail.ToLower().Contains(term))
                || (p.GuestPhone != null && p.GuestPhone.Contains(term))
                || (p.PaymentStatus != null && p.PaymentStatus.ToLower().Contains(term))
                || (p.Status != null && p.Status.ToLower().Contains(term))
                || (p.ApplicationUserId != null && matchingUserIds.Contains(p.ApplicationUserId)));
        }

        return q;
    }

    private async Task<IReadOnlyList<AdminServicePurchaseListItemDto>> MapListAsync(
        IReadOnlyList<ServicePurchase> purchases,
        CancellationToken cancellationToken)
    {
        if (purchases.Count == 0)
            return [];

        var userIds = purchases
            .Where(p => !string.IsNullOrEmpty(p.ApplicationUserId))
            .Select(p => p.ApplicationUserId!)
            .Distinct()
            .ToList();

        var users = userIds.Count == 0
            ? new Dictionary<string, ApplicationUser>()
            : await _db.Users.AsNoTracking()
                .Where(u => userIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, cancellationToken);

        return purchases.Select(p =>
        {
            users.TryGetValue(p.ApplicationUserId ?? string.Empty, out var user);
            var vat = CalculateVat(p.TotalAmount, p.DiscountAmount);

            return new AdminServicePurchaseListItemDto
            {
                Id = p.Id,
                ServiceSubscriptionId = p.ServiceSubscriptionId,
                ServiceTitle = p.ServiceSubscription?.Title ?? "N/A",
                CustomerName = user?.Name ?? p.GuestName ?? "N/A",
                Email = user?.Email ?? p.GuestEmail,
                Phone = user?.PhoneNumber ?? p.GuestPhone,
                TotalAmount = (double)p.TotalAmount,
                AmountPaid = (double)p.AmountPaid,
                DiscountAmount = (double)p.DiscountAmount,
                VatAmount = (double)vat,
                PaymentStatus = p.PaymentStatus,
                ServiceStatus = p.Status,
                PurchaseDate = p.PurchaseDate
            };
        }).ToList();
    }

    private static decimal CalculateVat(decimal totalAmount, decimal discountAmount)
    {
        var taxableAmount = totalAmount - discountAmount;
        return taxableAmount * (VatRate / (1 + VatRate));
    }

    private static string Csv(string value) =>
        $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
