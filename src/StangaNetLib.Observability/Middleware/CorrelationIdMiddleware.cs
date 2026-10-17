using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Guards;
using StangaNetLib.Observability.Correlation;
using StangaNetLib.Observability.Options;

namespace StangaNetLib.Observability.Middleware;

/// <summary>
/// Generates or propagates a correlation ID for each HTTP request.
/// Resolution order:
///   1. Existing <see cref="ObservabilityOptions.CorrelationIdHeader"/> from the incoming request.
///   2. <see cref="Activity.Current"/> TraceId when OpenTelemetry / distributed tracing is active.
///   3. A newly generated short Guid.
/// The ID and any active <c>TraceId</c>/<c>SpanId</c> are pushed to the ILogger scope so they
/// appear in all log entries for this request. The ID is also written to the response header.
/// </summary>
public sealed class CorrelationIdMiddleware
{
    private const int MaxClientCorrelationIdLength = 64;

    private readonly RequestDelegate _next;
    private readonly ObservabilityOptions _options;

    /// <summary>
    /// Initializes the middleware with the pipeline delegate and observability options.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">Observability configuration.</param>
    public CorrelationIdMiddleware(RequestDelegate next, IOptions<ObservabilityOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    /// <summary>
    /// Resolves the correlation ID for the request, stores it in <see cref="ICorrelationIdAccessor"/>,
    /// adds it to the response header, and pushes it to the <see cref="ILogger"/> scope.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        Guard.Against.Null(context, nameof(context));
        var correlationId = ResolveCorrelationId(context);
        // CorrelationIdAccessor is internal — resolve directly to set the value
        var accessor = context.RequestServices.GetRequiredService<CorrelationIdAccessor>();
        accessor.CorrelationId = correlationId;

        context.Response.OnStarting(() =>
        {
            if (!context.Response.Headers.ContainsKey(_options.CorrelationIdHeader))
                context.Response.Headers[_options.CorrelationIdHeader] = correlationId;
            return Task.CompletedTask;
        });

        var scope = BuildLogScope(correlationId);
        using (context.RequestServices
                   .GetRequiredService<ILogger<CorrelationIdMiddleware>>()
                   .BeginScope(scope))
        {
            await _next(context);
        }
    }

    private string ResolveCorrelationId(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(_options.CorrelationIdHeader, out var existing)
            && !string.IsNullOrWhiteSpace(existing))
        {
            var value = existing.ToString();
            return value.Length > MaxClientCorrelationIdLength
                ? value[..MaxClientCorrelationIdLength]
                : value;
        }

        if (Activity.Current is { } activity && activity.TraceId != default)
            return activity.TraceId.ToString();

        var len = Math.Clamp(_options.GeneratedCorrelationIdLength, 8, 32);
        return Guid.NewGuid().ToString("N")[..len];
    }

    private static Dictionary<string, object> BuildLogScope(string correlationId)
    {
        var scope = new Dictionary<string, object> { ["CorrelationId"] = correlationId };

        if (Activity.Current is { } activity)
        {
            scope["TraceId"] = activity.TraceId.ToString();
            scope["SpanId"] = activity.SpanId.ToString();
        }

        return scope;
    }
}
