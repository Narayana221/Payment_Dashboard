using System.Text;
using Payments.Domain.DataQuality;
using Payments.Domain.Records;
using Payments.Infrastructure.Records;
using Payments.Tests.Shared;
using Payments.Tests.Shared.Fixtures;

namespace Payments.Infrastructure.Tests.Records;

public class RecordJsonParserTests
{
    private const string ChargeJson = """
        {"id":"ch_1","object":"charge","created":"2026-06-13T09:00:00Z","amount":1500,"currency":"gbp","status":"succeeded",
         "order_ref":"TT-1","fee_details":[{"type":"stripe_fee","amount":41},{"type":"application_fee","amount":60}],
         "net":1399,"available_on":"2026-06-16"}
        """;

    private const string PayoutJson = """
        {"id":"po_1","object":"payout","created":"2026-06-16T04:00:00Z","amount":1399,"currency":"gbp","status":"paid",
         "arrival_date":"2026-06-18","included_balance_transactions":["ch_1"]}
        """;

    [Fact]
    public void Parse_RealFile_ReadsEveryUniqueRecordAndReportsTheTwoDuplicates()
    {
        Assert.Equal(GoldenFigures.UniqueCharges, RealRecords.Ledger.Charges.Count);
        Assert.Equal(GoldenFigures.UniqueRefunds, RealRecords.Ledger.Refunds.Count);
        Assert.Single(RealRecords.Ledger.Payouts);
        Assert.All(RealRecords.ParseIssues, issue => Assert.Equal(DataQualityIssueType.DuplicateRecord, issue.Type));
        Assert.Equal(["ch_0005", "re_0002"], RealRecords.ParseIssues.SelectMany(i => i.RecordIds).Order().ToArray());
    }

    [Fact]
    public void Parse_Charge_MapsFieldsAndFeeTypes()
    {
        var charge = Assert.Single(RecordJsonParser.Parse($"[{ChargeJson}]").Ledger.Charges);

        Assert.Equal("ch_1", charge.Id);
        Assert.Equal(new DateTimeOffset(2026, 6, 13, 9, 0, 0, TimeSpan.Zero), charge.Created);
        Assert.Equal("GBP", charge.Currency.Code);
        Assert.Equal(1_500, charge.Amount.MinorUnits);
        Assert.Equal(1_399, charge.Net.MinorUnits);
        Assert.Equal(41, charge.FeesOf(FeeType.StripeFee).MinorUnits);
        Assert.Equal(60, charge.FeesOf(FeeType.TicketTailorFee).MinorUnits);
        Assert.Equal(new DateOnly(2026, 6, 16), charge.AvailableOn);
        Assert.Equal("TT-1", charge.OrderRef);
    }

    [Fact]
    public void Parse_TimestampWithNonUtcOffset_IsNormalisedToUtc()
    {
        var json = ChargeJson.Replace("2026-06-13T09:00:00Z", "2026-06-13T10:00:00+01:00", StringComparison.Ordinal);

        var charge = Assert.Single(RecordJsonParser.Parse($"[{json}]").Ledger.Charges);

        Assert.Equal(new DateTimeOffset(2026, 6, 13, 9, 0, 0, TimeSpan.Zero), charge.Created);
        Assert.Equal(TimeSpan.Zero, charge.Created.Offset);
    }

    [Fact]
    public void Parse_TimestampWithoutOffset_IsReportedAsMalformed()
    {
        // Reading it in the machine's local time would make results differ between machines.
        var json = ChargeJson.Replace("2026-06-13T09:00:00Z", "2026-06-13T09:00:00", StringComparison.Ordinal);

        var result = RecordJsonParser.Parse($"[{json}]");

        Assert.Empty(result.Ledger.Charges);
        AssertMalformed(result, "created");
    }

    [Fact]
    public void Parse_IdenticalDuplicate_IsCountedOnceAndReported()
    {
        var result = RecordJsonParser.Parse($"[{ChargeJson},{ChargeJson}]");

        Assert.Single(result.Ledger.Charges);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(DataQualityIssueType.DuplicateRecord, issue.Type);
        Assert.False(issue.ExcludedFromTotals);
    }

    [Fact]
    public void Parse_ConflictingDuplicate_ExcludesEveryCopy()
    {
        var altered = ChargeJson.Replace("\"amount\":1500", "\"amount\":9999", StringComparison.Ordinal);

        var result = RecordJsonParser.Parse($"[{ChargeJson},{altered}]");

        Assert.Empty(result.Ledger.Charges);
        var issue = Assert.Single(result.Issues);
        Assert.Equal(DataQualityIssueType.ConflictingDuplicate, issue.Type);
        Assert.True(issue.ExcludedFromTotals);
    }

    [Fact]
    public void Parse_RecordsOutOfOrder_AreReturnedChronologically()
    {
        var later = ChargeJson.Replace("ch_1", "ch_2", StringComparison.Ordinal)
            .Replace("2026-06-13T09:00:00Z", "2026-06-14T09:00:00Z", StringComparison.Ordinal);

        var charges = RecordJsonParser.Parse($"[{later},{ChargeJson}]").Ledger.Charges;

        Assert.Equal(["ch_1", "ch_2"], charges.Select(c => c.Id).ToArray());
    }

    [Fact]
    public void Parse_MissingRequiredField_IsReportedAndTheRestAreKept()
    {
        var missingAmount = """{"id":"ch_bad","object":"charge","created":"2026-06-13T09:00:00Z","currency":"gbp","status":"succeeded","net":1,"available_on":"2026-06-16"}""";

        var result = RecordJsonParser.Parse($"[{ChargeJson},{missingAmount}]");

        Assert.Single(result.Ledger.Charges);
        AssertMalformed(result, "amount");
    }

    [Fact]
    public void Parse_FieldWithWrongJsonType_IsReportedAsMalformed()
    {
        var json = ChargeJson.Replace("\"amount\":1500", "\"amount\":\"lots\"", StringComparison.Ordinal);

        var result = RecordJsonParser.Parse($"[{json}]");

        Assert.Empty(result.Ledger.Charges);
        Assert.Equal(DataQualityIssueType.MalformedRecord, Assert.Single(result.Issues).Type);
    }

    [Fact]
    public void Parse_NullFeeEntry_IsReportedAsMalformedInsteadOfCrashing()
    {
        var json = ChargeJson.Replace("\"fee_details\":[", "\"fee_details\":[null,", StringComparison.Ordinal);

        var result = RecordJsonParser.Parse($"[{json}]");

        Assert.Empty(result.Ledger.Charges);
        AssertMalformed(result, "fee_details");
    }

    [Fact]
    public void Parse_NullPayoutTransactionId_IsReportedAsMalformedInsteadOfCrashing()
    {
        var json = PayoutJson.Replace("[\"ch_1\"]", "[\"ch_1\",null]", StringComparison.Ordinal);

        var result = RecordJsonParser.Parse($"[{ChargeJson},{json}]");

        Assert.Empty(result.Ledger.Payouts);
        AssertMalformed(result, "included_balance_transactions");
    }

    [Fact]
    public void Parse_ElementWithoutId_IsReportedAndIgnored()
    {
        var result = RecordJsonParser.Parse($$"""[{{ChargeJson}}, {"object":"charge"}, 42]""");

        Assert.Single(result.Ledger.Charges);
        Assert.Equal(2, result.Issues.Count(i => i.Type == DataQualityIssueType.MalformedRecord));
    }

    [Fact]
    public void Parse_UnknownRecordType_IsReportedAndIgnored()
    {
        var result = RecordJsonParser.Parse("""[{"id":"dp_1","object":"dispute"}]""");

        Assert.Equal(DataQualityIssueType.UnknownRecordType, Assert.Single(result.Issues).Type);
    }

    [Fact]
    public void Parse_UnknownFeeType_IsKeptAsOtherFeeAndReported()
    {
        var withExtraFee = ChargeJson.Replace(
            "{\"type\":\"stripe_fee\"", "{\"type\":\"tax\",\"amount\":10},{\"type\":\"stripe_fee\"", StringComparison.Ordinal);

        var result = RecordJsonParser.Parse($"[{withExtraFee}]");

        var charge = Assert.Single(result.Ledger.Charges);
        Assert.Equal(10, charge.FeesOf(FeeType.Other).MinorUnits);
        Assert.Equal(DataQualityIssueType.UnknownFeeType, Assert.Single(result.Issues).Type);
    }

    [Fact]
    public void Parse_MalformedRecordWithUnknownFee_ReportsOnlyThatItWasIgnored()
    {
        // The unknown fee is read before the missing status is discovered; its "shown under other fees" warning
        // must not survive alongside "was ignored".
        var json = ChargeJson
            .Replace("{\"type\":\"stripe_fee\"", "{\"type\":\"vat\",\"amount\":10},{\"type\":\"stripe_fee\"", StringComparison.Ordinal)
            .Replace("\"status\":\"succeeded\",", string.Empty, StringComparison.Ordinal);

        var result = RecordJsonParser.Parse($"[{json}]");

        AssertMalformed(result, "status");
    }

    [Theory]
    [InlineData("\"amount\":1500", "\"amount\":5000000000000000000")]
    [InlineData("\"net\":1399", "\"net\":-9223372036854775808")]
    [InlineData("\"amount\":41}", "\"amount\":9223372036854775807}")]
    public void Parse_AmountOutsidePlausibleRange_IsReportedAsMalformed(string original, string replacement)
    {
        // Values this large can't be ticket sales and would overflow when summed.
        var json = ChargeJson.Replace(original, replacement, StringComparison.Ordinal);

        var result = RecordJsonParser.Parse($"[{json}]");

        Assert.Empty(result.Ledger.Charges);
        AssertMalformed(result, "plausible range");
    }

    [Fact]
    public void Parse_RootIsNotAnArray_Throws() =>
        Assert.Throws<InvalidDataException>(() => RecordJsonParser.Parse("""{"records":[]}"""));

    [Fact]
    public async Task ParseAsync_Stream_ReadsTheSameAsParse()
    {
        await using var stream = new MemoryStream(Encoding.UTF8.GetBytes($"[{ChargeJson},{PayoutJson}]"));

        var result = await RecordJsonParser.ParseAsync(stream, CancellationToken.None);

        Assert.Single(result.Ledger.Charges);
        Assert.Equal(["ch_1"], Assert.Single(result.Ledger.Payouts).IncludedTransactionIds);
        Assert.Empty(result.Issues);
    }

    private static void AssertMalformed(RecordParseResult result, string field)
    {
        var issue = Assert.Single(result.Issues);
        Assert.Equal(DataQualityIssueType.MalformedRecord, issue.Type);
        Assert.True(issue.ExcludedFromTotals);
        Assert.Contains(field, issue.Message, StringComparison.Ordinal);
    }
}
