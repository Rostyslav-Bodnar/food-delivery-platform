using DF.PaymentService.Domain.Entities.Common;

namespace DF.PaymentService.Domain.Entities;

public class RefundRecord : ValueObject
{
    public Guid Id { get; }
    public Guid PaymentId { get; }
    public Money Amount { get; }
    public string? StripeRefundId { get; }
    public DateTime OccurredOnUtc { get; }

    private RefundRecord() { }

    private RefundRecord(Guid paymentId, Money amount, string? stripeRefundId)
    {
        Id = Guid.NewGuid();
        PaymentId = paymentId;
        Amount = amount;
        StripeRefundId = stripeRefundId;
        OccurredOnUtc = DateTime.UtcNow;
    }

    public static RefundRecord Create(Guid paymentId, Money amount, string? stripeRefundId = null)
        => new(paymentId, amount, stripeRefundId);

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Id;
        yield return PaymentId;
        yield return Amount;
        yield return StripeRefundId;
        yield return OccurredOnUtc;
    }
}
