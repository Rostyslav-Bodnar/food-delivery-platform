namespace DF.PaymentService.Domain.Entities.Common;

/// <summary>
/// Базовий клас для Value Object-ів у DDD.
/// Забезпечує рівність за складниками (structural equality), стабільний хеш
/// та оператори порівняння.
/// 
/// Наслідники мають реалізувати:
///   protected override IEnumerable<object?> GetEqualityComponents()
/// де повертається послідовність атомарних значень (у фіксованому порядку),
/// що повністю визначають рівність VO.
/// </summary>
public abstract class ValueObject
{
    /// <summary>
    /// Повертає колекцію атомарних значень (у фіксованому порядку),
    /// які визначають рівність даного ValueObject.
    /// Напр., для Money: yield return Amount; yield return Currency;
    /// </summary>
    protected abstract IEnumerable<object?> GetEqualityComponents();

    public override bool Equals(object? obj)
    {
        if (obj is null) return false;
        if (ReferenceEquals(this, obj)) return true;

        // Рівні лише об'єкти однакового конкретного типу
        if (obj.GetType() != GetType()) return false;

        var other = (ValueObject)obj;

        // Порівнюємо поелементно колекції складників
        using var thisValues = GetEqualityComponents().GetEnumerator();
        using var otherValues = other.GetEqualityComponents().GetEnumerator();

        while (true)
        {
            var thisMoved = thisValues.MoveNext();
            var otherMoved = otherValues.MoveNext();

            if (!thisMoved && !otherMoved)
                return true; // закінчили обидві колекції одночасно — рівні

            if (thisMoved != otherMoved)
                return false; // різна довжина колекцій

            var thisCurrent = thisValues.Current;
            var otherCurrent = otherValues.Current;

            if (thisCurrent is null ^ otherCurrent is null)
                return false;

            if (thisCurrent is null && otherCurrent is null)
                continue;

            // Якщо вкладені об'єкти також VO/Entities з перевизначеним Equals — це спрацює коректно
            if (!thisCurrent!.Equals(otherCurrent))
                return false;
        }
    }

    public override int GetHashCode()
    {
        // Комбінуємо хеші усіх атомарних значень
        // Важливо: порядок має бути таким самим, як у GetEqualityComponents()
        unchecked
        {
            const int seed = 17;
            const int multiplier = 23;

            return GetEqualityComponents()
                .Aggregate(seed, (hash, obj) =>
                {
                    var componentHash = obj?.GetHashCode() ?? 0;
                    return (hash * multiplier) + componentHash;
                });
        }
    }

    /// <summary>
    /// Допоміжні оператори (з урахуванням null).
    /// </summary>
    protected static bool EqualOperator(ValueObject? left, ValueObject? right)
    {
        if (left is null ^ right is null)
            return false;

        return left is null || left.Equals(right);
    }

    protected static bool NotEqualOperator(ValueObject? left, ValueObject? right)
        => !EqualOperator(left, right);

    public static bool operator ==(ValueObject? a, ValueObject? b) => EqualOperator(a, b);

    public static bool operator !=(ValueObject? a, ValueObject? b) => NotEqualOperator(a, b);
}