using Payments.Domain.Records;
using Payments.Domain.Shared;

namespace Payments.Domain.Reporting;

/// <summary>
/// The waterfall from what buyers paid to what the organiser keeps:
/// <c>GrossSales − StripeFees − TicketTailorFees − OtherFees − RefundsIssued + UnexplainedDifference = NetEarned</c>.
/// Figures come from the records' own amounts and fee details (decision A3: the record is the source of truth).
/// On clean data <see cref="OtherFees"/> and <see cref="UnexplainedDifference"/> are zero; when they aren't, the gap is
/// shown rather than hidden, so the waterfall always adds up (decision D-5).
/// </summary>
public sealed record SalesBreakdown(
    Money GrossSales,
    Money StripeFees,
    Money TicketTailorFeesCharged,
    Money TicketTailorFeesReturned,
    Money OtherFees,
    Money RefundsIssued,
    Money NetEarned,
    int SalesCount,
    int RefundCount)
{
    /// <summary>Ticket Tailor's fee after the share returned on refunds.</summary>
    public Money TicketTailorFees => TicketTailorFeesCharged - TicketTailorFeesReturned;

    /// <summary>Net earned that the amounts and fees on the records don't account for (records whose net disagrees with them).</summary>
    public Money UnexplainedDifference => NetEarned - (GrossSales - StripeFees - TicketTailorFees - OtherFees - RefundsIssued);

    public static SalesBreakdown From(IReadOnlyCollection<Charge> charges, IReadOnlyCollection<Refund> refunds, Currency currency)
    {
        ArgumentNullException.ThrowIfNull(charges);
        ArgumentNullException.ThrowIfNull(refunds);

        var all = charges.Concat<BalanceTransaction>(refunds).ToList();

        return new SalesBreakdown(
            GrossSales: Money.Sum(charges.Select(c => c.Amount), currency),
            // Includes any Stripe fee adjustment recorded on a refund, so such a record can't unbalance the waterfall.
            StripeFees: Money.Sum(all.Select(t => t.FeesOf(FeeType.StripeFee)), currency),
            TicketTailorFeesCharged: Money.Sum(charges.Select(c => c.FeesOf(FeeType.TicketTailorFee)), currency),
            // Returned fees are recorded as negative fees on the refund; report them as a positive amount given back.
            TicketTailorFeesReturned: -Money.Sum(refunds.Select(r => r.FeesOf(FeeType.TicketTailorFee)), currency),
            OtherFees: Money.Sum(all.Select(t => t.FeesOf(FeeType.Other)), currency),
            RefundsIssued: Money.Sum(refunds.Select(r => r.Amount), currency),
            NetEarned: Money.Sum(all.Select(t => t.Net), currency),
            SalesCount: charges.Count,
            RefundCount: refunds.Count);
    }
}
