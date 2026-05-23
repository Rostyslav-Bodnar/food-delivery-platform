using System.Text.Json;
using DF.Contracts.EventDriven;
using DF.OrderService.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace DF.OrderService.Application.Messaging.Publishers;

/// <summary>
/// Polls OutboxMessages and dispatches them via IEventPublisher. Pairs with
/// OrderService.* writes that persist the message in the same DB transaction as
/// the entity change, so events are never lost on broker failure.
/// </summary>
public sealed class OutboxPublisherHostedService(
    IServiceScopeFactory scopeFactory,
    IEventPublisher publisher,
    ILogger<OutboxPublisherHostedService> logger) : BackgroundService
{
    private static readonly TimeSpan PollInterval = TimeSpan.FromSeconds(2);
    private const int BatchSize = 50;
    private const int MaxRetries = 10;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("OutboxPublisher started (poll = {Interval})", PollInterval);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessBatchAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Outbox polling iteration failed");
            }

            try { await Task.Delay(PollInterval, stoppingToken); }
            catch (TaskCanceledException) { return; }
        }
    }

    private async Task ProcessBatchAsync(CancellationToken cancellationToken)
    {
        await using var scope = scopeFactory.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var batch = await db.OutboxMessages
            .Where(m => m.DispatchedAtUtc == null && m.RetryCount < MaxRetries)
            .OrderBy(m => m.OccurredAtUtc)
            .Take(BatchSize)
            .ToListAsync(cancellationToken);

        if (batch.Count == 0) return;

        foreach (var msg in batch)
        {
            try
            {
                await DispatchAsync(msg);
                msg.DispatchedAtUtc = DateTime.UtcNow;
                msg.LastError = null;
            }
            catch (Exception ex)
            {
                msg.RetryCount++;
                msg.LastError = ex.Message;
                logger.LogWarning(ex,
                    "Failed to dispatch outbox message {Id} ({EventType}); retry {Retry}/{Max}",
                    msg.Id, msg.EventType, msg.RetryCount, MaxRetries);
            }
        }

        await db.SaveChangesAsync(cancellationToken);
    }

    private Task DispatchAsync(Domain.Entities.OutboxMessage msg) => msg.EventType switch
    {
        nameof(OrderCreatedEvent) =>
            publisher.PublishOrderCreatedEvent(Deserialize<OrderCreatedEvent>(msg.Payload)),
        nameof(OrderPickedUpEvent) =>
            publisher.PublishOrderPickedUpEvent(Deserialize<OrderPickedUpEvent>(msg.Payload)),
        nameof(OrderCancelledEvent) =>
            publisher.PublishOrderCanceledEvent(Deserialize<OrderCancelledEvent>(msg.Payload)),
        nameof(OrderDeliveredEvent) =>
            publisher.PublishOrderDeliveredEvent(Deserialize<OrderDeliveredEvent>(msg.Payload)),
        nameof(OrderStatusChangedEvent) =>
            publisher.PublishOrderStatusChangedEvent(Deserialize<OrderStatusChangedEvent>(msg.Payload)),
        _ => throw new InvalidOperationException($"Unknown event type '{msg.EventType}'")
    };

    private static T Deserialize<T>(string payload)
        => JsonSerializer.Deserialize<T>(payload)
           ?? throw new InvalidOperationException($"Empty outbox payload for {typeof(T).Name}");
}
