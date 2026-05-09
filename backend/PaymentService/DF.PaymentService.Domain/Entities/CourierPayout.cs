using DF.PaymentService.Domain.Entities.Common;

namespace DF.PaymentService.Domain.Entities;

public class CourierPayout : Entity
{
    public Guid CourierId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public CourierPayoutStatus Status { get; private set; }
    public DateTime CreatedAtUtc { get; private set; }
    public DateTime? ProcessedAtUtc { get; private set; }
    public string? StripeTransferId { get; private set; }
    public string? FailureReason { get; private set; }

    private CourierPayout() { }

    private CourierPayout(Guid courierId, decimal amount, string currency)
    {
        if (amount <= 0m)
            throw new ArgumentException("Payout amount must be positive.", nameof(amount));

        Id = Guid.NewGuid();
        CourierId = courierId;
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        Status = CourierPayoutStatus.Processing;
        CreatedAtUtc = DateTime.UtcNow;
    }

    public static CourierPayout Create(Guid courierId, decimal amount, string currency)
        => new(courierId, amount, currency);

    public void MarkPaid(string stripeTransferId, DateTime paidAtUtc)
    {
        if (string.IsNullOrWhiteSpace(stripeTransferId))
            throw new ArgumentException("Stripe transfer id is required.", nameof(stripeTransferId));

        Status = CourierPayoutStatus.Paid;
        StripeTransferId = stripeTransferId;
        ProcessedAtUtc = paidAtUtc;
        FailureReason = null;
    }

    public void MarkFailed(string reason, DateTime failedAtUtc)
    {
        Status = CourierPayoutStatus.Failed;
        FailureReason = string.IsNullOrWhiteSpace(reason) ? "unknown_error" : reason;
        ProcessedAtUtc = failedAtUtc;
    }
}
