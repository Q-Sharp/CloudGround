# 0002. Minimal APIs with handlers called directly

Status: Accepted

## Context

Endpoints should be easy to find next to the logic they expose, cheap to add and free of indirection
that hides what a request does.

## Decision

HTTP endpoints are Minimal APIs, grouped per module with `MapGroup` and registered by the module.
Each endpoint calls its handler directly; handlers are plain classes registered in dependency
injection. Endpoints return typed results, errors are `ProblemDetails`, and requests are validated
by the built-in Minimal API validation.

## Alternatives

- **MVC controllers**: attribute routing, filters and model binding with more indirection, and a
  feature spread over a controller and a service. Microsoft's current templates and guidance lead
  with Minimal APIs.
- **A mediator such as MediatR**: decouples the endpoint from its handler through a registry and
  pipeline behaviors. The call from route to handler becomes invisible to code navigation, pipeline
  behaviors duplicate ASP.NET Core's filters, and MediatR moved to a commercial license in 2025.
- **FastEndpoints or Carter**: third-party layers over Minimal APIs; nothing they add is needed
  here.

## Consequences

- Code navigation leads from a route straight to its handler.
- Cross-cutting behavior lives in ASP.NET Core: endpoint filters, middleware and authorization
  policies.
- The architecture tests fail when an MVC controller appears.
