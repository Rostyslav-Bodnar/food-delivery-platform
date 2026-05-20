using System.Text.Json;
using DF.Contracts.EventDriven;
using DF.OrderService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.OrderService.Application.Messaging.Consumers;

public class CourierPayoutCompletedConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<CourierPayoutCompletedConsumer> logger
) : IConsumer
{
    private const string ExchangeName = "df.events";
    private const string QueueName = "orders.courier-payout-completed";
    private const string RoutingKey = "CourierPayoutCompletedEvent";

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

        logger.LogInformation("CourierPayoutCompletedConsumer started");
    }

    private async Task HandleAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        try
        {
            var evt = JsonSerializer.Deserialize<CourierPayoutCompletedEvent>(ea.Body.Span)
                      ?? throw new InvalidOperationException("Empty CourierPayoutCompletedEvent payload");

            await using var scope = scopeFactory.CreateAsyncScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var order = await orderRepository.Get(evt.OrderId);

            if (order is not null && !order.CourierPaid)
            {
                order.CourierPaid = true;
                await orderRepository.Update(order);
            }

            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle CourierPayoutCompleted event");
            // Poison message: send to DLQ instead of redelivering forever.
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
            logger.LogWarning(ex, "Error stopping CourierPayoutCompletedConsumer");
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
