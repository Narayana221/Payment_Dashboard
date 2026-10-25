using Payments.Domain.DataQuality;
using Payments.Domain.Reporting;
using Payments.Domain.Shared;

namespace Payments.Domain.Tests;

/// <summary>The identities every summary must satisfy (.claude/rules/money-and-domain.md, decision D-5).</summary>
internal static class Invariants
{
    /// <summary>
    /// Holds for every summary, clean or not: any gap must show up on an explicit line
    /// (other fees, unexplained difference, payout discrepancy) instead of the figures silently not adding up.
    /// </summary>
    public static void AssertBalances(DashboardSummary summary)
    {
        AssertWaterfallBalances(summary.AllTime);
        foreach (var period in summary.Periods)
        {
            AssertWaterfallBalances(period.Breakdown);
        }

        Assert.Equal(summary.AllTime.NetEarned, summary.PaidOut + summary.InTransit + summary.StillToCome + summary.PayoutDiscrepancy);
    }

    /// <summary>
    /// The reconciliation lines are computed as remainders, so <see cref="AssertBalances"/> alone can't catch a bug that adds to
    /// an already non-zero gap. This recomputes the gap independently, record by record, and requires a payout discrepancy to
    /// come with a reported payout issue.
    /// </summary>
    public static void AssertExplained(DashboardSummary summary, ValidationResult validated)
    {
        var ledger = validated.Ledger.AsOf(summary.AsOf).InCurrency(summary.Currency);
        var perRecordGaps =
            ledger.Charges.Select(c => c.Net - (c.Amount - c.TotalFees))
                .Concat(ledger.Refunds.Select(r => r.Net - (-r.Amount - r.TotalFees)));
        Assert.Equal(Money.Sum(perRecordGaps, summary.Currency), summary.AllTime.UnexplainedDifference);

        DataQualityIssueType[] payoutIssues =
        [
            DataQualityIssueType.UnknownPayoutTransaction,
            DataQualityIssueType.PayoutCurrencyMismatch,
            DataQualityIssueType.PayoutAmountMismatch,
            DataQualityIssueType.TransactionInMultiplePayouts,
            DataQualityIssueType.PayoutCoversLaterTransaction,
        ];
        if (!validated.Issues.Any(i => payoutIssues.Contains(i.Type)))
        {
            Assert.Equal(0, summary.PayoutDiscrepancy.MinorUnits);
        }
    }

    /// <summary>Clean data: the reconciliation lines are zero, so the two plain identities hold exactly.</summary>
    public static void AssertClean(DashboardSummary summary)
    {
        AssertBalances(summary);

        var b = summary.AllTime;
        Assert.Equal(0, b.OtherFees.MinorUnits);
        Assert.Equal(0, b.UnexplainedDifference.MinorUnits);
        Assert.Equal(0, summary.PayoutDiscrepancy.MinorUnits);
        Assert.Equal(b.NetEarned, b.GrossSales - b.StripeFees - b.TicketTailorFees - b.RefundsIssued);
        Assert.Equal(b.NetEarned, summary.PaidOut + summary.InTransit + summary.StillToCome);
    }

    private static void AssertWaterfallBalances(SalesBreakdown b) =>
        Assert.Equal(
            b.NetEarned,
            b.GrossSales - b.StripeFees - b.TicketTailorFees - b.OtherFees - b.RefundsIssued + b.UnexplainedDifference);
}
