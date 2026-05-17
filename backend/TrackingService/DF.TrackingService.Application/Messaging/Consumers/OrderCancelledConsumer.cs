using DF.Contracts.EventDriven;
using DF.TrackingService.Contracts.Models;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;

namespace DF.TrackingService.Application.Messaging.Consumers;

public sealed class OrderCancelledConsumer(
    IConnection connection,
    IServiceScopeFactory scopeFactory,
    ILogger<OrderCancelledConsumer> logger)
    : OrderTerminalStateConsumerBase<OrderCancelledEvent>(
        connection,
        scopeFactory,
        logger,
        queueName: "tracking.order-cancelled",
        terminalStage: OrderTrackingStages.Cancelled)
{
    protected override Guid GetOrderId(OrderCancelledEvent evt) => evt.OrderId;
}
