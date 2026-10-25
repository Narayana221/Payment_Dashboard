namespace Payments.Application.Shared.Dtos;

/// <param name="Type">camelCase issue type, e.g. "duplicateRecord".</param>
/// <param name="ExcludedFromTotals">True when the records were left out of every figure; false when kept with a warning.</param>
public sealed record DataQualityIssueDto(string Type, string Message, IReadOnlyList<string> RecordIds, bool ExcludedFromTotals);
