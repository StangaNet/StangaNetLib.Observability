using System.Security.Cryptography;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Primitives;
using StangaNetLib.Observability.Attributes;
using StangaNetLib.Observability.Options;

namespace StangaNetLib.Observability.Middleware;

/// <summary>
/// Adds ETag support and conditional request handling to GET and HEAD endpoints.
/// Computes an ETag from the SHA-256 of the response body and returns 304 Not Modified
/// when the client's If-None-Match matches.
/// Responses whose body grows beyond <see cref="ObservabilityOptions.ETagMaxBodySizeBytes"/>
/// are flushed through without an ETag to avoid large memory allocations.
/// Endpoints decorated with <see cref="NoETagAttribute"/> are excluded.
/// Use <c>[NoETag]</c> on streaming or real-time endpoints to skip buffering entirely.
/// </summary>
public sealed class ETagMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ETagMiddleware> _logger;
    private readonly ObservabilityOptions _options;

    /// <summary>
    /// Initializes the middleware with the pipeline delegate, logger, and observability options.
    /// </summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger used to emit ETag cache-hit debug entries.</param>
    /// <param name="options">Observability configuration.</param>
    public ETagMiddleware(
        RequestDelegate next,
        ILogger<ETagMiddleware> logger,
        IOptions<ObservabilityOptions> options)
    {
        _next = next;
        _logger = logger;
        _options = options.Value;
    }

    /// <summary>
    /// Buffers the response body for GET and HEAD requests, computes a SHA-256 ETag,
    /// and returns 304 Not Modified when the client's <c>If-None-Match</c> header matches.
    /// Non-GET/HEAD requests and endpoints marked with <see cref="NoETagAttribute"/> pass through unchanged.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        if (!HttpMethods.IsGet(context.Request.Method) && !HttpMethods.IsHead(context.Request.Method))
        {
            await _next(context);
            return;
        }

        if (context.GetEndpoint()?.Metadata.GetMetadata<NoETagAttribute>() is not null)
        {
            await _next(context);
            return;
        }

        var originalBody = context.Response.Body;
        using var buffer = new MemoryStream();
        context.Response.Body = buffer;

        try
        {
            await _next(context);

            // Skip if the response body grew beyond the threshold during streaming
            if (buffer.Length > _options.ETagMaxBodySizeBytes)
            {
                context.Response.Body = originalBody;
                buffer.Seek(0, SeekOrigin.Begin);
                await buffer.CopyToAsync(originalBody, context.RequestAborted);
                return;
            }

            buffer.Seek(0, SeekOrigin.Begin);
            var responseBytes = buffer.ToArray();

            if (context.Response.StatusCode == StatusCodes.Status200OK && responseBytes.Length > 0)
            {
                var etag = $"\"{ComputeETag(responseBytes)}\"";
                var ifNoneMatch = context.Request.Headers.IfNoneMatch;

                if (!StringValues.IsNullOrEmpty(ifNoneMatch) &&
                    (ifNoneMatch.ToString() == etag || ifNoneMatch.ToString().Trim('"') == etag.Trim('"')))
                {
                    _logger.LogDebug("ETag match for {Path} → 304 Not Modified", context.Request.Path);

                    context.Response.StatusCode = StatusCodes.Status304NotModified;
                    context.Response.ContentLength = 0;
                    context.Response.Headers.ETag = etag;
                    context.Response.Body = originalBody;
                    return;
                }

                context.Response.Headers.ETag = etag;
            }

            context.Response.Body = originalBody;
            buffer.Seek(0, SeekOrigin.Begin);
            await buffer.CopyToAsync(originalBody, context.RequestAborted);
        }
        catch
        {
            context.Response.Body = originalBody;
            throw;
        }
    }

    private static string ComputeETag(byte[] content)
    {
        var hash = SHA256.HashData(content);
        return Convert.ToBase64String(hash)[..22];
    }
}
