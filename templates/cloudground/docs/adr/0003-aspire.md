# 0003. Aspire for orchestration

Status: Accepted

## Context

Running the API, the web app, PostgreSQL, Garnet, RabbitMQ and Keycloak together needs wiring:
connection strings, ports, start order, health checks and telemetry, locally, in tests and in a
deployment.

## Decision

The Aspire AppHost in `src/CloudGround.App.AppHost` describes the whole application in C#.
`src/CloudGround.App.ServiceDefaults` adds OpenTelemetry, health checks, resilience and service
discovery to every .NET service. The integration tests start the same AppHost through
`Aspire.Hosting.Testing`, and Aspire's publish pipeline produces the Docker Compose deployment.

## Alternatives

- **A hand-written Docker Compose file**: no C# wiring, but connection strings, ports and start
  order are maintained in two places, and tests need their own setup.
- **Project Tye**: Microsoft's earlier experiment, archived in favor of Aspire.
- **A local Kubernetes cluster**: close to some production platforms, but heavy for the inner
  development loop.

## Consequences

- `dotnet run --project src/CloudGround.App.AppHost` starts everything, and the dashboard shows
  logs, traces and metrics.
- Some Aspire integrations used here are previews; they are pinned and updated by Dependabot.
- Deployment targets are those Aspire can publish to; Docker Compose is the one configured.
