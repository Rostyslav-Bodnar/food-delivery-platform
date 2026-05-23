using System.Text.Json;
using DF.Contracts.EventDriven;
using DF.TrackingService.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.TrackingService.Application.Messaging.Consumers;

/// <summary>
/// Fans out every order-status transition to the matching user-scoped SignalR
/// groups (customer/business/courier) so list pages refresh instantly without
/// polling. The per-order snapshot already gets richer updates from the
/// terminal-state consumers (OrderPickedUp/OrderDelivered/OrderCancelled); this
/// consumer is purely the broadcast bus for the generic transition event.
/// </summary>
public sealed class OrderStatusChangedConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderStatusChangedConsumer> logger) : IConsumer
{
    private const string ExchangeName = "df.events";
    private const string QueueName = "tracking.order-status-changed";
    private const string RoutingKey = nameof(OrderStatusChangedEvent);

    private IChannel? _channel;
    private string? _consumerTag;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await MessagingTopology.EnsureDeadLetterAsync(_channel, cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName, type: ExchangeType.Topic, durable: true,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: MessagingTopology.DeadLetterArgs(QueueName),
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleAsync;

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: QueueName, autoAck: false, consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation("OrderStatusChangedConsumer started");
    }

    private async Task HandleAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        try
        {
            var evt = JsonSerializer.Deserialize<OrderStatusChangedEvent>(ea.Body.Span)
                      ?? throw new InvalidOperationException("Empty OrderStatusChangedEvent payload");

            using var scope = scopeFactory.CreateScope();
            var notifier = scope.ServiceProvider.GetRequiredService<ITrackingNotifier>();

            await notifier.OrderStatusChangedAsync(
                evt.OrderId,
                evt.BusinessId,
                evt.CustomerId,
                evt.CourierId,
                evt.NewStatus,
                evt.PreviousStatus,
                evt.ChangedAtUtc);

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle OrderStatusChangedEvent");
            try { await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false); }
            catch (Exception nackEx) { logger.LogWarning(nackEx, "Failed to nack {DeliveryTag}", ea.DeliveryTag); }
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is null) return;

        try
        {
            if (!string.IsNullOrEmpty(_consumerTag))
                await _channel.BasicCancelAsync(_consumerTag, noWait: false, cancellationToken: cancellationToken);
            await _channel.CloseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error stopping OrderStatusChangedConsumer");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }
    }
}
