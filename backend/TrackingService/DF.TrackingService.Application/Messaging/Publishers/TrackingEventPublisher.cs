using System.Text.Json;
using DF.Contracts.EventDriven;
using RabbitMQ.Client;

namespace DF.TrackingService.Application.Messaging.Publishers;

public sealed class TrackingEventPublisher : IEventPublisher, IAsyncDisposable
{
    private const string Exchange = "df.events";

    private readonly IChannel _channel;

    private TrackingEventPublisher(IChannel channel) => _channel = channel;

    public static async Task<TrackingEventPublisher> CreateAsync(IConnection connection, CancellationToken ct = default)
    {
        var channel = await connection.CreateChannelAsync(cancellationToken: ct);
        // Topic exchange; routing key = event type name (e.g. nameof(LocationsCreatedForOrder)).
        await channel.ExchangeDeclareAsync(
            exchange: Exchange, type: ExchangeType.Topic,
            durable: true, autoDelete: false, cancellationToken: ct);
        return new TrackingEventPublisher(channel);
    }

    public Task PublishLocationsCreatedForOrder(LocationsCreatedForOrder evt) => PublishAsync(evt);

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
            exchange: Exchange, routingKey: eventName,
            mandatory: false, basicProperties: props, body: body);
    }

    public async ValueTask DisposeAsync()
    {
        try { await _channel.CloseAsync(); } catch { /* best-effort */ }
        await _channel.DisposeAsync();
    }
}
