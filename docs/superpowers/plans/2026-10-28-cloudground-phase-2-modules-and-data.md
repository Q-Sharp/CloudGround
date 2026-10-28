# CloudGround Phase 2: Modules and Data Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A generated solution gets a modular API host with PostgreSQL, EF Core migrations, `ProblemDetails`, built-in validation, OpenAPI, `HybridCache` on Garnet, Mapperly, architecture tests and a *Projects* sample module, and `--sample false` removes the module while everything still builds and passes.

**Architecture:** The AppHost adds PostgreSQL and Garnet containers and an EF migration resource per module DbContext. The API host owns cross-cutting infrastructure (errors, OpenAPI, cache); each module is a class library with one public entry point (`AddXModule`/`MapXModule`), its own schema, feature slices (request, handler, endpoint) and persistence. Architecture tests discover modules by assembly name and enforce the rules over whatever modules exist.

**Tech Stack:** Aspire 13.6.4 (PostgreSQL, Garnet, EF migrations), EF Core 10 with Npgsql, `Microsoft.Extensions.Validation`, `Microsoft.AspNetCore.OpenApi`, Scalar, `HybridCache`, Mapperly, ArchUnitNET, xUnit v3.

**Spec:** `docs/superpowers/specs/2026-10-27-cloudground-design.md`

## Global Constraints

- Everything from the phase 1 plan's Global Constraints still holds (C# on .NET 10, Minimal APIs, xUnit v3 test projects are executables, no explanatory comments, English, Conventional Commits with the Co-Authored-By trailer and no session links, warnings are errors).
- Pinned versions (newest stable on 2026-10-28): `Aspire.Hosting.PostgreSQL`, `Aspire.Hosting.Garnet`, `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Aspire.StackExchange.Redis.DistributedCaching` 13.6.4; `Microsoft.EntityFrameworkCore.Design`, `Microsoft.AspNetCore.OpenApi`, `Microsoft.Extensions.Validation` 10.0.14; `Microsoft.Extensions.Caching.Hybrid`, `Microsoft.Extensions.TimeProvider.Testing` 10.11.0; `Riok.Mapperly` 4.3.1; `Scalar.AspNetCore` 2.18.3; `TngTech.ArchUnitNET.xUnitV3` 0.13.5; `dotnet-ef` 10.0.14; `Npgsql` matching the version `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL` brings.
- **Author exception to the version policy (2026-10-28):** `Aspire.Hosting.EntityFrameworkCore` 13.6.4-preview.1.26523.11 is used although it is a preview, because it is Aspire's own way to run migrations. The spec records this.
- Each module lives in `src/Modules/CloudGround.App.Modules.<Name>/`; only its `<Name>Module` class is public. Module tests live in `tests/CloudGround.App.Modules.<Name>.Tests/`.
- Each module owns one DbContext with default schema `<name>` (lowercase module name) and its migrations history table in that schema.
- HTTP routes of the API are under `/api/`. Write endpoints get their role check in phase 4; this phase leaves them anonymous.
- Template conditionals use the symbol `sample`. Generated files never contain template directives.
- Tests that start the AppHost need a container runtime. Locally Podman serves `DOCKER_HOST`; GitHub runners have Docker.

## Review Focus

1. **`--sample false`** must leave no dangling reference (Program.cs, AppHost.cs, project references, `.slnx`) and must build and pass its tests. Pinned in Task 4 by `Solution_without_sample_builds_and_its_tests_pass`.
2. **Template directives leaking** into generated files (`#if (sample)`, `<!--#if`, the `sample` compile constant). Pinned in Task 4 by `Generated_solutions_contain_no_template_directives`.
3. **Stale cache**: a project read through the cache and then archived must be returned as archived. Pinned in Task 3 by `Archived_project_is_returned_as_archived_after_a_cached_read`.
4. **Blank or oversized input** (`"   "`, 101 characters) must return 400 `ProblemDetails` with a field error, never 500 or a stored row. Pinned in Task 3 by `Blank_name_is_rejected` and `Too_long_name_is_rejected`.
5. **Malformed JSON** must return 400 `ProblemDetails`, not 500. Pinned in Task 3 by `Malformed_json_returns_problem_details` (the first JSON endpoint).

---

### Task 0: Branch

- [ ] **Step 1:** `git -C ~/Sources/CloudGround switch -c feat/modules-and-data` from an up-to-date `main`.
Expected: `Switched to a new branch 'feat/modules-and-data'`

### Task 1: Spec, version policy and Dependabot

**Files:**
- Modify: `docs/superpowers/specs/2026-10-27-cloudground-design.md` (7.4 exception, 8.2 migrations)
- Modify: `.github/dependabot.yml`, `templates/cloudground/.github/dependabot.yml`
- Test: `tests/CloudGround.Templates.Tests/DependabotConfigurationTests.cs`

- [ ] **Step 1: Write the failing test** `Dependabot_ignores_major_updates_of_lts_bound_packages`: for the root and the generated `dependabot.yml`, every NuGet entry ignores `version-update:semver-major` for `Microsoft.Extensions.*`, `Microsoft.AspNetCore.*` and `Microsoft.EntityFrameworkCore.*`, plus `dotnet-ef`.
- [ ] **Step 2:** Run `dotnet test --solution CloudGround.Templates.slnx --filter-method "*Dependabot_ignores*"`. Expected: FAIL, `Microsoft.EntityFrameworkCore.*` missing.
- [ ] **Step 3:** Add the two ignore entries to both files. In the spec, add to 7.4 a row "Aspire.Hosting.EntityFrameworkCore — preview, Author exception (2026-10-28): Aspire's migration integration has no stable release yet"; in 8.2 add "Migrations run through Aspire's EF migration resource before the API starts; in operation they are published as a migration bundle (phase 6)."
- [ ] **Step 4:** Rerun the filter. Expected: PASS.
- [ ] **Step 5:** Commit `chore: keep EF Core on its LTS major and record the migration preview`.

### Task 2: API infrastructure, PostgreSQL and Garnet

**Files:**
- Modify: `templates/cloudground/src/CloudGround.App.AppHost/{CloudGround.App.AppHost.csproj,AppHost.cs}`
- Modify: `templates/cloudground/src/CloudGround.App.Api/{CloudGround.App.Api.csproj,Program.cs}`
- Modify: `templates/cloudground/Directory.Packages.props`
- Create: `templates/cloudground/tests/CloudGround.App.Integration.Tests/{AppHostFixture.cs,ProblemDetailsTests.cs,OpenApiTests.cs}`
- Modify: `templates/cloudground/tests/CloudGround.App.Integration.Tests/ApiHealthTests.cs`

**Interfaces:**
- Produces: `public sealed class AppHostFixture : IAsyncLifetime` with `DistributedApplication App { get; }` and `HttpClient CreateApiClient()` (plain `http` endpoint of resource `api`). It starts the AppHost once per test assembly (`[assembly: AssemblyFixture(typeof(AppHostFixture))]`) and waits until `api` is healthy.
- Produces: AppHost resources `postgres` (server), `database` (database on `postgres`), `cache` (Garnet), `api`. Later tasks reference `database` and `api` by these names.

- [ ] **Step 1: Write the failing tests** using the fixture:
  - `Unknown_route_returns_problem_details`: `GET /api/does-not-exist` → 404, content type `application/problem+json`.
  - `Api_serves_openapi_document`: `GET /openapi/v1.json` → 200 and the body contains `"openapi"`.
  - `Api_reports_healthy` now uses the fixture; with `database` and `cache` referenced, health includes the Garnet connection.
- [ ] **Step 2:** Run `dotnet test --solution templates/cloudground/CloudGround.App.slnx`. Expected: the problem-details and OpenAPI tests FAIL (plain 404, no document).
- [ ] **Step 3: Implement.** AppHost: `AddPostgres("postgres")`, `.AddDatabase("database")`, `AddGarnet("cache")`; `api` gets `WithReference` and `WaitFor` for both. No data volumes, so test runs never share state with a developer's data. Api: `AddRedisDistributedCache("cache")`, `AddHybridCache()`, `AddProblemDetails()`, `AddOpenApi()`; `UseExceptionHandler()`, `UseStatusCodePages()`; in Development `MapOpenApi()` and `MapScalarApiReference()`.
- [ ] **Step 4:** Rerun. Expected: PASS (4 tests).
- [ ] **Step 5:** Run `dotnet test --solution CloudGround.Templates.slnx`. Expected: PASS; the generated solutions start the containers too.
- [ ] **Step 6:** Commit `feat: add PostgreSQL, Garnet, ProblemDetails and OpenAPI to the API host`.

### Task 3: Projects module

**Files:**
- Create: `templates/cloudground/src/Modules/CloudGround.App.Modules.Projects/` with `CloudGround.App.Modules.Projects.csproj`, `ProjectsModule.cs`, `Domain/Project.cs`, `Features/{CreateProject,GetProject,ListProjects,ArchiveProject}.cs`, `Features/ProjectResponse.cs`, `Features/ProjectMapper.cs`, `Persistence/{ProjectsDbContext,ProjectConfiguration}.cs`, `Persistence/Migrations/*` (generated)
- Create: `templates/cloudground/tests/CloudGround.App.Modules.Projects.Tests/` with csproj, `ProjectTests.cs`, `ProjectMapperTests.cs`
- Create: `templates/cloudground/tests/CloudGround.App.Integration.Tests/ProjectsApiTests.cs`
- Create: `templates/cloudground/.config/dotnet-tools.json`
- Modify: AppHost (`AppHost.cs`, csproj), Api (`Program.cs`, csproj), `CloudGround.App.slnx`, `Directory.Packages.props`, `.editorconfig`, `tests/CloudGround.Templates.Tests/MappingGuardrailTests.cs` (create)

**Interfaces:**
- Produces: `public static class ProjectsModule { public static IHostApplicationBuilder AddProjectsModule(this IHostApplicationBuilder builder); public static IEndpointRouteBuilder MapProjectsModule(this IEndpointRouteBuilder endpoints); }`
- Produces: `internal sealed class ProjectsDbContext(DbContextOptions<ProjectsDbContext> options) : DbContext` with `public const string Schema = "projects"`, `DbSet<Project> Projects`.
- Produces: `internal sealed class Project` with `Guid Id` (version 7), `string Name`, `string? Description`, `DateTimeOffset CreatedAt`, `DateTimeOffset? ArchivedAt`; `static Project Create(string name, string? description, DateTimeOffset now)`; `bool TryArchive(DateTimeOffset now)` (false when already archived).
- Produces: `internal sealed record ProjectResponse(Guid Id, string Name, string? Description, DateTimeOffset CreatedAt, DateTimeOffset? ArchivedAt)`; Mapperly `ProjectMapper.ToResponse(Project)` and `ProjectMapper.ProjectToResponse(IQueryable<Project>)`.
- HTTP: `POST /api/projects` (`CreateProjectRequest([Required, StringLength(100)] string Name, [StringLength(1000)] string? Description)` → 201 + `Location`), `GET /api/projects` (200, ordered by name), `GET /api/projects/{id:guid}` (200 or 404 `ProblemDetails`, read through `HybridCache` key `projects/{id}`), `POST /api/projects/{id:guid}/archive` (204; 404; 409 when already archived; removes the cache entry). Each feature file holds its request, an `internal sealed class <Feature>Handler` and the endpoint mapping; endpoints call handlers directly.

- [ ] **Step 1: Write the failing unit tests** (`ProjectTests`, `ProjectMapperTests`, with `FakeTimeProvider` where a clock is needed): `Create_sets_name_description_and_creation_time`, `Create_assigns_a_version_7_id`, `TryArchive_sets_archived_time`, `TryArchive_on_archived_project_returns_false_and_keeps_first_time`, `ToResponse_copies_every_member`.
- [ ] **Step 2: Write the failing integration tests** (`ProjectsApiTests`): `Created_project_can_be_read`, `Created_project_appears_in_the_list`, `Blank_name_is_rejected` (`"   "` → 400, `errors` has `Name`), `Too_long_name_is_rejected` (101 chars → 400), `Malformed_json_returns_problem_details`, `Unknown_project_returns_problem_details` (404, `application/problem+json`), `Archived_project_is_returned_as_archived_after_a_cached_read` (GET, archive, GET → `ArchivedAt` set), `Archiving_twice_returns_conflict` (409), `Projects_tables_live_in_the_projects_schema` (Npgsql query on `information_schema.tables` with the connection string from `App.GetConnectionStringAsync("database")` finds `Projects` and `__EFMigrationsHistory` in schema `projects`).
- [ ] **Step 3:** Run `dotnet test --solution templates/cloudground/CloudGround.App.slnx`. Expected: FAIL (types and endpoints missing).
- [ ] **Step 4: Implement the module.** csproj: `Microsoft.NET.Sdk`, `FrameworkReference Microsoft.AspNetCore.App`, packages `Aspire.Npgsql.EntityFrameworkCore.PostgreSQL`, `Microsoft.Extensions.Validation`, `Riok.Mapperly`; `InternalsVisibleTo` for the module test project. `AddProjectsModule` registers the DbContext with `AddNpgsqlDbContext<ProjectsDbContext>("database", …)` (history table in `ProjectsDbContext.Schema`), calls `AddValidation()` inside the module assembly (the validation generator only sees endpoints in the assembly that calls it), registers the handlers and `TimeProvider.System` with `TryAddSingleton`. `MapProjectsModule` maps the group `/api/projects` with tag `Projects`. Mapperly diagnostics `RMG012` and `RMG020` are set to `error` in the template `.editorconfig`; `[**/Migrations/*.cs]` gets `generated_code = true`.
- [ ] **Step 5: Generate the migration** `InitialCreate` with the pinned `dotnet-ef` from `.config/dotnet-tools.json` into `Persistence/Migrations` (`--project` the module, `--startup-project` the Api; the design-time host needs `ConnectionStrings__database` set to any Npgsql connection string). The generated files stay as the tool writes them.
- [ ] **Step 6: Wire the host.** Api: project reference to the module, `Microsoft.EntityFrameworkCore.Design` (`PrivateAssets all`), `builder.AddProjectsModule()` and `app.MapProjectsModule()`. AppHost: `api.AddEFMigrations("projects-migrations", "CloudGround.App.Modules.Projects.Persistence.ProjectsDbContext")` with `WithMigrationsProject` (path overload, so the AppHost needs no project reference to a class library), `WithReference(database)`, `WaitFor(database)`, `RunDatabaseUpdateOnStart()`; `api.WaitForCompletion(projectsMigrations)`. The tool version comes from the manifest; if `dotnet tool exec` ignores the manifest, pass `WithToolVersion` with the same version and ledger it. Add the module and its test project to the `.slnx`.
- [ ] **Step 7:** Rerun the template solution's tests. Expected: PASS (all unit and integration tests).
- [ ] **Step 8: Mapping guardrail test** in the template tests, `Unmapped_response_member_fails_the_build`: generate `Guard.Rails`, add `string Owner` to `ProjectResponse`, `dotnet build` → nonzero exit code and output contains `RMG`. Run it, expect PASS (the guardrail exists since Step 4; this pins it).
- [ ] **Step 9:** Run `dotnet test --solution CloudGround.Templates.slnx`. Expected: PASS.
- [ ] **Step 10:** Commit `feat: add the Projects sample module with EF Core, validation, caching and mapping`.

### Task 4: `--sample` option

**Files:**
- Modify: `templates/cloudground/.template.config/template.json`, `Directory.Build.props`, `CloudGround.App.slnx`, Api `Program.cs` and csproj, AppHost `AppHost.cs`
- Create: `tests/CloudGround.Templates.Tests/SampleOptionTests.cs`
- Modify: `tests/CloudGround.Templates.Tests/TemplatePackage.cs` (`GenerateAsync(string name, params string[] options)`)
- Modify: `.github/workflows/ci.yml`

**Interfaces:**
- Produces: template symbol `sample` (`bool`, default `true`, description "Include the Projects sample module."), used as `--sample false`.
- Produces: `TemplatePackage.GenerateAsync(string name, params string[] options)`; existing calls keep working.

- [ ] **Step 1: Write the failing tests:** `Solution_without_sample_has_no_modules` (no file under `src/Modules/`, no `Modules.` in the `.slnx`, no `ProjectsModule` in any file), `Solution_without_sample_builds_and_its_tests_pass`, `Generated_solutions_contain_no_template_directives` (with and without the sample: no line starting with `#if`, no `<!--#`, no `DefineConstants` mentioning `sample`).
- [ ] **Step 2:** Run the filter `--filter-class "*SampleOptionTests"`. Expected: FAIL (`--sample` unknown).
- [ ] **Step 3: Implement.** `template.json`: symbol `sample`; a source modifier with `condition: "(!sample)"` excluding `src/Modules/**`, `tests/CloudGround.App.Modules.*/**` and `tests/CloudGround.App.Integration.Tests/Projects*.cs`. C# files use `#if (sample)`; MSBuild files use `<!--#if (sample) -->`. The template content compiles as is because its `Directory.Build.props` defines the constant `sample` inside a `<!--#if (false) -->` block that never reaches a generated solution. If the engine does not process `.slnx` as XML, add a `SpecialCustomOperations` entry for `**/*.slnx` with the XML conditional style and ledger it.
- [ ] **Step 4:** Rerun the filter. Expected: PASS.
- [ ] **Step 5:** Root CI: generate `Contoso.Shop` and `Contoso.Empty` (`--sample false`) and run the composite action on both (a job matrix over the two variants).
- [ ] **Step 6:** Run `dotnet test --solution CloudGround.Templates.slnx`. Expected: PASS.
- [ ] **Step 7:** Commit `feat: add the --sample option`.

### Task 5: Architecture tests

**Files:**
- Create: `templates/cloudground/tests/CloudGround.App.Architecture.Tests/` with csproj, `ModuleCatalog.cs`, `ModuleDiscoveryTests.cs`, `ModuleBoundaryTests.cs`, `EndpointTests.cs`, `DomainTests.cs`, `PersistenceTests.cs`
- Modify: `CloudGround.App.slnx`, `Directory.Packages.props`

**Interfaces:**
- Produces: `internal static class ModuleCatalog` with `IReadOnlyList<Assembly> Modules` (assemblies named `CloudGround.App.Modules.*` in the test output, excluding `*.Contracts` and `*.Tests`), `IReadOnlyList<string> ModuleProjectNames` (directories under `src/Modules/`, excluding `*.Contracts`) and `Architecture Architecture` (ArchUnitNET over the Api assembly and all module assemblies). The test project references the Api project, so every module the Api uses is in its output.

- [ ] **Step 1: Write the failing tests:**
  - `Module_discovery_finds_every_module_project`: discovered module assembly names equal `ModuleProjectNames`. Fails when a module exists that the Api does not reference.
  - `Modules_do_not_depend_on_each_others_internals`: for every pair of modules, no type of one depends on a type of the other (contracts assemblies are separate and allowed).
  - `Modules_expose_only_their_module_class`: the public types of each module assembly are exactly `<Name>Module`.
  - `No_endpoint_is_a_controller`: no type in the architecture derives from `ControllerBase` or carries `[ApiController]`.
  - `Domain_does_not_depend_on_asp_net_core_or_ef_core`: types in `*.Domain` namespaces do not depend on `Microsoft.AspNetCore` or `Microsoft.EntityFrameworkCore`.
  - `Every_module_db_context_uses_its_own_schema`: each `DbContext` subclass in module `<Name>`, built with `UseNpgsql()` and no connection, has default schema `<name>`.
  Each test iterates the discovered modules in a `[Fact]`, so the rules hold with zero modules too; ArchUnitNET rules use `WithoutRequiringPositiveResults()` where a rule's subject set can be empty.
- [ ] **Step 2:** Run `dotnet test --solution templates/cloudground/CloudGround.App.slnx --filter-class "*Architecture*"`. Expected: FAIL (project missing) → after creating the project with the tests only, the discovery test passes and rule tests pass against the real module; prove each rule bites by temporarily making `Project` reference `Microsoft.EntityFrameworkCore.DbContext`, observing `Domain_does_not_depend_on_asp_net_core_or_ef_core` FAIL, and reverting.
- [ ] **Step 3:** If `TngTech.ArchUnitNET.xUnitV3` does not run under xUnit v3 4.0.2 on Microsoft.Testing.Platform, use the core `TngTech.ArchUnitNET` package with plain `Assert` calls and ledger it.
- [ ] **Step 4:** Run `dotnet test --solution CloudGround.Templates.slnx`. Expected: PASS, including the solution without the sample.
- [ ] **Step 5:** Commit `feat: enforce module boundaries with architecture tests`.

### Task 6: Documentation and pull request

**Files:**
- Modify: `README.md` (status: phase 2 of 7; what a generated solution contains), `templates/cloudground/README.md` (modules, `--sample false`, how a migration is added: the dashboard's "Add Migration..." command on `projects-migrations`, or `dotnet ef migrations add` with the tool manifest), `docs/nuget/README.md` (`--sample`)
- Modify: `tests/CloudGround.Templates.Tests/LayoutTests.cs` (new projects and `.config/dotnet-tools.json` in the layout)

- [ ] **Step 1:** Extend `LayoutTests` with the new paths; run, expect PASS after the docs are in place.
- [ ] **Step 2:** Run both test commands from `AGENTS.md`. Expected: PASS.
- [ ] **Step 3:** Commit `docs: describe modules, data and the sample option`.
- [ ] **Step 4:** Final review of the whole branch (fresh reviewer, most capable model), fix pass, push, open the pull request `feat: modules and data`, wait for CI, squash-merge (the author allowed merging phase pull requests).
