# StangaNetLib.Observability

[![.NET CI](https://github.com/StangaNet/StangaNetLib.Observability/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.Observability/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0-512BD4)

StangaNetLib.Observability is an ASP.NET Core middleware suite for correlation IDs, structured request logging, global exception handling (RFC 7807 ProblemDetails), ETag conditional requests, security headers, and PII log masking. It integrates with `StangaNetLib.Core` (`Result<T>`, `Guard`, `DomainException`).

## Design Philosophy

- **Domain-Centric**: Cross-cutting concerns organized by purpose (Correlation, Masking, Middleware) rather than technical layers.
- **Functional Error Handling**: PII masking APIs return `Result<T>` from `StangaNetLib.Core`.
- **Defensive Programming**: Uses `Guard.Against` from Core for input validation.
- **Modern Standards**: Optimized for .NET 8.0 and 9.0 with OpenTelemetry-friendly correlation (Activity TraceId).

## Installation

The package is hosted on **GitHub Packages**.

```xml
<!-- NuGet.config — add the GitHub Packages source -->
<configuration>
  <packageSources>
    <add key="github" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- .csproj -->
<PackageReference Include="StangaNetLib.Observability" Version="1.0.0" />
```

---

## Core Components

*The library is organized into functional namespaces. For implementation details, refer directly to the source code.*

| Namespace | Core Components | Purpose |
| :--- | :--- | :--- |
| `StangaNetLib.Observability.Middleware` | `CorrelationIdMiddleware`, `ExceptionHandlingMiddleware`, `RequestLoggingMiddleware`, `ETagMiddleware`, `SecurityHeadersMiddleware` | HTTP pipeline cross-cutting concerns. |
| `StangaNetLib.Observability.Correlation` | `ICorrelationIdAccessor` | Per-request correlation ID access. |
| `StangaNetLib.Observability.Masking` | `ILogMaskingService`, `LogMaskingService` | PII masking for logs (email, IP, phone, name). |
| `StangaNetLib.Observability.Options` | `ObservabilityOptions`, `SecurityHeadersOptions` | Configuration models. |
| `StangaNetLib.Observability.Extensions` | `ServiceCollectionExtensions`, `ApplicationBuilderExtensions` | DI and pipeline registration. |

---

## Project Structure

```
StangaNetLib.Observability/
├── src/
│   └── StangaNetLib.Observability/
│       ├── Attributes/       # NoETagAttribute
│       ├── Correlation/      # ICorrelationIdAccessor
│       ├── Extensions/       # DI and pipeline registration
│       ├── Masking/          # PII log masking
│       ├── Middleware/       # HTTP middleware
│       └── Options/          # Configuration
└── tests/
    └── StangaNetLib.Observability.Tests/
```

---

## Quick Start

### Registration

```csharp
builder.Services.AddStangaNetLibObservability(opt =>
{
    opt.CorrelationIdHeader = "X-Correlation-Id";
    opt.EnableETag = true;
});

var app = builder.Build();
app.UseStangaNetLibObservability();
```

### PII Masking

```csharp
public class MyService(ILogMaskingService masking)
{
    public void LogUser(string email)
    {
        var masked = masking.MaskEmail(email);
        if (masked.IsSuccess)
            _logger.LogInformation("User {Email}", masked.Value);
    }
}
```

## About

ASP.NET Core observability middleware: correlation IDs, request logging, global exception handling, ETag, security headers, and PII log masking.
