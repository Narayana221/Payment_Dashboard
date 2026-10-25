using Payments.Domain.DataQuality;
using Payments.Domain.Shared;
using static Payments.Tests.Shared.Builders.TestRecords;

namespace Payments.Domain.Tests.DataQuality;

public class RecordValidatorTests
{
    private static readonly DateOnly Arrival = new(2026, 6, 18);

    [Fact]
    public void Validate_ConsistentLedger_ReportsNothing()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_500, stripeFee: 41, ticketTailorFee: 60)],
            refunds: [Refund("re_1", "ch_1", 1_500, ticketTailorFeeReturned: 60)],
            payouts: [Payout("po_1", 1_399, Arrival, ["ch_1"])]);

        var result = RecordValidator.Validate(ledger);

        Assert.Empty(result.Issues);
        Assert.Single(result.Ledger.Charges);
        Assert.Single(result.Ledger.Refunds);
    }

    [Fact]
    public void Validate_NonSucceededCharge_IsExcluded()
    {
        var result = RecordValidator.Validate(Ledger(charges: [Charge("ch_1", 1_000, status: "failed")]));

        Assert.Empty(result.Ledger.Charges);
        AssertSingleIssue(result, DataQualityIssueType.NonSucceededCharge, excluded: true);
    }

    [Fact]
    public void Validate_ChargeWithNegativeAmount_IsExcluded()
    {
        var result = RecordValidator.Validate(Ledger(charges: [Charge("ch_1", -1_000)]));

        Assert.Empty(result.Ledger.Charges);
        AssertSingleIssue(result, DataQualityIssueType.InvalidAmount, excluded: true);
    }

    [Fact]
    public void Validate_NegativelySignedRefund_IsExcludedAndCannotEnableADoubleRefund()
    {
        // Without a sign check, -1000 would pass the over-refund test and leave room for a second full refund.
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000, ticketTailorFee: 40)],
            refunds:
            [
                Refund("re_bad", "ch_1", -1_000, ticketTailorFeeReturned: -40, net: -960, created: Saturday.AddHours(1)),
                Refund("re_ok", "ch_1", 1_000, ticketTailorFeeReturned: 40, created: Saturday.AddHours(2)),
            ]);

        var result = RecordValidator.Validate(ledger);

        Assert.Equal("re_ok", Assert.Single(result.Ledger.Refunds).Id);
        var issue = AssertSingleIssue(result, DataQualityIssueType.InvalidAmount, excluded: true);
        Assert.Contains("re_bad", issue.RecordIds);
    }

    [Fact]
    public void Validate_RefundDatedBeforeItsCharge_IsKeptWithWarning()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000, created: Saturday)],
            refunds: [Refund("re_1", "ch_1", 1_000, created: Saturday.AddHours(-1))]);

        var result = RecordValidator.Validate(ledger);

        Assert.Single(result.Ledger.Refunds);
        AssertSingleIssue(result, DataQualityIssueType.RefundBeforeCharge, excluded: false);
    }

    [Fact]
    public void Validate_RefundForUnknownCharge_IsExcludedAsOrphan()
    {
        var result = RecordValidator.Validate(Ledger(refunds: [Refund("re_1", "ch_missing", 1_000)]));

        Assert.Empty(result.Ledger.Refunds);
        AssertSingleIssue(result, DataQualityIssueType.OrphanRefund, excluded: true);
    }

    [Fact]
    public void Validate_RefundsExceedingCharge_ExcludesTheRefundThatCrossesTheLine()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000, ticketTailorFee: 40)],
            refunds:
            [
                Refund("re_1", "ch_1", 600, ticketTailorFeeReturned: 24, created: Saturday.AddHours(1)),
                Refund("re_2", "ch_1", 600, ticketTailorFeeReturned: 24, created: Saturday.AddHours(2)),
            ]);

        var result = RecordValidator.Validate(ledger);

        Assert.Equal("re_1", Assert.Single(result.Ledger.Refunds).Id);
        var issue = AssertSingleIssue(result, DataQualityIssueType.OverRefund, excluded: true);
        Assert.Contains("re_2", issue.RecordIds);
    }

    [Fact]
    public void Validate_RefundInDifferentCurrency_IsExcluded()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000)],
            refunds: [Refund("re_1", "ch_1", 1_000, currency: Currency.From("EUR"))]);

        var result = RecordValidator.Validate(ledger);

        Assert.Empty(result.Ledger.Refunds);
        AssertSingleIssue(result, DataQualityIssueType.RefundCurrencyMismatch, excluded: true);
    }

    [Fact]
    public void Validate_ChargeNetDisagreesWithFees_IsKeptWithWarning()
    {
        var result = RecordValidator.Validate(Ledger(charges: [Charge("ch_1", 1_000, stripeFee: 30, net: 900)]));

        Assert.Single(result.Ledger.Charges);
        AssertSingleIssue(result, DataQualityIssueType.NetMismatch, excluded: false);
    }

    [Fact]
    public void Validate_RefundReturnsWrongShareOfTicketTailorFee_IsKeptWithWarning()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 4_500, ticketTailorFee: 120)],
            refunds: [Refund("re_1", "ch_1", 2_250, ticketTailorFeeReturned: 120)]);

        var result = RecordValidator.Validate(ledger);

        Assert.Single(result.Ledger.Refunds);
        AssertSingleIssue(result, DataQualityIssueType.RefundFeeMismatch, excluded: false);
    }

    [Fact]
    public void Validate_PartialRefundsReturningFeeOnRunningTotal_ReportsNothing()
    {
        // 125p fee, two half refunds: 63 then 62 is exactly the proportional share of the running total (D-6).
        var ledger = Ledger(
            charges: [Charge("ch_1", 4_500, ticketTailorFee: 125)],
            refunds:
            [
                Refund("re_1", "ch_1", 2_250, ticketTailorFeeReturned: 63, created: Saturday.AddHours(1)),
                Refund("re_2", "ch_1", 2_250, ticketTailorFeeReturned: 62, created: Saturday.AddHours(2)),
            ]);

        Assert.Empty(RecordValidator.Validate(ledger).Issues);
    }

    [Fact]
    public void Validate_PartialRefundsReturningMoreThanTheFeeCharged_FlagsTheSecondRefund()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 4_500, ticketTailorFee: 125)],
            refunds:
            [
                Refund("re_1", "ch_1", 2_250, ticketTailorFeeReturned: 63, created: Saturday.AddHours(1)),
                Refund("re_2", "ch_1", 2_250, ticketTailorFeeReturned: 63, created: Saturday.AddHours(2)),
            ]);

        var issue = AssertSingleIssue(RecordValidator.Validate(ledger), DataQualityIssueType.RefundFeeMismatch, excluded: false);
        Assert.Contains("re_2", issue.RecordIds);
    }

    [Fact]
    public void Validate_ExcludedOverRefund_DoesNotAdvanceTheFeeRunningTotals()
    {
        // r2 is excluded, so r3 is judged against r1 alone: 600 + 400 refunded, 60 + 40 returned. No fee mismatch.
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000, ticketTailorFee: 100)],
            refunds:
            [
                Refund("re_1", "ch_1", 600, ticketTailorFeeReturned: 60, created: Saturday.AddHours(1)),
                Refund("re_2", "ch_1", 600, ticketTailorFeeReturned: 60, created: Saturday.AddHours(2)),
                Refund("re_3", "ch_1", 400, ticketTailorFeeReturned: 40, created: Saturday.AddHours(3)),
            ]);

        var result = RecordValidator.Validate(ledger);

        Assert.Equal(["re_1", "re_3"], result.Ledger.Refunds.Select(r => r.Id).ToArray());
        var issue = AssertSingleIssue(result, DataQualityIssueType.OverRefund, excluded: true);
        Assert.Contains("re_2", issue.RecordIds);
    }

    [Fact]
    public void Validate_PayoutCoveringTransactionCreatedAfterIt_IsReported()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000, created: Saturday.AddDays(5))],
            payouts: [Payout("po_1", 1_000, Arrival, ["ch_1"], created: Saturday.AddDays(4))]);

        AssertSingleIssue(RecordValidator.Validate(ledger), DataQualityIssueType.PayoutCoversLaterTransaction, excluded: false);
    }

    [Fact]
    public void Validate_RefundReturningStripeFee_IsKeptWithWarning()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_500, stripeFee: 41, ticketTailorFee: 60)],
            refunds: [Refund("re_1", "ch_1", 1_500, ticketTailorFeeReturned: 60, stripeFeeReturned: 41)]);

        var result = RecordValidator.Validate(ledger);

        Assert.Single(result.Ledger.Refunds);
        AssertSingleIssue(result, DataQualityIssueType.StripeFeeReturned, excluded: false);
    }

    [Fact]
    public void Validate_PayoutInDifferentCurrencyToItsTransactions_IsReported()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000)],
            payouts: [Payout("po_1", 1_000, Arrival, ["ch_1"], currency: Currency.From("EUR"))]);

        AssertSingleIssue(RecordValidator.Validate(ledger), DataQualityIssueType.PayoutCurrencyMismatch, excluded: false);
    }

    [Fact]
    public void Validate_PayoutCoveringUnknownTransaction_IsReported()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000)],
            payouts: [Payout("po_1", 1_000, Arrival, ["ch_1", "ch_ghost"])]);

        var issue = AssertSingleIssue(RecordValidator.Validate(ledger), DataQualityIssueType.UnknownPayoutTransaction, excluded: false);
        Assert.Contains("ch_ghost", issue.RecordIds);
    }

    [Fact]
    public void Validate_PayoutAmountNotEqualToCoveredNets_IsReported()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000, stripeFee: 30)],
            payouts: [Payout("po_1", 1_000, Arrival, ["ch_1"])]);

        AssertSingleIssue(RecordValidator.Validate(ledger), DataQualityIssueType.PayoutAmountMismatch, excluded: false);
    }

    [Fact]
    public void Validate_TransactionInTwoPayouts_IsReported()
    {
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000)],
            payouts:
            [
                Payout("po_1", 1_000, Arrival, ["ch_1"], created: Saturday.AddDays(2)),
                Payout("po_2", 1_000, Arrival, ["ch_1"], created: Saturday.AddDays(3)),
            ]);

        AssertSingleIssue(RecordValidator.Validate(ledger), DataQualityIssueType.TransactionInMultiplePayouts, excluded: false);
    }

    private static DataQualityIssue AssertSingleIssue(ValidationResult result, DataQualityIssueType type, bool excluded)
    {
        var issue = Assert.Single(result.Issues);
        Assert.Equal(type, issue.Type);
        Assert.Equal(excluded, issue.ExcludedFromTotals);
        return issue;
    }
}
