using Payments.Application.Shared.Dtos;

namespace Payments.Application.Features.Dashboard.Dtos;

/// <summary>
/// The organiser's position in one currency.
/// <c>PaidOut + InTransit + StillToCome + PayoutDiscrepancy = AllTime.NetEarned</c>; the discrepancy is 0 on clean data.
/// </summary>
public sealed record CurrencySummaryDto(
    string Currency,
    SalesBreakdownDto AllTime,
    IReadOnlyList<PeriodBreakdownDto> Periods,
    MoneyDto PaidOut,
    MoneyDto InTransit,
    MoneyDto StillToCome,
    MoneyDto PayoutDiscrepancy,
    IReadOnlyList<AvailabilityBucketDto> StillToComeByDate,
    IReadOnlyList<PayoutLineDto> Payouts);
