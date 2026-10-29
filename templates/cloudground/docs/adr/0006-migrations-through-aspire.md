# 0006. Migrations through Aspire and migration bundles

Status: Accepted

## Context

Migrations must run before the API starts, in development, in tests and in the deployment, and two
migrations must not run against the database at the same time.

## Decision

The AppHost adds one EF Core migration resource per module with `WithModuleMigrations` in
`src/CloudGround.App.AppHost/ModuleMigrations.cs`. The migrations run one after another, and the API
waits until all of them have completed. In the deployment each runs as an EF Core migration bundle
container.

## Alternatives

- **`Database.Migrate()` when the API starts**: no extra resource, but several API instances race,
  and the API needs the rights to change the schema.
- **Running migrations by hand or as a pipeline step**: full control, but easy to forget and
  different in every environment.
- **SQL scripts with a migration tool such as DbUp or Flyway**: a database-first workflow that gives
  up EF Core's migrations generated from the model.

## Consequences

- `Aspire.Hosting.EntityFrameworkCore` is a preview package.
- A new module needs one line in the AppHost, and a test fails without it.
- Migration bundles start through the API project, so they run on the `aspnet` image, which has a
  shell.
