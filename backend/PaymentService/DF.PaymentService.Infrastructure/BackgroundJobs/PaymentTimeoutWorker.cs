using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DF.PaymentService.Infrastructure.BackgroundJobs;

public sealed class PaymentTimeoutWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentTimeoutWorker> logger,
    PaymentTimeoutOptions options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enable)
        {
            logger.LogInformation("PaymentTimeoutWorker disabled.");
            return;
        }

        logger.LogInformation("PaymentTimeoutWorker started. Polling={Polling}s", options.PollingInterval.TotalSeconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var now = DateTime.UtcNow.Subtract(options.SafetyWindow);

                var expired = await db.Payments
                    .Where(p =>
                        (p.Status == PaymentStatus.Pending || p.Status == PaymentStatus.RequiresAction) &&
                        p.ExpiresAt != null && p.ExpiresAt < now)
                    .OrderBy(p => p.ExpiresAt)
                    .Take(options.BatchSize)
                    .ToListAsync(stoppingToken);

                if (expired.Count > 0)
                {
                    IStripeService? stripe = null;
                    if (options.CancelStripePiOnTimeout)
                    {
                        stripe = scope.ServiceProvider.GetRequiredService<IStripeService>();
                    }

                    foreach (var p in expired)
                    {
                        try
                        {
                            if (options.CancelStripePiOnTimeout &&
                                p.Method == PaymentMethod.Online &&
                                !string.IsNullOrWhiteSpace(p.StripePaymentIntentId))
                            {
                                try
                                {
                                    await stripe!.CancelPaymentIntentAsync(p, stoppingToken);
                                }
                                catch (Exception ex)
                                {
                                    // Не блокуємо таймаут, лише лог
                                    logger.LogWarning(ex,
                                        "Stripe PI cancel on timeout failed. PaymentId={PaymentId} PI={PI}",
                                        p.Id, p.StripePaymentIntentId);
                                }
                            }

                            p.CancelWithReason("timeout"); // підніме доменну подію -> Outbox
                        }
                        catch (Exception ex)
                        {
                            logger.LogWarning(ex, "Failed to cancel timed-out payment {PaymentId}", p.Id);
                        }
                    }

                    await db.SaveChangesAsync(stoppingToken);
                    logger.LogInformation("PaymentTimeoutWorker cancelled {Count} payments.", expired.Count);
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "PaymentTimeoutWorker loop error.");
                await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
            }

            await Task.Delay(options.PollingInterval, stoppingToken);
        }

        logger.LogInformation("PaymentTimeoutWorker stopped.");
    }
}