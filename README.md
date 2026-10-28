# CloudGround

[![CI](https://github.com/Q-Sharp/CloudGround/actions/workflows/ci.yml/badge.svg)](https://github.com/Q-Sharp/CloudGround/actions/workflows/ci.yml)

> **Status: work in progress.** Phases 1 (foundation) and 2 (modules and data) of 7 are built.
> The plans live in [`docs/superpowers/plans/`](docs/superpowers/plans/), the design in
> [`docs/superpowers/specs/`](docs/superpowers/specs/).

CloudGround is a `dotnet new` template for container-based .NET solutions. One command creates a
production-shaped solution with a visible architecture; one more command runs all of it locally
through [Aspire](https://aspire.dev).

What a generated solution contains today:

- an Aspire AppHost with PostgreSQL and Garnet containers, and shared service defaults
  (OpenTelemetry, health checks, resilience, service discovery)
- a modular Minimal API host: `ProblemDetails`, built-in validation, OpenAPI with Scalar,
  `HybridCache` on Garnet
- a *Projects* sample module with its own schema, EF Core migrations that Aspire applies before the
  API starts, Mapperly mapping and caching on the read path
- architecture tests that enforce the module boundaries for whatever modules exist
- unit tests and integration tests that start the whole application through
  `Aspire.Hosting.Testing`
- automatic semantic versioning from Git tags (MinVer) and release pull requests (release-please)
- GitHub Actions for build, tests, formatting and Conventional Commit pull request titles
- Dependabot configuration

The roadmap adds messaging, a BFF with login, an Angular frontend, container deployment and an
AI collaboration kit, one phase at a time.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker or Podman (the AppHost starts PostgreSQL and Garnet as containers)

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

The Aspire dashboard opens in the browser and shows every resource, its logs and its traces.

`--sample false` creates the solution without the *Projects* sample module; the module
infrastructure stays.

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
