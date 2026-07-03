using IdealWeightNutrition.Application.Abstractions;
using IdealWeightNutrition.Infrastructure.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace IdealWeightNutrition.Infrastructure.Services.Hosted;

internal sealed class PaymentVerificationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _services;
    private readonly ILogger<PaymentVerificationBackgroundService> _logger;
    private readonly PaymentVerificationOptions _options;

    public PaymentVerificationBackgroundService(
        IServiceProvider services,
        ILogger<PaymentVerificationBackgroundService> logger,
        IOptions<PaymentVerificationOptions> options)
    {
        _services = services;
        _logger = logger;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_options.Enabled)
        {
            _logger.LogInformation("Payment verification job is disabled.");
            return;
        }

        await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = _services.CreateScope();
                var payments = scope.ServiceProvider.GetRequiredService<IPaymentService>();
                await payments.VerifyPendingPaymentsAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Payment verification job failed.");
            }

            await Task.Delay(TimeSpan.FromMinutes(_options.CheckIntervalMinutes), stoppingToken);
        }
    }
}
