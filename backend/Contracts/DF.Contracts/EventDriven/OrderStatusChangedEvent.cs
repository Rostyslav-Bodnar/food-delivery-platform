using System;

namespace DF.Contracts.EventDriven;

/// <summary>
/// Generic order-status-transition event used by TrackingService to push live
/// updates to subscribed customer/business/courier clients. Sits alongside the
/// existing OrderPickedUp/OrderDelivered/OrderCancelled events (which carry
/// domain-specific payloads consumed by PaymentService etc.) and is published
/// on every status change in OrderService.
/// </summary>
public record OrderStatusChangedEvent(
    Guid OrderId,
    Guid BusinessId,
    Guid CustomerId,
    Guid? CourierId,
    string NewStatus,
    string PreviousStatus,
    DateTime ChangedAtUtc);
