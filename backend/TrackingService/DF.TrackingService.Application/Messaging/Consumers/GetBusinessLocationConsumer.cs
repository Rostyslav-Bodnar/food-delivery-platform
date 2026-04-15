using System.Text;
using System.Text.Json;
using DF.Contracts.RPC.Requests.TrackingService;
using DF.Contracts.RPC.Responses.TrackingService;
using DF.TrackingService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.TrackingService.Application.Messaging.Consumers;

public class GetBusinessLocationConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory)
    : IConsumer
{
    private IChannel _channel = null!;

    public void Start()
    {
        _channel = connection.CreateChannelAsync().GetAwaiter().GetResult();

        _channel.QueueDeclareAsync(
            queue: "tracking.getbusinesslocations",
            durable: false,
            exclusive: false,
            autoDelete: false,
            arguments: null
        ).GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += Handle;

        _channel.BasicConsumeAsync(
            queue: "tracking.getbusinesslocations",
            autoAck: true,
            consumer: consumer
        ).GetAwaiter().GetResult();

        Console.WriteLine("GetLocationsConsumer started");
    }

    private async Task Handle(object sender, BasicDeliverEventArgs ea)
    {
        using var scope = scopeFactory.CreateScope();
        var businessLocationRepository = scope.ServiceProvider.GetRequiredService<IBusinessLocationRepository>();

        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        var request = JsonSerializer.Deserialize<GetBusinessLocationsRequest>(json);

        if (request == null)
            return;

        var businessLocations = await businessLocationRepository.GetByBusinessIdAsync(request.BusinessId);
        
        if (!businessLocations.Any())
            return;

        var response = new GetBusinessLocationsResponse(
            request.BusinessId,
            businessLocations.Select(x => new GetBusinessLocationResponse(
                x.Id,
                x.LocationId,
                x.Location.FullAddress,
                x.Location.City,
                x.Location.Street,
                x.Location.House,
                x.Location.GeoPoint.Y,
                x.Location.GeoPoint.X
            ))
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