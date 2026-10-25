using static Payments.Tests.Shared.Builders.TestRecords;

namespace Payments.Domain.Tests.Records;

public class LedgerTests
{
    [Fact]
    public void LatestActivityAt_EmptyLedger_IsNull() => Assert.Null(Ledger().LatestActivityAt);

    [Fact]
    public void LatestActivityAt_ConsidersEveryRecordKind()
    {
        var newest = Saturday.AddDays(9);
        var ledger = Ledger(
            charges: [Charge("ch_1", 1_000, created: Saturday)],
            refunds: [Refund("re_1", "ch_1", 1_000, created: Saturday.AddDays(2))],
            payouts: [Payout("po_1", 0, new DateOnly(2026, 6, 25), [], created: newest)]);

        Assert.Equal(newest, ledger.LatestActivityAt);
    }

    [Fact]
    public void AsOf_DropsRecordsCreatedLater()
    {
        var ledger = Ledger(charges: [Charge("ch_1", 1_000, created: Saturday), Charge("ch_2", 1_000, created: Saturday.AddDays(1))]);

        Assert.Equal(["ch_1"], ledger.AsOf(Saturday).Charges.Select(c => c.Id).ToArray());
    }
}
