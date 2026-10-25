using Payments.Application.Abstractions;
using Payments.Application.Features.Dashboard.Dtos;
using Payments.Application.Shared.Mapping;
using Payments.Domain.DataQuality;
using Payments.Domain.Reporting;

namespace Payments.Application.Features.Dashboard;

/// <summary>Load → validate → calculate → map. The figures themselves are worked out in the Domain.</summary>
public sealed class DashboardService(IRecordSource records, TimeProvider timeProvider) : IDashboardService
{
    public async Task<DashboardResponseDto?> GetSummaryAsync(
        string organiserId, DateTimeOffset? asOf, CancellationToken cancellationToken)
    {
        var loaded = await records.LoadAsync(organiserId, cancellationToken).ConfigureAwait(false);
        if (loaded is null)
        {
            return null;
        }

        var at = (asOf ?? timeProvider.GetUtcNow()).ToUniversalTime();

        // Validate the ledger as it stood at asOf (D-8), so the issues describe only records that existed then.
        // The validator works chronologically, so earlier records are judged exactly as they would be on the full ledger.
        var validated = RecordValidator.Validate(loaded.Ledger.AsOf(at));

        return new DashboardResponseDto(
            organiserId,
            at,
            loaded.Ledger.LatestActivityAt,
            DashboardCalculator.Calculate(validated.Ledger, at).Select(s => s.ToDto()).ToList(),
            loaded.ParseIssues.Concat(validated.Issues).Select(i => i.ToDto()).ToList());
    }
}
