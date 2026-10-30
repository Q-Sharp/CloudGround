# 0015. OpenTelemetry for logs, traces and metrics

Status: Accepted

## Context

One page request crosses the API, the Node server, the API again for the page's data, the database
and the broker. Following it needs correlated logs, traces and metrics.

## Decision

OpenTelemetry everywhere: the .NET services through `src/CloudGround.App.ServiceDefaults`, the Node
server through the OpenTelemetry Node SDK in `src/CloudGround.App.Web/src/instrumentation.mjs`, and
messages through Wolverine's tracing. Everything is exported over OTLP: locally to the Aspire
dashboard, in operation to whatever `OTEL_EXPORTER_OTLP_ENDPOINT` names.

## Alternatives

- **A vendor SDK such as Application Insights**: rich features, but bound to one backend.
- **Logging sinks per backend**: logs only, and one integration per backend.
- **No tracing**: logs without correlation make problems across services guesswork.

## Consequences

- A trace follows a page request from the API through Node and back into the API, and a write into
  the handler of an integration event.
- The backend can change without code changes.
