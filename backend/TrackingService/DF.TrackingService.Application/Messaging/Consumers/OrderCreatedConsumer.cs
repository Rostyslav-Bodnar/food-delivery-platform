using System.Text;
using System.Text.Json;
using DF.Contracts.EventDriven;
using DF.TrackingService.Application.Messaging.Publishers;
using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Location = DF.TrackingService.Domain.Entities.Location;

namespace DF.TrackingService.Application.Messaging.Consumers;

public class OrderCreatedConsumer(
    IConnection connection,
    GeolocationService geolocationService,
    IServiceScopeFactory scopeFactory,
    IEventPublisher eventPublisher)
    : IConsumer
{
    public void Start()
    {
        var channel = connection.CreateChannelAsync().GetAwaiter().GetResult();
        channel.ExchangeDeclareAsync("orders", ExchangeType.Fanout, durable: true)
            .GetAwaiter().GetResult();

        var queueOk = channel.QueueDeclareAsync("trackingservice.ordercreated", durable: true)
            .GetAwaiter().GetResult();

        channel.QueueBindAsync(queueOk.QueueName, "orders", "")
            .GetAwaiter().GetResult();

        var consumer = new AsyncEventingBasicConsumer(channel);
        consumer.ReceivedAsync += HandleMessage;
        channel.BasicConsumeAsync(queueOk.QueueName, autoAck: true, consumer: consumer)
            .GetAwaiter().GetResult();

        Console.WriteLine("OrderCreatedConsumer started");
    }

    private async Task HandleMessage(object sender, BasicDeliverEventArgs ea)
    {
        var json = Encoding.UTF8.GetString(ea.Body.ToArray());
        var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

        if (evt == null) return;

        using var scope = scopeFactory.CreateScope();
        var locationRepository = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

        var deliverToGeolocation = await geolocationService.GetGeodataAsync(evt.DeliverTo.FullAddress);
        if(deliverToGeolocation == null) return;
        var deliverTo = await locationRepository.Create(new Location
        {
            FullAddress = evt.DeliverTo.FullAddress,
            GeoPoint = new Point(deliverToGeolocation.Latitude, deliverToGeolocation.Longitude)
        });

        await Task.Delay(1000);
        
        var deliverFromGeolocation = await geolocationService.GetGeodataAsync(evt.DeliverFrom.FullAddress);
        if(deliverFromGeolocation == null) return;
        
        var deliverFrom = await locationRepository.Create(new Location
        {
            FullAddress = evt.DeliverFrom.FullAddress,
            GeoPoint = new Point(deliverFromGeolocation.Latitude, deliverFromGeolocation.Longitude)
        });

        await eventPublisher.PublishLocationsCreatedForOrder(
            new LocationsCreatedForOrder(
                evt.OrderId, 
                new LocationDTO(deliverTo.Id, deliverToGeolocation.Latitude, deliverToGeolocation.Longitude),
                new LocationDTO(deliverFrom.Id, deliverFromGeolocation.Latitude, deliverFromGeolocation.Longitude))
        );
    }
}