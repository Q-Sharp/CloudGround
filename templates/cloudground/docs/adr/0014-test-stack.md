# 0014. xUnit v3, Aspire.Hosting.Testing and Playwright

Status: Accepted

## Context

Every level needs tests, and integration tests should run against the real dependencies, not against
mocks of them.

## Decision

xUnit v3 on Microsoft.Testing.Platform for every .NET test. Unit tests use real objects and small
hand-written fakes; FakeItEasy is the mocking library when one is needed. Integration tests start
the whole AppHost through `Aspire.Hosting.Testing`, with real PostgreSQL, Garnet, RabbitMQ and
Keycloak containers. End-to-end tests drive Chromium with Playwright for .NET. Angular unit tests
use the Angular CLI's default runner.

## Alternatives

- **NUnit or MSTest**: equally capable; xUnit v3 is the chosen framework and runs natively on
  Microsoft.Testing.Platform.
- **Moq**: popular, but its SponsorLink incident in 2023 cost it trust.
- **Testcontainers**: real containers too, but a second description of the application next to the
  AppHost.
- **An in-memory database for integration tests**: fast, but it behaves differently from PostgreSQL
  and hides bugs.
- **Cypress or Selenium**: Playwright is Microsoft's, fast, and runs inside the .NET test projects.

## Consequences

- Integration and end-to-end tests need Docker or Podman and take minutes;
  `--filter-not-trait Category=EndToEnd` leaves out the slowest level.
- One fixture starts the application once per test assembly.
