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
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));
        await channel.BasicPublishAsync(
            exchange: "orders",
            routingKey: "",
            body: body
        );
    }
}