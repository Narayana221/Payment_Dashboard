using Payments.Domain.Shared;

namespace Payments.Domain.Tests.Shared;

public class MoneyTests
{
    private static readonly Currency Gbp = Currency.From("gbp");
    private static readonly Currency Eur = Currency.From("EUR");

    [Fact]
    public void From_LowerCaseCode_IsNormalisedToUpperCase() => Assert.Equal("GBP", Gbp.Code);

    [Theory]
    [InlineData("GB")]
    [InlineData("GBPX")]
    [InlineData("G1P")]
    public void From_InvalidCode_Throws(string code) => Assert.Throws<ArgumentException>(() => Currency.From(code));

    [Fact]
    public void Arithmetic_SameCurrency_IsExact()
    {
        var total = new Money(1_000, Gbp) + new Money(250, Gbp) - new Money(1, Gbp);

        Assert.Equal(new Money(1_249, Gbp), total);
    }

    [Fact]
    public void Add_DifferentCurrencies_Throws() =>
        Assert.Throws<InvalidOperationException>(() => new Money(1, Gbp) + new Money(1, Eur));

    [Fact]
    public void Sum_Empty_IsZeroInRequestedCurrency() => Assert.Equal(Money.Zero(Gbp), Money.Sum([], Gbp));
}
