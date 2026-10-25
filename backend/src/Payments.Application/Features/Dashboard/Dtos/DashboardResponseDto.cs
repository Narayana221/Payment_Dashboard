using Payments.Application.Shared.Dtos;

namespace Payments.Application.Features.Dashboard.Dtos;

/// <param name="AsOf">The instant every figure is calculated at (UTC).</param>
/// <param name="LatestActivityAt">
/// When the newest record in the source was created (whether or not it counts towards the figures), so a client can
/// default its "as of" to it. Null with no records.
/// </param>
/// <param name="Summaries">One per currency; amounts are never summed across currencies.</param>
/// <param name="DataQuality">
/// What the organiser should know about the source: problems reading the file, plus validation issues for records that
/// existed at <paramref name="AsOf"/> (D-8).
/// </param>
public sealed record DashboardResponseDto(
    string OrganiserId,
    DateTimeOffset AsOf,
    DateTimeOffset? LatestActivityAt,
    IReadOnlyList<CurrencySummaryDto> Summaries,
    IReadOnlyList<DataQualityIssueDto> DataQuality);
