using DF.Contracts.EventDriven;

namespace DF.TrackingService.Application.Messaging.Publishers;

public interface IEventPublisher
{
    Task PublishLocationsCreatedForOrder(LocationsCreatedForOrder evt);
}