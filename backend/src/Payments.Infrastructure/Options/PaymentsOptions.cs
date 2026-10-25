namespace Payments.Infrastructure.Options;

/// <summary>The <c>Payments</c> configuration section.</summary>
public sealed class PaymentsOptions
{
    public const string SectionName = "Payments";

    /// <summary>The one organiser this extract belongs to. Production would map organisers to their own data.</summary>
    public string OrganiserId { get; set; } = string.Empty;

    /// <summary>Path to records.json; a relative path is resolved against the app's content root.</summary>
    public string RecordsPath { get; set; } = string.Empty;

    /// <summary>The file is untrusted input; refuse anything larger than this.</summary>
    public long MaxFileBytes { get; set; } = 10 * 1024 * 1024;
}
