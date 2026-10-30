# 0017. Docker Compose from Aspire's publish pipeline

Status: Accepted

## Context

The application must run outside development without a second, hand-maintained description of it.

## Decision

Aspire's Docker publisher writes `docker-compose.yaml` and `.env.Production` from the AppHost. The
AppHost adds what a production baseline needs: health checks, restart policies, ports on the
loopback interface only, fixed volume names, migration bundles and the dashboard as default OTLP
backend. The dashboard runs the released `mcr.microsoft.com/dotnet/aspire-dashboard` image of the
AppHost's Aspire major and minor version instead of Aspire's default nightly image. The identity
provider, TLS termination and secrets come from the operator's configuration.

## Alternatives

- **Kubernetes manifests or Helm charts**: the common platform at scale, but more than many teams
  need at the start.
- **A hand-written Compose file**: drifts from the AppHost.
- **Cloud-specific infrastructure as code**: binds the solution to one cloud.

## Consequences

- One command produces a deployment for any machine with Docker.
- What Compose cannot express, such as TLS termination and backups, stays with the operator;
  `README.md` describes it.
