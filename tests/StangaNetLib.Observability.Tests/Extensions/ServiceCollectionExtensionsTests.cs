using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using StangaNetLib.Observability.Correlation;
using StangaNetLib.Observability.Extensions;
using StangaNetLib.Observability.Masking;
using StangaNetLib.Observability.Options;

namespace StangaNetLib.Observability.Tests.Extensions;

public sealed class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddStangaNetLibObservability_RegistersExpectedServices()
    {
        var services = new ServiceCollection();
        services.AddStangaNetLibObservability();

        using var provider = services.BuildServiceProvider();

        provider.GetRequiredService<ILogMaskingService>().Should().BeOfType<LogMaskingService>();
        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<ICorrelationIdAccessor>().Should().NotBeNull();
    }

    [Fact]
    public void AddStangaNetLibObservability_WithOptions_AppliesConfiguration()
    {
        var services = new ServiceCollection();
        services.AddStangaNetLibObservability(opt =>
        {
            opt.CorrelationIdHeader = "X-Request-Id";
            opt.EnableETag = false;
        });

        using var provider = services.BuildServiceProvider();
        var options = provider.GetRequiredService<IOptions<ObservabilityOptions>>().Value;

        options.CorrelationIdHeader.Should().Be("X-Request-Id");
        options.EnableETag.Should().BeFalse();
    }
}
