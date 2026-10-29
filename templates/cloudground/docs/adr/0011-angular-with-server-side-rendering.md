# 0011. Angular with server-side rendering per route

Status: Accepted

## Context

The web app has to serve public pages, which need a fast first paint, work without JavaScript and
can be indexed, as well as a signed-in area. AI tools still tend to produce outdated Angular
patterns.

## Decision

Angular, always on its newest stable major, with standalone components, signals, zoneless change
detection and built-in control flow. Each route has a render mode: public pages are rendered on the
server for each request, the signed-in area in the browser. Server rendering reads the API's
anonymous endpoints directly; the browser calls the API through the BFF.

## Alternatives

- **React or Vue**: equally capable; Angular's opinionated structure, CLI and first-party server
  rendering give a team one way of doing things.
- **Blazor**: C# end to end, but Angular has the larger ecosystem for public web frontends and
  first-party server rendering with hydration.
- **Rendering in the browser only**: simpler, but public pages are empty without JavaScript and
  slower to show.
- **Prerendering at build time**: the fastest pages, but no API runs while the image is built, so
  pages with data must be rendered per request.

## Consequences

- The web app needs Node at runtime and its own Dockerfile.
- `AGENTS.md` lists the modern Angular rules, and the Angular CLI's MCP server gives agents current
  guidance.
