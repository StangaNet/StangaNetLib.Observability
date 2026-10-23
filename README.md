# StangaNetLib.Observability

[![.NET CI](https://github.com/StangaNet/StangaNetLib.Observability/actions/workflows/main.yml/badge.svg)](https://github.com/StangaNet/StangaNetLib.Observability/actions/workflows/main.yml)
![NuGet](https://img.shields.io/badge/nuget-1.0.0-blue)
![.NET](https://img.shields.io/badge/.NET-8.0%20%7C%209.0%20%7C%2010.0-512BD4)

ASP.NET Core observability middleware: correlation IDs, structured request logging, global exception handling (RFC 7807 ProblemDetails), ETag conditional requests, security headers, and PII log masking. Integrates with [`StangaNetLib.Core`](https://github.com/StangaNet/StangaNetLib.Core) (`Result`, `Guard`, `DomainException`).

## Design Philosophy

- **Domain-centric**: correlation, masking, and middleware as capabilities.
- **Functional failures**: PII masking returns `Result<T>` from Core.
- **Defensive programming**: `Guard.Against` for input validation.
- **Modern .NET**: `net8.0` / `net9.0` / `net10.0` with OpenTelemetry-friendly correlation.

## Installation

The package is hosted on **GitHub Packages** (pulls Core from the same feed).

```xml
<!-- NuGet.config — source key must match your packageSourceCredentials -->
<configuration>
  <packageSources>
    <add key="StangaNet" value="https://nuget.pkg.github.com/StangaNet/index.json" />
  </packageSources>
</configuration>
```

```xml
<!-- .csproj -->
<PackageReference Include="StangaNetLib.Observability" Version="1.0.0" />
```

See [CHANGELOG.md](./CHANGELOG.md) for release history.

## Core Components

| Namespace | Components | Purpose |
|---|---|---|
| `StangaNetLib.Observability.Middleware` | Correlation, logging, exceptions, ETag, security headers | HTTP pipeline |
| `StangaNetLib.Observability.Correlation` | `ICorrelationIdAccessor` | Per-request ID |
| `StangaNetLib.Observability.Masking` | `ILogMaskingService` | PII masking |
| `StangaNetLib.Observability.Options` | `ObservabilityOptions`, `SecurityHeadersOptions` | Configuration |
| `StangaNetLib.Observability.Extensions` | `AddStangaNetLibObservability`, `UseStangaNetLibObservability` | DI / pipeline |

## Project Structure

```
StangaNetLib.Observability/
├── assets/
│   └── package-icon.png
├── build/
│   └── StangaNetLib.Observability.props
├── src/
│   └── StangaNetLib.Observability/
│       ├── Attributes/
│       ├── Correlation/
│       ├── Extensions/
│       ├── Masking/
│       ├── Middleware/
│       └── Options/
└── tests/
    └── StangaNetLib.Observability.Tests/
```

## Quick Start

```csharp
builder.Services.AddStangaNetLibObservability();

var app = builder.Build();
app.UseStangaNetLibObservability();
```

## CI/CD

On push/PR to `main` and `workflow_dispatch`, the shared StangaNet workflow:

1. Restores, builds, and tests on .NET 8 and .NET 9
2. Packs the NuGet package (with symbols)
3. Publishes to GitHub Packages outside pull requests
4. Uploads the `.nupkg` as a build artifact
