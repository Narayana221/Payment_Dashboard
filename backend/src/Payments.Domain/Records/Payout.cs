using Payments.Domain.Shared;

namespace Payments.Domain.Records;

public enum PayoutStatus
{
    Pending,
    InTransit,
    Paid,
    Failed,
    Canceled,
}

/// <summary>Money moved from the Stripe balance to the organiser's bank account.</summary>
public sealed record Payout
{
    public required string Id { get; init; }

    public required DateTimeOffset Created { get; init; }

    public required Money Amount { get; init; }

    public required PayoutStatus Status { get; init; }

    /// <summary>The date the money lands in the bank.</summary>
    public required DateOnly ArrivalDate { get; init; }

    /// <summary>Ids of the charges and refunds this payout covered.</summary>
    public required IReadOnlyList<string> IncludedTransactionIds { get; init; }

    public Currency Currency => Amount.Currency;

    /// <summary>A failed or cancelled payout returns its funds to the balance, so it covers nothing.</summary>
    public bool MovesMoney => Status is PayoutStatus.Pending or PayoutStatus.InTransit or PayoutStatus.Paid;

    public bool HasArrivedBy(DateOnly date) => Status == PayoutStatus.Paid && ArrivalDate <= date;
}
