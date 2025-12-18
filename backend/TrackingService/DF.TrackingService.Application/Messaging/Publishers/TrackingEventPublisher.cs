using System.Text;
using System.Text.Json;
using DF.Contracts.EventDriven;
using RabbitMQ.Client;

namespace DF.TrackingService.Application.Messaging.Publishers;

public class TrackingEventPublisher : IEventPublisher
{
    private readonly IChannel channel;

    public TrackingEventPublisher(IConnection connection)
    {
        channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

        channel.ExchangeDeclareAsync(
            exchange: "trackingservice",
            type: ExchangeType.Fanout,
            durable: true
        ).GetAwaiter().GetResult();
    }

    public async Task PublishLocationsCreatedForOrder(LocationsCreatedForOrder evt)
    {
        var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(evt));

        await channel.BasicPublishAsync(
            exchange: "trackingservice",
            routingKey: "",
            mandatory: false,
            body: body
        );
    }
}