using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text.RegularExpressions;

namespace Payments.Api.Shared.Validation;

/// <summary>
/// Parses the <c>asOf</c> query parameter. Like the records file, it must carry an explicit UTC offset,
/// so the same request means the same instant whichever server answers it.
/// </summary>
public static partial class AsOfParameter
{
    private static readonly DateTimeOffset Earliest = new(2000, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly DateTimeOffset Latest = new(2100, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <param name="instant">The parsed instant, or <c>null</c> when <paramref name="value"/> is absent (meaning "now").</param>
    public static bool TryParse(string? value, out DateTimeOffset? instant, [NotNullWhen(false)] out string? error)
    {
        instant = null;
        error = null;

        if (string.IsNullOrEmpty(value))
        {
            return true;
        }

        if (!ExplicitOffset().IsMatch(value)
            || !DateTimeOffset.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            error = "asOf must be an ISO 8601 date and time with a UTC offset, e.g. 2026-06-21T00:00:00Z.";
            return false;
        }

        if (parsed < Earliest || parsed >= Latest)
        {
            error = "asOf must be between the years 2000 and 2099.";
            return false;
        }

        instant = parsed.ToUniversalTime();
        return true;
    }

    [GeneratedRegex(@"^\d{4}-\d{2}-\d{2}T[\d:.]+(Z|[+-]\d{2}:\d{2})$", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitOffset();
}
