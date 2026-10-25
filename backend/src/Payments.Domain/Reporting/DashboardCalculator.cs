using Payments.Domain.Records;
using Payments.Domain.Shared;

namespace Payments.Domain.Reporting;

/// <summary>
/// Builds the dashboard figures from a validated ledger. Pure: the caller supplies <c>asOf</c>, so results are
/// deterministic and "last week" means the same thing in a test as in production (decision A7).
/// </summary>
public static class DashboardCalculator
{
    /// <returns>One summary per currency, ordered by currency code. Amounts are never summed across currencies (A11).</returns>
    public static IReadOnlyList<DashboardSummary> Calculate(Ledger ledger, DateTimeOffset asOf)
    {
        ArgumentNullException.ThrowIfNull(ledger);

        var asAt = ledger.AsOf(asOf);
        return asAt.Currencies.Select(currency => CalculateFor(asAt.InCurrency(currency), currency, asOf)).ToList();
    }

    private static DashboardSummary CalculateFor(Ledger ledger, Currency currency, DateTimeOffset asOf)
    {
        var asOfDate = DateOnly.FromDateTime(asOf.UtcDateTime);

        var payouts = ledger.Payouts.Where(p => p.MovesMoney).ToList();
        var arrived = payouts.Where(p => p.HasArrivedBy(asOfDate)).ToList();
        var inTransit = payouts.Except(arrived).ToList();

        var coveredByPayout = payouts.SelectMany(p => p.IncludedTransactionIds).ToHashSet(StringComparer.Ordinal);
        var stillToComeByDate = ledger.Transactions
            .Where(t => !coveredByPayout.Contains(t.Id))
            .GroupBy(t => t.AvailableOn)
            .OrderBy(g => g.Key)
            .Select(g => new AvailabilityBucket(g.Key, Money.Sum(g.Select(t => t.Net), currency), IsAvailable: g.Key <= asOfDate))
            .ToList();

        return new DashboardSummary
        {
            Currency = currency,
            AsOf = asOf,
            AllTime = SalesBreakdown.From(ledger.Charges, ledger.Refunds, currency),
            Periods = Enum.GetValues<ReportingPeriod>().Select(period => BreakdownFor(ledger, period, asOf, currency)).ToList(),
            PaidOut = Money.Sum(arrived.Select(p => p.Amount), currency),
            InTransit = Money.Sum(inTransit.Select(p => p.Amount), currency),
            StillToCome = Money.Sum(stillToComeByDate.Select(b => b.Amount), currency),
            StillToComeByDate = stillToComeByDate,
            Payouts = payouts
                .Select(p => new PayoutLine(p.Id, p.Amount, p.ArrivalDate, HasArrived: p.HasArrivedBy(asOfDate)))
                .ToList(),
        };
    }

    // Records are attributed to the period they were created in, so a later refund never rewrites an earlier period (A8).
    private static PeriodBreakdown BreakdownFor(Ledger ledger, ReportingPeriod period, DateTimeOffset asOf, Currency currency)
    {
        var window = ReportingWindow.For(period, asOf);
        var breakdown = SalesBreakdown.From(
            ledger.Charges.Where(c => window.Contains(c.Created)).ToList(),
            ledger.Refunds.Where(r => window.Contains(r.Created)).ToList(),
            currency);
        return new PeriodBreakdown(period, window, breakdown);
    }
}
