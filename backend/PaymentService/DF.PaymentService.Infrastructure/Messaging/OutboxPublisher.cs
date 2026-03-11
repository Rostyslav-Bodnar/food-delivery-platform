using System.Diagnostics.Metrics;
using DF.PaymentService.Application.Common.Interfaces;
using DF.PaymentService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DF.PaymentService.Infrastructure.Messaging;

public class OutboxPublisher(
    IServiceScopeFactory scopeFactory,
    ILogger<OutboxPublisher> logger,
    IEventBus eventBus,
    OutboxOptions options)
    : BackgroundService
{
    private static readonly Meter Meter = new("DF.PaymentService.Outbox", "1.0.0");
    private static readonly Counter<long> PublishedCounter = Meter.CreateCounter<long>("outbox.published_total");
    private static readonly Counter<long> FailedCounter = Meter.CreateCounter<long>("outbox.failed_total");
    private static readonly Counter<long> PoisonedCounter = Meter.CreateCounter<long>("outbox.poisoned_total");
    private static readonly Histogram<double> PublishDuration = Meter.CreateHistogram<double>("outbox.publish_duration_ms");

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxPublisher started with BatchSize={Batch}, MaxRetries={MaxRetries}",
            options.BatchSize, options.MaxRetries);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                using var scope = scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

                var now = DateTime.UtcNow;

                var messages = await db.OutboxMessages
                    .Where(x => x.ProcessedOn == null && !x.Poisoned && (x.NextAttempt == null || x.NextAttempt <= now))
                    .OrderBy(x => x.OccurredOn)
                    .Take(options.BatchSize)
                    .ToListAsync(stoppingToken);

                if (messages.Count == 0)
                {
                    await Task.Delay(options.PollingInterval, stoppingToken);
                    continue;
                }

                foreach (var message in messages)
                {
                    var started = DateTime.UtcNow;
                    try
                    {
                        await eventBus.PublishAsync(message.Type, message.Content, stoppingToken);

                        message.ProcessedOn = DateTime.UtcNow;
                        message.Error = null;

                        PublishedCounter.Add(1);
                        PublishDuration.Record((DateTime.UtcNow - started).TotalMilliseconds);
                    }
                    catch (Exception ex)
                    {
                        message.RetryCount += 1;
                        message.Error = $"{ex.GetType().Name}: {ex.Message}";
                        FailedCounter.Add(1);

                        if (message.RetryCount >= options.MaxRetries)
                        {
                            if (options.MoveToDeadLetter)
                            {
                                // Перенос у окрему таблицю DLQ
                                db.OutboxDeadMessages.Add(new Domain.Entities.OutboxDeadMessage
                                {
                                    Id = message.Id,
                                    Type = message.Type,
                                    Content = message.Content,
                                    OccurredOn = message.OccurredOn,
                                    FailedOn = DateTime.UtcNow,
                                    RetryCount = message.RetryCount,
                                    Error = message.Error,
                                    CorrelationId = message.CorrelationId
                                });

                                db.OutboxMessages.Remove(message);
                            }
                            else
                            {
                                // Позначаємо як Poisoned
                                message.Poisoned = true;
                            }

                            PoisonedCounter.Add(1);

                            logger.LogError(ex,
                                "Outbox message {MessageId} poisoned after {Retries} retries. Type={Type}",
                                message.Id, message.RetryCount, message.Type);
                        }
                        else
                        {
                            // Обчислюємо наступний backoff з jitter
                            var delay = ComputeBackoff(message.RetryCount, options);
                            message.NextAttempt = DateTime.UtcNow.Add(delay);

                            logger.LogWarning(ex,
                                "Outbox message {MessageId} failed. Retry {Retry}/{Max}. NextAttempt in {Delay}s. Type={Type}",
                                message.Id, message.RetryCount, options.MaxRetries, delay.TotalSeconds, message.Type);
                        }
                    }
                }

                await db.SaveChangesAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                // graceful shutdown
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "OutboxPublisher outer loop error.");
                // невелика пауза, щоб не гойдати CPU при постійній помилці
                await Task.Delay(TimeSpan.FromSeconds(2), stoppingToken);
            }
        }

        logger.LogInformation("OutboxPublisher stopped.");
    }

    private static TimeSpan ComputeBackoff(int retryCount, OutboxOptions options)
    {
        // Експоненційний backoff: baseDelay * 2^(retry-1), з капом + jitter
        var factor = Math.Pow(2, Math.Max(0, retryCount - 1));
        var ms = Math.Min(options.BaseDelay.TotalMilliseconds * factor, options.MaxDelay.TotalMilliseconds);
        var jitter = Random.Shared.Next(50, 250); // невеликий джиттер
        return TimeSpan.FromMilliseconds(ms + jitter);
    }
}

public sealed class OutboxOptions
{
    public int BatchSize { get; init; } = 50;
    public TimeSpan PollingInterval { get; init; } = TimeSpan.FromSeconds(2);
    public int MaxRetries { get; init; } = 5;
    public TimeSpan BaseDelay { get; init; } = TimeSpan.FromSeconds(2);
    public TimeSpan MaxDelay { get; init; } = TimeSpan.FromSeconds(60);
    public bool MoveToDeadLetter { get; init; } = true; // false => Poisoned=true
}