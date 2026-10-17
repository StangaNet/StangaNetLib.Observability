using Microsoft.Extensions.DependencyInjection;
using StangaNetLib.Observability.Correlation;
using StangaNetLib.Observability.Masking;
using StangaNetLib.Observability.Options;

namespace StangaNetLib.Observability.Extensions;

/// <summary>
/// Extension methods for registering StangaNetLib.Observability services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all StangaNetLib.Observability services into the DI container.
    /// Call this in Program.cs before <c>builder.Build()</c>.
    /// Options can also be bound from <c>appsettings.json</c> via
    /// <c>services.Configure&lt;ObservabilityOptions&gt;(config.GetSection("Observability"))</c>
    /// before or after calling this method.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.AddStangaNetLibObservability();
    /// // or with custom options:
    /// builder.Services.AddStangaNetLibObservability(opt =>
    /// {
    ///     opt.CorrelationIdHeader = "X-Request-Id";
    ///     opt.EnableETag = false;
    /// });
    /// </code>
    /// </example>
    /// <param name="services">The service collection to register into.</param>
    /// <param name="configureOptions">Optional delegate to configure <see cref="ObservabilityOptions"/>.</param>
    /// <param name="configureSecurityHeaders">Optional delegate to configure <see cref="SecurityHeadersOptions"/>.</param>
    /// <returns>The same <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddStangaNetLibObservability(
        this IServiceCollection services,
        Action<ObservabilityOptions>? configureOptions = null,
        Action<SecurityHeadersOptions>? configureSecurityHeaders = null)
    {
        if (configureOptions is not null)
            services.Configure<ObservabilityOptions>(configureOptions);
        else
            services.Configure<ObservabilityOptions>(_ => { });

        if (configureSecurityHeaders is not null)
            services.Configure<SecurityHeadersOptions>(configureSecurityHeaders);
        else
            services.Configure<SecurityHeadersOptions>(_ => { });

        services.AddSingleton<ILogMaskingService, LogMaskingService>();

        services.AddScoped<CorrelationIdAccessor>();
        services.AddScoped<ICorrelationIdAccessor>(sp => sp.GetRequiredService<CorrelationIdAccessor>());

        return services;
    }
}
