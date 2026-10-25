using System.Text.Json;
using Payments.Domain.DataQuality;
using Payments.Domain.Records;

namespace Payments.Infrastructure.Records;

internal sealed record RecordParseResult(Ledger Ledger, IReadOnlyList<DataQualityIssue> Issues);

/// <summary>
/// Reads the records.json extract: a JSON array of charge, refund and payout records in no particular order.
/// The file as a whole must be a JSON array; individual bad records are reported and skipped, never fatal.
/// </summary>
internal static class RecordJsonParser
{
    // Records are flat; anything nested deeper than this is not a records file and shouldn't be walked.
    private static readonly JsonDocumentOptions DocumentOptions = new() { MaxDepth = 16 };

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public static RecordParseResult Parse(string json)
    {
        using var document = JsonDocument.Parse(json, DocumentOptions);
        return Parse(document);
    }

    public static async Task<RecordParseResult> ParseAsync(Stream utf8Json, CancellationToken cancellationToken)
    {
        using var document = await JsonDocument.ParseAsync(utf8Json, DocumentOptions, cancellationToken).ConfigureAwait(false);
        return Parse(document);
    }

    private static RecordParseResult Parse(JsonDocument document)
    {
        if (document.RootElement.ValueKind != JsonValueKind.Array)
        {
            throw new InvalidDataException("The records file must contain a JSON array.");
        }

        var issues = new List<DataQualityIssue>();
        var unique = RecordDeduplicator.Deduplicate(document.RootElement.EnumerateArray().ToList(), issues);

        var charges = new List<Charge>();
        var refunds = new List<Refund>();
        var payouts = new List<Payout>();

        foreach (var element in unique)
        {
            var id = element.GetProperty("id").GetString()!;
            try
            {
                var raw = element.Deserialize<RecordJson>(SerializerOptions)!;
                switch (raw.Object)
                {
                    case "charge":
                        charges.Add(RecordJsonMapper.ToCharge(raw, issues));
                        break;
                    case "refund":
                        refunds.Add(RecordJsonMapper.ToRefund(raw, issues));
                        break;
                    case "payout":
                        payouts.Add(RecordJsonMapper.ToPayout(raw));
                        break;
                    default:
                        issues.Add(new DataQualityIssue(DataQualityIssueType.UnknownRecordType,
                            $"Record {id} has unknown type '{raw.Object}' and was ignored.", [id], ExcludedFromTotals: true));
                        break;
                }
            }
            catch (Exception ex) when (ex is MalformedRecordException or JsonException)
            {
                issues.Add(new DataQualityIssue(DataQualityIssueType.MalformedRecord,
                    $"Record {id} could not be read ({ex.Message}) and was ignored.", [id], ExcludedFromTotals: true));
            }
        }

        return new RecordParseResult(Ledger.Create(charges, refunds, payouts), issues);
    }
}
