using DF.PaymentService.Domain.Entities.Common;
using DF.PaymentService.Domain.Events;

namespace DF.PaymentService.Domain.Entities;

public class CourierEarning : AggregateRoot
{
    public Guid CourierId { get; private set; }
    public Guid OrderId { get; private set; }
    public decimal Amount { get; private set; }
    public string Currency { get; private set; } = default!;
    public CourierEarningStatus Status { get; private set; }
    public DateTime EarnedAtUtc { get; private set; }
    public DateTime? AvailableAtUtc { get; private set; }
    public DateTime? PaidAtUtc { get; private set; }
    public Guid? CourierPayoutId { get; private set; }
    public string? StripeTransferId { get; private set; }

    private CourierEarning() { }

    private CourierEarning(
        Guid courierId,
        Guid orderId,
        decimal amount,
        string currency,
        CourierEarningStatus status,
        DateTime earnedAtUtc,
        DateTime? availableAtUtc)
    {
        if (amount <= 0m)
            throw new ArgumentException("Courier earning amount must be positive.", nameof(amount));

        Id = Guid.NewGuid();
        CourierId = courierId;
        OrderId = orderId;
        Amount = amount;
        Currency = currency.Trim().ToUpperInvariant();
        Status = status;
        EarnedAtUtc = earnedAtUtc;
        AvailableAtUtc = availableAtUtc;
    }

    public static CourierEarning CreateBlocked(
        Guid courierId,
        Guid orderId,
        decimal amount,
        string currency,
        DateTime earnedAtUtc)
        => new(courierId, orderId, amount, currency, CourierEarningStatus.Blocked, earnedAtUtc, null);

    public static CourierEarning CreatePending(
        Guid courierId,
        Guid orderId,
        decimal amount,
        string currency,
        DateTime earnedAtUtc,
        DateTime availableAtUtc)
        => new(courierId, orderId, amount, currency, CourierEarningStatus.Pending, earnedAtUtc, availableAtUtc);

    public void Activate(DateTime availableAtUtc)
    {
        if (Status != CourierEarningStatus.Blocked)
            throw new InvalidOperationException("Only blocked earnings can be activated.");

        Status = CourierEarningStatus.Pending;
        AvailableAtUtc = availableAtUtc;
    }

    public void MarkProcessing(Guid payoutId)
    {
        if (Status != CourierEarningStatus.Pending)
            throw new InvalidOperationException("Only pending earnings can enter processing.");

        Status = CourierEarningStatus.Processing;
        CourierPayoutId = payoutId;
    }

    public void MarkPaid(Guid payoutId, string? stripeTransferId, DateTime paidAtUtc)
    {
        if (Status is not CourierEarningStatus.Pending and not CourierEarningStatus.Processing)
            throw new InvalidOperationException("Only pending or processing earnings can be paid.");

        Status = CourierEarningStatus.Paid;
        CourierPayoutId = payoutId;
        StripeTransferId = stripeTransferId;
        PaidAtUtc = paidAtUtc;

        AddDomainEvent(new CourierPayoutCompletedEvent(
            orderId: OrderId,
            courierId: CourierId,
            amount: Amount,
            currency: Currency,
            payoutId: payoutId,
            stripeTransferId: stripeTransferId,
            paidAtUtc: paidAtUtc));
    }

    public void Reopen()
    {
        if (Status != CourierEarningStatus.Processing)
            throw new InvalidOperationException("Only processing earnings can be reopened.");

        Status = CourierEarningStatus.Pending;
        CourierPayoutId = null;
        StripeTransferId = null;
    }

    public void Void()
    {
        if (Status is CourierEarningStatus.Paid or CourierEarningStatus.Voided)
            return;

        Status = CourierEarningStatus.Voided;
    }
}
