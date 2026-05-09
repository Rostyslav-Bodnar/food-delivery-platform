using System;

namespace DF.Contracts.EventDriven;

public record OrderCancelledEvent(Guid OrderId, string PaymentMethod);