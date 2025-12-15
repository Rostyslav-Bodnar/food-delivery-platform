namespace DF.Contracts.EventDriven;

public record LocationsCreatedForOrder(Guid OrderId, Guid DeliverToId, Guid DeliverFromId);