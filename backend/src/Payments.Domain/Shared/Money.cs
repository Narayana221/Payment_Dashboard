namespace Payments.Domain.Shared;

/// <summary>
/// An amount in the currency's smallest unit (pence for GBP). Arithmetic is exact and refuses to mix currencies.
/// </summary>
public readonly record struct Money(long MinorUnits, Currency Currency)
{
    public static Money Zero(Currency currency) => new(0, currency);

    public static Money Sum(IEnumerable<Money> values, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(values);
        return values.Aggregate(Zero(currency), (total, value) => total + value);
    }

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(checked(left.MinorUnits + right.MinorUnits), left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(checked(left.MinorUnits - right.MinorUnits), left.Currency);
    }

    public static Money operator -(Money value) => new(checked(-value.MinorUnits), value.Currency);

    public static Money Add(Money left, Money right) => left + right;

    public static Money Subtract(Money left, Money right) => left - right;

    public static Money Negate(Money value) => -value;

    public override string ToString() => $"{MinorUnits} {Currency}";

    private static void EnsureSameCurrency(Money left, Money right)
    {
        // Reading Code fails fast on default(Money), whose currency was never set.
        if (left.Currency.Code != right.Currency.Code)
        {
            throw new InvalidOperationException($"Cannot combine {left.Currency} and {right.Currency} amounts.");
        }
    }
}
