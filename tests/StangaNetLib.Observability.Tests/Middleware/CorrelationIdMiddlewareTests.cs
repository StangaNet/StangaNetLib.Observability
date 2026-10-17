using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StangaNetLib.Observability.Extensions;

namespace StangaNetLib.Observability.Tests.Middleware;

public class CorrelationIdMiddlewareTests
{
    private static IHost BuildHost(Action<IApplicationBuilder>? configure = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(webBuilder =>
            {
                webBuilder.UseTestServer();
                webBuilder.ConfigureServices(services =>
                {
                    services.AddStangaNetLibObservability(opt =>
                    {
                        opt.EnableRequestLogging = false;
                        opt.EnableETag = false;
                        opt.EnableSecurityHeaders = false;
                        opt.EnableExceptionHandling = false;
                    });
                });
                webBuilder.Configure(app =>
                {
                    app.UseStangaNetLibObservability();
                    configure?.Invoke(app);
                    app.Run(ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; });
                });
            })
            .Build();
    }

    [Fact]
    public async Task Request_WithoutCorrelationId_ShouldAddOneToResponse()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost();
        await host.StartAsync(ct);

        var client = host.GetTestServer().CreateClient();
        var response = await client.GetAsync("/test", ct);

        response.Headers.Contains("X-Correlation-Id").Should().BeTrue();
        response.Headers.GetValues("X-Correlation-Id").Single().Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Request_WithExistingCorrelationId_ShouldEchoItBack()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost();
        await host.StartAsync(ct);

        var client = host.GetTestServer().CreateClient();
        client.DefaultRequestHeaders.Add("X-Correlation-Id", "my-custom-id-123");

        var response = await client.GetAsync("/test", ct);
        var correlationId = response.Headers.GetValues("X-Correlation-Id").Single();

        correlationId.Should().Be("my-custom-id-123");
    }

    [Fact]
    public async Task TwoRequests_WithoutCorrelationId_ShouldGetDifferentIds()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost();
        await host.StartAsync(ct);

        var client = host.GetTestServer().CreateClient();
        var r1 = await client.GetAsync("/test", ct);
        var r2 = await client.GetAsync("/test", ct);

        var id1 = r1.Headers.GetValues("X-Correlation-Id").Single();
        var id2 = r2.Headers.GetValues("X-Correlation-Id").Single();

        id1.Should().NotBe(id2);
    }
}
