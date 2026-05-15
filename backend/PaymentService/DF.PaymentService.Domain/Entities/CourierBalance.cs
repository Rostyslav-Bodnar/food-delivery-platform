using DF.PaymentService.Domain.Entities.Common;

namespace DF.PaymentService.Domain.Entities;

public class CourierBalance : Entity
{
    public Guid CourierId { get; private set; }
    public decimal PendingAmount { get; private set; }
    public decimal AvailableAmount { get; private set; }
    public string Currency { get; private set; } = default!;
    public string? StripeAccountId { get; private set; }
    public bool PayoutsEnabled { get; private set; }
    public DateTime UpdatedAtUtc { get; private set; }

    private CourierBalance() { }

    private CourierBalance(Guid courierId, string currency)
    {
        Id = Guid.NewGuid();
        CourierId = courierId;
        Currency = NormalizeCurrency(currency);
        PendingAmount = 0m;
        AvailableAmount = 0m;
        PayoutsEnabled = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public static CourierBalance Create(Guid courierId, string currency) => new(courierId, currency);

    public void AddPending(decimal amount, string currency)
    {
        EnsureCurrency(currency);
        EnsurePositiveAmount(amount);

        PendingAmount += amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RemovePending(decimal amount, string currency)
    {
        EnsureCurrency(currency);
        EnsurePositiveAmount(amount);

        if (PendingAmount < amount)
            throw new InvalidOperationException("Pending courier balance cannot become negative.");

        PendingAmount -= amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void Reserve(decimal amount, string currency)
    {
        EnsureCurrency(currency);
        EnsurePositiveAmount(amount);

        if (PendingAmount < amount)
            throw new InvalidOperationException("Not enough pending balance to reserve payout.");

        PendingAmount -= amount;
        AvailableAmount += amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void CompleteReserved(decimal amount, string currency)
    {
        EnsureCurrency(currency);
        EnsurePositiveAmount(amount);

        if (AvailableAmount < amount)
            throw new InvalidOperationException("Not enough reserved balance to complete payout.");

        AvailableAmount -= amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void RevertReserved(decimal amount, string currency)
    {
        EnsureCurrency(currency);
        EnsurePositiveAmount(amount);

        if (AvailableAmount < amount)
            throw new InvalidOperationException("Not enough reserved balance to revert payout.");

        AvailableAmount -= amount;
        PendingAmount += amount;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetStripeAccount(string stripeAccountId)
    {
        if (string.IsNullOrWhiteSpace(stripeAccountId))
            throw new ArgumentException("Stripe account id is required.", nameof(stripeAccountId));

        StripeAccountId = stripeAccountId.Trim();
        PayoutsEnabled = true;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void DisablePayouts()
    {
        PayoutsEnabled = false;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    private void EnsureCurrency(string currency)
    {
        var normalizedCurrency = NormalizeCurrency(currency);
        if (!string.Equals(Currency, normalizedCurrency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Courier balance currency mismatch.");
    }

    private static void EnsurePositiveAmount(decimal amount)
    {
        if (amount <= 0m)
            throw new InvalidOperationException("Amount must be positive.");
    }

    private static string NormalizeCurrency(string currency)
    {
        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required.", nameof(currency));

        return currency.Trim().ToUpperInvariant();
    }
}
