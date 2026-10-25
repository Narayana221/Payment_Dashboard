using Payments.Domain.Records;
using Payments.Domain.Reporting;
using Payments.Domain.Shared;

namespace Payments.Domain.DataQuality;

public sealed record ValidationResult(Ledger Ledger, IReadOnlyList<DataQualityIssue> Issues);

/// <summary>
/// Checks referential integrity and the fee rules (decision A10). Records that cannot be counted safely are excluded
/// and reported; records that can be counted but look wrong are kept (the record is what actually moved money) and reported.
/// Nothing is dropped silently.
/// </summary>
public static class RecordValidator
{
    public static ValidationResult Validate(Ledger ledger)
    {
        ArgumentNullException.ThrowIfNull(ledger);
        var issues = new List<DataQualityIssue>();

        var charges = ValidateCharges(ledger.Charges, issues);
        var refunds = ValidateRefunds(ledger.Refunds, charges, issues);
        ValidatePayouts(ledger.Payouts, charges.Values.Concat<BalanceTransaction>(refunds), issues);

        return new ValidationResult(Ledger.Create(charges.Values, refunds, ledger.Payouts), issues);
    }

    private static Dictionary<string, Charge> ValidateCharges(IReadOnlyList<Charge> charges, List<DataQualityIssue> issues)
    {
        var valid = new Dictionary<string, Charge>(StringComparer.Ordinal);

        foreach (var charge in charges)
        {
            if (!charge.Succeeded)
            {
                issues.Add(Excluded(DataQualityIssueType.NonSucceededCharge,
                    $"Charge {charge.Id} has status '{charge.Status}', so it is not counted as a sale.", charge.Id));
                continue;
            }

            if (charge.Amount.MinorUnits < 0
                || charge.FeesOf(FeeType.StripeFee).MinorUnits < 0
                || charge.FeesOf(FeeType.TicketTailorFee).MinorUnits < 0)
            {
                issues.Add(Excluded(DataQualityIssueType.InvalidAmount,
                    $"Charge {charge.Id} has a negative amount or fee, which a sale can't have.", charge.Id));
                continue;
            }

            var expectedNet = charge.Amount - charge.TotalFees;
            if (charge.Net != expectedNet)
            {
                issues.Add(Warning(DataQualityIssueType.NetMismatch,
                    $"Charge {charge.Id} net is {charge.Net.MinorUnits} but amount minus fees is {expectedNet.MinorUnits}.", charge.Id));
            }

            valid[charge.Id] = charge;
        }

        return valid;
    }

    private static List<Refund> ValidateRefunds(
        IReadOnlyList<Refund> refunds, Dictionary<string, Charge> charges, List<DataQualityIssue> issues)
    {
        var valid = new List<Refund>();
        var refundedSoFar = new Dictionary<string, Money>(StringComparer.Ordinal);
        var feeReturnedSoFar = new Dictionary<string, Money>(StringComparer.Ordinal);

        // Refunds arrive in chronological order, so an over-refund excludes the refund that crossed the line.
        foreach (var refund in refunds)
        {
            if (!charges.TryGetValue(refund.ChargeId, out var charge))
            {
                issues.Add(Excluded(DataQualityIssueType.OrphanRefund,
                    $"Refund {refund.Id} points at charge {refund.ChargeId}, which is missing, not yet created at this point, or was not a successful sale.",
                    refund.Id));
                continue;
            }

            if (refund.Currency != charge.Currency)
            {
                issues.Add(Excluded(DataQualityIssueType.RefundCurrencyMismatch,
                    $"Refund {refund.Id} is in {refund.Currency} but charge {charge.Id} is in {charge.Currency}.", refund.Id, charge.Id));
                continue;
            }

            // Signs follow the file: a refund gives money back (positive amount, net ≤ 0) and can only return fees, never take more.
            // Without this a negatively-signed refund would slip past the over-refund check below.
            if (refund.Amount.MinorUnits <= 0 || refund.Net.MinorUnits > 0 || refund.FeesOf(FeeType.TicketTailorFee).MinorUnits > 0)
            {
                issues.Add(Excluded(DataQualityIssueType.InvalidAmount,
                    $"Refund {refund.Id} has an amount, net or fee with the wrong sign for a refund.", refund.Id));
                continue;
            }

            var alreadyRefunded = refundedSoFar.GetValueOrDefault(charge.Id, Money.Zero(charge.Currency));
            if ((alreadyRefunded + refund.Amount).MinorUnits > charge.Amount.MinorUnits)
            {
                issues.Add(Excluded(DataQualityIssueType.OverRefund,
                    $"Refund {refund.Id} would take refunds on charge {charge.Id} above the {charge.Amount.MinorUnits} originally paid.",
                    refund.Id, charge.Id));
                continue;
            }

            if (refund.Created < charge.Created)
            {
                issues.Add(Warning(DataQualityIssueType.RefundBeforeCharge,
                    $"Refund {refund.Id} is dated before charge {charge.Id}; in views as of a time between the two, the refund is left out until its charge exists.",
                    refund.Id, charge.Id));
            }

            var alreadyReturned = feeReturnedSoFar.GetValueOrDefault(charge.Id, Money.Zero(charge.Currency));
            var returned = -refund.FeesOf(FeeType.TicketTailorFee);
            CheckRefundFees(refund, charge, alreadyRefunded, alreadyReturned, issues);

            refundedSoFar[charge.Id] = alreadyRefunded + refund.Amount;
            feeReturnedSoFar[charge.Id] = alreadyReturned + returned;
            valid.Add(refund);
        }

        return valid;
    }

    private static void CheckRefundFees(
        Refund refund, Charge charge, Money alreadyRefunded, Money alreadyReturned, List<DataQualityIssue> issues)
    {
        var expectedNet = -refund.Amount - refund.TotalFees;
        if (refund.Net != expectedNet)
        {
            issues.Add(Warning(DataQualityIssueType.NetMismatch,
                $"Refund {refund.Id} net is {refund.Net.MinorUnits} but amount and fees imply {expectedNet.MinorUnits}.", refund.Id));
        }

        // Compare against the running total (D-6): what should have come back by now, less what already has.
        var returned = -refund.FeesOf(FeeType.TicketTailorFee);
        var expectedReturn = RefundFeeRule.TicketTailorFeeReturnable(charge, alreadyRefunded + refund.Amount) - alreadyReturned;
        if (returned != expectedReturn)
        {
            issues.Add(Warning(DataQualityIssueType.RefundFeeMismatch,
                $"Refund {refund.Id} returns {returned.MinorUnits} of Ticket Tailor's fee; the proportional rule gives {expectedReturn.MinorUnits}.",
                refund.Id, charge.Id));
        }

        if (refund.FeesOf(FeeType.StripeFee).MinorUnits != 0)
        {
            issues.Add(Warning(DataQualityIssueType.StripeFeeReturned,
                $"Refund {refund.Id} adjusts Stripe's fee, which should not be returned on a refund.", refund.Id));
        }
    }

    private static void ValidatePayouts(
        IReadOnlyList<Payout> payouts, IEnumerable<BalanceTransaction> transactions, List<DataQualityIssue> issues)
    {
        var byId = transactions.ToDictionary(t => t.Id, StringComparer.Ordinal);
        var paidBy = new Dictionary<string, string>(StringComparer.Ordinal);

        foreach (var payout in payouts.Where(p => p.MovesMoney))
        {
            var unknown = payout.IncludedTransactionIds.Where(id => !byId.ContainsKey(id)).ToList();
            if (unknown.Count > 0)
            {
                issues.Add(Warning(DataQualityIssueType.UnknownPayoutTransaction,
                    $"Payout {payout.Id} covers {unknown.Count} transaction(s) that are missing, excluded, or not yet created at this point: {string.Join(", ", unknown)}.",
                    [payout.Id, .. unknown]));
            }

            var included = payout.IncludedTransactionIds.Where(byId.ContainsKey).Select(id => byId[id]).ToList();

            var otherCurrency = included.Where(t => t.Currency != payout.Currency).Select(t => t.Id).ToList();
            if (otherCurrency.Count > 0)
            {
                issues.Add(Warning(DataQualityIssueType.PayoutCurrencyMismatch,
                    $"Payout {payout.Id} is in {payout.Currency} but covers transactions in another currency.", [payout.Id, .. otherCurrency]));
            }

            var includedNet = Money.Sum(included.Where(t => t.Currency == payout.Currency).Select(t => t.Net), payout.Currency);
            if (unknown.Count == 0 && otherCurrency.Count == 0 && includedNet != payout.Amount)
            {
                issues.Add(Warning(DataQualityIssueType.PayoutAmountMismatch,
                    $"Payout {payout.Id} is {payout.Amount.MinorUnits} but the transactions it covers net to {includedNet.MinorUnits}.", payout.Id));
            }

            var later = included.Where(t => t.Created > payout.Created).Select(t => t.Id).ToList();
            if (later.Count > 0)
            {
                // Viewed as of a time between the two, the payout would be counted before the money it carries existed.
                issues.Add(Warning(DataQualityIssueType.PayoutCoversLaterTransaction,
                    $"Payout {payout.Id} covers transaction(s) created after the payout itself: {string.Join(", ", later)}.",
                    [payout.Id, .. later]));
            }

            foreach (var transaction in included)
            {
                if (paidBy.TryGetValue(transaction.Id, out var earlierPayout))
                {
                    issues.Add(Warning(DataQualityIssueType.TransactionInMultiplePayouts,
                        $"Transaction {transaction.Id} appears in both payout {earlierPayout} and payout {payout.Id}.",
                        transaction.Id, earlierPayout, payout.Id));
                }
                else
                {
                    paidBy[transaction.Id] = payout.Id;
                }
            }
        }
    }

    private static DataQualityIssue Excluded(DataQualityIssueType type, string message, params string[] ids) =>
        new(type, message, ids, ExcludedFromTotals: true);

    private static DataQualityIssue Warning(DataQualityIssueType type, string message, params string[] ids) =>
        new(type, message, ids, ExcludedFromTotals: false);
}
