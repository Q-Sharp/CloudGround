# Working rules

These rules apply to every change in this solution, by humans and AI agents alike. Tests and the CI
workflow enforce most of them, so a change that breaks a rule fails the build.

## Solution

`CloudGround.App` is a modular monolith on .NET 10 with an Angular frontend. Aspire runs all of it
locally and publishes it as a Docker Compose deployment.

| Folder | Content |
|---|---|
| `src/CloudGround.App.AppHost` | the Aspire AppHost: wires every service and container and publishes the deployment |
| `src/CloudGround.App.ServiceDefaults` | OpenTelemetry, health checks, resilience and service discovery for every .NET service |
| `src/CloudGround.App.Api` | the API host: composes the modules and owns errors, OpenAPI, caching, messaging and token validation |
| `src/Modules` | one class library per module, and a contracts project for each module that others use |
| `src/CloudGround.App.Bff` | the backend for frontend: login, session cookie, XSRF protection and the `/api` proxy |
| `src/CloudGround.App.Web` | the Angular app with server-side rendering |
| `tests` | architecture, unit, integration and end-to-end tests |
| `docs/adr` | architecture decision records |

## Commands

```bash
dotnet run --project src/CloudGround.App.AppHost
dotnet build
dotnet format --verify-no-changes
dotnet test --solution CloudGround.App.slnx
dotnet test --solution CloudGround.App.slnx --filter-not-trait Category=EndToEnd
```

In `src/CloudGround.App.Web`, run `npm ci` once, then `npm run build` (lint and build) and
`npm test`. "Add a migration" in `README.md` shows how to add an EF Core migration. Integration and
end-to-end tests start the whole application and need Docker or Podman.

## Architecture rules

- The API host composes modules. A module is a class library under `src/Modules/` with its own
  endpoints, handlers, persistence and PostgreSQL schema, named after the module in lowercase.
- A module references another module only through that module's `*.Contracts` project: contract
  interfaces for synchronous calls, integration events for reactions. Contracts depend on no module,
  ASP.NET Core, EF Core or Wolverine.
- A module exposes only its module class, the request types of its endpoints and the Wolverine
  handlers in its `Messaging` namespace; everything else is `internal`.
- Domain types depend on neither ASP.NET Core nor EF Core.
- Every module's `DbContext` uses the module's schema and retries transient failures. The AppHost
  applies every module's migrations, one after another, before the API starts.
- Integration events go through Wolverine's transactional outbox and RabbitMQ. Delivery is at least
  once, so every handler leaves the same state when it receives an event twice.
- HTTP endpoints are Minimal APIs, grouped per module and feature; there are no MVC controllers.
  An endpoint calls its handler directly; there is no mediator.
- Requests are validated by .NET's built-in Minimal API validation; errors are `ProblemDetails`.
- The browser talks only to the BFF, and tokens never reach it.

The architecture tests in `tests/CloudGround.App.Architecture.Tests` and the AppHost model tests in
`tests/CloudGround.App.Integration.Tests` check these rules for every module they find.

## Adding a module or a feature

Follow `.claude/skills/add-module/SKILL.md` to create a module and
`.claude/skills/add-feature/SKILL.md` to add an endpoint, a use case or a page. Both are plain
Markdown procedures with example files; they work with any agent and by hand.

## Libraries

Microsoft libraries come first: `Microsoft.Extensions.Logging` exported through OpenTelemetry,
built-in Minimal API validation, `Microsoft.AspNetCore.OpenApi`, `HybridCache`,
`Microsoft.Extensions.Http.Resilience`, `ProblemDetails`, YARP, Aspire and Playwright. The
third-party libraries are:

| Library | Use |
|---|---|
| xUnit v3 | every .NET test |
| Mapperly | object mapping; a target member without a source fails the build |
| ArchUnitNET | architecture tests |
| Wolverine | messaging with outbox, inbox, retries and error queues; not for HTTP |
| Scalar | the OpenAPI UI in Development |
| MinVer | versions from Git tags |
| Npgsql, StackExchange.Redis | database and cache clients, through the Aspire integrations |

Prefer real objects and small hand-written fakes in tests; when a test needs a mocking library, use
FakeItEasy. Do not add MediatR, AutoMapper, Swashbuckle, FluentValidation, Serilog or Polly
directly. Any other third-party library needs an ADR first.

## Code

- C# on .NET 10 with nullable reference types; warnings are errors, and the .NET analyzers run at
  the recommended level.
- No explanatory comments in code or configuration, including YAML, JSON, `.editorconfig` and
  `.gitignore`. Names, small functions and tests carry the meaning. Tool directives without
  explanation, such as `#pragma warning disable` or `// eslint-disable-next-line`, are allowed.
  Generated files such as EF Core migrations stay as the tool writes them.
- Write the failing test first, watch it fail, then make it pass. .NET tests use xUnit v3 on
  Microsoft.Testing.Platform; filter them with `--filter-class` or `--filter-method`.
- Everything is in English.

## Angular

- Standalone components only; no NgModules.
- Signals for state, `computed()` for derived state, `toSignal()` at the boundary to observables.
- `input()`, `output()` and `inject()` instead of `@Input()`, `@Output()` and constructor injection.
- Built-in control flow (`@if`, `@for`, `@switch`); no `*ngIf`, `*ngFor` or `*ngSwitch`.
- Zoneless change detection; no zone.js.
- Every route has a render mode in `src/CloudGround.App.Web/src/app/app.routes.server.ts`: server
  rendering for public pages, client rendering for pages behind the login.
- `@angular/ssr`, `@angular/platform-server` and `express` are imported only in `src/server.ts` and
  `*.server.ts` files, so they stay out of the browser bundle; ESLint rejects them elsewhere.
- Requests go to relative `/api/...` and `/bff/...` URLs through `HttpClient`; the browser never
  holds a token.
- Angular Material for UI components; `npm run build` fails on ESLint errors.

## Versions

Every component is on its newest LTS release, or on its newest stable release where it has no LTS
line. Dependabot proposes updates. A major update of a component that follows an LTS line is a
deliberate pull request with an ADR.

## Commits and pull requests

Commit messages and pull request titles follow Conventional Commits (`feat:`, `fix:`, `docs:`,
`refactor:`, `test:`, `ci:`, `chore:`); `feat!:` marks a breaking change. Pull requests are
squash-merged, so the title becomes the commit message and decides the next version.

## Decisions

Significant decisions are recorded in `docs/adr/`. A change that reverses or extends a decision
updates or supersedes its ADR.
