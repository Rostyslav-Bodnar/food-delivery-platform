using System;

namespace DF.Contracts.EventDriven;

public record OrderPickedUpEvent(
    Guid OrderId,
    Guid CourierId,
    DateTime PickedUpAtUtc);