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
            routingKey: nameof(OrderCreatedEvent)
        );

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleMessage;

        await _channel.BasicConsumeAsync(
            queue: queue.QueueName,
            autoAck: false,
            consumer: consumer
        );

        Console.WriteLine("✅ Tracking OrderCreatedConsumer started");
    }

    private async Task HandleMessage(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

            if (evt is null)
            {
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            using var scope = scopeFactory.CreateScope();

            var locationRepository =
                scope.ServiceProvider.GetRequiredService<ILocationRepository>();

            var businessLocationRepository =
                scope.ServiceProvider.GetRequiredService<IBusinessLocationRepository>();

            // ✅ 1. Геокодуємо ЛИШЕ клієнта
            var toGeo = await geolocationService.GetGeodataAsync(evt.DeliverTo.FullAddress);
            if (toGeo is null)
            {
                Console.WriteLine($"❌ Cannot geocode DeliverTo for order {evt.OrderId}");
                await _channel!.BasicAckAsync(ea.DeliveryTag, false);
                return;
            }

            // ✅ 2. Створюємо Location ТІЛЬКИ для клієнта
            var deliverTo = await locationRepository.Create(new Location
            {
                FullAddress = evt.DeliverTo.FullAddress,
                GeoPoint = new Point(toGeo.Longitude, toGeo.Latitude) // X=Lon, Y=Lat
            });

            // ✅ 3. Отримуємо ВСІ бізнес-локації
            var businessLocations =
                (await businessLocationRepository.GetByBusinessIdAsync(evt.BusinessId))
                .ToList();

            if (!businessLocations.Any())
            {
                throw new InvalidOperationException(
                    $"Business {evt.BusinessId} has no registered locations");
            }

            // ✅ 4. Обираємо НАЙБЛИЖЧУ локацію бізнесу
            var nearest = businessLocations
                .Select(bl => new
                {
                    bl.LocationId,
                    bl.Location,
                    DistanceKm = HaversineKm(
                        toGeo.Latitude,
                        toGeo.Longitude,
                        bl.Location.GeoPoint.Y, // lat
                        bl.Location.GeoPoint.X  // lon
                    )
                })
                .OrderBy(x => x.DistanceKm)
                .First();

            // ✅ 5. Публікуємо подію (БЕЗ створення нової Location для бізнесу)
            await eventPublisher.PublishLocationsCreatedForOrder(
                new LocationsCreatedForOrder(
                    evt.OrderId,
                    new LocationDTO(
                        deliverTo.Id,
                        toGeo.Latitude,
                        toGeo.Longitude
                    ),
                    new LocationDTO(
                        nearest.LocationId,
                        nearest.Location.GeoPoint.Y,
                        nearest.Location.GeoPoint.X
                    )
                )
            );

            await _channel!.BasicAckAsync(ea.DeliveryTag, false);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"❌ Tracking OrderCreatedConsumer error: {ex}");

            await _channel!.BasicNackAsync(
                ea.DeliveryTag,
                multiple: false,
                requeue: true
            );
        }
    }

    // ✅ Простий та надійний Haversine
    private static double HaversineKm(
        double lat1, double lon1,
        double lat2, double lon2)
    {
        const double R = 6371;

        double dLat = DegreesToRadians(lat2 - lat1);
        double dLon = DegreesToRadians(lon2 - lon1);

        double a =
            Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
            Math.Cos(DegreesToRadians(lat1)) *
            Math.Cos(DegreesToRadians(lat2)) *
            Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        return 2 * R * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static double DegreesToRadians(double deg)
        => deg * Math.PI / 180.0;
}
