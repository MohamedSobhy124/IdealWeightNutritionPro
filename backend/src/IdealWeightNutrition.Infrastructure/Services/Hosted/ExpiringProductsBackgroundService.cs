using System.Globalization;
using System.Text;
using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Catalogue;
using IdealWeightNutrition.Domain.Time;
using IdealWeightNutrition.Infrastructure.Options;
using IdealWeightNutrition.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services.Hosted;

internal sealed class ExpiringProductsBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<ExpiringProductsBackgroundService> _logger;
    private readonly ExpiringProductsAlertOptions _options;

    public ExpiringProductsBackgroundService(
        IServiceProvider services,
        ILogger<ExpiringProductsBackgroundService> logger,
        IOptions<ExpiringProductsAlertOptions> options)
    {
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Expiring products alert job is disabled.");
            return;
        }

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var now = UaeDateTime.Now;
                var nextRun = now.Date.AddHours(_options.CheckTimeHour);
                if (now.TimeOfDay > TimeSpan.FromHours(_options.CheckTimeHour))
                    nextRun = nextRun.AddDays(1);

                var delay = nextRun - now;
                _logger.LogInformation("Next expiring products check at {NextRun}", nextRun);
                await Task.Delay(delay, stoppingToken);

                await RunCheckAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Expiring products check failed");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }

    private async Task RunCheckAsync(CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_options.AdminEmail))
        {
            _logger.LogWarning("ExpiringProductsAlert:AdminEmail is not configured.");
            return;
        }

        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var email = scope.ServiceProvider.GetRequiredService<IEmailService>();
        var notifications = scope.ServiceProvider.GetRequiredService<IInAppNotificationService>();
        var clock = scope.ServiceProvider.GetRequiredService<IDateTimeProvider>();

        var today = clock.UtcNow.Date;
        var threshold = today.AddDays(_options.DaysBeforeExpiry);

        var variants = await db.Set<ProductVariant>().AsNoTracking()
            .Where(v => !v.IsDeleted && v.ExpiryDate != null && v.ExpiryDate >= today && v.ExpiryDate <= threshold)
            .Join(
                db.Products.AsNoTracking().Where(p => !p.IsDeleted && p.ProductType == ProductType.Variable),
                v => v.ProductId,
                p => p.Id,
                (v, p) => new { Variant = v, Product = p })
            .ToListAsync(cancellationToken);

        if (variants.Count == 0)
        {
            _logger.LogInformation("No expiring product variants found.");
            return;
        }

        var rows = variants
            .Select(x => new ExpiringRow(
                x.Product.Id,
                x.Product.Title,
                x.Variant.Sku,
                x.Variant.StockQuantity,
                x.Variant.ExpiryDate!.Value,
                (x.Variant.ExpiryDate!.Value.Date - today).Days))
            .OrderBy(r => r.ExpiryDate)
            .ToList();

        var csv = BuildCsv(rows);
        var csvBytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv)).ToArray();
        var subject = $"Expiring products alert — {rows.Count} variant(s) in next {_options.DaysBeforeExpiry} days";
        var body = BuildEmailBody(rows, _options.DaysBeforeExpiry);

        await email.SendWithAttachmentAsync(
            _options.AdminEmail.Trim(),
            subject,
            body,
            csvBytes,
            $"Expiring-Products-{today:yyyy-MM-dd}.csv",
            cancellationToken);

        await notifications.NotifyAdminsAsync(
            "Expiring products",
            $"{rows.Count} product variant(s) expire within {_options.DaysBeforeExpiry} days.",
            "Stock",
            cancellationToken: cancellationToken);

        _logger.LogInformation("Sent expiring products alert for {Count} variants.", rows.Count);
    }

    private static string BuildCsv(IReadOnlyList<ExpiringRow> rows)
    {
        var sb = new StringBuilder();
        sb.AppendLine("ProductId,Title,Sku,Stock,ExpiryDate,DaysUntilExpiry");
        foreach (var row in rows)
        {
            sb.Append(row.ProductId).Append(',')
                .Append(Csv(row.Title)).Append(',')
                .Append(Csv(row.Sku ?? string.Empty)).Append(',')
                .Append(row.Stock).Append(',')
                .Append(row.ExpiryDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)).Append(',')
                .Append(row.DaysUntilExpiry)
                .AppendLine();
        }

        return sb.ToString();
    }

    private static string BuildEmailBody(IReadOnlyList<ExpiringRow> rows, int daysWindow)
    {
        var preview = string.Join(
            "<br/>",
            rows.Take(15).Select(r =>
                $"#{r.ProductId} {System.Net.WebUtility.HtmlEncode(r.Title)} — SKU {System.Net.WebUtility.HtmlEncode(r.Sku ?? "-")} — expires {r.ExpiryDate:dd MMM yyyy} ({r.DaysUntilExpiry} days)"));

        return $"""
            <p><strong>{rows.Count}</strong> product variant(s) expire within the next {daysWindow} days.</p>
            <p>{preview}</p>
            <p>See the attached CSV for the full list.</p>
            """;
    }

    private static string Csv(string value)
    {
        if (value.Contains(',') || value.Contains('"'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private sealed record ExpiringRow(
        int ProductId,
        string Title,
        string? Sku,
        int Stock,
        DateTime ExpiryDate,
        int DaysUntilExpiry);
}
