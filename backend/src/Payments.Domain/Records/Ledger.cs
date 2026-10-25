using Payments.Domain.Shared;

namespace Payments.Domain.Records;

/// <summary>
/// An organiser's charges, refunds and payouts. Each list is in chronological order (by <c>Created</c>, then <c>Id</c>);
/// the source file is not chronological, so ordering is established here and nothing downstream depends on input order.
/// </summary>
public sealed class Ledger
{
    private Ledger(IReadOnlyList<Charge> charges, IReadOnlyList<Refund> refunds, IReadOnlyList<Payout> payouts)
    {
        Charges = charges;
        Refunds = refunds;
        Payouts = payouts;
    }

    public static Ledger Empty { get; } = new([], [], []);

    public IReadOnlyList<Charge> Charges { get; }

    public IReadOnlyList<Refund> Refunds { get; }

    public IReadOnlyList<Payout> Payouts { get; }

    public IEnumerable<BalanceTransaction> Transactions => Charges.Concat<BalanceTransaction>(Refunds);

    /// <summary>When the newest record of any kind was created; <c>null</c> for an empty ledger.</summary>
    public DateTimeOffset? LatestActivityAt =>
        Transactions.Select(t => t.Created)
            .Concat(Payouts.Select(p => p.Created))
            .Select(created => (DateTimeOffset?)created)
            .Max();

    public IReadOnlyList<Currency> Currencies =>
        Transactions.Select(t => t.Currency)
            .Concat(Payouts.Select(p => p.Currency))
            .Distinct()
            .OrderBy(c => c.Code, StringComparer.Ordinal)
            .ToList();

    public static Ledger Create(IEnumerable<Charge> charges, IEnumerable<Refund> refunds, IEnumerable<Payout> payouts) =>
        new(
            charges.OrderBy(c => c.Created).ThenBy(c => c.Id, StringComparer.Ordinal).ToList(),
            refunds.OrderBy(r => r.Created).ThenBy(r => r.Id, StringComparer.Ordinal).ToList(),
            payouts.OrderBy(p => p.Created).ThenBy(p => p.Id, StringComparer.Ordinal).ToList());

    /// <summary>The ledger as it stood at <paramref name="asOf"/>: anything created later had not happened yet.</summary>
    public Ledger AsOf(DateTimeOffset asOf) =>
        new(
            Charges.Where(c => c.Created <= asOf).ToList(),
            Refunds.Where(r => r.Created <= asOf).ToList(),
            Payouts.Where(p => p.Created <= asOf).ToList());

    public Ledger InCurrency(Currency currency) =>
        new(
            Charges.Where(c => c.Currency == currency).ToList(),
            Refunds.Where(r => r.Currency == currency).ToList(),
            Payouts.Where(p => p.Currency == currency).ToList());
}
