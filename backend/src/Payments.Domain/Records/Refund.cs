namespace Payments.Domain.Records;

/// <summary>Money returned to a buyer against an earlier charge.</summary>
public sealed record Refund : BalanceTransaction
{
    public required string ChargeId { get; init; }
}
