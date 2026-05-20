using System.Text.Json;
using DF.Contracts.EventDriven;
using RabbitMQ.Client;

namespace DF.OrderService.Application.Messaging.Publishers;

public sealed class OrderEventPublisher : IEventPublisher, IAsyncDisposable
{
    private const string ExchangeName = "df.events";

    private readonly IChannel _channel;

    private OrderEventPublisher(IChannel channel) => _channel = channel;

    public static async Task<OrderEventPublisher> CreateAsync(IConnection connection, CancellationToken ct = default)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        await channel.ExchangeDeclareAsync(
            exchange: ExchangeName, type: ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: ct);
        return new OrderEventPublisher(channel);
    }

    public Task PublishOrderCreatedEvent(OrderCreatedEvent evt) => PublishAsync(evt);
    public Task PublishOrderPickedUpEvent(OrderPickedUpEvent evt) => PublishAsync(evt);
    public Task PublishOrderCanceledEvent(OrderCancelledEvent evt) => PublishAsync(evt);
    public Task PublishOrderDeliveredEvent(OrderDeliveredEvent evt) => PublishAsync(evt);

    private async Task PublishAsync<T>(T evt)
    {
        var eventName = typeof(T).Name;
        var body = JsonSerializer.SerializeToUtf8Bytes(evt);
        var msgId = Guid.NewGuid().ToString("N");

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = msgId,
            CorrelationId = msgId,
            Headers = new Dictionary<string, object?>
            {
                ["x-event-name"] = eventName,
                ["x-retry-count"] = 0
            }
        };

        await _channel.BasicPublishAsync(
            exchange: ExchangeName, routingKey: eventName,
            mandatory: false, basicProperties: props, body: body);
    }

    public async ValueTask DisposeAsync()
    {
        try { await _channel.CloseAsync(); } catch { /* best-effort */ }
        await _channel.DisposeAsync();
    }
}
