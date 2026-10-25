using Payments.Domain.DataQuality;
using Payments.Domain.Records;

namespace Payments.Application.Abstractions;

/// <summary>Where an organiser's Stripe activity comes from (today: the records.json extract).</summary>
public interface IRecordSource
{
    /// <returns>The organiser's records, or <c>null</c> if this source knows nothing about the organiser.</returns>
    Task<OrganiserRecords?> LoadAsync(string organiserId, CancellationToken cancellationToken);
}

/// <param name="ParseIssues">Problems found while reading the source (duplicates, malformed records), before validation.</param>
public sealed record OrganiserRecords(Ledger Ledger, IReadOnlyList<DataQualityIssue> ParseIssues);
