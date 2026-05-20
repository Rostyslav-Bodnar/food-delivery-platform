using DF.Contracts.EventDriven;
using DF.TrackingService.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.TrackingService.Application.Messaging.Consumers;

public sealed class OrderPickedUpConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderPickedUpConsumer> logger)
    : OrderTerminalStateConsumerBase<OrderPickedUpEvent>(
        connection,
        scopeFactory,
        logger,
        queueName: "tracking.order-picked-up",
        terminalStage: OrderTrackingStages.ToCustomer,
        orderStatus: "PickedUp")
{
    protected override Guid GetOrderId(OrderPickedUpEvent evt) => evt.OrderId;
}
