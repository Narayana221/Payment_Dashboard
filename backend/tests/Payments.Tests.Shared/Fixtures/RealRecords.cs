using Payments.Domain.DataQuality;
using Payments.Domain.Records;
using Payments.Infrastructure.Records;

namespace Payments.Tests.Shared.Fixtures;

/// <summary>The real records.json extract, copied next to the test assemblies and parsed once (the ledger is immutable).</summary>
public static class RealRecords
{
    // Declared before Parsed: static initialisers run in source order.
    public static string Path { get; } = System.IO.Path.Combine(AppContext.BaseDirectory, "Data", "records.json");

    private static readonly Lazy<RecordParseResult> Parsed =
        new(() => RecordJsonParser.Parse(File.ReadAllText(Path)));

    public static Ledger Ledger => Parsed.Value.Ledger;

    /// <summary>Issues found while reading the file (de-duplication, malformed records), before validation.</summary>
    public static IReadOnlyList<DataQualityIssue> ParseIssues => Parsed.Value.Issues;
}
