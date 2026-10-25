using Payments.Domain.Reporting;
using static Payments.Tests.Shared.Builders.TestRecords;

namespace Payments.Domain.Tests.Reporting;

public class RefundFeeRuleTests
{
    [Fact]
    public void TicketTailorFeeReturnable_FullRefund_ReturnsWholeFee()
    {
        var charge = Charge("ch_1", amount: 1_500, stripeFee: 41, ticketTailorFee: 60);

        Assert.Equal(Pence(60), RefundFeeRule.TicketTailorFeeReturnable(charge, Pence(1_500)));
    }

    [Fact]
    public void TicketTailorFeeReturnable_HalfRefunded_ReturnsHalfTheFee()
    {
        // re_0003: £22.50 refunded on a £45.00 charge with a £1.20 Ticket Tailor fee.
        var charge = Charge("ch_0050", amount: 4_500, stripeFee: 83, ticketTailorFee: 120);

        Assert.Equal(Pence(60), RefundFeeRule.TicketTailorFeeReturnable(charge, Pence(2_250)));
    }

    [Theory]
    [InlineData(45, 1, 2, 23)] // 22.5 -> 23 (half rounds away from zero)
    [InlineData(46, 1, 3, 15)] // 15.33 -> 15
    [InlineData(46, 2, 3, 31)] // 30.67 -> 31
    public void TicketTailorFeeReturnable_FractionalPence_RoundsHalfAwayFromZero(
        long fee, long totalRefunded, long chargeAmount, long expected)
    {
        var charge = Charge("ch_1", amount: chargeAmount, ticketTailorFee: fee);

        Assert.Equal(Pence(expected), RefundFeeRule.TicketTailorFeeReturnable(charge, Pence(totalRefunded)));
    }

    [Fact]
    public void TicketTailorFeeReturnable_RunningTotal_NeverExceedsTheFeeCharged()
    {
        // Two half refunds of a 125p fee: rounding each refund separately would return 63 + 63 = 126.
        var charge = Charge("ch_1", amount: 4_500, ticketTailorFee: 125);

        var afterFirst = RefundFeeRule.TicketTailorFeeReturnable(charge, Pence(2_250));
        var afterBoth = RefundFeeRule.TicketTailorFeeReturnable(charge, Pence(4_500));

        Assert.Equal(Pence(63), afterFirst);
        Assert.Equal(Pence(62), afterBoth - afterFirst);
    }

    [Fact]
    public void TicketTailorFeeReturnable_ZeroValueCharge_ReturnsZero()
    {
        var charge = Charge("ch_free", amount: 0);

        Assert.Equal(Pence(0), RefundFeeRule.TicketTailorFeeReturnable(charge, Pence(0)));
    }
}
