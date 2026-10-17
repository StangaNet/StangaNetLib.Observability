using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StangaNetLib.Observability.Middleware;
using StangaNetLib.Observability.Options;

namespace StangaNetLib.Observability.Extensions;

/// <summary>
/// Extension methods for adding StangaNetLib.Observability middleware to the pipeline.
/// </summary>
public static class ApplicationBuilderExtensions
{
    /// <summary>
    /// Adds all StangaNetLib.Observability middleware in the correct order:
    /// <list type="number">
    ///   <item><see cref="ExceptionHandlingMiddleware"/> — outermost, catches everything</item>
    ///   <item><see cref="SecurityHeadersMiddleware"/> — security headers on every response</item>
    ///   <item><see cref="CorrelationIdMiddleware"/> — generates/propagates correlation ID</item>
    ///   <item><see cref="RequestLoggingMiddleware"/> — logs with correlation ID already available</item>
    ///   <item><see cref="ETagMiddleware"/> — conditional requests</item>
    /// </list>
    /// Individual middleware can be disabled via <see cref="ObservabilityOptions"/>.
    /// </summary>
    /// <example>
    /// <code>
    /// app.UseStangaNetLibObservability();
    /// // then: app.UseAuthentication(); app.UseAuthorization(); etc.
    /// </code>
    /// </example>
    /// <param name="app">The application builder to register middleware into.</param>
    /// <returns>The same <see cref="IApplicationBuilder"/> for chaining.</returns>
    public static IApplicationBuilder UseStangaNetLibObservability(this IApplicationBuilder app)
    {
        var options = app.ApplicationServices.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        if (options.EnableExceptionHandling)
            app.UseMiddleware<ExceptionHandlingMiddleware>();

        if (options.EnableSecurityHeaders)
            app.UseMiddleware<SecurityHeadersMiddleware>();

        // Correlation ID must come before request logging
        app.UseMiddleware<CorrelationIdMiddleware>();

        if (options.EnableRequestLogging)
            app.UseMiddleware<RequestLoggingMiddleware>();

        if (options.EnableETag)
            app.UseMiddleware<ETagMiddleware>();

        return app;
    }
}
