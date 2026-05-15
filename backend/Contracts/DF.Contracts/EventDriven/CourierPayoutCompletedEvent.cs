using System;

namespace DF.Contracts.EventDriven;

public record CourierPayoutCompletedEvent(
    Guid OrderId,
    Guid CourierId,
    decimal Amount,
    string Currency,
    Guid PayoutId,
    string? StripeTransferId,
    DateTime PaidAtUtc
);
