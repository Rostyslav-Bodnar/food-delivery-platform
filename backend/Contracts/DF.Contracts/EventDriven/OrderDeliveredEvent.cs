using System;

namespace DF.Contracts.EventDriven;

public record OrderDeliveredEvent(
    Guid OrderId,
    Guid CourierId,
    decimal CourierFee,
    string Currency,
    DateTime DeliveredAtUtc
);
