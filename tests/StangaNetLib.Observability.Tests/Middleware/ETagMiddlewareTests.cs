using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StangaNetLib.Observability.Attributes;
using StangaNetLib.Observability.Extensions;

namespace StangaNetLib.Observability.Tests.Middleware;

public sealed class ETagMiddlewareTests
{
    private static IHost BuildHost(string responseBody = "Hello World", Action<IApplicationBuilder>? extra = null)
    {
        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddStangaNetLibObservability(opt =>
                    {
                        opt.EnableRequestLogging  = false;
                        opt.EnableSecurityHeaders = false;
                        opt.EnableExceptionHandling = false;
                        opt.EnableETag = true;
                    });
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseStangaNetLibObservability();
                    extra?.Invoke(app);
                    app.Run(async ctx =>
                    {
                        ctx.Response.StatusCode  = 200;
                        ctx.Response.ContentType = "text/plain";
                        await ctx.Response.WriteAsync(responseBody);
                    });
                });
            })
            .Build();
    }

    // ── GET → ETag header is set ──────────────────────────────────────────

    [Fact]
    public async Task Get_Response_AddsETagHeader()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost();
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.Headers.ETag.Should().NotBeNull();
        response.Headers.ETag!.Tag.Should().NotBeNullOrEmpty();
    }

    // ── Same content → same ETag ──────────────────────────────────────────

    [Fact]
    public async Task Get_SameContent_ProducesSameETag()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost("Deterministic body");
        await host.StartAsync(ct);

        var client = host.GetTestServer().CreateClient();
        var r1 = await client.GetAsync("/", ct);
        var r2 = await client.GetAsync("/", ct);

        r1.Headers.ETag!.Tag.Should().Be(r2.Headers.ETag!.Tag);
    }

    // ── If-None-Match match → 304 ────────────────────────────────────────

    [Fact]
    public async Task Get_MatchingIfNoneMatch_Returns304()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost("Cacheable body");
        await host.StartAsync(ct);

        var client  = host.GetTestServer().CreateClient();
        var first   = await client.GetAsync("/", ct);
        var etag    = first.Headers.ETag!.Tag;

        var request = new HttpRequestMessage(HttpMethod.Get, "/");
        request.Headers.IfNoneMatch.ParseAdd(etag);
        var second  = await client.SendAsync(request, ct);

        second.StatusCode.Should().Be(System.Net.HttpStatusCode.NotModified);
    }

    // ── POST → no ETag ────────────────────────────────────────────────────

    [Fact]
    public async Task Post_NoETagHeader()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost();
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient()
            .PostAsync("/", new StringContent("data"), ct);

        response.Headers.ETag.Should().BeNull();
    }

    // ── [NoETag] endpoint → no ETag ───────────────────────────────────────

    [Fact]
    public async Task Get_NoETagAttribute_SkipsETag()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.ConfigureServices(services =>
                {
                    services.AddRouting();
                    services.AddStangaNetLibObservability(opt =>
                    {
                        opt.EnableRequestLogging  = false;
                        opt.EnableSecurityHeaders = false;
                        opt.EnableExceptionHandling = false;
                        opt.EnableETag = true;
                    });
                });
                web.Configure(app =>
                {
                    app.UseRouting();
                    app.UseStangaNetLibObservability();
                    app.UseEndpoints(ep => ep.MapGet("/noetag",
                        [NoETag] async (HttpContext ctx) =>
                        {
                            ctx.Response.ContentType = "text/plain";
                            await ctx.Response.WriteAsync("streaming data");
                        }));
                });
            })
            .Build();

        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/noetag", ct);

        response.Headers.ETag.Should().BeNull();
    }
}
