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
    private IChannel? _channel;

    public async void Start()
    {
        _channel = await connection.CreateChannelAsync();

        await _channel.ExchangeDeclareAsync(
            exchange: "df.events",
            type: ExchangeType.Topic,
            durable: true
        );

        var queue = await _channel.QueueDeclareAsync(
            queue: "tracking.order-created",
            durable: true
        );

        await _channel.QueueBindAsync(
            queue: queue.QueueName,
            exchange: "df.events",
            routingKey: "OrderCreatedEvent" // або "order.created"
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleMessage;

        await _channel.BasicConsumeAsync(
            queue: queue.QueueName,
            autoAck: false,
            consumer: consumer
        );

        Console.WriteLine("Tracking OrderCreatedConsumer started");
    }

    private async Task HandleMessage(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

            if (evt == null)
            {
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var locationRepository = scope.ServiceProvider.GetRequiredService<ILocationRepository>();

            // 🔥 ПАРАЛЕЛЬНО
            var toTask = geolocationService.GetGeodataAsync(evt.DeliverTo.FullAddress);
            var fromTask = geolocationService.GetGeodataAsync(evt.DeliverFrom.FullAddress);

            await Task.WhenAll(toTask, fromTask);

            var deliverToGeo = await toTask;
            var deliverFromGeo = await fromTask;

            if (deliverToGeo == null || deliverFromGeo == null)
            {
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            // 🔥 ВАЖЛИВО: X=Lon, Y=Lat
            var deliverTo = await locationRepository.Create(new Location
            {
                FullAddress = evt.DeliverTo.FullAddress,
                GeoPoint = new Point(
                    deliverToGeo.Longitude,
                    deliverToGeo.Latitude
                )
            });

            var deliverFrom = await locationRepository.Create(new Location
            {
                FullAddress = evt.DeliverFrom.FullAddress,
                GeoPoint = new Point(
                    deliverFromGeo.Longitude,
                    deliverFromGeo.Latitude
                )
            });

            await eventPublisher.PublishLocationsCreatedForOrder(
                new LocationsCreatedForOrder(
                    evt.OrderId,
                    new LocationDTO(deliverTo.Id, deliverToGeo.Latitude, deliverToGeo.Longitude),
                    new LocationDTO(deliverFrom.Id, deliverFromGeo.Latitude, deliverFromGeo.Longitude)
                )
            );

            await _channel!.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Error: {ex.Message}");

            await _channel!.BasicNackAsync(
                ea.DeliveryTag,
                multiple: false,
                requeue: true
            );
        }
    }
}