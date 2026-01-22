using System.Text;
using System.Text.Json;
using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Responses.TrackingService;
using DF.TrackingService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.TrackingService.Application.Messaging.Consumers;

public class GetLocationsConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory)
    : IConsumer
{
    private IChannel _channel = null!;

    public void Start()
    {
        _channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: "tracking.getlocations",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        ).GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += Handle;

        _channel.BasicConsumeAsync(
            queue: "tracking.getlocations",
            autoAck: true,
            consumer: consumer
        ).GetAwaiter().GetResult();

        Console.WriteLine("GetLocationsConsumer started");
    }

    private async Task Handle(object sender, BasicDeliverEventArgs ea)
    {
        using var scope = scopeFactory.CreateScope();
        var locationRepository = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        var request = JsonSerializer.Deserialize<GetLocationRequest>(json);

        if (request == null)
            return;

        var deliverTo = await locationRepository.Get(request.DeliverToId);
        var deliverFrom = await locationRepository.Get(request.DeliverFromId);

        if (deliverTo == null || deliverFrom == null)
            return;

        var response = new GetLocationsResponse(
            DeliverTo: new LocationDTO(
                deliverTo.Id,
                deliverTo.FullAddress,
                deliverTo.GeoPoint!.Y, // latitude
                deliverTo.GeoPoint!.X  // longitude
            ),
            DeliverFrom: new LocationDTO(
                deliverFrom.Id,
                deliverFrom.FullAddress,
                deliverFrom.GeoPoint!.Y,
                deliverFrom.GeoPoint!.X
            )
        );

        var responseBytes = Encoding.UTF8.GetBytes(
            JsonSerializer.Serialize(response)
        );

        var props = new BasicProperties
        {
            CorrelationId = ea.BasicProperties.CorrelationId
        };

        await _channel.BasicPublishAsync(
            exchange: "",
            routingKey: ea.BasicProperties.ReplyTo!,
            mandatory: false,
            basicProperties: props,
            body: responseBytes
        );
    }
}