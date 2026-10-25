using Payments.Domain.Shared;

namespace Payments.Domain.Records;

public enum FeeType
{
    /// <summary>Stripe's processing fee (<c>stripe_fee</c>). Never returned on a refund.</summary>
    StripeFee,

    /// <summary>Ticket Tailor's application fee (<c>application_fee</c>). Returned in proportion on a refund.</summary>
    TicketTailorFee,

    /// <summary>A fee type the rules don't cover. Kept so the waterfall still balances, and reported (decision D-5).</summary>
    Other,
}

/// <summary>
/// A deduction recorded against a balance transaction. Positive on a charge (taken from the organiser);
/// negative on a refund (given back to the organiser).
/// </summary>
public sealed record Fee(FeeType Type, Money Amount);
