namespace Payments.Api.Shared.Options;

/// <summary>The browser origin allowed to call this API (<c>Payments:AllowedOrigin</c>). An HTTP-edge concern, so it lives here.</summary>
public sealed class FrontendOptions
{
    public string AllowedOrigin { get; set; } = string.Empty;

    /// <summary>
    /// True for a bare origin such as <c>http://localhost:4200</c>. A browser's Origin header never has a path or trailing
    /// slash, so anything else would silently match no request.
    /// </summary>
    public bool HasValidOrigin() =>
        Uri.TryCreate(AllowedOrigin, UriKind.Absolute, out var uri)
        && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps)
        && string.Equals(uri.GetLeftPart(UriPartial.Authority), AllowedOrigin, StringComparison.OrdinalIgnoreCase);
}
