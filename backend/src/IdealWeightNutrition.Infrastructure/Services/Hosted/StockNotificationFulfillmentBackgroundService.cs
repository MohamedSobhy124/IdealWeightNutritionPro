using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services.Hosted;

internal sealed class StockNotificationFulfillmentBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<StockNotificationFulfillmentBackgroundService> _logger;
    private readonly StockNotificationFulfillmentOptions _options;

    public StockNotificationFulfillmentBackgroundService(
        IServiceProvider services,
        ILogger<StockNotificationFulfillmentBackgroundService> logger,
        IOptions<StockNotificationFulfillmentOptions> options)
    {
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Stock notification fulfillment job is disabled.");
            return;
        }

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var fulfillment = scope.ServiceProvider.GetRequiredService<IStockNotificationFulfillmentService>();
                await fulfillment.ProcessPendingAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Stock notification fulfillment job failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.CheckIntervalMinutes), stoppingToken);
        }
    }
}
