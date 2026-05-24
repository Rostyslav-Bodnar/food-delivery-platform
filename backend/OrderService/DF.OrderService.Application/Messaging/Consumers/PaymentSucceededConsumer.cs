using System.Text.Json;
using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.OrderService.Application.Messaging.Consumers;

/// <summary>
/// Consumes <c>PaymentSucceededEvent</c> published by PaymentService through
/// its outbox. Flips <c>Order.IsPaid = true</c> so the order can be marked
/// Delivered — the transition guard in <c>OrderStatusTransitions</c> blocks
/// Online → Delivered when this flag is still false.
/// </summary>
public class PaymentSucceededConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<PaymentSucceededConsumer> logger
) : IConsumer
{
    // Platform convention: every cross-service event flows through df.events
    // (topic exchange), routing key = the event-type class name on the
    // publisher side (PaymentService's domain event class is named
    // PaymentSucceededEvent — OutboxMessage.Create uses GetType().Name).
    private const string ExchangeName = "df.events";
    private const string QueueName = "orders.paymentsucceeded";
    private const string RoutingKey = "PaymentSucceededEvent";

    // Local DTO matching the PaymentService payload shape:
    //     { "PaymentId": "...", "OrderId": "..." }
    // We don't share a contract via DF.Contracts for this one because the
    // payload is owned by PaymentService's domain event — flat structural
    // matching is enough.
    private sealed record PaymentSucceededPayload(Guid PaymentId, Guid OrderId);

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

        logger.LogInformation("PaymentSucceededConsumer started on queue {Queue}", QueueName);
    }

    private async Task HandleAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        try
        {
            var evt = JsonSerializer.Deserialize<PaymentSucceededPayload>(ea.Body.Span);
            if (evt is null || evt.OrderId == Guid.Empty)
            {
                logger.LogWarning("PaymentSucceededEvent payload missing OrderId; acking");
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            await using var scope = scopeFactory.CreateAsyncScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();

            var order = await orderRepository.Get(evt.OrderId);
            if (order is null)
            {
                // The order may belong to a different service deployment or
                // have been deleted. Ack and move on — Stripe's webhook is
                // the source of truth for the payment, we shouldn't keep
                // requeueing this forever.
                logger.LogWarning("PaymentSucceededEvent for unknown order {OrderId}; dropping", evt.OrderId);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            if (order.IsPaid)
            {
                // Already flipped (Stripe retried the webhook or the event
                // was redelivered). Idempotent.
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            order.IsPaid = true;
            await orderRepository.Update(order);

            logger.LogInformation("Order {OrderId} marked as paid", evt.OrderId);
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle PaymentSucceededEvent");
            try { await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: !ea.Redelivered); }
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
            logger.LogWarning(ex, "Error stopping PaymentSucceededConsumer");
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
