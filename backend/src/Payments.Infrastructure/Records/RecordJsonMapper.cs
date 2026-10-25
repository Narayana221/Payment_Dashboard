using System.Globalization;
using System.Text.RegularExpressions;
using Payments.Domain.DataQuality;
using Payments.Domain.Records;
using Payments.Domain.Shared;

namespace Payments.Infrastructure.Records;

/// <summary>Turns raw records into domain types. A missing or invalid required field throws <see cref="MalformedRecordException"/>.</summary>
internal static partial class RecordJsonMapper
{
    /// <summary>£10bn in pence. Anything larger is not a ticket sale, and summing such values could overflow.</summary>
    private const long MaxAbsoluteMinorUnits = 1_000_000_000_000;

    public static Charge ToCharge(RecordJson raw, ICollection<DataQualityIssue> issues)
    {
        // Fee warnings are only published once the whole record has mapped, so a malformed record never also
        // claims its fee is "shown under other fees".
        var feeIssues = new List<DataQualityIssue>();
        var currency = CurrencyOf(raw);
        var charge = new Charge
        {
            Id = Require(raw.Id, "id"),
            Created = InstantOf(raw.Created),
            Amount = AmountOf(raw.Amount, "amount", currency),
            Net = AmountOf(raw.Net, "net", currency),
            AvailableOn = Require(raw.AvailableOn, "available_on"),
            Fees = FeesOf(raw, currency, feeIssues),
            Status = Require(raw.Status, "status"),
            OrderRef = raw.OrderRef,
        };
        feeIssues.ForEach(issues.Add);
        return charge;
    }

    public static Refund ToRefund(RecordJson raw, ICollection<DataQualityIssue> issues)
    {
        var feeIssues = new List<DataQualityIssue>();
        var currency = CurrencyOf(raw);
        var refund = new Refund
        {
            Id = Require(raw.Id, "id"),
            Created = InstantOf(raw.Created),
            Amount = AmountOf(raw.Amount, "amount", currency),
            Net = AmountOf(raw.Net, "net", currency),
            AvailableOn = Require(raw.AvailableOn, "available_on"),
            Fees = FeesOf(raw, currency, feeIssues),
            ChargeId = Require(raw.Charge, "charge"),
        };
        feeIssues.ForEach(issues.Add);
        return refund;
    }

    public static Payout ToPayout(RecordJson raw) => new()
    {
        Id = Require(raw.Id, "id"),
        Created = InstantOf(raw.Created),
        Amount = AmountOf(raw.Amount, "amount", CurrencyOf(raw)),
        Status = PayoutStatusOf(Require(raw.Status, "status")),
        ArrivalDate = Require(raw.ArrivalDate, "arrival_date"),
        IncludedTransactionIds = Require(raw.IncludedBalanceTransactions, "included_balance_transactions")
            .Select(id => Require(id, "included_balance_transactions[]"))
            .ToList(),
    };

    private static List<Fee> FeesOf(RecordJson raw, Currency currency, List<DataQualityIssue> issues)
    {
        var fees = new List<Fee>();
        foreach (var feeJson in raw.FeeDetails ?? [])
        {
            var fee = Require(feeJson, "fee_details[]");
            var amount = AmountOf(fee.Amount, "fee_details[].amount", currency);
            switch (fee.Type)
            {
                case "stripe_fee":
                    fees.Add(new Fee(FeeType.StripeFee, amount));
                    break;
                case "application_fee":
                    fees.Add(new Fee(FeeType.TicketTailorFee, amount));
                    break;
                default:
                    fees.Add(new Fee(FeeType.Other, amount));
                    issues.Add(new DataQualityIssue(DataQualityIssueType.UnknownFeeType,
                        $"Record {raw.Id} has a fee of unknown type '{fee.Type}' ({fee.Amount}); it is shown under other fees.",
                        [raw.Id!], ExcludedFromTotals: false));
                    break;
            }
        }

        return fees;
    }

    /// <summary>Timestamps must carry an explicit offset ("Z" or "+01:00"), so the same file means the same instants on every machine.</summary>
    private static DateTimeOffset InstantOf(string? created)
    {
        var text = Require(created, "created");
        if (!ExplicitOffset().IsMatch(text)
            || !DateTimeOffset.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.None, out var instant))
        {
            throw new MalformedRecordException($"'created' must be an ISO 8601 timestamp with a UTC offset, got '{text}'");
        }

        return instant.ToUniversalTime();
    }

    private static Money AmountOf(long? value, string field, Currency currency)
    {
        var minorUnits = Require(value, field);
        if (minorUnits is > MaxAbsoluteMinorUnits or < -MaxAbsoluteMinorUnits)
        {
            throw new MalformedRecordException($"'{field}' of {minorUnits} is outside the plausible range");
        }

        return new Money(minorUnits, currency);
    }

    private static Currency CurrencyOf(RecordJson raw)
    {
        try
        {
            return Currency.From(Require(raw.Currency, "currency"));
        }
        catch (ArgumentException ex)
        {
            throw new MalformedRecordException(ex.Message, ex);
        }
    }

    private static PayoutStatus PayoutStatusOf(string status) => status switch
    {
        "pending" => PayoutStatus.Pending,
        "in_transit" => PayoutStatus.InTransit,
        "paid" => PayoutStatus.Paid,
        "failed" => PayoutStatus.Failed,
        "canceled" => PayoutStatus.Canceled,
        _ => throw new MalformedRecordException(
            $"unknown payout status '{status}'; the transactions it lists stay in still to come"),
    };

    private static T Require<T>(T? value, string field)
        where T : class =>
        value ?? throw new MalformedRecordException($"missing '{field}'");

    private static T Require<T>(T? value, string field)
        where T : struct =>
        value ?? throw new MalformedRecordException($"missing '{field}'");

    [GeneratedRegex(@"(Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitOffset();
}

internal sealed class MalformedRecordException : Exception
{
    public MalformedRecordException(string message)
        : base(message)
    {
    }

    public MalformedRecordException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
