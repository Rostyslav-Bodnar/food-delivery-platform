using System.Text;
using System.Text.Json;
using DF.Contracts.EventDriven;
using RabbitMQ.Client;

namespace DF.OrderService.Application.Messaging.Publishers;

public class OrderEventPublisher : IEventPublisher
{
    private readonly IChannel channel;

    public OrderEventPublisher(IConnection connection)
    {
        channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
        channel.ExchangeDeclareAsync("orders", ExchangeType.Fanout, durable: true);
    }

    public async Task PublishOrderCreatedEvent(OrderCreatedEvent evt)
    {
        var eventName = evt.GetType().Name;
        
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        var msgId = Guid.NewGuid().ToString("N");

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = msgId,
            CorrelationId = msgId,
            Headers = new Dictionary<string, object?>()
            {
                ["x-event-name"] = eventName,
                ["x-retry-count"] = 0
            }
        };

        await channel.BasicPublishAsync(
            exchange: "df.events",
            routingKey: eventName,
            mandatory: false,
            basicProperties: props,
            body: body);
    }

    public async Task PublishOrderCanceledEvent(OrderCancelledEvent evt)
    {
        var eventName = evt.GetType().Name;
        
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        var msgId = Guid.NewGuid().ToString("N");

        var props = new BasicProperties
        {
            Persistent = true,
            ContentType = "application/json",
            MessageId = msgId,
            CorrelationId = msgId,
            Headers = new Dictionary<string, object?>()
            {
                ["x-event-name"] = eventName,
                ["x-retry-count"] = 0
            }
        };

        await channel.BasicPublishAsync(
            exchange: "df.events",
            routingKey: eventName,
            mandatory: false,
            basicProperties: props,
            body: body);
    }
}