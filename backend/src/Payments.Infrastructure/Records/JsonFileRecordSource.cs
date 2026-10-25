using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Payments.Application.Abstractions;
using Payments.Infrastructure.Options;

namespace Payments.Infrastructure.Records;

/// <summary>
/// Serves the records.json extract for its single organiser. The file is read once, on first use, and cached:
/// the ledger is immutable, so every request shares it.
/// </summary>
internal sealed partial class JsonFileRecordSource(
    IOptions<PaymentsOptions> options,
    ILogger<JsonFileRecordSource> logger) : IRecordSource, IDisposable
{
    private readonly SemaphoreSlim _loadLock = new(1, 1);
    private volatile OrganiserRecords? _cached;

    public async Task<OrganiserRecords?> LoadAsync(string organiserId, CancellationToken cancellationToken)
    {
        if (!string.Equals(organiserId, options.Value.OrganiserId, StringComparison.Ordinal))
        {
            return null;
        }

        if (_cached is { } cached)
        {
            return cached;
        }

        await _loadLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            return _cached ??= await ReadAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _loadLock.Release();
        }
    }

    public void Dispose() => _loadLock.Dispose();

    private async Task<OrganiserRecords> ReadAsync(CancellationToken cancellationToken)
    {
        var settings = options.Value;
        var file = new FileInfo(settings.RecordsPath);
        if (!file.Exists)
        {
            throw new FileNotFoundException("The records file configured in Payments:RecordsPath does not exist.", file.FullName);
        }

        if (file.Length > settings.MaxFileBytes)
        {
            throw new InvalidDataException(
                $"The records file is {file.Length} bytes, over the {settings.MaxFileBytes}-byte limit in Payments:MaxFileBytes.");
        }

        var stream = new FileStream(file.FullName, FileMode.Open, FileAccess.Read, FileShare.Read, bufferSize: 4096, useAsync: true);
        await using (stream.ConfigureAwait(false))
        {
            var result = await RecordJsonParser.ParseAsync(stream, cancellationToken).ConfigureAwait(false);
            LogLoaded(result.Ledger.Charges.Count, result.Ledger.Refunds.Count, result.Ledger.Payouts.Count, result.Issues.Count);
            return new OrganiserRecords(result.Ledger, result.Issues);
        }
    }

    // Counts only: record contents (and buyer order references) are never logged.
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Loaded {ChargeCount} charges, {RefundCount} refunds and {PayoutCount} payouts with {IssueCount} parse issues")]
    private partial void LogLoaded(int chargeCount, int refundCount, int payoutCount, int issueCount);
}
