# 0004. Microsoft libraries first

Status: Accepted

## Context

Every third-party package is a supply-chain, licensing and upgrade risk, and several popular .NET
libraries moved to commercial licenses recently.

## Decision

Where Microsoft ships a solution, the solution uses it: `Microsoft.Extensions.Logging` exported
through OpenTelemetry, built-in Minimal API validation, `Microsoft.AspNetCore.OpenApi`,
`HybridCache`, `Microsoft.Extensions.Http.Resilience`, `ProblemDetails`, YARP and Playwright.
Third-party libraries are used only where Microsoft has nothing; `AGENTS.md` lists them.

## Alternatives

- **Serilog**: richer sinks and enrichers, but `Microsoft.Extensions.Logging` with OpenTelemetry
  export already gives structured logs, trace correlation and any backend.
- **FluentValidation**: expressive rules, but .NET 10 validates Minimal API requests from data
  annotations without extra code.
- **Swashbuckle**: left the ASP.NET Core templates with .NET 9; `Microsoft.AspNetCore.OpenApi`
  generates the document, and Scalar shows it in Development.
- **Polly directly**: `Microsoft.Extensions.Http.Resilience` builds on Polly and configures standard
  pipelines for HTTP clients.

## Consequences

- Fewer packages to vet and update, and behavior that Microsoft documents and supports.
- Some conveniences of the alternatives are missing and take a few lines of code.
- A new third-party library needs an ADR.
