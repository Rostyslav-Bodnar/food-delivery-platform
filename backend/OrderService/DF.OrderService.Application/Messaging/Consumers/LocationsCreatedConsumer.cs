using System.Text.Json;
using DF.Contracts.EventDriven;
using DF.OrderService.Application.Options;
using DF.OrderService.Application.Repositories.Interfaces;
using DF.OrderService.Application.Services;
using DF.OrderService.Application.Services.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;

namespace DF.OrderService.Application.Messaging.Consumers;

public class LocationsCreatedConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    IOptions<CourierCompensationOptions> courierCompensationOptions,
    ILogger<LocationsCreatedConsumer> logger
) : IConsumer
{
    // Platform convention: every cross-service event flows through df.events (topic),
    // routing key = event-type name. TrackingService publishes LocationsCreatedForOrder
    // here, not to the old "trackingservice" fanout.
    private const string ExchangeName = "df.events";
    private const string QueueName = "orders.locationscreated";
    private const string RoutingKey = "LocationsCreatedForOrder";

    private IChannel? _channel;
    private string? _consumerTag;

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _channel = await connection.CreateChannelAsync(cancellationToken: cancellationToken);

        await MessagingTopology.EnsureDeadLetterAsync(_channel, cancellationToken);

        await _channel.ExchangeDeclareAsync(
            exchange: ExchangeName, type: ExchangeType.Topic, durable: true,
            cancellationToken: cancellationToken);

        await _channel.QueueDeclareAsync(
            queue: QueueName, durable: true, exclusive: false, autoDelete: false,
            arguments: MessagingTopology.DeadLetterArgs(QueueName),
            cancellationToken: cancellationToken);

        await _channel.QueueBindAsync(
            queue: QueueName, exchange: ExchangeName, routingKey: RoutingKey,
            cancellationToken: cancellationToken);

        var consumer = new AsyncEventingBasicConsumer(_channel);
        consumer.ReceivedAsync += HandleAsync;

        _consumerTag = await _channel.BasicConsumeAsync(
            queue: QueueName, autoAck: false, consumer: consumer,
            cancellationToken: cancellationToken);

        logger.LogInformation("LocationsCreatedConsumer started");
    }

    private async Task HandleAsync(object sender, BasicDeliverEventArgs ea)
    {
        if (_channel is null) return;

        try
        {
            var evt = JsonSerializer.Deserialize<LocationsCreatedForOrder>(ea.Body.Span)
                      ?? throw new InvalidOperationException("Empty LocationsCreatedForOrder payload");

            await using var scope = scopeFactory.CreateAsyncScope();
            var orderRepository = scope.ServiceProvider.GetRequiredService<IOrderRepository>();
            var distanceService = scope.ServiceProvider.GetRequiredService<IDistanceService>();

            var distanceKm = await distanceService.GetDistanceKmAsync(
                evt.DeliverFromId.Latitude, evt.DeliverFromId.Longitude,
                evt.DeliverTo.Latitude, evt.DeliverTo.Longitude);

            var order = await orderRepository.Get(evt.OrderId);
            if (order is null)
            {
                logger.LogWarning("LocationsCreated event for unknown order {OrderId}; dropping", evt.OrderId);
                await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
                return;
            }

            order.DeliverFromId = evt.DeliverFromId.Id;
            order.DeliverToId = evt.DeliverTo.Id;

            // Pickup: customer collects at the restaurant, so no delivery / courier fee.
            if (order.DeliveryMethod == DF.OrderService.Domain.Entities.DeliveryMethod.Pickup)
            {
                order.DeliveryFee = 0m;
                order.CourierFee = 0m;
                order.Profit = 0m;
            }
            else
            {
                var deliveryFee = DeliveryFeeCalculator.Calculate(order.TotalPrice, distanceKm);
                var courierSharePercent = Math.Clamp(courierCompensationOptions.Value.CourierSharePercent, 0m, 1m);
                var courierFee = decimal.Round(deliveryFee * courierSharePercent, 2, MidpointRounding.AwayFromZero);

                order.DeliveryFee = deliveryFee;
                order.CourierFee = courierFee;
                order.Profit = deliveryFee - courierFee;
            }

            await orderRepository.Update(order);
            await _channel.BasicAckAsync(ea.DeliveryTag, multiple: false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle LocationsCreated event");
            try { await _channel.BasicNackAsync(ea.DeliveryTag, multiple: false, requeue: false); }
            catch (Exception nackEx) { logger.LogWarning(nackEx, "Failed to nack {DeliveryTag}", ea.DeliveryTag); }
        }
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
            logger.LogWarning(ex, "Error stopping LocationsCreatedConsumer");
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
}
