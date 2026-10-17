# StangaNetLib.Observability — Copilot instructions

- ASP.NET Core middleware: correlation IDs, request logging, global exception handling (ProblemDetails), ETag, security headers, PII log masking.
- Prefer `Result` / `Result<T>` and `Guard` from `StangaNetLib.Core` — do not reintroduce local Result/Guard types.
- Keep public APIs documented (`///`) and options-driven via `ObservabilityOptions` / `SecurityHeadersOptions`.
- DI: `AddStangaNetLibObservability`; pipeline: `UseStangaNetLibObservability`.
- Match existing naming, file-scoped namespaces, and EditorConfig / Roslyn conventions.
- Tests use xUnit v3 + FluentAssertions + TestHost; cover both `net8.0` and `net9.0` when changing shared behavior.
