using Payments.Domain.Shared;

namespace Payments.Domain.Reporting;

/// <summary>Everything the organiser dashboard reports, for one currency, as of one instant.</summary>
public sealed record DashboardSummary
{
    public required Currency Currency { get; init; }

    public required DateTimeOffset AsOf { get; init; }

    /// <summary>All activity up to <see cref="AsOf"/>.</summary>
    public required SalesBreakdown AllTime { get; init; }

    /// <summary>Activity in each rolling period ending at <see cref="AsOf"/> (Q1: last day, week, month).</summary>
    public required IReadOnlyList<PeriodBreakdown> Periods { get; init; }

    /// <summary>Payouts that have arrived in the bank by <see cref="AsOf"/>.</summary>
    public required Money PaidOut { get; init; }

    /// <summary>Payouts that have left Stripe but not yet arrived (decision A6).</summary>
    public required Money InTransit { get; init; }

    /// <summary>Net earned not yet covered by any payout.</summary>
    public required Money StillToCome { get; init; }

    /// <summary><see cref="StillToCome"/> split by the date each part becomes available. A bucket can be negative (decision A4).</summary>
    public required IReadOnlyList<AvailabilityBucket> StillToComeByDate { get; init; }

    /// <summary>
    /// Net earned that payouts and remaining funds don't account for, so that
    /// <c>PaidOut + InTransit + StillToCome + PayoutDiscrepancy = NetEarned</c> always holds (decision D-5).
    /// Zero on clean data; non-zero only alongside a reported payout data-quality issue.
    /// </summary>
    public Money PayoutDiscrepancy => AllTime.NetEarned - PaidOut - InTransit - StillToCome;

    public required IReadOnlyList<PayoutLine> Payouts { get; init; }
}

public sealed record PeriodBreakdown(ReportingPeriod Period, ReportingWindow Window, SalesBreakdown Breakdown);

/// <summary>
/// Funds that become eligible for payout on <paramref name="AvailableOn"/>. When <paramref name="IsAvailable"/> is true the date
/// has passed and the money is waiting for the next payout (decision A5). This is never a predicted payout date.
/// </summary>
public sealed record AvailabilityBucket(DateOnly AvailableOn, Money Amount, bool IsAvailable);

public sealed record PayoutLine(string Id, Money Amount, DateOnly ArrivalDate, bool HasArrived);
