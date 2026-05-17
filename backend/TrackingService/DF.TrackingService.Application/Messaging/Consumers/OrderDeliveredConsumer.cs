using DF.Contracts.EventDriven;
using DF.TrackingService.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.TrackingService.Application.Messaging.Consumers;

public sealed class OrderDeliveredConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderDeliveredConsumer> logger)
    : OrderTerminalStateConsumerBase<OrderDeliveredEvent>(
        connection,
        scopeFactory,
        logger,
        queueName: "tracking.order-delivered",
        terminalStage: OrderTrackingStages.Delivered)
{
    protected override Guid GetOrderId(OrderDeliveredEvent evt) => evt.OrderId;
}
