namespace DF.PaymentService.Domain.Entities;

public sealed class Money : IEquatable<Money>, IComparable<Money>
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative", nameof(amount));

        if (string.IsNullOrWhiteSpace(currency))
            throw new ArgumentException("Currency is required", nameof(currency));

        Amount = amount;
        Currency = currency.ToUpperInvariant();
    }

    private static void EnsureSameCurrency(Money a, Money b)
    {
        if (!string.Equals(a.Currency, b.Currency, StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Money currency mismatch.");
    }

    // ---------------------------
    // Arithmetic operators
    // ---------------------------

    public static Money operator +(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return new Money(a.Amount + b.Amount, a.Currency);
    }

    public static Money operator -(Money a, Money b)
    {
        EnsureSameCurrency(a, b);

        if (a.Amount < b.Amount)
            throw new InvalidOperationException("Money result cannot be negative.");

        return new Money(a.Amount - b.Amount, a.Currency);
    }

    // ---------------------------
    // Comparison operators
    // ---------------------------

    public static bool operator >(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount > b.Amount;
    }

    public static bool operator <(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount < b.Amount;
    }

    public static bool operator >=(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount >= b.Amount;
    }

    public static bool operator <=(Money a, Money b)
    {
        EnsureSameCurrency(a, b);
        return a.Amount <= b.Amount;
    }

    // ---------------------------
    // Equality
    // ---------------------------

    public static bool operator ==(Money a, Money b)
    {
        if (ReferenceEquals(a, b))
            return true;

        if (a is null || b is null)
            return false;

        return a.Amount == b.Amount &&
               string.Equals(a.Currency, b.Currency, StringComparison.OrdinalIgnoreCase);
    }

    public static bool operator !=(Money a, Money b) => !(a == b);

    public bool Equals(Money? other)
    {
        if (other is null) return false;

        return Amount == other.Amount &&
               string.Equals(Currency, other.Currency, StringComparison.OrdinalIgnoreCase);
    }

    public override bool Equals(object? obj) => Equals(obj as Money);

    public override int GetHashCode()
        => HashCode.Combine(Amount, Currency.ToUpperInvariant());

    // ---------------------------
    // Compare
    // ---------------------------

    public int CompareTo(Money? other)
    {
        if (other is null) return 1;

        EnsureSameCurrency(this, other);
        return Amount.CompareTo(other.Amount);
    }

    public override string ToString()
        => $"{Amount} {Currency}";
}