namespace StangaNetLib.Observability.Options;

/// <summary>
/// Configures the security headers added by <see cref="StangaNetLib.Observability.Middleware.SecurityHeadersMiddleware"/>.
/// Set any property to <c>null</c> to omit that header from the response.
/// </summary>
public class SecurityHeadersOptions
{
    /// <summary>X-Content-Type-Options. Default: "nosniff".</summary>
    public string? XContentTypeOptions { get; set; } = "nosniff";

    /// <summary>X-Frame-Options. Default: "DENY".</summary>
    public string? XFrameOptions { get; set; } = "DENY";

    /// <summary>X-XSS-Protection. Default: "1; mode=block".</summary>
    public string? XXssProtection { get; set; } = "1; mode=block";

    /// <summary>Referrer-Policy. Default: "strict-origin-when-cross-origin".</summary>
    public string? ReferrerPolicy { get; set; } = "strict-origin-when-cross-origin";

    /// <summary>Permissions-Policy. Default: disables geolocation, microphone, camera.</summary>
    public string? PermissionsPolicy { get; set; } = "geolocation=(), microphone=(), camera=()";

    /// <summary>
    /// Strict-Transport-Security (HSTS). Default: <c>null</c> (omitted).
    /// Set to e.g. <c>"max-age=31536000; includeSubDomains"</c> for HTTPS-only apps.
    /// Do not enable in Development — it permanently forces HTTPS in the browser.
    /// </summary>
    public string? StrictTransportSecurity { get; set; } = null;

    /// <summary>
    /// Content-Security-Policy. Default: <c>null</c> (omitted).
    /// Highly app-specific; set a restrictive policy for production, e.g.
    /// <c>"default-src 'self'; object-src 'none'; frame-ancestors 'none'"</c>.
    /// </summary>
    public string? ContentSecurityPolicy { get; set; } = null;

    /// <summary>
    /// Additional custom headers to add. Key = header name, Value = header value.
    /// </summary>
    public Dictionary<string, string> AdditionalHeaders { get; set; } = [];
}
