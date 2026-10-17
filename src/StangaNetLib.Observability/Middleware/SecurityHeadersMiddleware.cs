using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using StangaNetLib.Core.Guards;
using StangaNetLib.Observability.Options;

namespace StangaNetLib.Observability.Middleware;

/// <summary>
/// Adds security-related HTTP response headers to every response.
/// Headers are configurable via <see cref="SecurityHeadersOptions"/>.
/// Set any header value to null to omit it.
/// </summary>
public sealed class SecurityHeadersMiddleware
{
    private readonly RequestDelegate _next;
    private readonly SecurityHeadersOptions _options;

    /// <summary>
    /// Initializes the middleware with the pipeline delegate and security header options.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="options">Security header configuration.</param>
    public SecurityHeadersMiddleware(RequestDelegate next, IOptions<SecurityHeadersOptions> options)
    {
        _next = next;
        _options = options.Value;
    }

    /// <summary>
    /// Registers an <c>OnStarting</c> callback that appends configured security headers to the response,
    /// skipping any header already set by the endpoint or a downstream middleware.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        Guard.Against.Null(context, nameof(context));

        context.Response.OnStarting(() =>
        {
            var headers = context.Response.Headers;

            SetIfAbsent(headers, "X-Content-Type-Options", _options.XContentTypeOptions);
            SetIfAbsent(headers, "X-Frame-Options", _options.XFrameOptions);
            SetIfAbsent(headers, "X-XSS-Protection", _options.XXssProtection);
            SetIfAbsent(headers, "Referrer-Policy", _options.ReferrerPolicy);
            SetIfAbsent(headers, "Permissions-Policy", _options.PermissionsPolicy);
            SetIfAbsent(headers, "Strict-Transport-Security", _options.StrictTransportSecurity);
            SetIfAbsent(headers, "Content-Security-Policy", _options.ContentSecurityPolicy);

            foreach (var (key, value) in _options.AdditionalHeaders)
                SetIfAbsent(headers, key, value);

            return Task.CompletedTask;
        });

        await _next(context);
    }

    private static void SetIfAbsent(IHeaderDictionary headers, string name, string? value)
    {
        if (value is not null && !headers.ContainsKey(name))
            headers[name] = value;
    }
}
