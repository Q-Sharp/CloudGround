# CloudGround.App

Created with the [CloudGround](https://github.com/Q-Sharp/CloudGround) template.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker or Podman; the AppHost starts PostgreSQL, Garnet, RabbitMQ and Keycloak as containers

## Run

```bash
dotnet run --project src/CloudGround.App.AppHost
```

The Aspire dashboard opens in the browser and shows every resource, its logs and its traces.
PostgreSQL and Keycloak keep their data in container volumes, so it survives restarts; remove the
volume whose name ends in `postgres-data` or `keycloak-data` to start over. The integration tests
always start without volumes, on an empty database and a freshly imported realm.
Before the API starts, the AppHost applies the EF Core migrations of every module. In Development
the API serves its OpenAPI document at `/openapi/v1.json` and the Scalar UI at `/scalar`. The
dashboard links the RabbitMQ management UI of the `messaging` resource.

## Sign-in

The browser talks only to the BFF in `src/CloudGround.App.Bff`; tokens never reach it. Open the
BFF's `/bff/login` from the dashboard and sign in as one of the demo users:

| User | Password | Roles |
|---|---|---|
| `alice` | `alice` | `editor` |
| `bob` | `bob` | none |

The BFF signs users in with OpenID Connect against Keycloak and offers:

- `GET /bff/login?returnUrl=/path` starts the login and returns only to local paths
- `POST /bff/logout` ends the session at the BFF and at Keycloak
- `GET /bff/user` returns the user's name and roles, or 401
- `/api/*` is forwarded to the API with the user's access token; the browser's cookies stay at the BFF

For a signed-in user, the BFF sets an `XSRF-TOKEN` cookie and expects its value in the
`X-XSRF-TOKEN` header on every state-changing request, which is what Angular's `HttpClient` does
on its own. The session ticket and the Data Protection keys live in Garnet, so several BFF
instances share the same sessions; the integration tests run two of them. Access tokens are
refreshed a minute before they expire.

The API accepts Keycloak access tokens for the `cloudground-app-api` audience. Reads are anonymous;
every write endpoint requires the policy of its module (`<Module>Policies.Edit`), which requires
the `editor` realm role. A test reads the OpenAPI document and checks that every write endpoint
rejects anonymous callers.

The realm lives in `src/CloudGround.App.AppHost/Realms/realm.json` and is imported
when Keycloak starts on an empty volume; after editing the file, remove the `keycloak-data`
volume. The realm is for local development only: it accepts any redirect URI and contains the
`cloudground-app-tests` client, which lets the integration tests get tokens with a password. Locally
Keycloak serves plain HTTP, so the solution runs without a trusted developer certificate.

## Architecture

The API host in `src/CloudGround.App.Api` is a modular monolith. It owns the cross-cutting
concerns: errors as `ProblemDetails`, OpenAPI, and `HybridCache` backed by Garnet. Each module in
`src/Modules/` is a class library that owns its endpoints, handlers, persistence and its own
PostgreSQL schema. A feature slice keeps its request, handler and endpoint in one file, and the
endpoint calls the handler directly. Cached reads skip `HybridCache`'s in-process copy and use
Garnet only, so a change that removes a cache entry is visible on every API instance at once. Requests are validated by the built-in Minimal API
validation, and Mapperly maps entities to responses; a response member without a source fails the
build.

Modules meet only through contracts. A module that offers something to others has a
`*.Contracts` project with interfaces and integration events, and other modules reference only
that project. Synchronous calls go through contract interfaces, reactions through integration
events. The *Projects* and *Tasks* sample modules, which `--sample false` leaves out, show both with
`IProjectCatalog` and `ProjectArchived`.

The architecture tests in `tests/CloudGround.App.Architecture.Tests` enforce these rules for every
module they find:

- every module project in `src/Modules/` is referenced by the API host
- a module references another module only through its contracts project
- modules do not depend on each other's types
- contracts do not depend on ASP.NET Core, EF Core, Wolverine or any module
- a module exposes only its module class, its request types and the message handlers in its
  `Messaging` namespace
- domain types do not depend on ASP.NET Core or EF Core
- every module's `DbContext` uses a schema named after the module and retries transient failures
- no endpoint is an MVC controller

## Messaging

The API runs [Wolverine](https://wolverinefx.net) with RabbitMQ as transport. Its durable inbox and
outbox live in the `wolverine` schema, which Wolverine creates and updates itself. A module
publishes an integration event through `IDbContextOutbox<TDbContext>`: the event is stored in the
same transaction as the change that raised it and sent after the commit, so it is neither lost nor
sent for a change that failed. Integration events always travel through RabbitMQ, also when the
handler runs in the same process. A failing handler is retried with growing pauses; after that the
message moves to the error queue.

Delivery is at least once, so every handler must leave the same state when it receives an event
twice. Cross-module checks are not transactions: in the sample, creating a task asks the Projects
module whether the project is active and checks again after saving, so a task created while its
project is being archived ends up closed instead of open.

## Add an integration event

1. Add a record to the publishing module's `*.Contracts` project.
2. Publish it the way the sample's `ArchiveProjectHandler` does: inside
   `Database.CreateExecutionStrategy().ExecuteAsync(...)`, begin a transaction, call
   `PublishAsync` on the outbox and then `SaveChangesAndFlushMessagesAsync`. EF Core retries the
   whole unit after a transient database failure; a retried unit can send the event twice, which
   at-least-once delivery allows.
3. In the reacting module, reference the contracts project and add a public static
   `<Event>Handler` with a `HandleAsync` method to the module's `Messaging` namespace. Wolverine
   requires public handlers, so the handler only forwards to an internal service through a public
   interface, as the sample's `ProjectArchivedHandler` does with `ICloseProjectTasks`.
4. In the module's `Add<Module>Module`, register the assembly and the interface with Wolverine:
   `ConfigureWolverine(options => { options.Discovery.IncludeAssembly(...);
   options.CodeGeneration.AlwaysUseServiceLocationFor<TInterface>(); })`.

## Add a migration

Change the model, then use the *Add Migration...* command of the module's migration resource in the
Aspire dashboard, or run the pinned `dotnet-ef` from the solution root:

```bash
dotnet tool restore
ConnectionStrings__database="Host=localhost" dotnet ef migrations add <Name> --project src/Modules/CloudGround.App.Modules.<Module> \
  --startup-project src/CloudGround.App.Api --context <Module>DbContext --output-dir Persistence/Migrations
```

The design-time host only needs the connection string to exist; it does not connect.

## Test

```bash
dotnet test --solution CloudGround.App.slnx
```

## Versioning and releases

Versions follow semantic versioning and come from Git tags through MinVer. Pull request titles
follow [Conventional Commits](https://www.conventionalcommits.org/); they decide the next
version. release-please keeps a release pull request open with the changelog. Merging it tags the
release.

## GitHub setup

release-please opens its release pull request with the workflow's `GITHUB_TOKEN`. Allow that
once per repository, either under *Settings → Actions → General → Workflow permissions → Allow
GitHub Actions to create and approve pull requests*, or with:

```bash
gh api -X PUT repos/<owner>/<repo>/actions/permissions/workflow \
  -f default_workflow_permissions=read -F can_approve_pull_request_reviews=true
```

Use squash merges with the pull request title as the commit message, so the Conventional Commit
title reaches the main branch.
