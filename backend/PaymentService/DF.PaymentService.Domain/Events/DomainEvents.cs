using DF.PaymentService.Domain.Entities.Common;

namespace DF.PaymentService.Domain.Events;

public sealed class PaymentCreatedEvent(Guid paymentId, Guid orderId) : DomainEvent
{
    public Guid PaymentId { get; } = paymentId;
    public Guid OrderId { get; } = orderId;
}

public sealed class PaymentRequiresActionEvent(Guid paymentId, Guid orderId, string? clientSecret, string? reason)
    : DomainEvent
{
    public Guid PaymentId { get; } = paymentId;
    public Guid OrderId { get; } = orderId;
    public string? ClientSecret { get; } = clientSecret;
    public string? Reason { get; } = reason;
}

public sealed class PaymentSucceededEvent(Guid paymentId, Guid orderId) : DomainEvent
{
    public Guid PaymentId { get; } = paymentId;
    public Guid OrderId { get; } = orderId;
}

public sealed class PaymentFailedEvent(Guid paymentId, Guid orderId, string? reason = null) : DomainEvent
{
    public Guid PaymentId { get; } = paymentId;
    public Guid OrderId { get; } = orderId;
    public string? Reason { get; } = reason;
}

public sealed class PaymentCancelledEvent(Guid paymentId, Guid orderId, string? reason = null) : DomainEvent
{
    public Guid PaymentId { get; } = paymentId;
    public Guid OrderId { get; } = orderId;
    public string? Reason { get; } = reason;
}

public sealed class PaymentPartiallyRefundedEvent(
    Guid paymentId,
    Guid orderId,
    decimal lastAmount,
    string currency,
    decimal totalRefunded)
    : DomainEvent
{
    public Guid PaymentId { get; } = paymentId;
    public Guid OrderId { get; } = orderId;
    public decimal LastAmount { get; } = lastAmount;
    public string Currency { get; } = currency;
    public decimal TotalRefunded { get; } = totalRefunded;
}

public sealed class PaymentRefundedEvent(
    Guid paymentId,
    Guid orderId,
    decimal totalRefunded,
    decimal originalAmount,
    string currency)
    : DomainEvent
{
    public Guid PaymentId { get; } = paymentId;
    public Guid OrderId { get; } = orderId;
    public decimal TotalRefunded { get; } = totalRefunded;
    public decimal OriginalAmount { get; } = originalAmount;
    public string Currency { get; } = currency;
}

public sealed class CourierPayoutCompletedEvent(
    Guid orderId,
    Guid courierId,
    decimal amount,
    string currency,
    Guid payoutId,
    string? stripeTransferId,
    DateTime paidAtUtc) : DomainEvent
{
    public Guid OrderId { get; } = orderId;
    public Guid CourierId { get; } = courierId;
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;
    public Guid PayoutId { get; } = payoutId;
    public string? StripeTransferId { get; } = stripeTransferId;
    public DateTime PaidAtUtc { get; } = paidAtUtc;
}
