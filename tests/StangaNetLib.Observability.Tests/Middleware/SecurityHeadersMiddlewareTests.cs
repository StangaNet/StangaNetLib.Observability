using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StangaNetLib.Observability.Extensions;
using StangaNetLib.Observability.Options;

namespace StangaNetLib.Observability.Tests.Middleware;

public sealed class SecurityHeadersMiddlewareTests
{
    private static IHost BuildHost(Action<SecurityHeadersOptions>? configureHeaders = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddStangaNetLibObservability(
                        opt =>
                        {
                            opt.EnableRequestLogging  = false;
                            opt.EnableETag            = false;
                            opt.EnableExceptionHandling = false;
                            opt.EnableSecurityHeaders = true;
                        },
                        headerOpt => configureHeaders?.Invoke(headerOpt));
                });
                web.Configure(app =>
                {
                    app.UseStangaNetLibObservability();
                    app.Run(ctx => { ctx.Response.StatusCode = 200; return Task.CompletedTask; });
                });
            })
            .Build();
    }

    // ── Default headers ───────────────────────────────────────────────────

    [Fact]
    public async Task Response_IncludesXContentTypeOptions()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost();
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.Headers.GetValues("X-Content-Type-Options").Single()
            .Should().Be("nosniff");
    }

    [Fact]
    public async Task Response_IncludesXFrameOptions()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost();
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.Headers.GetValues("X-Frame-Options").Single()
            .Should().Be("DENY");
    }

    [Fact]
    public async Task Response_IncludesXXssProtection()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost();
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.Headers.GetValues("X-XSS-Protection").Single()
            .Should().Be("1; mode=block");
    }

    [Fact]
    public async Task Response_IncludesReferrerPolicy()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost();
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.Headers.GetValues("Referrer-Policy").Single()
            .Should().Be("strict-origin-when-cross-origin");
    }

    // ── Null header value → header omitted ───────────────────────────────

    [Fact]
    public async Task Response_NullHeaderValue_OmitsHeader()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(opt => opt.XFrameOptions = null);
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.Headers.Contains("X-Frame-Options").Should().BeFalse();
    }

    // ── HSTS enabled ──────────────────────────────────────────────────────

    [Fact]
    public async Task Response_HstsEnabled_IncludesStrictTransportSecurity()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(opt =>
            opt.StrictTransportSecurity = "max-age=31536000; includeSubDomains");
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.Headers.GetValues("Strict-Transport-Security").Single()
            .Should().Contain("max-age=31536000");
    }

    // ── Additional headers ────────────────────────────────────────────────

    [Fact]
    public async Task Response_AdditionalHeader_IsIncluded()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(opt =>
            opt.AdditionalHeaders["X-Custom-Header"] = "custom-value");
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.Headers.GetValues("X-Custom-Header").Single()
            .Should().Be("custom-value");
    }
}
