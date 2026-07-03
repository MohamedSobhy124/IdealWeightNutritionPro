using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Domain.Time;
using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services.Hosted;

internal sealed class LowStockDigestBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<LowStockDigestBackgroundService> _logger;
    private readonly StockAlertOptions _options;

    public LowStockDigestBackgroundService(
        IServiceProvider services,
        ILogger<LowStockDigestBackgroundService> logger,
        IOptions<StockAlertOptions> options)
    {
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.DailyDigestEnabled)
        {
            _logger.LogInformation("Low stock daily digest is disabled.");
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
                _logger.LogInformation("Next low stock digest at {NextRun}", nextRun);
                await Task.Delay(delay, stoppingToken);

                using var scope = _services.CreateScope();
                var notifications = scope.ServiceProvider.GetRequiredService<IAdminNotificationService>();
                await notifications.SendLowStockDigestAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Low stock digest job failed.");
                await Task.Delay(TimeSpan.FromHours(1), stoppingToken);
            }
        }
    }
}
