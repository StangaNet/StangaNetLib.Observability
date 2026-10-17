using FluentAssertions;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using StangaNetLib.Core.Exceptions;
using StangaNetLib.Observability.Extensions;
using System.Net;
using System.Text.Json;

namespace StangaNetLib.Observability.Tests.Middleware;

// Concrete DomainException needed for tests (it's abstract in Core)
file sealed class TestDomainException : DomainException
{
    public TestDomainException(string message) : base(message) { }
}

public sealed class ExceptionHandlingMiddlewareTests
{
    private static IHost BuildHost(
        RequestDelegate? handler  = null,
        bool includeDetails       = false)
    {
        return new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.UseEnvironment("Production");
                web.ConfigureServices(services =>
                {
                    services.AddStangaNetLibObservability(opt =>
                    {
                        opt.EnableRequestLogging  = false;
                        opt.EnableETag            = false;
                        opt.EnableSecurityHeaders = false;
                        opt.EnableExceptionHandling = true;
                        opt.IncludeExceptionDetailsInResponse = includeDetails;
                    });
                });
                web.Configure(app =>
                {
                    app.UseStangaNetLibObservability();
                    app.Run(handler ?? (_ => Task.CompletedTask));
                });
            })
            .Build();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response)
    {
        var body = await response.Content.ReadAsStringAsync();
        return JsonDocument.Parse(body);
    }

    // ── ArgumentException → 400 ───────────────────────────────────────────

    [Fact]
    public async Task ArgumentException_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(_ => throw new ArgumentException("bad arg"));
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
    }

    // ── KeyNotFoundException → 404 ────────────────────────────────────────

    [Fact]
    public async Task KeyNotFoundException_Returns404()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(_ => throw new KeyNotFoundException("not found"));
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    // ── UnauthorizedAccessException → 401 ────────────────────────────────

    [Fact]
    public async Task UnauthorizedAccessException_Returns401()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(_ => throw new UnauthorizedAccessException());
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    // ── DomainException → 400 ─────────────────────────────────────────────

    [Fact]
    public async Task DomainException_Returns400()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(_ => throw new TestDomainException("domain rule violated"));
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    // ── Generic exception → 500 ───────────────────────────────────────────

    [Fact]
    public async Task GenericException_Returns500()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(_ => throw new InvalidOperationException("internal"));
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    // ── Response includes correlationId ───────────────────────────────────

    [Fact]
    public async Task Exception_ResponseBody_ContainsCorrelationId()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(_ => throw new Exception("boom"));
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        using var doc = await ReadJsonAsync(response);
        doc.RootElement.GetProperty("correlationId").GetString()
            .Should().NotBeNullOrEmpty();
    }

    // ── Production: no stack trace exposed ───────────────────────────────

    [Fact]
    public async Task Production_NoDetails_OmitsStackTrace()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(_ => throw new Exception("boom"), includeDetails: false);
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);
        var body     = await response.Content.ReadAsStringAsync(ct);

        body.Should().NotContain("stackTrace");
        body.Should().NotContain("exceptionType");
    }

    // ── IncludeDetails=true: exception type exposed ───────────────────────

    [Fact]
    public async Task IncludeDetails_True_ExposesExceptionType()
    {
        var ct = TestContext.Current.CancellationToken;
        using var host = BuildHost(_ => throw new InvalidOperationException("detailed"), includeDetails: true);
        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);
        var body     = await response.Content.ReadAsStringAsync(ct);

        body.Should().Contain("exceptionType");
        body.Should().Contain("InvalidOperationException");
    }

    // ── Custom exception → custom status code ────────────────────────────

    [Fact]
    public async Task CustomExceptionMapping_ReturnsConfiguredStatusCode()
    {
        var ct = TestContext.Current.CancellationToken;

        using var host = new HostBuilder()
            .ConfigureWebHost(web =>
            {
                web.UseTestServer();
                web.UseEnvironment("Production");
                web.ConfigureServices(services =>
                {
                    services.AddStangaNetLibObservability(opt =>
                    {
                        opt.EnableRequestLogging  = false;
                        opt.EnableETag            = false;
                        opt.EnableSecurityHeaders = false;
                        opt.EnableExceptionHandling = true;
                        opt.CustomExceptionStatusCodes[typeof(NotSupportedException)] = 418;
                    });
                });
                web.Configure(app =>
                {
                    app.UseStangaNetLibObservability();
                    app.Run(_ => throw new NotSupportedException());
                });
            })
            .Build();

        await host.StartAsync(ct);

        var response = await host.GetTestServer().CreateClient().GetAsync("/", ct);

        ((int)response.StatusCode).Should().Be(418);
    }
}
