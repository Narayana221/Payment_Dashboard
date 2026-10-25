using Payments.Application.Features.Dashboard.Dtos;

namespace Payments.Tests.Shared;

/// <summary>
/// The D-5 identities, checked on the API contract rather than the domain, so a mapping mistake (e.g. two same-typed
/// fields swapped) shows up as a non-zero residual. Every residual must be 0.
/// </summary>
public static class ContractIdentities
{
    public static long WaterfallResidual(SalesBreakdownDto b)
    {
        ArgumentNullException.ThrowIfNull(b);
        return b.NetEarned.AmountMinor - (b.GrossSales.AmountMinor - b.StripeFees.AmountMinor - b.TicketTailorFees.AmountMinor
            - b.OtherFees.AmountMinor - b.RefundsIssued.AmountMinor + b.UnexplainedDifference.AmountMinor);
    }

    public static long TicketTailorFeeResidual(SalesBreakdownDto b)
    {
        ArgumentNullException.ThrowIfNull(b);
        return b.TicketTailorFees.AmountMinor - (b.TicketTailorFeesCharged.AmountMinor - b.TicketTailorFeesReturned.AmountMinor);
    }

    public static long PayoutResidual(CurrencySummaryDto s)
    {
        ArgumentNullException.ThrowIfNull(s);
        return s.AllTime.NetEarned.AmountMinor
            - (s.PaidOut.AmountMinor + s.InTransit.AmountMinor + s.StillToCome.AmountMinor + s.PayoutDiscrepancy.AmountMinor);
    }

    /// <summary>All residuals for a summary and each of its periods, labelled, for a single "all zero" assertion.</summary>
    public static IEnumerable<(string Check, long Residual)> All(CurrencySummaryDto s)
    {
        ArgumentNullException.ThrowIfNull(s);
        yield return ("payout position", PayoutResidual(s));
        foreach (var (label, breakdown) in s.Periods.Select(p => (p.Period, p.Breakdown)).Prepend(("allTime", s.AllTime)))
        {
            yield return ($"{label} waterfall", WaterfallResidual(breakdown));
            yield return ($"{label} ticket tailor fees", TicketTailorFeeResidual(breakdown));
        }
    }
}
