namespace Payments.Infrastructure.Records;

/// <summary>
/// The raw shape of one entry in records.json (snake_case on the wire). Every field and list element is optional here,
/// because System.Text.Json does not enforce nullability; <see cref="RecordJsonMapper"/> decides what each type requires.
/// </summary>
internal sealed record RecordJson
{
    public string? Id { get; init; }

    public string? Object { get; init; }

    /// <summary>Kept as text so a timestamp without a UTC offset can be rejected rather than read in local time.</summary>
    public string? Created { get; init; }

    public long? Amount { get; init; }

    public string? Currency { get; init; }

    public string? Status { get; init; }

    public string? OrderRef { get; init; }

    public IReadOnlyList<FeeJson?>? FeeDetails { get; init; }

    public long? Net { get; init; }

    public DateOnly? AvailableOn { get; init; }

    public string? Charge { get; init; }

    public DateOnly? ArrivalDate { get; init; }

    public IReadOnlyList<string?>? IncludedBalanceTransactions { get; init; }
}

internal sealed record FeeJson
{
    public string? Type { get; init; }

    public long? Amount { get; init; }
}
