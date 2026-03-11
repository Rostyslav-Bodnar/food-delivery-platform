using System.Collections.ObjectModel;
using DF.PaymentService.Domain.Entities.Common;
using DF.PaymentService.Domain.Events;

namespace DF.PaymentService.Domain.Entities;

public class Payment : AggregateRoot
{
    public Guid OrderId { get; private set; }
    public Money Amount { get; private set; }
    public PaymentMethod Method { get; private set; }
    public PaymentStatus Status { get; private set; }

    // Stripe debug/trace
    public string? StripePaymentIntentId { get; private set; }
    public string? StripeClientSecret { get; private set; }

    // 🔁 EPIC A — нові поля
    public DateTime? ExpiresAt { get; private set; }

    // Сума повернень (partial/full). Важливо тримати ту ж валюту, що й Amount.
    public Money TotalRefunded { get; private set; }

    // Аудит часткових рефандів (опційно). Тримайте як ReadOnlyCollection назовні.
    private readonly List<RefundRecord> _refunds = new();
    public IReadOnlyCollection<RefundRecord> Refunds => new ReadOnlyCollection<RefundRecord>(_refunds);

    // Причини
    public string? CancelReason { get; private set; }
    public string? FailReason { get; private set; }

    // Спори (optional)
    public string? DisputeId { get; private set; }
    public string? DisputeStatus { get; private set; }
    public DateTime? EvidenceSubmittedAt { get; private set; }

    private Payment() { }

    public Payment(Guid orderId, Money amount, PaymentMethod method)
    {
        Id = Guid.NewGuid();
        OrderId = orderId;
        Amount = amount ?? throw new ArgumentNullException(nameof(amount));
        Method = method;

        Status = method == PaymentMethod.CashOnDelivery
            ? PaymentStatus.AwaitingCashCollection
            : PaymentStatus.Pending;

        // Ініціалізація суми повернень нулем у тій же валюті
        TotalRefunded = new Money(0m, amount.Currency);
    }

    public static Payment CreatePayment(Guid orderId, Money amount, PaymentMethod method)
    {
        var payment = new Payment(orderId, amount, method);
        payment.AddDomainEvent(new PaymentCreatedEvent(payment.Id, orderId));
        return payment;
    }

    public void SetStripeSecrets(string paymentIntentId, string clientSecret)
    {
        if (string.IsNullOrWhiteSpace(paymentIntentId))
            throw new ArgumentException("Stripe Payment Intent Id cannot be empty.", nameof(paymentIntentId));

        StripePaymentIntentId = paymentIntentId;
        StripeClientSecret = clientSecret;
    }

    /// <summary>
    /// Перехід у RequiresAction (SCA/3DS). Доречний коли бекенд робить confirm
    /// і Stripe вимагає додаткової дії. Можна також встановити/оновити ExpiresAt.
    /// </summary>
    public void MarkRequiresAction(DateTime? expiresAt = null, string? reason = null)
    {
        EnsureNotTerminal();

        if (Status is not PaymentStatus.Pending and not PaymentStatus.RequiresAction)
            throw new InvalidOperationException("RequiresAction is allowed only from Pending or RequiresAction.");

        Status = PaymentStatus.RequiresAction;
        if (expiresAt.HasValue) ExpiresAt = expiresAt;

        AddDomainEvent(new PaymentRequiresActionEvent(Id, OrderId, StripeClientSecret, reason));
    }

    /// <summary>
    /// Успіх оплати (Online або CoD колекція). Дозволено з Pending/RequiresAction/AwaitingCashCollection/Failed (якщо банківський ретрай?), але НЕ з Cancelled/Refunded.
    /// </summary>
    public void MarkSucceeded()
    {
        if (Status is PaymentStatus.Succeeded or PaymentStatus.Refunded)
            throw new InvalidOperationException("Invalid transition: payment already finalized as Succeeded/Refunded.");

        if (Status == PaymentStatus.Cancelled)
            throw new InvalidOperationException("Cannot succeed a cancelled payment.");

        Status = PaymentStatus.Succeeded;
        ExpiresAt = null; // вже не потрібен TTL

        AddDomainEvent(new PaymentSucceededEvent(Id, OrderId));
    }

    /// <summary>
    /// Fail із причиною (наприклад, insufficient_funds). Не дозволено якщо вже Succeeded/Refunded.
    /// </summary>
    public void FailWithReason(string reason)
    {
        EnsureNotFinalizedSucceededOrRefunded();

        Status = PaymentStatus.Failed;
        FailReason = reason;

        AddDomainEvent(new PaymentFailedEvent(Id, OrderId, reason));
    }

    /// <summary>
    /// Backward-compatible шорткат.
    /// </summary>
    public void MarkFailed() => FailWithReason("generic_failure");

    /// <summary>
    /// Скасування з причиною (user_cancel, timeout, stripe_canceled). Заборонено після Succeeded/Refunded.
    /// </summary>
    public void CancelWithReason(string reason)
    {
        if (Status is PaymentStatus.Succeeded or PaymentStatus.Refunded)
            throw new InvalidOperationException("Cannot cancel a finalized payment (Succeeded/Refunded).");

        Status = PaymentStatus.Cancelled;
        CancelReason = reason;

        AddDomainEvent(new PaymentCancelledEvent(Id, OrderId, reason));
    }

    /// <summary>
    /// Backward-compatible шорткат.
    /// </summary>
    public void MarkCancelled() => CancelWithReason("generic_cancel");

    /// <summary>
    /// Частковий рефанд. Дозволено тільки після Succeeded. Сума не може перевищувати залишок.
    /// </summary>
    public void ApplyPartialRefund(Money amount, string? stripeRefundId = null)
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("Partial refund is allowed only from Succeeded state.");

        if (amount is null) throw new ArgumentNullException(nameof(amount));
        if (!string.Equals(amount.Currency, Amount.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refund currency must match payment currency.");

        var remaining = Amount - TotalRefunded;
        if (amount.Amount <= 0m)
            throw new InvalidOperationException("Refund amount must be positive.");
        if (amount.Amount > remaining.Amount)
            throw new InvalidOperationException("Refund exceeds remaining paid amount.");

        // Фіксуємо запис рефанду (аудит)
        var record = RefundRecord.Create(Id, amount, stripeRefundId);
        _refunds.Add(record);

        // Оновлюємо TotalRefunded
        TotalRefunded = TotalRefunded + amount;

        AddDomainEvent(new PaymentPartiallyRefundedEvent(
            Id, OrderId, amount.Amount, amount.Currency, TotalRefunded.Amount));

        // Якщо вибили всю суму — фіналізуємо як Refunded
        var newRemaining = Amount - TotalRefunded;
        if (newRemaining.Amount == 0m)
        {
            MarkRefundedInternal();
        }
    }

    /// <summary>
    /// Повний рефанд (миттєво робимо у домені). Використовуйте коли Stripe підтвердив full refund.
    /// </summary>
    public void ApplyFullRefund(string? stripeRefundId = null)
    {
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("Full refund is allowed only from Succeeded state.");

        var remaining = Amount - TotalRefunded;
        if (remaining.Amount <= 0m)
        {
            // вже фактично все повернено
            MarkRefundedInternal();
            return;
        }

        var record = RefundRecord.Create(Id, remaining, stripeRefundId);
        _refunds.Add(record);

        TotalRefunded = Amount; // вибили все
        MarkRefundedInternal();
    }

    /// <summary>
    /// Внутрішній перехід у Refunded (без додаткових перевірок). Викликається тільки з ApplyPartial/Full.
    /// </summary>
    private void MarkRefundedInternal()
    {
        // тут стан має бути Succeeded, але на всякий — перевірка:
        if (Status != PaymentStatus.Succeeded)
            throw new InvalidOperationException("Invalid transition to Refunded from non-succeeded state.");

        Status = PaymentStatus.Refunded;

        AddDomainEvent(new PaymentRefundedEvent(
            Id, OrderId, TotalRefunded.Amount, Amount.Amount, Amount.Currency));
    }

    /// <summary>
    /// Службові методи для Dispute (за потреби).
    /// </summary>
    public void MarkDisputed(string disputeId, string disputeStatus)
    {
        DisputeId = disputeId;
        DisputeStatus = disputeStatus;
        // Можна додати окремий доменний івент PaymentDisputeOpenedEvent
    }

    public void UpdateDisputeStatus(string disputeStatus, DateTime? evidenceSubmittedAt = null)
    {
        DisputeStatus = disputeStatus;
        EvidenceSubmittedAt = evidenceSubmittedAt ?? EvidenceSubmittedAt;
        // Можна додати PaymentDisputeUpdatedEvent
    }

    /// <summary>
    /// Хелпери-інваріанти.
    /// </summary>
    private void EnsureNotTerminal()
    {
        if (Status is PaymentStatus.Cancelled or PaymentStatus.Refunded)
            throw new InvalidOperationException("Payment is terminal (Cancelled/Refunded).");
    }

    private void EnsureNotFinalizedSucceededOrRefunded()
    {
        if (Status is PaymentStatus.Succeeded or PaymentStatus.Refunded)
            throw new InvalidOperationException("Payment is finalized as Succeeded/Refunded.");
    }

    /// <summary>
    /// Утиліта для встановлення TTL (використовуйте при створенні PI або при переході в RequiresAction).
    /// </summary>
    public void SetExpiration(DateTime? expiresAt) => ExpiresAt = expiresAt;
}