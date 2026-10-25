namespace Payments.Domain.Shared;

/// <summary>An ISO 4217 currency code, normalised to upper case (the extract uses "gbp").</summary>
public readonly record struct Currency
{
    private readonly string? _code;

    private Currency(string code) => _code = code;

    /// <exception cref="InvalidOperationException">The value is <c>default(Currency)</c>, which has no code.</exception>
    public string Code => _code ?? throw new InvalidOperationException("Currency was never initialised; use Currency.From.");

    public static Currency From(string code)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(code);

        if (code.Length != 3 || !code.All(char.IsAsciiLetter))
        {
            throw new ArgumentException($"'{code}' is not a three-letter currency code.", nameof(code));
        }

        return new Currency(code.ToUpperInvariant());
    }

    public override string ToString() => Code;
}
