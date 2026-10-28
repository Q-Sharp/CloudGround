# CloudGround Phase 3: Messaging Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Modules talk to each other through contracts: synchronously through a contract interface and asynchronously through an integration event that travels from a transactional outbox through RabbitMQ to an idempotent handler, shown by a *Tasks* module that reacts to `ProjectArchived`.

**Architecture:** Each module that offers something to others gets a `*.Contracts` project with interfaces and integration events; other modules reference only that project. Wolverine runs in the API host with RabbitMQ as transport and PostgreSQL message storage (schema `wolverine`); modules mark their assembly as a Wolverine module so their handlers are discovered without the host knowing them. Publishing uses Wolverine's EF Core outbox in the same transaction as the change.

**Tech Stack:** Wolverine 6.56.0 (`WolverineFx`, `WolverineFx.RabbitMQ`, `WolverineFx.Postgresql`, `WolverineFx.EntityFrameworkCore`), Aspire RabbitMQ hosting 13.6.4, EF Core 10, xUnit v3, ArchUnitNET.

**Spec:** `docs/superpowers/specs/2026-10-27-cloudground-design.md` (7.1 Messaging, 7.3 Wolverine, 8.2 Backend, 8.4 Observability, 8.5 Tests)

## Global Constraints

- Everything in the Global Constraints of the phase 1 and phase 2 plans still holds.
- Pinned versions (newest stable on 2026-10-28): `WolverineFx`, `WolverineFx.RabbitMQ`, `WolverineFx.Postgresql`, `WolverineFx.EntityFrameworkCore` 6.56.0; `Aspire.Hosting.RabbitMQ` 13.6.4.
- Wolverine is used for messaging only; HTTP endpoints still call handlers directly (spec 7.3).
- A module references another module only through its `CloudGround.App.Modules.<Name>.Contracts` project. Contracts contain interfaces, records and integration events, nothing that depends on ASP.NET Core, EF Core or Wolverine.
- Wolverine's envelope tables live in schema `wolverine` and are managed by Wolverine, not by module migrations.
- With `--sample false`, RabbitMQ, Wolverine and its storage stay; only the modules go.

## Review Focus

1. **Duplicate delivery**: the `ProjectArchived` handler runs twice for the same event (redelivery) and must leave the tasks exactly as after the first run. Pinned in Task 3 by `Closing_twice_keeps_the_first_closing_time` and the inbox configuration.
2. **Event lost on failure**: if saving the archived project fails, no `ProjectArchived` may be published; if publishing fails after commit, the event must still go out later. Covered by using the EF Core outbox (`IDbContextOutbox<ProjectsDbContext>`); pinned in Task 3 by `Archiving_a_project_closes_its_open_tasks` going through the outbox, not `IMessageBus`.
3. **Creating a task for an archived or unknown project** must return 409 or 404 `ProblemDetails`, not create the task. Pinned in Task 1 by `Task_for_unknown_project_is_rejected` and `Task_for_archived_project_is_rejected`.
4. **A module reaching into another module's internals** must fail the build's tests. Pinned in Task 4 by the now non-vacuous `Modules_do_not_depend_on_each_others_internals` plus `Modules_reference_other_modules_only_through_contracts`, each proven by a mutation.
5. **`--sample false`** with Wolverine and RabbitMQ but no handlers must start and pass its tests. Pinned in Task 5 by the existing `Solution_without_sample_builds_and_its_tests_pass`.

---

### Task 0: Branch and phase 2 follow-ups

- [ ] **Step 1:** `git switch -c feat/messaging` from an up-to-date `main` (phase 2 merged).
- [ ] **Step 2: Follow-ups from the phase 2 review**, each test first:
  - `Domain_does_not_depend_on_asp_net_core_or_ef_core` also covers sub-namespaces (`\.Domain(\..+)?$`); prove it with a mutation in a `Domain.Events` type.
  - `Api_documents_validation_errors_for_creating_a_project`: the OpenAPI document lists `400` for `POST /api/projects` (`ProducesValidationProblem()`).
  - `Concurrent_archives_conflict`: two simultaneous archive requests give one 204 and one 409 (`UseXminAsConcurrencyToken()`, `DbUpdateConcurrencyException` → 409).
  - `Blank_name_is_rejected` and `Too_long_name_is_rejected` also assert that no project was stored.
  - The generated README's migration command drops `ConnectionStrings__cache`.
- [ ] **Step 3:** Commit `fix: close the gaps found in the phase 2 review`.

### Task 1: Projects contract and the Tasks module

**Files:**
- Create: `src/Modules/CloudGround.App.Modules.Projects.Contracts/` (csproj, `IProjectCatalog.cs`, `ProjectStatus.cs`)
- Create: `src/Modules/CloudGround.App.Modules.Tasks/` (csproj, `TasksModule.cs`, `Domain/TaskItem.cs`, `Features/{CreateTask,GetTask,ListProjectTasks,CompleteTask,TaskResponse,TaskMapper}.cs`, `Persistence/{TasksDbContext,TaskItemConfiguration}.cs`, `Persistence/Migrations/*` generated)
- Create: `tests/CloudGround.App.Modules.Tasks.Tests/` (csproj, `TaskItemTests.cs`, `TaskMapperTests.cs`)
- Create: `tests/CloudGround.App.Integration.Tests/TasksApiTests.cs`
- Modify: Projects module (`ProjectCatalog.cs` implementing the contract, registration), Api (`Program.cs`, csproj), AppHost (`tasks-migrations`), `.slnx`

**Interfaces:**
- Produces: `public interface IProjectCatalog { Task<ProjectStatus?> FindAsync(Guid projectId, CancellationToken cancellationToken); }` and `public sealed record ProjectStatus(Guid Id, bool IsArchived);` in namespace `CloudGround.App.Modules.Projects.Contracts`.
- Produces: `internal sealed class TaskItem` with `Guid Id` (v7), `Guid ProjectId`, `string Title` (max 200), `DateTimeOffset CreatedAt`, `DateTimeOffset? CompletedAt`, `DateTimeOffset? ClosedAt`; `static TaskItem Create(Guid projectId, string title, DateTimeOffset now)`; `bool TryComplete(DateTimeOffset now)` (false when completed or closed); `void Close(DateTimeOffset now)` (no-op unless open). Timestamps at microsecond precision as in `Project`.
- Produces: `TasksDbContext` with schema `tasks`; `public static class TasksModule { AddTasksModule; MapTasksModule; }`.
- HTTP: `POST /api/projects/{projectId:guid}/tasks` (`CreateTaskRequest([Required, StringLength(200)] string Title)` → 201; 404 unknown project; 409 archived project), `GET /api/tasks/{id:guid}`, `GET /api/projects/{projectId:guid}/tasks`, `POST /api/tasks/{id:guid}/complete` (204; 404; 409 when not open).

- [ ] **Step 1:** Failing unit tests: `Create_sets_project_title_and_creation_time`, `TryComplete_sets_completion_time`, `TryComplete_on_closed_task_returns_false`, `Close_sets_closing_time_on_open_task`, `Close_leaves_a_completed_task_unchanged`, `Closing_twice_keeps_the_first_closing_time`, `ToResponse_copies_every_member`.
- [ ] **Step 2:** Failing integration tests: `Created_task_can_be_read`, `Project_tasks_are_listed`, `Task_for_unknown_project_is_rejected` (404 problem), `Task_for_archived_project_is_rejected` (409 problem), `Blank_title_is_rejected`, `Completing_twice_returns_conflict`, `Tasks_tables_live_in_the_tasks_schema`.
- [ ] **Step 3:** Run both; expect FAIL.
- [ ] **Step 4:** Implement as in phase 2's Projects module (feature slices, handlers, Mapperly, own schema, migration `InitialCreate` with the pinned `dotnet-ef`, `tasks-migrations` resource; the API waits for both migration resources). `CreateTaskHandler` asks `IProjectCatalog`; `ProjectCatalog` in the Projects module reads without tracking.
- [ ] **Step 5:** Run both test commands from `AGENTS.md`; expect PASS. Commit `feat: add the Tasks module and the Projects contract`.

### Task 2: Wolverine with RabbitMQ and PostgreSQL storage

**Files:**
- Modify: AppHost (`AddRabbitMQ("messaging").WithManagementPlugin()`, `api` references and waits for it), Api (`Program.cs`, csproj), ServiceDefaults (`Extensions.cs`: tracing source and meter `Wolverine`), `Directory.Packages.props`
- Create: `tests/CloudGround.App.Integration.Tests/MessagingTests.cs`

- [ ] **Step 1:** Failing test `Wolverine_storage_lives_in_the_wolverine_schema`: after start, `information_schema.tables` has `wolverine_incoming_envelopes` and `wolverine_outgoing_envelopes` in schema `wolverine`.
- [ ] **Step 2:** Run; expect FAIL.
- [ ] **Step 3:** In the Api: `builder.UseWolverine(...)` with `UseRabbitMqUsingNamedConnection("messaging").AutoProvision()`, `PersistMessagesWithPostgresqlUsingNamedConnection("database", "wolverine")`, `UseEntityFrameworkCoreTransactions()`, `Policies.AutoApplyTransactions()`, `Policies.UseDurableInboxOnAllListeners()`, `Policies.UseDurableOutboxOnAllSendingEndpoints()`, conventional RabbitMQ routing, and an error policy that retries with cooldown and then moves the message to the error queue. Module assemblies are found through `[assembly: WolverineModule]` in each module; if that attribute does not exist in 6.56, the module's `Add<Name>Module` calls `builder.Services.ConfigureWolverine(opts => opts.Discovery.IncludeAssembly(...))` instead, and the choice is ledgered. ServiceDefaults adds the `Wolverine` activity source and meter.
- [ ] **Step 4:** Run; expect PASS. Run the template tests (both variants start Wolverine). Commit `feat: run Wolverine on RabbitMQ with PostgreSQL message storage`.

### Task 3: `ProjectArchived` through the outbox

**Files:**
- Create: `src/Modules/CloudGround.App.Modules.Projects.Contracts/ProjectArchived.cs` (`public sealed record ProjectArchived(Guid ProjectId, DateTimeOffset ArchivedAt)`)
- Modify: `Features/ArchiveProject.cs` (publish through `IDbContextOutbox<ProjectsDbContext>`; `SaveChangesAndFlushMessagesAsync`)
- Create: `src/Modules/CloudGround.App.Modules.Tasks/Features/CloseTasksOfArchivedProject.cs` (Wolverine handler `ProjectArchivedHandler` that closes the project's open tasks)
- Create: `tests/CloudGround.App.Integration.Tests/ProjectArchivedFlowTests.cs`

- [ ] **Step 1:** Failing test `Archiving_a_project_closes_its_open_tasks`: create a project and two tasks, complete one, archive the project, then poll `GET /api/projects/{id}/tasks` for up to 30 s until the open task has `ClosedAt`; the completed one keeps `ClosedAt == null`.
- [ ] **Step 2:** Run; expect FAIL (timeout).
- [ ] **Step 3:** Implement. The Wolverine handler is a plain class in the Tasks module; it loads open tasks of the project and calls `Close`. The Tasks module references only `Projects.Contracts`.
- [ ] **Step 4:** Run; expect PASS. Commit `feat: close a project's open tasks when it is archived`.

### Task 4: Architecture rules for contracts

**Files:**
- Modify: `tests/CloudGround.App.Architecture.Tests/{ModuleCatalog,ModuleBoundaryTests}.cs`, create `ContractTests.cs`

- [ ] **Step 1:** Tests: `Modules_reference_other_modules_only_through_contracts` (project references of every module project point only to `*.Contracts` projects), `Contracts_do_not_depend_on_asp_net_core_ef_core_or_wolverine`, `Contracts_do_not_depend_on_any_module`, and the existing `Modules_do_not_depend_on_each_others_internals`, which now has two modules.
- [ ] **Step 2:** Prove each rule with a mutation (Tasks references the Projects project; a contract record uses an EF Core type; a Tasks type uses `ProjectsDbContext` through `InternalsVisibleTo`), observe the failure, revert. Ledger each.
- [ ] **Step 3:** Run; expect PASS. Commit `test: enforce that modules meet only through contracts`.

### Task 5: Sample option, docs and pull request

- [ ] **Step 1:** `template.json`: the `(!sample)` exclusion also covers `tests/CloudGround.App.Integration.Tests/Tasks*.cs` and `ProjectArchived*.cs`; `Program.cs` and `AppHost.cs` conditionals cover the Tasks module and `tasks-migrations`. Extend `SampleOptionTests.SampleReferences` with `TasksModule`, `Modules.Tasks`, `tasks-migrations`.
- [ ] **Step 2:** Docs: generated README (contracts, messaging, how to add an integration event, RabbitMQ management UI in the dashboard), root README status (phase 3 of 7), NuGet README, `LayoutTests` for the new projects.
- [ ] **Step 3:** Both test commands; expect PASS. Commit `docs: describe contracts and messaging`.
- [ ] **Step 4:** Final review (fresh reviewer, most capable model), fix pass, push, pull request `feat: messaging`, CI green, squash-merge.
