using Payments.Application.Features.Dashboard.Dtos;
using Payments.Application.Shared.Mapping;
using Payments.Domain.Reporting;

namespace Payments.Application.Features.Dashboard;

internal static class DashboardMappingExtensions
{
    // Named arguments throughout: several parameters share a type, and a swap would otherwise compile silently.
    public static CurrencySummaryDto ToDto(this DashboardSummary summary) => new(
        Currency: summary.Currency.Code,
        AllTime: summary.AllTime.ToDto(),
        Periods: summary.Periods.Select(p => p.ToDto()).ToList(),
        PaidOut: summary.PaidOut.ToDto(),
        InTransit: summary.InTransit.ToDto(),
        StillToCome: summary.StillToCome.ToDto(),
        PayoutDiscrepancy: summary.PayoutDiscrepancy.ToDto(),
        StillToComeByDate: summary.StillToComeByDate
            .Select(b => new AvailabilityBucketDto(AvailableOn: b.AvailableOn, Amount: b.Amount.ToDto(), IsAvailable: b.IsAvailable))
            .ToList(),
        Payouts: summary.Payouts
            .Select(p => new PayoutLineDto(Id: p.Id, Amount: p.Amount.ToDto(), ArrivalDate: p.ArrivalDate, HasArrived: p.HasArrived))
            .ToList());

    private static PeriodBreakdownDto ToDto(this PeriodBreakdown period) => new(
        Period: MappingExtensions.ContractName(period.Period),
        WindowStart: period.Window.Start,
        WindowEnd: period.Window.End,
        Breakdown: period.Breakdown.ToDto());

    private static SalesBreakdownDto ToDto(this SalesBreakdown b) => new(
        GrossSales: b.GrossSales.ToDto(),
        StripeFees: b.StripeFees.ToDto(),
        TicketTailorFeesCharged: b.TicketTailorFeesCharged.ToDto(),
        TicketTailorFeesReturned: b.TicketTailorFeesReturned.ToDto(),
        TicketTailorFees: b.TicketTailorFees.ToDto(),
        OtherFees: b.OtherFees.ToDto(),
        RefundsIssued: b.RefundsIssued.ToDto(),
        UnexplainedDifference: b.UnexplainedDifference.ToDto(),
        NetEarned: b.NetEarned.ToDto(),
        SalesCount: b.SalesCount,
        RefundCount: b.RefundCount);
}
