using DF.PaymentService.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DF.PaymentService.Infrastructure.BackgroundJobs;

public sealed class CourierPayoutWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<CourierPayoutWorker> logger,
    CourierPayoutOptions options) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enable)
        {
            logger.LogInformation("CourierPayoutWorker disabled.");
            return;
        }

        logger.LogInformation(
            "CourierPayoutWorker started. Polling={PollingSeconds}s HoldPeriod={HoldPeriodHours}h",
            options.PollingIntervalSeconds,
            options.HoldPeriodHours);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var courierPayoutService = scope.ServiceProvider.GetRequiredService<ICourierPayoutService>();
                var processed = await courierPayoutService.ProcessDuePayoutsAsync(stoppingToken);
                if (processed > 0)
                {
                    logger.LogInformation("CourierPayoutWorker processed {Count} payouts.", processed);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "CourierPayoutWorker loop error.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            await Task.Delay(options.PollingInterval, stoppingToken);
        }

        logger.LogInformation("CourierPayoutWorker stopped.");
    }
}
