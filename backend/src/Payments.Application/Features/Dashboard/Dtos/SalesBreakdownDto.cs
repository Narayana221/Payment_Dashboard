using Payments.Application.Shared.Dtos;

namespace Payments.Application.Features.Dashboard.Dtos;

/// <summary>
/// <c>GrossSales − StripeFees − TicketTailorFees − OtherFees − RefundsIssued + UnexplainedDifference = NetEarned</c>.
/// <c>OtherFees</c> and <c>UnexplainedDifference</c> are 0 on clean data.
/// </summary>
public sealed record SalesBreakdownDto(
    MoneyDto GrossSales,
    MoneyDto StripeFees,
    MoneyDto TicketTailorFeesCharged,
    MoneyDto TicketTailorFeesReturned,
    MoneyDto TicketTailorFees,
    MoneyDto OtherFees,
    MoneyDto RefundsIssued,
    MoneyDto UnexplainedDifference,
    MoneyDto NetEarned,
    int SalesCount,
    int RefundCount);
