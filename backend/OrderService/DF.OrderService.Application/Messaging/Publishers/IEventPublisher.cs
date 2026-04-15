using DF.Contracts.EventDriven;

namespace DF.OrderService.Application.Messaging.Publishers;

public interface IEventPublisher
{
    Task PublishOrderCreatedEvent(OrderCreatedEvent evt);
    Task PublishOrderCanceledEvent(OrderCancelledEvent evt);
    Task PublishOrderDeliveredEvent(OrderDeliveredEvent evt);
}
