using DF.OrderService.Application.Options;
using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services.Interfaces;
using DF.OrderService.Contracts.Exceptions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DF.OrderService.Application.BackgroundJobs;

/// <summary>
/// Auto-cancels card-paid orders whose customer never completed the Stripe
/// payment within the configured deadline. Mirrors the "15-minute pay or
/// lose the order" pattern used by major food-delivery platforms — keeps
/// restaurants from preparing food they won't get paid for.
///
/// Only touches orders still in Preparing / Ready. Once a courier picks
/// it up, the restaurant has already incurred cost and any cancel should
/// be operator-driven, not automated.
/// </summary>
public sealed class OrderPaymentTimeoutWorker(
    IServiceScopeFactory scopeFactory,
    OrderPaymentTimeoutOptions options,
    ILogger<OrderPaymentTimeoutWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enabled)
        {
            logger.LogInformation("OrderPaymentTimeoutWorker disabled by config; exiting.");
            return;
        }

        logger.LogInformation(
            "OrderPaymentTimeoutWorker started. Deadline={Deadline}, Interval={Interval}, Batch={Batch}",
            options.PaymentDeadline, options.PollingInterval, options.BatchSize);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await PollOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OrderPaymentTimeoutWorker poll iteration failed");
            }

            try { await Task.Delay(options.PollingInterval, stoppingToken); }
            catch (OperationCanceledException) { /* shutdown */ }
        }

        logger.LogInformation("OrderPaymentTimeoutWorker stopped.");
    }

    private async Task PollOnceAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var repository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
        var orderService = scope.ServiceProvider.GetRequiredService<IOrderService>();

        var cutoff = DateTime.UtcNow - options.PaymentDeadline;
        var stale = await repository.GetStaleUnpaidOnlineOrdersAsync(cutoff, options.BatchSize, ct);

        if (stale.Count == 0) return;

        logger.LogInformation(
            "Cancelling {Count} stale unpaid Online orders (cutoff={CutoffUtc:O})",
            stale.Count, cutoff);

        foreach (var order in stale)
        {
            if (ct.IsCancellationRequested) break;

            try
            {
                // CancelOrderAsync emits OrderCancelledEvent + OrderStatusChangedEvent
                // via the outbox, so PaymentService cancels the Stripe PI and the
                // customer/business get the SignalR notification.
                await orderService.CancelOrderAsync(order.Id);
                logger.LogInformation(
                    "Auto-cancelled unpaid order {OrderId} (created {OrderDate:O}, status was {Status})",
                    order.Id, order.OrderDate, order.OrderStatus);
            }
            catch (OrderStateException ex)
            {
                // Order moved to a non-cancellable state between our query and
                // the cancel call (race with manual cancel, status update, etc.).
                // Safe to skip — next poll will re-evaluate.
                logger.LogWarning(ex,
                    "Skipped auto-cancel for order {OrderId}: {Reason}",
                    order.Id, ex.Message);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to auto-cancel order {OrderId}", order.Id);
            }
        }
    }
}
