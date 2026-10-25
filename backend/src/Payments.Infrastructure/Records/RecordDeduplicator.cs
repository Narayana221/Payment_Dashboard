using System.Text.Json;
using Payments.Domain.DataQuality;

namespace Payments.Infrastructure.Records;

/// <summary>
/// Delivery was not reliable, so the same record can arrive more than once (decision A1).
/// Identical copies are counted once; copies that disagree are all excluded, because picking one would hide the conflict.
/// </summary>
internal static class RecordDeduplicator
{
    public static IReadOnlyList<JsonElement> Deduplicate(IReadOnlyList<JsonElement> elements, ICollection<DataQualityIssue> issues)
    {
        var groups = new Dictionary<string, List<JsonElement>>(StringComparer.Ordinal);
        var firstSeenOrder = new List<string>();

        for (var position = 0; position < elements.Count; position++)
        {
            var element = elements[position];
            if (element.ValueKind != JsonValueKind.Object
                || !element.TryGetProperty("id", out var idProperty)
                || idProperty.ValueKind != JsonValueKind.String
                || string.IsNullOrWhiteSpace(idProperty.GetString()))
            {
                issues.Add(new DataQualityIssue(DataQualityIssueType.MalformedRecord,
                    $"Record at position {position} has no id and was ignored.", [], ExcludedFromTotals: true));
                continue;
            }

            var id = idProperty.GetString()!;
            if (!groups.TryGetValue(id, out var copies))
            {
                groups[id] = copies = [];
                firstSeenOrder.Add(id);
            }

            copies.Add(element);
        }

        var unique = new List<JsonElement>();
        foreach (var id in firstSeenOrder)
        {
            var copies = groups[id];
            if (copies.Count == 1)
            {
                unique.Add(copies[0]);
            }
            else if (copies.All(copy => JsonElement.DeepEquals(copy, copies[0])))
            {
                unique.Add(copies[0]);
                issues.Add(new DataQualityIssue(DataQualityIssueType.DuplicateRecord,
                    $"Record {id} was delivered {copies.Count} times with identical content; it is counted once.", [id],
                    ExcludedFromTotals: false));
            }
            else
            {
                issues.Add(new DataQualityIssue(DataQualityIssueType.ConflictingDuplicate,
                    $"Record {id} was delivered {copies.Count} times with different content, so none of the copies is counted.", [id],
                    ExcludedFromTotals: true));
            }
        }

        return unique;
    }
}
