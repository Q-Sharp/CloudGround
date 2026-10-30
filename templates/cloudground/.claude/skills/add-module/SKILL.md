---
name: add-module
description: Creates a new module in this solution with its project, database schema, registration, migrations resource and test project, and its contracts project once another module needs one. Use when asked for a new module or bounded context, including in a solution that has no modules yet.
---

# Add a module

A module is a class library under `src/Modules/` that owns its endpoints, handlers, persistence and
its own PostgreSQL schema. Other modules reach it only through its contracts project. The
architecture tests in `tests/CloudGround.App.Architecture.Tests` enforce this for every module they
find, and the AppHost model tests in `tests/CloudGround.App.Integration.Tests` check that every
module applies its migrations before the API starts.

## Steps

1. Choose the module name in PascalCase, for example `OrderHistory`. Its lowercase form,
   `orderhistory`, names the database schema, the route prefix `/api/orderhistory` and the AppHost
   resource `orderhistory-migrations`.
2. Copy the folders `src/` and `tests/` of this skill, `.claude/skills/add-module/src/` and
   `.claude/skills/add-module/tests/`, to the solution root. Replace `ModuleName` with the module
   name and `modulename` with its lowercase form, in folder names, file names and file contents.
3. Add both projects to the solution and reference the module from the API:

   ```bash
   dotnet sln add src/Modules/CloudGround.App.Modules.<Module>/CloudGround.App.Modules.<Module>.csproj \
     tests/CloudGround.App.Modules.<Module>.Tests/CloudGround.App.Modules.<Module>.Tests.csproj
   dotnet reference add src/Modules/CloudGround.App.Modules.<Module>/CloudGround.App.Modules.<Module>.csproj \
     --project src/CloudGround.App.Api/CloudGround.App.Api.csproj
   ```

4. Register the module in `src/CloudGround.App.Api/Program.cs`: add
   `using CloudGround.App.Modules.<Module>;`, call `builder.Add<Module>Module();` after the other
   `builder.Add…` calls, and `app.Map<Module>Module();` after `app.MapDefaultEndpoints();` and the
   other modules' `Map…Module()` calls.
5. Apply its migrations before the API starts: in `src/CloudGround.App.AppHost/AppHost.cs`, add
   `api.WithModuleMigrations("<Module>", database);` before `builder.TagImagesWithVersion();`.
   Modules migrate one after another, in the order of these calls.
6. Build and test:

   ```bash
   dotnet build
   dotnet format --verify-no-changes
   dotnet test --solution CloudGround.App.slnx --filter-not-trait Category=EndToEnd
   ```

The new module has no entities and no migrations yet. Add its first feature with the add-feature
skill in `.claude/skills/add-feature/SKILL.md`, which also creates the first migration.

## Contracts

A module that offers something to other modules gets a contracts project. Create it when another
module first needs it:

1. Create `src/Modules/CloudGround.App.Modules.<Module>.Contracts/CloudGround.App.Modules.<Module>.Contracts.csproj`
   with this content and nothing else:

   ```xml
   <Project Sdk="Microsoft.NET.Sdk">

   </Project>
   ```

2. Add it to the solution, and reference it from the module and from every module that uses it:

   ```bash
   dotnet sln add src/Modules/CloudGround.App.Modules.<Module>.Contracts/CloudGround.App.Modules.<Module>.Contracts.csproj
   dotnet reference add src/Modules/CloudGround.App.Modules.<Module>.Contracts/CloudGround.App.Modules.<Module>.Contracts.csproj \
     --project src/Modules/CloudGround.App.Modules.<Module>/CloudGround.App.Modules.<Module>.csproj
   ```

3. Put the public interfaces and integration events there. Other modules never reference the module
   itself, and contracts depend on no module, ASP.NET Core, EF Core or Wolverine.

## What the copied files contain

- `CloudGround.App.Modules.<Module>`: the module class with `Add<Module>Module`, which registers the
  module's `DbContext` on the `database` connection with its migrations history in the module's
  schema, the `Edit` policy that requires the `editor` realm role, and validation, and with
  `Map<Module>Module`, which maps the endpoint group `/api/<module>`. `<Module>Policies` holds the
  policy name and the role, `Persistence/<Module>DbContext` the schema.
- `CloudGround.App.Modules.<Module>.Tests`: the module's unit tests, starting with one that pins the
  edit policy.

## Rules

- Everything in the module is `internal` except the module class, the request types of its
  endpoints and the Wolverine handlers in its `Messaging` namespace.
- The module talks to another module only through that module's contracts project: synchronously
  through a contract interface, asynchronously through an integration event (see "Add an
  integration event" in `README.md`).
- The schema is the lowercase module name; the architecture tests compare them.
