using Payments.Domain.Records;
using Payments.Domain.Shared;

namespace Payments.Domain.Reporting;

/// <summary>
/// Fee rule: on a refund, Ticket Tailor's fee is returned in proportion to the amount refunded; Stripe's fee is not returned.
/// </summary>
public static class RefundFeeRule
{
    /// <summary>
    /// How much of Ticket Tailor's fee on <paramref name="charge"/> should have been returned once
    /// <paramref name="totalRefunded"/> of it has been refunded in total.
    /// Working from the running total (decision D-6) means partial refunds can never return more than the fee charged.
    /// Fractions of a minor unit round half away from zero.
    /// </summary>
    public static Money TicketTailorFeeReturnable(Charge charge, Money totalRefunded)
    {
        ArgumentNullException.ThrowIfNull(charge);

        if (totalRefunded.Currency != charge.Currency)
        {
            throw new InvalidOperationException($"Refund in {totalRefunded.Currency} cannot be applied to a {charge.Currency} charge.");
        }

        if (charge.Amount.MinorUnits == 0)
        {
            return Money.Zero(charge.Currency);
        }

        var fee = charge.FeesOf(FeeType.TicketTailorFee).MinorUnits;
        var exact = (decimal)fee * totalRefunded.MinorUnits / charge.Amount.MinorUnits;
        return new Money((long)Math.Round(exact, MidpointRounding.AwayFromZero), charge.Currency);
    }
}
