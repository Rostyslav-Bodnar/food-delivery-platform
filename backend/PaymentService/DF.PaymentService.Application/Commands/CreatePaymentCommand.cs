using DF.PaymentService.Domain.Entities;

namespace DF.PaymentService.Application.Commands;

public class CreatePaymentCommand(
    Guid orderId,
    decimal amount,
    string currency,
    PaymentMethod method,
    string? destinationStripeAccountId = null)
{
    public Guid OrderId { get; } = orderId;
    public decimal Amount { get; } = amount;
    public string Currency { get; } = currency;
    public PaymentMethod Method { get; } = method;

    // Connected-account ID (acct_*) — when set on an Online payment, the
    // PaymentIntent is created as a destination charge so funds settle on
    // the business's Stripe Connect account (minus the platform fee).
    public string? DestinationStripeAccountId { get; } = destinationStripeAccountId;
}
