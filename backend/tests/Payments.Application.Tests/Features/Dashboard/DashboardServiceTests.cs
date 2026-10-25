using Microsoft.Extensions.Time.Testing;
using Payments.Application.Features.Dashboard;
using Payments.Application.Features.Dashboard.Dtos;
using Payments.Domain.DataQuality;
using Payments.Tests.Shared;
using Payments.Tests.Shared.Fakes;
using Payments.Tests.Shared.Fixtures;
using static Payments.Tests.Shared.Builders.TestRecords;

namespace Payments.Application.Tests.Features.Dashboard;

public class DashboardServiceTests
{
    private const string Organiser = "acct_demo";

    private readonly FakeTimeProvider _clock = new(GoldenFigures.BeforePayoutArrived.AsOf);

    [Fact]
    public async Task GetSummaryAsync_RealData_MapsGoldenFiguresIntoTheContract()
    {
        var response = await RealDataService().GetSummaryAsync(Organiser, GoldenFigures.AsOf, CancellationToken.None);

        Assert.NotNull(response);
        var gbp = Assert.Single(response.Summaries);
        var allTime = gbp.AllTime;
        Assert.Equal("GBP", gbp.Currency);

        // Every field, so a swap between same-typed fields in the mapping can't pass.
        Assert.Equal(GoldenFigures.GrossSales, allTime.GrossSales.AmountMinor);
        Assert.Equal(GoldenFigures.StripeFees, allTime.StripeFees.AmountMinor);
        Assert.Equal(GoldenFigures.TicketTailorFeesCharged, allTime.TicketTailorFeesCharged.AmountMinor);
        Assert.Equal(GoldenFigures.TicketTailorFeesReturned, allTime.TicketTailorFeesReturned.AmountMinor);
        Assert.Equal(GoldenFigures.TicketTailorFees, allTime.TicketTailorFees.AmountMinor);
        Assert.Equal(0, allTime.OtherFees.AmountMinor);
        Assert.Equal(GoldenFigures.RefundsIssued, allTime.RefundsIssued.AmountMinor);
        Assert.Equal(0, allTime.UnexplainedDifference.AmountMinor);
        Assert.Equal(GoldenFigures.NetEarned, allTime.NetEarned.AmountMinor);
        Assert.Equal(GoldenFigures.UniqueCharges, allTime.SalesCount);
        Assert.Equal(GoldenFigures.UniqueRefunds, allTime.RefundCount);

        Assert.Equal(GoldenFigures.PaidOut, gbp.PaidOut.AmountMinor);
        Assert.Equal(GoldenFigures.InTransit, gbp.InTransit.AmountMinor);
        Assert.Equal(GoldenFigures.StillToCome, gbp.StillToCome.AmountMinor);
        Assert.Equal(0, gbp.PayoutDiscrepancy.AmountMinor);
        Assert.Equal(
            GoldenFigures.StillToComeByDate,
            gbp.StillToComeByDate.Select(b => (b.AvailableOn, b.Amount.AmountMinor)).ToList());
        Assert.All(gbp.StillToComeByDate, b => Assert.Equal("GBP", b.Amount.Currency));
        var payout = Assert.Single(gbp.Payouts);
        Assert.Equal(("po_0001", GoldenFigures.PaidOut, new DateOnly(2026, 6, 18), true),
            (payout.Id, payout.Amount.AmountMinor, payout.ArrivalDate, payout.HasArrived));

        var week = gbp.Periods.Single(p => p.Period == "week").Breakdown;
        Assert.Equal(
            (GoldenFigures.LastWeek.GrossSales, GoldenFigures.LastWeek.StripeFees, GoldenFigures.LastWeek.TicketTailorFeesCharged,
                GoldenFigures.LastWeek.TicketTailorFeesReturned, GoldenFigures.LastWeek.RefundsIssued, GoldenFigures.LastWeek.NetEarned),
            (week.GrossSales.AmountMinor, week.StripeFees.AmountMinor, week.TicketTailorFeesCharged.AmountMinor,
                week.TicketTailorFeesReturned.AmountMinor, week.RefundsIssued.AmountMinor, week.NetEarned.AmountMinor));

        Assert.All(ContractIdentities.All(gbp), r => Assert.True(r.Residual == 0, $"{r.Check} is off by {r.Residual}"));
    }

    [Fact]
    public async Task GetSummaryAsync_IssueAboutALaterRecord_IsNotReportedForAnEarlierAsOf()
    {
        // D-8: re_B (over-refund) didn't exist yet on day 3, so the day-3 view mustn't say a figure is incomplete.
        var source = new InMemoryRecordSource(Organiser, Ledger(
            charges: [Charge("ch_1", 1_000)],
            refunds:
            [
                Refund("re_A", "ch_1", 600, created: Saturday.AddDays(1)),
                Refund("re_B", "ch_1", 600, created: Saturday.AddDays(6)),
            ]));
        var service = new DashboardService(source, _clock);

        var early = await service.GetSummaryAsync(Organiser, Saturday.AddDays(3), CancellationToken.None);
        var later = await service.GetSummaryAsync(Organiser, Saturday.AddDays(7), CancellationToken.None);

        Assert.Empty(early!.DataQuality);
        Assert.Equal(600, Assert.Single(early.Summaries).AllTime.RefundsIssued.AmountMinor);
        Assert.Equal("overRefund", Assert.Single(later!.DataQuality).Type);
        Assert.Equal(600, Assert.Single(later.Summaries).AllTime.RefundsIssued.AmountMinor);
    }

    [Fact]
    public async Task GetSummaryAsync_NoAsOf_UsesTheClock()
    {
        var response = await RealDataService().GetSummaryAsync(Organiser, asOf: null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(GoldenFigures.BeforePayoutArrived.AsOf, response.AsOf);
        Assert.Equal(GoldenFigures.BeforePayoutArrived.InTransit, Assert.Single(response.Summaries).InTransit.AmountMinor);
    }

    [Fact]
    public async Task GetSummaryAsync_AsOfWithOffset_IsReportedInUtc()
    {
        var response = await RealDataService().GetSummaryAsync(
            Organiser, new DateTimeOffset(2026, 6, 21, 1, 0, 0, TimeSpan.FromHours(1)), CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(TimeSpan.Zero, response.AsOf.Offset);
        Assert.Equal(GoldenFigures.AsOf, response.AsOf);
    }

    [Fact]
    public async Task GetSummaryAsync_RealData_ReportsLatestActivityAndPeriods()
    {
        var response = await RealDataService().GetSummaryAsync(Organiser, GoldenFigures.AsOf, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Equal(new DateTimeOffset(2026, 6, 20, 11, 30, 0, TimeSpan.Zero), response.LatestActivityAt); // re_0004
        var periods = Assert.Single(response.Summaries).Periods;
        Assert.Equal(["day", "week", "month"], periods.Select(p => p.Period).ToArray());
        Assert.Equal(GoldenFigures.LastWeek.NetEarned, periods.Single(p => p.Period == "week").Breakdown.NetEarned.AmountMinor);
        Assert.All(periods, p => Assert.Equal(GoldenFigures.AsOf, p.WindowEnd));
    }

    [Fact]
    public async Task GetSummaryAsync_CombinesParseAndValidationIssues()
    {
        var parseIssue = new DataQualityIssue(DataQualityIssueType.DuplicateRecord, "dup", ["ch_1"], ExcludedFromTotals: false);
        var source = new InMemoryRecordSource(
            Organiser,
            Ledger(charges: [Charge("ch_1", 1_000)], refunds: [Refund("re_orphan", "ch_missing", 500)]),
            [parseIssue]);

        var response = await new DashboardService(source, _clock).GetSummaryAsync(Organiser, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Collection(
            response.DataQuality,
            i => Assert.Equal(("duplicateRecord", false), (i.Type, i.ExcludedFromTotals)),
            i => Assert.Equal(("orphanRefund", true), (i.Type, i.ExcludedFromTotals)));
    }

    [Fact]
    public async Task GetSummaryAsync_RefundDatedBeforeItsCharge_IsLeftOutUntilTheChargeExists()
    {
        // D-9: between the refund (10:00) and its charge (11:00), the refund has no sale behind it, so it isn't counted.
        var chargeAt = Saturday.AddHours(1);
        var source = new InMemoryRecordSource(Organiser, Ledger(
            charges: [Charge("ch_1", 1_000, created: chargeAt)],
            refunds: [Refund("re_1", "ch_1", 1_000, created: Saturday)]));
        var service = new DashboardService(source, _clock);

        var between = await service.GetSummaryAsync(Organiser, Saturday.AddMinutes(30), CancellationToken.None);
        var after = await service.GetSummaryAsync(Organiser, chargeAt.AddMinutes(30), CancellationToken.None);

        Assert.Empty(between!.Summaries);
        var orphan = Assert.Single(between.DataQuality);
        Assert.Equal(("orphanRefund", true), (orphan.Type, orphan.ExcludedFromTotals));
        Assert.Contains("not yet created", orphan.Message, StringComparison.Ordinal);

        Assert.Equal(0, Assert.Single(after!.Summaries).AllTime.NetEarned.AmountMinor);
        Assert.Equal("refundBeforeCharge", Assert.Single(after.DataQuality).Type);
    }

    [Fact]
    public async Task GetSummaryAsync_UnknownOrganiser_ReturnsNull()
    {
        Assert.Null(await RealDataService().GetSummaryAsync("acct_someone_else", null, CancellationToken.None));
    }

    [Fact]
    public async Task GetSummaryAsync_NoRecords_ReturnsEmptySummariesAndNoLatestActivity()
    {
        var service = new DashboardService(new InMemoryRecordSource(Organiser, Ledger()), _clock);

        var response = await service.GetSummaryAsync(Organiser, null, CancellationToken.None);

        Assert.NotNull(response);
        Assert.Empty(response.Summaries);
        Assert.Null(response.LatestActivityAt);
    }

    [Fact]
    public async Task GetSummaryAsync_DataKeptWithWarning_ExposesTheReconciliationLines()
    {
        var source = new InMemoryRecordSource(Organiser, Ledger(charges:
        [
            Charge("ch_1", 1_000, stripeFee: 30, net: 900),
            Charge("ch_2", 1_000, stripeFee: 30, ticketTailorFee: 50, otherFee: 10),
        ]));

        var response = await new DashboardService(source, _clock).GetSummaryAsync(Organiser, null, CancellationToken.None);

        var gbp = Assert.Single(response!.Summaries);
        Assert.Equal(10, gbp.AllTime.OtherFees.AmountMinor);
        Assert.Equal(-70, gbp.AllTime.UnexplainedDifference.AmountMinor);
        Assert.All(ContractIdentities.All(gbp), r => Assert.True(r.Residual == 0, $"{r.Check} is off by {r.Residual}"));
    }

    private DashboardService RealDataService() =>
        new(new InMemoryRecordSource(Organiser, RealRecords.Ledger, RealRecords.ParseIssues), _clock);
}
