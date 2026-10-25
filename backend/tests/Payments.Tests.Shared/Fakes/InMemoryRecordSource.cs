using Payments.Application.Abstractions;
using Payments.Domain.DataQuality;
using Payments.Domain.Records;

namespace Payments.Tests.Shared.Fakes;

/// <summary>An <see cref="IRecordSource"/> holding one organiser's ledger in memory.</summary>
public sealed class InMemoryRecordSource(string knownOrganiserId, Ledger ledger, IReadOnlyList<DataQualityIssue>? parseIssues = null)
    : IRecordSource
{
    public Task<OrganiserRecords?> LoadAsync(string organiserId, CancellationToken cancellationToken) =>
        Task.FromResult(organiserId == knownOrganiserId ? new OrganiserRecords(ledger, parseIssues ?? []) : null);
}
