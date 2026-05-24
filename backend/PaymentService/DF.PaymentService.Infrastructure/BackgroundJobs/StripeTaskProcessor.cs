using DF.PaymentService.Application.Services.Interfaces;
using DF.PaymentService.Domain.Entities;
using DF.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DF.PaymentService.Infrastructure.BackgroundJobs;

public sealed class StripeTaskProcessor(
    IServiceScopeFactory scopeFactory,
    ILogger<StripeTaskProcessor> logger,
    StripeTaskProcessorOptions options)
    : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!options.Enable)
        {
            logger.LogInformation("StripeTaskProcessor disabled.");
            return;
        }

        logger.LogInformation("StripeTaskProcessor started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                var stripe = scope.ServiceProvider.GetRequiredService<IStripeService>();

                var now = DateTime.UtcNow;

                var tasks = await db.PaymentTasks
                    .Where(t => t.ProcessedOnUtc == null && (t.NextAttemptUtc == null || t.NextAttemptUtc <= now))
                    .OrderBy(t => t.CreatedOnUtc)
                    .Take(options.BatchSize)
                    .ToListAsync(stoppingToken);

                if (tasks.Count == 0)
                {
                    await Task.Delay(options.PollingInterval, stoppingToken);
                    continue;
                }

                foreach (var task in tasks)
                {
                    try
                    {
                        switch (task.Type)
                        {
                            case PaymentTaskType.CreateStripePaymentIntent:
                                await HandleCreatePiAsync(db, stripe, task, stoppingToken);
                                break;
                            default:
                                task.ProcessedOnUtc = DateTime.UtcNow;
                                task.Error = "Unknown task type";
                                break;
                        }
                    }
                    catch (Exception ex)
                    {
                        await ApplyBackoffAsync(db, task, ex, stoppingToken);
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
            catch (Exception ex)
            {
                logger.LogError(ex, "StripeTaskProcessor loop error.");
                await Task.Delay(TimeSpan.FromSeconds(3), stoppingToken);
            }
        }

        logger.LogInformation("StripeTaskProcessor stopped.");
    }

    private async Task HandleCreatePiAsync(AppDbContext db, IStripeService stripe, PaymentTask task, CancellationToken ct)
    {
        var payment = await db.Payments.FirstOrDefaultAsync(p => p.Id == task.PaymentId, ct);
        if (payment is null)
        {
            task.ProcessedOnUtc = DateTime.UtcNow;
            task.Error = "Payment not found";
            return;
        }

        if (payment.Method != PaymentMethod.Online)
        {
            task.ProcessedOnUtc = DateTime.UtcNow;
            task.Error = "Payment method is not Online";
            return;
        }

        if (!string.IsNullOrWhiteSpace(payment.StripePaymentIntentId))
        {
            task.ProcessedOnUtc = DateTime.UtcNow;
            return; // idempotent
        }

        if (payment.Status != PaymentStatus.Pending && payment.Status != PaymentStatus.RequiresAction)
        {
            task.ProcessedOnUtc = DateTime.UtcNow;
            task.Error = $"Payment in state {payment.Status}";
            return;
        }

        // Destination-charge path when the business is onboarded with Stripe Connect.
        // Falls back to a plain platform charge when DestinationStripeAccountId is null
        // (older orders, businesses that haven't completed onboarding).
        var result = payment.FundsFlow == FundsFlow.Destination
                     && !string.IsNullOrWhiteSpace(payment.DestinationStripeAccountId)
            ? await stripe.CreateDestinationPaymentIntentAsync(
                payment,
                payment.DestinationStripeAccountId!,
                ct: ct)
            : await stripe.CreatePaymentIntentAsync(payment, ct);

        payment.SetStripeSecrets(result.PaymentIntentId, result.ClientSecret);
        payment.SetExpiration(DateTime.UtcNow.Add(options.PaymentExpiration));

        await db.SaveChangesAsync(ct);

        task.ProcessedOnUtc = DateTime.UtcNow;
        task.Error = null;
    }

    private async Task ApplyBackoffAsync(AppDbContext db, PaymentTask task, Exception ex, CancellationToken ct)
    {
        task.RetryCount += 1;
        task.Error = $"{ex.GetType().Name}: {ex.Message}";

        if (task.RetryCount >= options.MaxRetries)
        {
            task.ProcessedOnUtc = DateTime.UtcNow;
            logger.LogError(ex, "PaymentTask {TaskId} poisoned after {Retries} retries. PaymentId={PaymentId}",
                task.Id, task.RetryCount, task.PaymentId);
        }
        else
        {
            var delay = ComputeBackoff(task.RetryCount);
            task.NextAttemptUtc = DateTime.UtcNow.Add(delay);
            logger.LogWarning(ex, "PaymentTask {TaskId} failed. Retry {Retry}/{Max}. Next in {Delay}s",
                task.Id, task.RetryCount, options.MaxRetries, delay.TotalSeconds);
        }

        await db.SaveChangesAsync(ct);
    }

    private TimeSpan ComputeBackoff(int retryCount)
    {
        var factor = Math.Pow(2, Math.Max(0, retryCount - 1));
        var ms = Math.Min(options.BaseDelay.TotalMilliseconds * factor, options.MaxDelay.TotalMilliseconds);
        var jitter = Random.Shared.Next(50, 250);
        return TimeSpan.FromMilliseconds(ms + jitter);
    }
}