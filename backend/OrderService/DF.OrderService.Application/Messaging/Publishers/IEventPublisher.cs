using DF.Contracts.EventDriven;

namespace DF.OrderService.Application.Messaging.Publishers;

public interface IEventPublisher
{
    Task PublishOrderCreatedEvent(OrderCreatedEvent evt);
}