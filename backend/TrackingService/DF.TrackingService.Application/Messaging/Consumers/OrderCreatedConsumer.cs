using System.Text;
using System.Text.Json;
using DF.Contracts.EventDriven;
using DF.TrackingService.Application.Messaging.Publishers;
using DF.TrackingService.Application.Repositories.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite.Geometries;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Location = DF.TrackingService.Domain.Entities.Location;

namespace DF.TrackingService.Application.Messaging.Consumers;

public class OrderCreatedConsumer(
    IConnection connection,
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

        var deliverTo = await locationRepository.Create(new Location
        {
            FullAddress = evt.DeliverTo.FullAddress,
            City = evt.DeliverTo.City,
            Street = evt.DeliverTo.Street,
            House = evt.DeliverTo.House,
            GeoPoint = new Point(evt.DeliverTo.Longitude, evt.DeliverTo.Latitude)
        });

        var deliverFrom = await locationRepository.Create(new Location
        {
            FullAddress = evt.DeliverFrom.FullAddress,
            City = evt.DeliverFrom.City,
            Street = evt.DeliverFrom.Street,
            House = evt.DeliverFrom.House,
            GeoPoint = new Point(evt.DeliverFrom.Longitude, evt.DeliverFrom.Latitude)
        });

        await eventPublisher.PublishLocationsCreatedForOrder(
            new LocationsCreatedForOrder(evt.OrderId, deliverTo.Id, deliverFrom.Id)
        );
    }
}