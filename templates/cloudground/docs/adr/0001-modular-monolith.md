# 0001. Modular monolith with feature slices

Status: Accepted

## Context

A new solution needs boundaries that keep business areas apart from the first day, without paying
for a distributed system before the domain is understood.

## Decision

One API host is composed of modules. Each module is a class library under `src/Modules/` that owns
its endpoints, handlers, persistence and database schema. Modules talk to each other only through
contracts projects: contract interfaces for synchronous calls, integration events for reactions.

Inside a module, code is organized by feature, not by layer: one file per use case holds its
request, handler and endpoint.

## Alternatives

- **Microservices**: independent deployment and scaling per service, at the price of network calls,
  distributed consistency, versioned APIs between services and a platform to run them. Premature
  while the boundaries are still being found; a module with clean contracts can be extracted later.
- **Layered architecture with controllers, services and repositories**: separates by technical
  concern, so one feature touches every layer, and nothing stops one business area from reaching
  into another's tables.
- **Clean Architecture with a project per layer**: strong dependency rules, but several projects per
  business area and much ceremony for small use cases. The architecture tests enforce the rules that
  matter here with less structure.

## Consequences

- One deployable unit and one database server; modules cannot be scaled separately.
- The architecture tests in `tests/CloudGround.App.Architecture.Tests` enforce the boundaries, not
  the network.
- Extracting a module into a service means replacing its contract interfaces with remote calls; its
  integration events already travel through RabbitMQ.
