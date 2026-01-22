using System;

namespace DF.Contracts.EventDriven;

// DTO контракт
public record OrderCreatedEvent(
    Guid OrderId,
    Guid BusinessId,
    Guid OrderedBy,
    DateTime OrderDate,
    decimal TotalPrice,
    LocationDto DeliverTo,
    LocationDto DeliverFrom
);

public record LocationDto(
    string FullAddress
);