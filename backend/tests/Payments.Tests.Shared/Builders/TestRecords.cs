using Payments.Domain.Records;
using Payments.Domain.Shared;

namespace Payments.Tests.Shared.Builders;

/// <summary>
/// Small, consistent records for edge-case tests. Nets are derived from amount and fees the same way the
/// real data does, so a fixture only differs from "valid" where a test says so.
/// </summary>
public static class TestRecords
{
    public static readonly Currency Gbp = Currency.From("GBP");

    public static readonly DateTimeOffset Saturday = new(2026, 6, 13, 12, 0, 0, TimeSpan.Zero);

    public static Money Pence(long amount, Currency? currency = null) => new(amount, currency ?? Gbp);

    public static Charge Charge(
        string id,
        long amount,
        long stripeFee = 0,
        long ticketTailorFee = 0,
        DateTimeOffset? created = null,
        DateOnly? availableOn = null,
        string status = Domain.Records.Charge.SucceededStatus,
        long? net = null,
        Currency? currency = null,
        long otherFee = 0)
    {
        var c = currency ?? Gbp;
        List<Fee> fees = [new Fee(FeeType.StripeFee, Pence(stripeFee, c)), new Fee(FeeType.TicketTailorFee, Pence(ticketTailorFee, c))];
        if (otherFee != 0)
        {
            fees.Add(new Fee(FeeType.Other, Pence(otherFee, c)));
        }

        return new Charge
        {
            Id = id,
            Created = created ?? Saturday,
            Amount = Pence(amount, c),
            Net = Pence(net ?? amount - stripeFee - ticketTailorFee - otherFee, c),
            AvailableOn = availableOn ?? new DateOnly(2026, 6, 16),
            Fees = fees,
            Status = status,
        };
    }

    /// <param name="ticketTailorFeeReturned">Positive amount of Ticket Tailor's fee given back (recorded as a negative fee).</param>
    /// <param name="stripeFeeReturned">Positive amount of Stripe's fee given back; the rules say this should never happen.</param>
    public static Refund Refund(
        string id,
        string chargeId,
        long amount,
        long ticketTailorFeeReturned = 0,
        DateTimeOffset? created = null,
        DateOnly? availableOn = null,
        long? net = null,
        Currency? currency = null,
        long stripeFeeReturned = 0)
    {
        var c = currency ?? Gbp;
        List<Fee> fees = [new Fee(FeeType.TicketTailorFee, Pence(-ticketTailorFeeReturned, c))];
        if (stripeFeeReturned != 0)
        {
            fees.Add(new Fee(FeeType.StripeFee, Pence(-stripeFeeReturned, c)));
        }

        return new Refund
        {
            Id = id,
            ChargeId = chargeId,
            Created = created ?? Saturday.AddDays(1),
            Amount = Pence(amount, c),
            Net = Pence(net ?? -amount + ticketTailorFeeReturned + stripeFeeReturned, c),
            AvailableOn = availableOn ?? new DateOnly(2026, 6, 17),
            Fees = fees,
        };
    }

    public static Payout Payout(
        string id,
        long amount,
        DateOnly arrivalDate,
        IReadOnlyList<string> includes,
        DateTimeOffset? created = null,
        PayoutStatus status = PayoutStatus.Paid,
        Currency? currency = null) => new()
    {
        Id = id,
        Created = created ?? Saturday.AddDays(3),
        Amount = Pence(amount, currency),
        Status = status,
        ArrivalDate = arrivalDate,
        IncludedTransactionIds = includes,
    };

    public static Ledger Ledger(
        IEnumerable<Charge>? charges = null, IEnumerable<Refund>? refunds = null, IEnumerable<Payout>? payouts = null) =>
        Domain.Records.Ledger.Create(charges ?? [], refunds ?? [], payouts ?? []);
}
