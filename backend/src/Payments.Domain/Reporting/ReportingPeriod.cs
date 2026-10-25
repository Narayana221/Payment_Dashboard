namespace Payments.Domain.Reporting;

public enum ReportingPeriod
{
    Day,
    Week,
    Month,
}

/// <summary>
/// A rolling window ending at <see cref="End"/> (decision A9). Start is exclusive and End inclusive,
/// so back-to-back windows never count the same record twice.
/// </summary>
public readonly record struct ReportingWindow(DateTimeOffset Start, DateTimeOffset End)
{
    public static ReportingWindow For(ReportingPeriod period, DateTimeOffset asOf) => new(asOf - LengthOf(period), asOf);

    public static TimeSpan LengthOf(ReportingPeriod period) => period switch
    {
        ReportingPeriod.Day => TimeSpan.FromDays(1),
        ReportingPeriod.Week => TimeSpan.FromDays(7),
        ReportingPeriod.Month => TimeSpan.FromDays(30),
        _ => throw new ArgumentOutOfRangeException(nameof(period), period, "Unknown reporting period."),
    };

    public bool Contains(DateTimeOffset instant) => instant > Start && instant <= End;
}
