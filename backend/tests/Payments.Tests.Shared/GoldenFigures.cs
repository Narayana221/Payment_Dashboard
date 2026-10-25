namespace Payments.Tests.Shared;

/// <summary>
/// Expected figures for the real records.json, in pence. Worked out independently of the application code
/// (PowerShell over the raw file, de-duplicated by id), with the derivation of each in README.md
/// ("How I know the numbers are right").
/// Change these only with the user's approval and a matching decision in README.md.
/// </summary>
public static class GoldenFigures
{
    public const string Currency = "GBP";

    /// <summary>After every record, and after po_0001 has arrived (2026-06-18).</summary>
    public static readonly DateTimeOffset AsOf = new(2026, 6, 21, 0, 0, 0, TimeSpan.Zero);

    public const int UniqueCharges = 60;
    public const int UniqueRefunds = 4;
    public const int UniquePayouts = 1;

    public const long GrossSales = 116_250;
    public const long StripeFees = 2_831;
    public const long TicketTailorFeesCharged = 4_125;
    public const long TicketTailorFeesReturned = 320;
    public const long TicketTailorFees = 3_805;
    public const long RefundsIssued = 10_750;
    public const long NetEarned = 98_864;

    public const long PaidOut = 44_536;
    public const long InTransit = 0;
    public const long StillToCome = 54_328;

    public static readonly IReadOnlyList<(DateOnly AvailableOn, long Amount)> StillToComeByDate =
    [
        (new DateOnly(2026, 6, 16), 7_962),
        (new DateOnly(2026, 6, 17), 50_746),
        (new DateOnly(2026, 6, 20), -4_380),
    ];

    /// <summary>Last 24 hours to <see cref="AsOf"/>: only re_0004 (refund of an already paid-out charge).</summary>
    public static class LastDay
    {
        public const int SalesCount = 0;
        public const int RefundCount = 1;
        public const long GrossSales = 0;
        public const long TicketTailorFeesReturned = 120;
        public const long RefundsIssued = 4_500;
        public const long NetEarned = -4_380;
    }

    /// <summary>Last 7 days to <see cref="AsOf"/>: Sunday's sales (ch_0031–ch_0060) plus all four refunds.</summary>
    public static class LastWeek
    {
        public const int SalesCount = 30;
        public const int RefundCount = 4;
        public const long GrossSales = 60_350;
        public const long StripeFees = 1_447;
        public const long TicketTailorFeesCharged = 2_107;
        public const long TicketTailorFeesReturned = 320;
        public const long RefundsIssued = 10_750;
        public const long NetEarned = 46_366;
    }

    /// <summary>Midday on 2026-06-17: po_0001 sent but not arrived; re_0004 not yet issued.</summary>
    public static class BeforePayoutArrived
    {
        public static readonly DateTimeOffset AsOf = new(2026, 6, 17, 12, 0, 0, TimeSpan.Zero);
        public const long NetEarned = 103_244;
        public const long PaidOut = 0;
        public const long InTransit = 44_536;
        public const long StillToCome = 58_708;
    }
}
