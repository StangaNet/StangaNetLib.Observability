namespace StangaNetLib.Observability.Options;

/// <summary>
/// Configures the StangaNetLib.Observability middleware stack.
/// </summary>
public class ObservabilityOptions
{
    /// <summary>
    /// Name of the HTTP header used to propagate the correlation ID.
    /// Default: "X-Correlation-Id".
    /// </summary>
    public string CorrelationIdHeader { get; set; } = "X-Correlation-Id";

    /// <summary>
    /// Length of the generated correlation ID when not provided by the client.
    /// Default: 12 characters (hex substring of a Guid).
    /// </summary>
    public int GeneratedCorrelationIdLength { get; set; } = 12;

    /// <summary>
    /// Whether to enable the <see cref="StangaNetLib.Observability.Middleware.RequestLoggingMiddleware"/>.
    /// Default: true.
    /// </summary>
    public bool EnableRequestLogging { get; set; } = true;

    /// <summary>
    /// Whether to enable the <see cref="StangaNetLib.Observability.Middleware.ETagMiddleware"/>.
    /// Default: true.
    /// </summary>
    public bool EnableETag { get; set; } = true;

    /// <summary>
    /// Whether to enable the <see cref="StangaNetLib.Observability.Middleware.SecurityHeadersMiddleware"/>.
    /// Default: true.
    /// </summary>
    public bool EnableSecurityHeaders { get; set; } = true;

    /// <summary>
    /// Whether to enable the <see cref="StangaNetLib.Observability.Middleware.ExceptionHandlingMiddleware"/>.
    /// Default: true.
    /// </summary>
    public bool EnableExceptionHandling { get; set; } = true;

    /// <summary>
    /// When true, exception details (message, stack trace) are included in error responses.
    /// Should be false in production. Automatically overridden to true in Development environment.
    /// Default: false.
    /// </summary>
    public bool IncludeExceptionDetailsInResponse { get; set; } = false;

    /// <summary>
    /// Requests whose elapsed time exceeds this threshold are logged at Warning level.
    /// Set to 0 to disable. Default: 2000 ms.
    /// </summary>
    public int SlowRequestThresholdMs { get; set; } = 2000;

    /// <summary>
    /// When true, the query string is appended to request log entries (parameter names only; values are masked).
    /// Default: false.
    /// </summary>
    public bool LogQueryString { get; set; } = false;

    /// <summary>
    /// Maximum response body size (in bytes) for which the ETag middleware will compute a hash.
    /// Responses larger than this threshold bypass buffering. Default: 262144 (256 KB).
    /// </summary>
    public int ETagMaxBodySizeBytes { get; set; } = 256 * 1024;

    /// <summary>
    /// Request paths that are excluded from request logging.
    /// Comparison is case-insensitive prefix match.
    /// Default: <c>/health</c>, <c>/healthz</c>, <c>/metrics</c>.
    /// </summary>
    public IList<string> ExcludedLogPaths { get; set; } = ["/health", "/healthz", "/metrics"];

    /// <summary>
    /// Additional exception-to-HTTP-status-code mappings used by <see cref="StangaNetLib.Observability.Middleware.ExceptionHandlingMiddleware"/>.
    /// Custom mappings take precedence over the built-in ones.
    /// Example: <c>{ [typeof(MyNotFoundException)] = 404 }</c>.
    /// </summary>
    public Dictionary<Type, int> CustomExceptionStatusCodes { get; set; } = [];
}
