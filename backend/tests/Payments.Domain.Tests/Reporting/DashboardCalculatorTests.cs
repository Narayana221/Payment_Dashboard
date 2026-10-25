using Payments.Domain.DataQuality;
using Payments.Domain.Records;
using Payments.Domain.Reporting;
using Payments.Domain.Shared;
using static Payments.Tests.Shared.Builders.TestRecords;

namespace Payments.Domain.Tests.Reporting;

public class DashboardCalculatorTests
{
    private static readonly DateOnly June16 = new(2026, 6, 16);
    private static readonly DateOnly June18 = new(2026, 6, 18);
    private static readonly DateOnly June20 = new(2026, 6, 20);
    private static readonly DateTimeOffset June21 = new(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Calculate_RefundAfterPayout_CreatesNegativeBucketAndLeavesPaidOutUnchanged()
    {
        // A4: the charge was paid out, then refunded; the refund is clawed back from future funds.
        var ledger = Ledger(
            charges: [Charge("ch_1", 4_500, stripeFee: 83, ticketTailorFee: 120, availableOn: June16)],
            refunds: [Refund("re_1", "ch_1", 4_500, ticketTailorFeeReturned: 120, created: Saturday.AddDays(7), availableOn: June20)],
            payouts: [Payout("po_1", 4_297, June18, ["ch_1"])]);

        var summary = CalculateSingle(ledger, June21);

        Assert.Equal(Pence(4_297), summary.PaidOut);
        var bucket = Assert.Single(summary.StillToComeByDate);
        Assert.Equal(new AvailabilityBucket(June20, Pence(-4_380), IsAvailable: true), bucket);
        Invariants.AssertClean(summary);
    }

    [Fact]
    public void Calculate_PayoutNotYetArrived_IsInTransitNotPaidOut()
    {
        // A6: paid out means "in the bank".
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000)],
            payouts: [Payout("po_1", 1_000, June18, ["ch_1"])]);

        var summary = CalculateSingle(ledger, new DateTimeOffset(2026, 6, 17, 23, 59, 0, TimeSpan.Zero));

        Assert.Equal(Pence(0), summary.PaidOut);
        Assert.Equal(Pence(1_000), summary.InTransit);
        Assert.False(Assert.Single(summary.Payouts).HasArrived);
        Invariants.AssertClean(summary);
    }

    [Fact]
    public void Calculate_PayoutArrivingOnAsOfDate_CountsAsPaidOut()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000)],
            payouts: [Payout("po_1", 1_000, June18, ["ch_1"])]);

        var summary = CalculateSingle(ledger, new DateTimeOffset(2026, 6, 18, 0, 0, 0, TimeSpan.Zero));

        Assert.Equal(Pence(1_000), summary.PaidOut);
        Invariants.AssertClean(summary);
    }

    [Fact]
    public void Calculate_AvailableButNotPaidOut_IsStillToComeAndMarkedAvailable()
    {
        // A5: past its available_on date but not in any payout.
        var ledger = Ledger(charges: [Charge("ch_1", 1_000, availableOn: June16)]);

        var summary = CalculateSingle(ledger, June21);

        Assert.Equal(new AvailabilityBucket(June16, Pence(1_000), IsAvailable: true), Assert.Single(summary.StillToComeByDate));
        Invariants.AssertClean(summary);
    }

    [Fact]
    public void Calculate_FundsNotYetAvailable_AreMarkedNotAvailable()
    {
        var ledger = Ledger(charges: [Charge("ch_1", 1_000, availableOn: June20)]);

        var summary = CalculateSingle(ledger, new DateTimeOffset(2026, 6, 19, 0, 0, 0, TimeSpan.Zero));

        Assert.False(Assert.Single(summary.StillToComeByDate).IsAvailable);
        Invariants.AssertClean(summary);
    }

    [Fact]
    public void Calculate_FailedPayout_DoesNotCountAndItsFundsRemainStillToCome()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000)],
            payouts: [Payout("po_1", 1_000, June18, ["ch_1"], status: PayoutStatus.Failed)]);

        var summary = CalculateSingle(ledger, June21);

        Assert.Equal(Pence(0), summary.PaidOut);
        Assert.Equal(Pence(1_000), summary.StillToCome);
        Assert.Empty(summary.Payouts);
        Invariants.AssertClean(summary);
    }

    [Fact]
    public void Calculate_RecordsCreatedAfterAsOf_AreIgnored()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000, created: Saturday), Charge("ch_2", 2_000, created: Saturday.AddDays(5))]);

        var summary = CalculateSingle(ledger, Saturday.AddDays(1));

        Assert.Equal(Pence(1_000), summary.AllTime.GrossSales);
        Invariants.AssertClean(summary);
    }

    [Fact]
    public void Calculate_Periods_StartIsExclusiveAndEndIsInclusive()
    {
        var asOf = Saturday.AddDays(7);
        var ledger = Ledger(charges:
        [
            Charge("ch_on_week_start", 100, created: asOf.AddDays(-7)),
            Charge("ch_inside_week", 200, created: asOf.AddDays(-6)),
            Charge("ch_at_as_of", 400, created: asOf),
        ]);

        var summary = CalculateSingle(ledger, asOf);

        Assert.Equal(Pence(600), PeriodOf(summary, ReportingPeriod.Week).GrossSales);
        Assert.Equal(Pence(400), PeriodOf(summary, ReportingPeriod.Day).GrossSales);
        Assert.Equal(Pence(700), PeriodOf(summary, ReportingPeriod.Month).GrossSales);
        Invariants.AssertClean(summary);
    }

    [Fact]
    public void Calculate_RefundIsAttributedToThePeriodItWasIssuedIn()
    {
        // A8: a refund issued today reduces today's figures, not the day of the original sale.
        var asOf = Saturday.AddDays(10);
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000, created: Saturday)],
            refunds: [Refund("re_1", "ch_1", 1_000, created: asOf.AddHours(-1))]);

        var summary = CalculateSingle(ledger, asOf);
        var lastDay = PeriodOf(summary, ReportingPeriod.Day);

        Assert.Equal(Pence(0), lastDay.GrossSales);
        Assert.Equal(Pence(1_000), lastDay.RefundsIssued);
        Assert.Equal(Pence(-1_000), lastDay.NetEarned);
        Invariants.AssertClean(summary);
    }

    [Fact]
    public void Calculate_MultipleCurrencies_ProducesOneSummaryEachAndNeverMixes()
    {
        var eur = Currency.From("EUR");
        var ledger = Ledger(charges: [Charge("ch_gbp", 1_000), Charge("ch_eur", 2_000, currency: eur)]);

        var summaries = DashboardCalculator.Calculate(ledger, June21);

        Assert.Collection(
            summaries,
            s => Assert.Equal(new Money(2_000, eur), s.AllTime.GrossSales),
            s => Assert.Equal(Pence(1_000), s.AllTime.GrossSales));
        Assert.All(summaries, Invariants.AssertClean);
    }

    [Fact]
    public void Calculate_EmptyLedger_ReturnsNoSummaries() => Assert.Empty(DashboardCalculator.Calculate(Ledger(), June21));

    // Records kept with a warning (D-5): the figures still add up, and the gap is visible on its own line.

    [Fact]
    public void Calculate_UnknownFeeType_IsShownAsOtherFeesAndTheWaterfallBalances()
    {
        var summary = ValidateAndCalculate(Ledger(charges: [Charge("ch_1", 1_000, stripeFee: 30, ticketTailorFee: 50, otherFee: 10)]));

        Assert.Equal(Pence(10), summary.AllTime.OtherFees);
        Assert.Equal(Pence(0), summary.AllTime.UnexplainedDifference);
        Invariants.AssertBalances(summary);
    }

    [Fact]
    public void Calculate_ChargeNetDisagreesWithFees_ShowsUnexplainedDifference()
    {
        var summary = ValidateAndCalculate(Ledger(charges: [Charge("ch_1", 1_000, stripeFee: 30, net: 900)]));

        Assert.Equal(Pence(-70), summary.AllTime.UnexplainedDifference);
        Invariants.AssertBalances(summary);
    }

    [Fact]
    public void Calculate_StripeFeeReturnedOnRefund_ReducesStripeFeesAndTheWaterfallBalances()
    {
        var summary = ValidateAndCalculate(Ledger(
            charges: [Charge("ch_1", 1_500, stripeFee: 41, ticketTailorFee: 60)],
            refunds: [Refund("re_1", "ch_1", 1_500, ticketTailorFeeReturned: 60, stripeFeeReturned: 41)]));

        Assert.Equal(Pence(0), summary.AllTime.StripeFees);
        Assert.Equal(Pence(0), summary.AllTime.UnexplainedDifference);
        Invariants.AssertBalances(summary);
    }

    [Fact]
    public void Calculate_PayoutAmountNotMatchingItsTransactions_ShowsPayoutDiscrepancy()
    {
        var summary = ValidateAndCalculate(Ledger(
            charges: [Charge("ch_1", 1_000, stripeFee: 30)],
            payouts: [Payout("po_1", 1_000, June18, ["ch_1"])]));

        Assert.Equal(Pence(-30), summary.PayoutDiscrepancy);
        Invariants.AssertBalances(summary);
    }

    [Fact]
    public void Calculate_TransactionInTwoPayouts_ShowsPayoutDiscrepancy()
    {
        var summary = ValidateAndCalculate(Ledger(
            charges: [Charge("ch_1", 1_000)],
            payouts:
            [
                Payout("po_1", 1_000, June18, ["ch_1"], created: Saturday.AddDays(2)),
                Payout("po_2", 1_000, June18, ["ch_1"], created: Saturday.AddDays(3)),
            ]));

        Assert.Equal(Pence(2_000), summary.PaidOut);
        Assert.Equal(Pence(-1_000), summary.PayoutDiscrepancy);
        Invariants.AssertBalances(summary);
    }

    [Fact]
    public void Calculate_PayoutCoveringAnExcludedRefund_ShowsPayoutDiscrepancy()
    {
        // The orphan refund is excluded, but the payout that (supposedly) netted it off still landed in the bank.
        var summary = ValidateAndCalculate(Ledger(
            charges: [Charge("ch_1", 1_000)],
            refunds: [Refund("re_orphan", "ch_missing", 400)],
            payouts: [Payout("po_1", 600, June18, ["ch_1", "re_orphan"])]));

        Assert.Equal(Pence(600), summary.PaidOut);
        Assert.Equal(Pence(400), summary.PayoutDiscrepancy);
        Invariants.AssertBalances(summary);
    }

    [Fact]
    public void Calculate_TwoGapsOnTheSameSummary_AreEachAccountedFor()
    {
        // Two independent problems: a charge net mismatch (−70) and an unknown fee (10, which balances by itself).
        // AssertExplained checks the unexplained difference is exactly the sum of per-record gaps, not just "non-zero".
        var summary = ValidateAndCalculate(Ledger(charges:
        [
            Charge("ch_1", 1_000, stripeFee: 30, net: 900),
            Charge("ch_2", 1_000, stripeFee: 30, ticketTailorFee: 50, otherFee: 10),
        ]));

        Assert.Equal(Pence(10), summary.AllTime.OtherFees);
        Assert.Equal(Pence(-70), summary.AllTime.UnexplainedDifference);
        Invariants.AssertBalances(summary);
    }

    private static DashboardSummary CalculateSingle(Ledger ledger, DateTimeOffset asOf) =>
        Assert.Single(DashboardCalculator.Calculate(ledger, asOf));

    private static DashboardSummary ValidateAndCalculate(Ledger ledger)
    {
        var validated = RecordValidator.Validate(ledger);
        var summary = CalculateSingle(validated.Ledger, June21);
        Invariants.AssertExplained(summary, validated);
        return summary;
    }

    private static SalesBreakdown PeriodOf(DashboardSummary summary, ReportingPeriod period) =>
        summary.Periods.Single(p => p.Period == period).Breakdown;
}
