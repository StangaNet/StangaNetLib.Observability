using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StangaNetLib.Observability.Correlation;
using StangaNetLib.Observability.Options;

namespace StangaNetLib.Observability.Middleware;

/// <summary>
/// Logs every HTTP request with: method, path, status code, elapsed milliseconds, and correlation ID.
/// Logs at Warning if the response status is 5xx or if elapsed time exceeds
/// <see cref="ObservabilityOptions.SlowRequestThresholdMs"/>.
/// Query string parameter names (not values) are included when
/// <see cref="ObservabilityOptions.LogQueryString"/> is true.
/// Must be placed after <see cref="CorrelationIdMiddleware"/> in the pipeline.
/// </summary>
public sealed class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;
    private readonly ObservabilityOptions _options;

    /// <summary>
    /// Initializes the middleware with the pipeline delegate, logger, and observability options.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger used to emit request/response entries.</param>
    /// <param name="options">Observability configuration.</param>
    public RequestLoggingMiddleware(
        RequestDelegate next,
        ILogger<RequestLoggingMiddleware> logger,
        IOptions<ObservabilityOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Logs the incoming request and the outgoing response. Skips paths listed in
    /// <see cref="ObservabilityOptions.ExcludedLogPaths"/>.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="correlationIdAccessor">Provides the correlation ID resolved earlier in the pipeline.</param>
    public async Task InvokeAsync(HttpContext context, ICorrelationIdAccessor correlationIdAccessor)
    {
        if (IsExcluded(context.Request.Path))
        {
            await _next(context);
            return;
        }

        var correlationId = correlationIdAccessor.CorrelationId ?? "-";
        var path = BuildPathForLog(context);

        _logger.LogInformation(
            "→ {Method} {Path} [{CorrelationId}]",
            context.Request.Method, path, correlationId);

        var sw = Stopwatch.StartNew();
        try
        {
            await _next(context);
        }
        finally
        {
            sw.Stop();
            var statusCode = context.Response.StatusCode;
            var isServerError = statusCode >= 500;
            var isSlow = _options.SlowRequestThresholdMs > 0
                         && sw.ElapsedMilliseconds >= _options.SlowRequestThresholdMs;

            var level = isServerError || isSlow ? LogLevel.Warning : LogLevel.Information;

            if (isSlow)
                _logger.Log(level,
                    "← {Method} {Path} {StatusCode} in {ElapsedMs}ms [SLOW] [{CorrelationId}]",
                    context.Request.Method, path, statusCode, sw.ElapsedMilliseconds, correlationId);
            else
                _logger.Log(level,
                    "← {Method} {Path} {StatusCode} in {ElapsedMs}ms [{CorrelationId}]",
                    context.Request.Method, path, statusCode, sw.ElapsedMilliseconds, correlationId);
        }
    }

    private bool IsExcluded(PathString path) =>
        _options.ExcludedLogPaths.Any(p =>
            path.StartsWithSegments(p, StringComparison.OrdinalIgnoreCase));

    private string BuildPathForLog(HttpContext context)
    {
        var path = context.Request.Path.Value ?? "/";

        if (!_options.LogQueryString || !context.Request.QueryString.HasValue)
            return path;

        // Log parameter names only — values are omitted to avoid accidental PII/token exposure
        var keys = context.Request.Query.Keys;
        var maskedQuery = string.Join("&", keys.Select(k => $"{k}=***"));
        return $"{path}?{maskedQuery}";
    }
}
