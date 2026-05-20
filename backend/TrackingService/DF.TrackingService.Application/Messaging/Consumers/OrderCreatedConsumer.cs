using System.Text;
using System.Text.Json;
using DF.Contracts.EventDriven;
using DF.TrackingService.Application.Messaging.Publishers;
using DF.TrackingService.Application.Repositories.Interfaces;
using DF.TrackingService.Application.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NetTopologySuite.Geometries;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using Location = DF.TrackingService.Domain.Entities.Location;

namespace DF.TrackingService.Application.Messaging.Consumers;

public sealed class OrderCreatedConsumer(
    IConnection connection,
    GeolocationService geolocationService,
    IServiceScopeFactory scopeFactory,
    IEventPublisher eventPublisher,
    ILogger<OrderCreatedConsumer> logger) : IConsumer
{
    private IChannel? _channel;
    private string? _consumerTag;
    private const string QueueName = "tracking.order-created";

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await MessagingTopology.EnsureDeadLetterAsync(_channel, cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: "df.events",
            type: ExchangeType.Topic,
            durable: true,
            cancellationToken: cancellationToken);

        var queue = await _channel.QueueDeclareAsync(
            queue: QueueName,
            durable: true,
            exclusive: false,
            autoDelete: false,
            arguments: MessagingTopology.DeadLetterArgs(QueueName),
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: queue.QueueName,
            exchange: "df.events",
            routingKey: nameof(OrderCreatedEvent),
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleMessage;

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: queue.QueueName,
            autoAck: false,
            consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation("OrderCreatedConsumer started on queue {Queue}", queue.QueueName);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_channel is null) return;

        try
        {
            if (!string.IsNullOrEmpty(_consumerTag))
                await _channel.BasicCancelAsync(_consumerTag, noWait: false, cancellationToken: cancellationToken);
            await _channel.CloseAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Error while stopping OrderCreatedConsumer");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_channel is not null)
        {
            await _channel.DisposeAsync();
            _channel = null;
        }
    }

    private async Task HandleMessage(object sender, BasicDeliverEventArgs ea)
    {
        try
        {
            var json = Encoding.UTF8.GetString(ea.Body.ToArray());
            var evt = JsonSerializer.Deserialize<OrderCreatedEvent>(json);

            if (evt is null)
            {
                logger.LogWarning("Null OrderCreatedEvent payload; acking and skipping");
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            using var scope = scopeFactory.CreateScope();
            var locationRepository = scope.ServiceProvider
                .GetRequiredService<ILocationRepository>();
            var businessLocationRepository = scope.ServiceProvider
                .GetRequiredService<IBusinessLocationRepository>();

            // ----- IDEMPOTENCY -----
            // RabbitMQ is at-least-once. If a Location for this OrderId already
            // exists, this is a redelivery — ack and skip.
            var existing = await locationRepository.GetByOrderIdAsync(evt.OrderId);
            if (existing is not null)
            {
                logger.LogInformation(
                    "OrderCreatedEvent {OrderId} already processed (Location {LocationId}); skipping",
                    evt.OrderId, existing.Id);
                await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            // ----- GEOCODE CUSTOMER -----
            var toGeo = await geolocationService.GetGeodataAsync(evt.DeliverTo.FullAddress);
            if (toGeo is null)
            {
                logger.LogWarning(
                    "Cannot geocode DeliverTo for order {OrderId}; requeueing",
                    evt.OrderId);
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: !ea.Redelivered);
                return;
            }

            // ----- NEAREST BUSINESS LOCATION -----
            var businessLocations =
                (await businessLocationRepository.GetByBusinessIdAsync(evt.BusinessId))
                .ToList();

            if (businessLocations.Count == 0)
            {
                logger.LogError(
                    "Business {BusinessId} has no registered locations; routing {OrderId} to DLQ",
                    evt.BusinessId, evt.OrderId);
                // Permanent failure: send to DLQ so ops can inspect rather than silently dropping.
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false);
                return;
            }

            var nearest = businessLocations
                .Select(bl => new
                {
                    bl.LocationId,
                    bl.Location,
                    DistanceKm = HaversineKm(
                        toGeo.Latitude, toGeo.Longitude,
                        bl.Location.GeoPoint!.Y, bl.Location.GeoPoint.X)
                })
                .OrderBy(x => x.DistanceKm)
                .First();

            // ----- PERSIST DELIVERTO -----
            var deliverTo = await locationRepository.Create(new Location
            {
                FullAddress = evt.DeliverTo.FullAddress,
                GeoPoint = new Point(toGeo.Longitude, toGeo.Latitude) { SRID = 4326 },
                OrderId = evt.OrderId
            });

            // ----- PUBLISH -----
            await eventPublisher.PublishLocationsCreatedForOrder(
                new LocationsCreatedForOrder(
                    evt.OrderId,
                    new LocationDTO(deliverTo.Id, toGeo.Latitude, toGeo.Longitude),
                    new LocationDTO(
                        nearest.LocationId,
                        nearest.Location.GeoPoint!.Y,
                        nearest.Location.GeoPoint.X)));

            await _channel!.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "OrderCreatedConsumer handler failed; requeue={Requeue}", !ea.Redelivered);
            try
            {
                await _channel!.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: !ea.Redelivered);
            }
            catch (Exception nackEx)
            {
                logger.LogError(nackEx, "Failed to nack on OrderCreatedConsumer");
            }
        }
    }

    private static double HaversineKm(double lat1, double lon1, double lat2, double lon2)
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

    private static double DegreesToRadians(double deg) => deg * Math.PI / 180.0;
}
