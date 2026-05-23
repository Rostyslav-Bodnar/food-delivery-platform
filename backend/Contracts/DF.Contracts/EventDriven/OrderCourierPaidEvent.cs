using System;

namespace DF.Contracts.EventDriven;

/// <summary>
/// Fires when a cash-on-delivery order's courier confirms cash receipt
/// (CourierPaid flips true). Doesn't change order status, but downstream
/// consumers (TrackingService) still want to push the update live to the
/// affected user groups.
/// </summary>
public record OrderCourierPaidEvent(
    Guid OrderId,
    Guid BusinessId,
    Guid CustomerId,
    Guid CourierId,
    DateTime PaidAtUtc);
