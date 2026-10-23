# Changelog

All notable changes to **StangaNetLib.Observability** are documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]


## [1.0.1] - 2026-10-23

### Changed
- Add `net10.0` target framework alongside `net8.0` and `net9.0`.
- Require .NET SDK 10 (global.json) to multi-target `net8.0` / `net9.0` / `net10.0`; opt in Microsoft.Testing.Platform as the `dotnet test` runner.
- Remove redundant Microsoft.Extensions.* PackageReferences from ASP.NET Core projects (provided by Microsoft.AspNetCore.App; avoids NU1510 under the .NET 10 SDK).

## [1.0.0] - 2026-10-17

### Added
- Correlation ID middleware with `ICorrelationIdAccessor` and OpenTelemetry-friendly TraceId fallback.
- Structured request logging with slow-request threshold and path exclusions.
- Global exception handling to RFC 7807 ProblemDetails (`DomainException` aware).
- ETag / conditional GET middleware with `[NoETag]` opt-out.
- Security headers middleware (`SecurityHeadersOptions`).
- PII log masking (`ILogMaskingService`) returning `Result<string>` from Core.
- DI (`AddStangaNetLibObservability`) and pipeline (`UseStangaNetLibObservability`) registration.
- Dual targeting `net8.0` / `net9.0`, XML docs, symbols (`snupkg`), package icon, and MSBuild consumer metadata stamp.
- Depends on `StangaNetLib.Core` 1.0.0; Microsoft.Extensions.* pinned to 8.x.
- CI via shared StangaNet reusable workflow (push / PR / manual; publish outside PRs).

### Removed
- Unused local `Common/Result` and `Common/Guard` duplicates (use `StangaNetLib.Core`).

[Unreleased]: https://github.com/StangaNet/StangaNetLib.Observability/compare/v1.0.1...HEAD
[1.0.1]: https://github.com/StangaNet/StangaNetLib.Observability/compare/v1.0.0...v1.0.1
[1.0.0]: https://github.com/StangaNet/StangaNetLib.Observability/releases/tag/v1.0.0
