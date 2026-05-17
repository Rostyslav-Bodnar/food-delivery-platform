using System.Text;
using System.Text.Json;
using DF.TrackingService.Application.Services.Interfaces;
using DF.TrackingService.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.TrackingService.Application.Messaging.Consumers;

/// <summary>
/// Base for event consumers that move the live tracking snapshot into a
/// terminal stage (delivered / cancelled). Listens on df.events keyed by the
/// event-name routing key, decodes the OrderId, and rewrites the Redis
/// snapshot + broadcasts to subscribed clients.
/// </summary>
public abstract class OrderTerminalStateConsumerBase<TEvent>(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger logger,
    string queueName,
    string terminalStage) : IConsumer
    where TEvent : class
{
    private IChannel? _channel;
    private string? _consumerTag;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await MessagingTopology.EnsureDeadLetterAsync(_channel, cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: "df.events",
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        var queue = await _channel.QueueDeclareAsync(
            queue: queueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: MessagingTopology.DeadLetterArgs(queueName),
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: queue.QueueName,
            exchange: "df.events",
            routingKey: typeof(TEvent).Name,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleAsync;

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: queue.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation(
            "Terminal-state consumer started for {Event} on {Queue} → stage {Stage}",
            typeof(TEvent).Name, queue.QueueName, terminalStage);
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
            logger.LogWarning(ex, "Error while stopping {Consumer}", GetType().Name);
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

    private async Task HandleAsync(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<TEvent>(json);
            if (evt is null)
            {
                logger.LogWarning("Null {Event} payload; acking and skipping", typeof(TEvent).Name);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            var orderId = GetOrderId(evt);
            if (orderId == Guid.Empty)
            {
                logger.LogWarning("Missing OrderId on {Event}; acking and skipping", typeof(TEvent).Name);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var store = scope.ServiceProvider.GetRequiredService<IOrderTrackingSnapshotStore>();
            var notifier = scope.ServiceProvider.GetRequiredService<ITrackingNotifier>();

            var snapshot = await store.ReadAsync(orderId);
            snapshot = snapshot with
            {
                Stage = terminalStage,
                UpdatedAtUtc = DateTime.UtcNow
            };

            await store.SaveAsync(snapshot);
            await notifier.SnapshotUpdatedAsync(snapshot);

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "{Consumer} handler failed; requeue={Requeue}",
                GetType().Name, !ea.Redelivered);
            try
            {
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: !ea.Redelivered);
            }
            catch (Exception nackEx)
            {
                logger.LogError(nackEx, "Failed to nack on {Consumer}", GetType().Name);
            }
        }
    }

    protected abstract Guid GetOrderId(TEvent evt);
}
