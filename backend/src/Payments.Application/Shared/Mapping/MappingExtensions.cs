using System.Text.Json;
using Payments.Application.Shared.Dtos;
using Payments.Domain.DataQuality;
using Payments.Domain.Shared;

namespace Payments.Application.Shared.Mapping;

/// <summary>Domain → contract mapping for the cross-cutting types. Domain enums leave as camelCase strings, never as numbers.</summary>
internal static class MappingExtensions
{
    public static MoneyDto ToDto(this Money money) => new(money.MinorUnits, money.Currency.Code);

    public static DataQualityIssueDto ToDto(this DataQualityIssue issue)
    {
        ArgumentNullException.ThrowIfNull(issue);
        return new DataQualityIssueDto(ContractName(issue.Type), issue.Message, issue.RecordIds, issue.ExcludedFromTotals);
    }

    public static string ContractName<TEnum>(TEnum value)
        where TEnum : struct, Enum =>
        JsonNamingPolicy.CamelCase.ConvertName(value.ToString());
}
