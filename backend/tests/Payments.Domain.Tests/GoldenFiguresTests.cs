using System.Globalization;
using Payments.Domain.DataQuality;
using Payments.Domain.Reporting;
using Payments.Domain.Shared;
using Payments.Tests.Shared;
using Payments.Tests.Shared.Fixtures;

namespace Payments.Domain.Tests;

/// <summary>
/// End to end over the real records.json: parse → validate → calculate, checked against figures worked out
/// independently (derivation in README.md). If this fails, the code is wrong until proven otherwise.
/// </summary>
[Trait("Category", "Golden")]
public class GoldenFiguresTests
{
    private static readonly Currency Gbp = Currency.From(GoldenFigures.Currency);

    [Fact]
    public void Validate_RealData_ReportsNoIssuesAndKeepsEveryUniqueRecord()
    {
        var validated = RecordValidator.Validate(RealRecords.Ledger);

        Assert.Empty(validated.Issues);
        Assert.Equal(GoldenFigures.UniqueCharges, validated.Ledger.Charges.Count);
        Assert.Equal(GoldenFigures.UniqueRefunds, validated.Ledger.Refunds.Count);
        Assert.Equal("po_0001", Assert.Single(validated.Ledger.Payouts).Id);
    }

    [Fact]
    public void Calculate_RealData_AllTimeFiguresMatchGolden()
    {
        var summary = Summarise(GoldenFigures.AsOf);
        var allTime = summary.AllTime;

        Assert.Equal(Gbp, summary.Currency);
        Assert.Equal(Pence(GoldenFigures.GrossSales), allTime.GrossSales);
        Assert.Equal(Pence(GoldenFigures.StripeFees), allTime.StripeFees);
        Assert.Equal(Pence(GoldenFigures.TicketTailorFeesCharged), allTime.TicketTailorFeesCharged);
        Assert.Equal(Pence(GoldenFigures.TicketTailorFeesReturned), allTime.TicketTailorFeesReturned);
        Assert.Equal(Pence(GoldenFigures.TicketTailorFees), allTime.TicketTailorFees);
        Assert.Equal(Pence(GoldenFigures.RefundsIssued), allTime.RefundsIssued);
        Assert.Equal(Pence(GoldenFigures.NetEarned), allTime.NetEarned);
    }

    [Fact]
    public void Calculate_RealData_PayoutPositionMatchesGolden()
    {
        var summary = Summarise(GoldenFigures.AsOf);

        Assert.Equal(Pence(GoldenFigures.PaidOut), summary.PaidOut);
        Assert.Equal(Pence(GoldenFigures.InTransit), summary.InTransit);
        Assert.Equal(Pence(GoldenFigures.StillToCome), summary.StillToCome);
        Assert.Equal(
            GoldenFigures.StillToComeByDate,
            summary.StillToComeByDate.Select(b => (b.AvailableOn, b.Amount.MinorUnits)).ToList());
        Assert.All(summary.StillToComeByDate, b => Assert.True(b.IsAvailable));
    }

    [Fact]
    public void Calculate_RealData_LastDayAndWeekMatchGolden()
    {
        var summary = Summarise(GoldenFigures.AsOf);
        var day = summary.Periods.Single(p => p.Period == ReportingPeriod.Day).Breakdown;
        var week = summary.Periods.Single(p => p.Period == ReportingPeriod.Week).Breakdown;
        var month = summary.Periods.Single(p => p.Period == ReportingPeriod.Month).Breakdown;

        Assert.Equal(GoldenFigures.LastDay.SalesCount, day.SalesCount);
        Assert.Equal(GoldenFigures.LastDay.RefundCount, day.RefundCount);
        Assert.Equal(Pence(GoldenFigures.LastDay.GrossSales), day.GrossSales);
        Assert.Equal(Pence(GoldenFigures.LastDay.TicketTailorFeesReturned), day.TicketTailorFeesReturned);
        Assert.Equal(Pence(GoldenFigures.LastDay.RefundsIssued), day.RefundsIssued);
        Assert.Equal(Pence(GoldenFigures.LastDay.NetEarned), day.NetEarned);

        Assert.Equal(GoldenFigures.LastWeek.SalesCount, week.SalesCount);
        Assert.Equal(GoldenFigures.LastWeek.RefundCount, week.RefundCount);
        Assert.Equal(Pence(GoldenFigures.LastWeek.GrossSales), week.GrossSales);
        Assert.Equal(Pence(GoldenFigures.LastWeek.StripeFees), week.StripeFees);
        Assert.Equal(Pence(GoldenFigures.LastWeek.TicketTailorFeesCharged), week.TicketTailorFeesCharged);
        Assert.Equal(Pence(GoldenFigures.LastWeek.TicketTailorFeesReturned), week.TicketTailorFeesReturned);
        Assert.Equal(Pence(GoldenFigures.LastWeek.RefundsIssued), week.RefundsIssued);
        Assert.Equal(Pence(GoldenFigures.LastWeek.NetEarned), week.NetEarned);

        // The whole weekend falls inside the last 30 days.
        Assert.Equal(summary.AllTime, month);
    }

    [Fact]
    public void Calculate_RealDataBeforePayoutArrived_ShowsPayoutInTransit()
    {
        var summary = Summarise(GoldenFigures.BeforePayoutArrived.AsOf);

        Assert.Equal(Pence(GoldenFigures.BeforePayoutArrived.NetEarned), summary.AllTime.NetEarned);
        Assert.Equal(Pence(GoldenFigures.BeforePayoutArrived.PaidOut), summary.PaidOut);
        Assert.Equal(Pence(GoldenFigures.BeforePayoutArrived.InTransit), summary.InTransit);
        Assert.Equal(Pence(GoldenFigures.BeforePayoutArrived.StillToCome), summary.StillToCome);
    }

    [Theory]
    [InlineData("2026-06-13T12:00:00Z")]
    [InlineData("2026-06-16T03:59:00Z")]
    [InlineData("2026-06-16T04:00:00Z")]
    [InlineData("2026-06-17T12:00:00Z")]
    [InlineData("2026-06-21T00:00:00Z")]
    public void Calculate_RealData_IsCleanAndBalancedAtEveryPointInTheWeekend(string asOf)
    {
        Invariants.AssertClean(Summarise(DateTimeOffset.Parse(asOf, CultureInfo.InvariantCulture)));
    }

    private static DashboardSummary Summarise(DateTimeOffset asOf) =>
        Assert.Single(DashboardCalculator.Calculate(RecordValidator.Validate(RealRecords.Ledger).Ledger, asOf));

    private static Money Pence(long amount) => new(amount, Gbp);
}
