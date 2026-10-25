namespace Payments.Domain.Records;

/// <summary>A completed ticket sale.</summary>
public sealed record Charge : BalanceTransaction
{
    public const string SucceededStatus = "succeeded";

    public required string Status { get; init; }

    public string? OrderRef { get; init; }

    public bool Succeeded => Status == SucceededStatus;
}
