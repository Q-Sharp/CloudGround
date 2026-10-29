# CloudGround

[![CI](https://github.com/Q-Sharp/CloudGround/actions/workflows/ci.yml/badge.svg)](https://github.com/Q-Sharp/CloudGround/actions/workflows/ci.yml)

> **Status: work in progress.** Phases 1 (foundation), 2 (modules and data), 3 (messaging), 4 (BFF
> and login), 5 (Angular SSR) and 6 (operations) of 7 are built.
> The plans live in [`docs/superpowers/plans/`](docs/superpowers/plans/), the design in
> [`docs/superpowers/specs/`](docs/superpowers/specs/).

CloudGround is a `dotnet new` template for container-based .NET solutions. One command creates a
production-shaped solution with a visible architecture; one more command runs all of it locally
through [Aspire](https://aspire.dev).

What a generated solution contains today:

- an Aspire AppHost with PostgreSQL, Garnet, RabbitMQ and Keycloak containers, and shared service defaults
  (OpenTelemetry, health checks, resilience, service discovery)
- an Angular frontend with server-side rendering per route: public pages rendered on the server
  from the API's anonymous endpoints, a browser-rendered area for signed-in users, Angular
  Material, angular-eslint, unit tests, OpenTelemetry in Node and a Dockerfile
- a BFF that serves the frontend, signs users in against Keycloak, keeps sessions in Garnet, protects writes with
  Angular-compatible XSRF tokens and forwards `/api/*` with the user's access token, so tokens never
  reach the browser
- a modular Minimal API host: `ProblemDetails`, built-in validation, OpenAPI with Scalar, role checks on writes,
  `HybridCache` on Garnet
- *Projects* and *Tasks* sample modules, each with its own schema, EF Core migrations that Aspire
  applies before the API starts, Mapperly mapping and caching on a read path
- modules that meet only through contracts: a contract interface for synchronous calls and an
  integration event that travels from a transactional outbox through RabbitMQ to an idempotent
  handler (Wolverine)
- architecture tests that enforce the module and contract boundaries for whatever modules exist
- unit tests, integration tests that start the whole application through
  `Aspire.Hosting.Testing`, and Playwright end-to-end tests that drive it in Chromium
- container images for every service, tagged with the solution's version: .NET services through
  SDK container publishing on Ubuntu 26.04 chiseled images, the web app from its own Dockerfile
- a Docker Compose deployment from Aspire's publish pipeline, with EF Core migration bundles,
  health checks and restart policies, and the identity provider, TLS and secrets left to
  configuration
- a devcontainer with the .NET SDK, Node and Docker in Docker, so the quickstart also runs in VS
  Code and GitHub Codespaces
- automatic semantic versioning from Git tags (MinVer) and release pull requests (release-please)
- GitHub Actions for build, lint, all test levels, formatting, container images, the Compose check
  and Conventional Commit pull request titles
- Dependabot configuration for NuGet, npm, container images, devcontainer features and GitHub
  Actions

The last phase adds an AI collaboration kit and the architecture documentation.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- [Node.js](https://nodejs.org) 26 with npm (the web app and its build)
- Docker or Podman (the AppHost starts PostgreSQL, Garnet, RabbitMQ and Keycloak as containers)

## Install from source

```bash
dotnet pack CloudGround.Templates.csproj --output artifacts
dotnet new install artifacts/CloudGround.Templates.*.nupkg
```

## Create and run a solution

```bash
dotnet new cloudground --name Contoso.Shop
cd Contoso.Shop
dotnet run --project src/Contoso.Shop.AppHost
```

The Aspire dashboard opens in the browser and shows every resource, its logs and its traces. The
`bff` resource serves the application.

`--sample false` creates the solution without the *Projects* and *Tasks* sample modules and their
pages; the module infrastructure, messaging, the BFF, the login and the web app's shell stay.

The name becomes the namespace, so characters that are not valid there turn into underscores:
`--name my-shop` creates `my_shop.slnx` and `src/my_shop.Api`.

## Test

```bash
dotnet test --solution Contoso.Shop.slnx
```

## Working on the template

See [CONTRIBUTING.md](CONTRIBUTING.md).

## How this is built

This repository is built with AI assistance. The AI works from a written design, implementation
plans and explicit rules ([`AGENTS.md`](AGENTS.md)), and every change passes the same tests and
checks as hand-written code. The design, the plans and the commit history are part of the
repository.

## License

[MIT](LICENSE)
