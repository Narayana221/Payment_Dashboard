using Payments.Domain.Shared;

namespace Payments.Domain.Records;

/// <summary>A movement on the organiser's Stripe balance: a charge (money in) or a refund (money out).</summary>
public abstract record BalanceTransaction
{
    public required string Id { get; init; }

    public required DateTimeOffset Created { get; init; }

    /// <summary>What the buyer paid (charge) or got back (refund). Always positive.</summary>
    public required Money Amount { get; init; }

    /// <summary>The effect on the organiser's balance after fees: positive for a charge, negative for a refund.</summary>
    public required Money Net { get; init; }

    /// <summary>The date these funds become eligible for payout.</summary>
    public required DateOnly AvailableOn { get; init; }

    public required IReadOnlyList<Fee> Fees { get; init; }

    public Currency Currency => Amount.Currency;

    public Money FeesOf(FeeType type) =>
        Money.Sum(Fees.Where(fee => fee.Type == type).Select(fee => fee.Amount), Currency);

    public Money TotalFees => Money.Sum(Fees.Select(fee => fee.Amount), Currency);
}
