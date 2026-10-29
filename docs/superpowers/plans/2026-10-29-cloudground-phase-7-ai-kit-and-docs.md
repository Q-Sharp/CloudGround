# CloudGround Phase 7: AI Collaboration Kit and Documentation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Every generated solution ships an AI collaboration kit (`AGENTS.md`, `CLAUDE.md`, two skills that create a module and add a feature, MCP configuration for the Angular CLI), architecture decision records and a README with C4 diagrams and a module map. The template repository gets its own ADRs, a skill for changing the template and its final README.

**Architecture:** The skills are Markdown procedures with bundled scaffold files. The template engine renames the scaffold files with the solution name, and the agent copies them and replaces a module token. A root test applies both skills mechanically to a solution generated with `--sample false`, adds a migration and runs the solution's whole test suite, so the scaffold files are proven to compile, follow the architecture rules and work end to end. The AppHost gets one extension method that wires a module's migrations, so adding a module to the AppHost is one line, and a convention test fails when a module has no migrations resource. Documentation tests check that every path and link in the Markdown files exists, in both template variants and in the repository, and that every ADR names its alternatives.

**Tech Stack:** Markdown, Mermaid flowcharts, Claude Code skills (`SKILL.md` with YAML front matter), the Angular CLI MCP server, Aspire 13.6.4, xUnit v3 on Microsoft.Testing.Platform.

**Spec:** `docs/superpowers/specs/2026-10-27-cloudground-design.md` (sections 3, 8.8, 8.9, 9)

## Global Constraints

- C# on .NET 10. Newest LTS, otherwise newest stable. No new third-party package; every package used is already in `Directory.Packages.props`.
- HTTP endpoints are Minimal APIs; no controllers. Endpoints call handlers directly.
- No explanatory comments in code or configuration, including YAML, JSON, `.editorconfig` and `.gitignore`. Tool directives without explanation are allowed. Markdown documents are content, not code.
- Warnings are errors; `dotnet format --verify-no-changes` must pass on every generated solution.
- Everything in English.
- Inside `templates/cloudground/`, `CloudGround.App`, `CloudGround_App` and `cloudground-app` are the placeholders for the solution name. A solution generated with `--sample false` mentions none of `ProjectsModule`, `Modules.Projects`, `projects-migrations`, `TasksModule`, `Modules.Tasks`, `tasks-migrations`.
- Conventional Commits. Commits end with the `Co-Authored-By` trailer of the agent; commit messages and pull request descriptions contain no links to agent sessions.
- No personal information in the repository.
- Spec 8.9: `AGENTS.md` is tool-neutral and covers architecture rules, module boundaries, how to add a feature, build and test commands, the Microsoft-first library rule, the no-comments rule, Conventional Commits, and modern-Angular rules. `CLAUDE.md` imports `AGENTS.md` and adds Claude Code specifics only.
- Spec 8.8: the generated README has C4 context and container diagrams in Mermaid, the module map, and how to run, test and add a feature. `docs/adr/` holds one ADR per significant decision in spec sections 7 and 8, with the alternatives and why they lost.
- Spec 9: the repository README shows within the first screen what CloudGround is, the one-command quickstart, the topology diagram and a "How this was built with AI" section that links to the evidence.

## Review Focus

1. A module whose name has two words, such as `OrderHistory`: its schema, migrations resource and route use the lowercase form `orderhistory` everywhere, and the architecture tests accept it. The skill test scaffolds `OrderHistory`.
2. A module added after other modules: its migrations run after the previous module's and the API waits for all of them. The AppHost convention test checks the chain.
3. A scaffolded module and feature must pass the formatting check that CI runs. The skill test runs `dotnet format --verify-no-changes`.
4. Paths in the documentation of a solution generated with `--sample false` that point at sample files. The documentation test runs on both variants.
5. A fresh clone where `npm ci` has not run yet: the Angular CLI MCP server still starts from the configured command. The MCP test runs on a freshly generated solution without installing the web app's packages.

---

### Task 1: Module migrations in the AppHost

**Files:**
- Create: `templates/cloudground/src/CloudGround.App.AppHost/ModuleMigrations.cs`
- Modify: `templates/cloudground/src/CloudGround.App.AppHost/AppHost.cs` (the `#if (sample)` migrations block)
- Test: `templates/cloudground/tests/CloudGround.App.Integration.Tests/AppHostModelTests.cs`

**Interfaces:**
- Produces: `public static IResourceBuilder<ProjectResource> WithModuleMigrations(this IResourceBuilder<ProjectResource> api, string module, IResourceBuilder<PostgresDatabaseResource> database)` in namespace `CloudGround.App.AppHost`. It adds the EF migrations resource `<module lowercase>-migrations` for `CloudGround.App.Modules.<module>.Persistence.<module>DbContext` from `../Modules/CloudGround.App.Modules.<module>/CloudGround.App.Modules.<module>.csproj`, references and waits for `database`, runs the update on start, publishes the bundle container on `mcr.microsoft.com/dotnet/aspnet:10.0-resolute`, waits for the completion of the previously added module migrations, and makes `api` wait for its completion. Task 2's skill tells the agent to add `api.WithModuleMigrations("<Module>", database);`.

- [ ] **Step 1: Write the convention test**

In `AppHostModelTests`, add `Every_module_applies_its_migrations_before_the_api_starts`. It reads the module project folders from `<AppHostDirectory>/../Modules` (every folder whose name starts with `CloudGround.App.Modules.` and does not end with `.Contracts`) and, for each in folder order, asserts:

```csharp
var migrations = Assert.IsType<EFMigrationResource>(appHost.Resources.Single(resource => resource.Name == $"{module.ToLowerInvariant()}-migrations"));
Assert.Contains(api.Annotations.OfType<WaitAnnotation>(), wait => wait.Resource == migrations && wait.WaitType == WaitType.WaitForCompletion);
```

and that each module's migrations after the first wait for the completion of the one before it. Folder order is ordinal by name; the AppHost adds the sample modules in that order.

- [ ] **Step 2: Run it against the current AppHost**

Run: `dotnet test --project templates/cloudground/tests/CloudGround.App.Integration.Tests --filter-method "*Every_module_applies_its_migrations_before_the_api_starts"`
Expected: PASS, because the sample wires both modules. Then remove `.WaitForCompletion(projectsMigrations)` from the tasks migrations, run again, expect FAIL on the chain assertion, and restore the line.

- [ ] **Step 3: Implement `WithModuleMigrations` and use it in `AppHost.cs`**

The `#if (sample)` block becomes `api.WithModuleMigrations("Projects", database).WithModuleMigrations("Tasks", database);`. The previous module's migrations are the last `EFMigrationResource` in `api.ApplicationBuilder.Resources` before the new one is added.

- [ ] **Step 4: Run the AppHost model and Compose tests**

Run: `dotnet test --project templates/cloudground/tests/CloudGround.App.Integration.Tests --filter-class "*AppHostModelTests" --filter-class "*MigrationComposeTests" --filter-class "*ComposeFileTests"`
Expected: PASS, including `Migrations_run_one_after_another` and `Migration_bundles_run_on_the_ubuntu_aspnet_image`.

- [ ] **Step 5: Commit**

```bash
git add templates/cloudground/src/CloudGround.App.AppHost templates/cloudground/tests/CloudGround.App.Integration.Tests/AppHostModelTests.cs
git commit -m "refactor: wire module migrations with one call"
```

### Task 2: Skill that creates a module

**Files:**
- Create: `templates/cloudground/.claude/skills/add-module/SKILL.md`
- Create under `templates/cloudground/.claude/skills/add-module/scaffold/`:
  - `src/Modules/CloudGround.App.Modules.ModuleName/CloudGround.App.Modules.ModuleName.csproj`
  - `src/Modules/CloudGround.App.Modules.ModuleName/ModuleNameModule.cs`
  - `src/Modules/CloudGround.App.Modules.ModuleName/ModuleNamePolicies.cs`
  - `src/Modules/CloudGround.App.Modules.ModuleName/Persistence/ModuleNameDbContext.cs`
  - `src/Modules/CloudGround.App.Modules.ModuleName.Contracts/CloudGround.App.Modules.ModuleName.Contracts.csproj`
  - `tests/CloudGround.App.Modules.ModuleName.Tests/CloudGround.App.Modules.ModuleName.Tests.csproj`
  - `tests/CloudGround.App.Modules.ModuleName.Tests/ModuleNameModuleTests.cs`
- Create: `tests/CloudGround.Templates.Tests/ModuleSkillTests.cs`, `tests/CloudGround.Templates.Tests/SkillScaffold.cs`

**Interfaces:**
- Consumes: `WithModuleMigrations` from Task 1.
- Produces: the module token `ModuleName` (PascalCase) and `modulename` (lowercase) in scaffold paths and contents. `SkillScaffold.ApplyModuleAsync(GeneratedSolution solution, string module)` returning `Task` copies the scaffold, replaces both tokens, then runs the CLI steps and the source edits the skill lists. Task 3 extends `SkillScaffold` with `ApplyExampleFeatureAsync(GeneratedSolution solution, string module)`.

The scaffold mirrors the *Projects* module: the module class with `Add<Module>Module` (Npgsql `DbContext` named `database` with the migrations history table in the module schema, the `Edit` policy requiring the `editor` role, `AddValidation`, `TryAddSingleton(TimeProvider.System)`) and `Map<Module>Module` (`MapGroup("/api/modulename").WithTags("ModuleName")`), internal `ModuleNamePolicies` with `Edit = "modulename:edit"` and `EditorRole = "editor"`, internal `ModuleNameDbContext` with `Schema = "modulename"`, an empty contracts project, and a test project whose test `Editing_requires_the_editor_role` builds a host with `AddModuleNameModule` and asserts that the `Edit` policy's `RolesAuthorizationRequirement` allows exactly `editor`.

`SKILL.md` front matter: `name: add-module`, a `description` that says when to use it (creating a new module, also in a solution without modules). Steps, each one command or one edit:

1. Pick the module name in PascalCase; the lowercase form names the schema, the route and the migrations resource.
2. Copy `scaffold/` to the solution root, replacing `ModuleName` and `modulename` in paths and contents.
3. `dotnet sln add` the three projects; `dotnet reference add` the module project to the API project.
4. In `src/CloudGround.App.Api/Program.cs`, add the `using`, `builder.Add<Module>Module();` after the other `Add…` calls and `app.Map<Module>Module();` after `app.MapDefaultEndpoints();` and the other module maps.
5. In `src/CloudGround.App.AppHost/AppHost.cs`, add `api.WithModuleMigrations("<Module>", database);` before `builder.TagImagesWithVersion();`.
6. Add contracts only when another module needs them; add features with the add-feature skill, which also creates the first migration.
7. Build and run the tests; the architecture and AppHost tests now include the module.

- [ ] **Step 1: Write the failing skill test**

`ModuleSkillTests.Scaffolded_module_follows_the_architecture_rules`: generate `Skills.Shop` with `--sample false`, `await SkillScaffold.ApplyModuleAsync(solution, "OrderHistory")`, then `dotnet build`, `dotnet format --verify-no-changes --no-restore`, and `dotnet test --no-build` for `tests/Skills.Shop.Architecture.Tests`, `tests/Skills.Shop.Modules.OrderHistory.Tests`, and `tests/Skills.Shop.Integration.Tests` filtered to `*AppHostModelTests`. Every exit code is 0, and the architecture test output reports the module (`ModuleCatalog` finds `Skills.Shop.Modules.OrderHistory`).

- [ ] **Step 2: Run it to see it fail**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-method "*Scaffolded_module_follows_the_architecture_rules"`
Expected: FAIL, because `.claude/skills/add-module/scaffold` does not exist in the generated solution.

- [ ] **Step 3: Write `SKILL.md` and the scaffold files; implement `SkillScaffold.ApplyModuleAsync` to follow the skill's steps**

The source edits insert lines at the anchors the skill names: after the last line starting with `builder.Add` that ends with `Module();` or after `builder.AddApiAuthentication();`; after `app.MapDefaultEndpoints();` or the last `app.Map…Module();`; before `builder.TagImagesWithVersion();`.

- [ ] **Step 4: Run the skill test**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-method "*Scaffolded_module_follows_the_architecture_rules"`
Expected: PASS. If `dotnet sln add` puts the projects outside the `/src/Modules/` and `/tests/` folders the existing solution uses, add `--solution-folder` to the skill step and the test.

- [ ] **Step 5: Commit**

```bash
git add templates/cloudground/.claude tests/CloudGround.Templates.Tests/ModuleSkillTests.cs tests/CloudGround.Templates.Tests/SkillScaffold.cs
git commit -m "feat: skill that creates a module"
```

### Task 3: Skill that adds a feature

**Files:**
- Create: `templates/cloudground/.claude/skills/add-feature/SKILL.md`
- Create under `templates/cloudground/.claude/skills/add-feature/example/`:
  - `module/Domain/Item.cs`
  - `module/Persistence/ItemConfiguration.cs`
  - `module/Features/CreateItem.cs`
  - `module/Features/GetItem.cs`
  - `module/Features/ItemResponse.cs`
  - `module/Features/ItemMapper.cs`
  - `module-tests/ItemTests.cs`
  - `integration-tests/ItemsApiTests.cs`
- Modify: `tests/CloudGround.Templates.Tests/SkillScaffold.cs`, `tests/CloudGround.Templates.Tests/ModuleSkillTests.cs`

**Interfaces:**
- Consumes: `SkillScaffold.ApplyModuleAsync` and the module token from Task 2.
- Produces: `SkillScaffold.ApplyExampleFeatureAsync(GeneratedSolution solution, string module)`.

The example is one slice in the *Projects* shape, named `Item` and using the `ModuleName` token: an `Item` entity with `Name` (`NameMaxLength = 100`), `CreatedAt` and a version-7 `Guid` id; its configuration with the maximum length and a row version; `CreateItemRequest` with `[Required, StringLength(Item.NameMaxLength)]`, `CreateItemHandler`, and `MapCreateItem` requiring `ModuleNamePolicies.Edit` and returning `CreatedAtRoute`; `GetItemHandler` and `MapGetItem` (anonymous, `404` when missing); `ItemResponse` and a Mapperly `ItemMapper`. `ItemTests.Created_item_keeps_its_name_and_creation_time` is the unit test. `ItemsApiTests` has `Created_item_can_be_read_anonymously`, `Writes_need_the_editor_role` and `Blank_name_is_rejected` against `/api/modulename/`, using `AppHostFixture.CreateApiClientAsync("alice")`, `CreateApiClientAsync("bob")` and `CreateApiClient()`.

`SKILL.md` front matter: `name: add-feature`, a `description` that says when to use it (adding an endpoint, a use case or a page to an existing module). Steps: the endpoint group and the handler in one file per use case under `Features/`; validation attributes on the request; persistence (`DbSet` on the module's `DbContext`, a configuration class, a migration with the pinned `dotnet ef` exactly as the README's "Add a migration" section shows); Mapperly mapping; register the handlers in `Add<Module>Module` and chain the `Map…` calls in `Map<Module>Module`; tests at each level (unit next to the module, integration through the API, end to end only for a page); optional UI following `AGENTS.md`'s Angular rules, with the route in `src/CloudGround.App.Web/src/app/app.routes.ts` and its render mode in `src/CloudGround.App.Web/src/app/app.routes.server.ts`; caching and integration events point to the README sections that already describe them.

- [ ] **Step 1: Extend the skill test**

Add `ModuleSkillTests.Scaffolded_feature_passes_the_solution_tests`: generate `Feature.Shop` with `--sample false`, apply the module `OrderHistory` and the example feature, `dotnet tool restore`, add the migration `AddItems` with `ConnectionStrings__database=Host=localhost` as the README shows, `dotnet build`, `dotnet format --verify-no-changes --no-restore`, then `DotnetCli.TestArguments("Feature.Shop.slnx")`. Every exit code is 0, and the test output names `Created_item_can_be_read_anonymously`.

- [ ] **Step 2: Run it to see it fail**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-method "*Scaffolded_feature_passes_the_solution_tests"`
Expected: FAIL, because `.claude/skills/add-feature/example` does not exist.

- [ ] **Step 3: Write `SKILL.md`, the example files and `ApplyExampleFeatureAsync`**

`ApplyExampleFeatureAsync` copies `module/` into the module project, `module-tests/` into the module's test project and `integration-tests/` into the integration test project, replaces the module tokens, adds `public DbSet<Item> Items => Set<Item>();` to the `DbContext`, registers `CreateItemHandler` and `GetItemHandler` after `TryAddSingleton(TimeProvider.System);`, and chains `.MapCreateItem().MapGetItem()` after `.WithTags("<Module>")`.

- [ ] **Step 4: Run both skill tests**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-class "*ModuleSkillTests"`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add templates/cloudground/.claude tests/CloudGround.Templates.Tests
git commit -m "feat: skill that adds a feature slice"
```

### Task 4: Agent rules, Claude Code instructions and the Angular MCP server

**Files:**
- Create: `templates/cloudground/AGENTS.md`, `templates/cloudground/CLAUDE.md`, `templates/cloudground/.mcp.json`
- Create: `tests/CloudGround.Templates.Tests/AiKitTests.cs`, `tests/CloudGround.Templates.Tests/StdioMcpClient.cs`
- Modify: `tests/CloudGround.Templates.Tests/LayoutTests.cs`

**Interfaces:**
- Consumes: the skill folders from Tasks 2 and 3.
- Produces: `StdioMcpClient.ListToolsAsync(string command, IReadOnlyList<string> arguments, string workingDirectory, CancellationToken cancellationToken)` returning `Task<IReadOnlyList<string>>`; it sends `initialize`, the `notifications/initialized` notification and `tools/list` as newline-delimited JSON-RPC over standard input and returns the tool names. `SkillFrontMatter.Read(string skillFile)` returning `(string Name, string Description)`, used by Task 6 for the repository's skill.

`AGENTS.md` sections: the solution in one paragraph and its folders; commands (run, test, end-to-end filter, format check, web app `npm ci`/`npm run build`/`npm test`, migrations through the README); architecture rules matching the architecture tests listed in the README; how to add a module or a feature (the two skills, readable without Claude Code); libraries (Microsoft first, the third-party packages in use and what each is for, no MediatR, no AutoMapper, no Swashbuckle, no controllers); code rules (no explanatory comments and the allowed tool directives, warnings as errors, nullable, xUnit v3 with FakeItEasy, tests first); Angular rules (standalone components without NgModules, signals for state, `input()`/`output()`/`inject()`, built-in control flow `@if`/`@for`/`@switch` instead of `*ngIf`/`*ngFor`, zoneless change detection without zone.js, render mode per route, relative `/api` and `/bff` calls only, no tokens in the browser); versions (newest LTS, otherwise newest stable; major updates of LTS-bound components are deliberate pull requests with an ADR); commits and pull requests (Conventional Commits, squash merges); decisions (new significant decisions get an ADR in `docs/adr/`).

`CLAUDE.md`: first line `@AGENTS.md`, then the Claude Code specifics: `/add-module` and `/add-feature`, and the `angular-cli` MCP server from `.mcp.json`, which Claude Code asks to approve once.

`.mcp.json`: `{"mcpServers": {"angular-cli": {"command": "npx", "args": ["-y", "@angular/cli", "mcp"]}}}`, the configuration angular.dev documents.

- [ ] **Step 1: Write the failing tests**

`LayoutTests.Generated_solution_has_root_file` gets the rows `AGENTS.md`, `CLAUDE.md`, `.mcp.json`, `.claude/skills/add-module/SKILL.md`, `.claude/skills/add-feature/SKILL.md`. `AiKitTests`:
- `Claude_instructions_import_the_agent_rules`: the first line of `CLAUDE.md` is `@AGENTS.md`.
- `Skills_are_named_after_their_folder`: for every `.claude/skills/*/SKILL.md`, the front matter `name` equals the folder name and matches `^[a-z0-9-]{1,64}$`, and `description` is not empty and at most 1024 characters.
- `Angular_mcp_server_starts_from_the_configured_command`: on a freshly generated solution without `npm ci`, the `angular-cli` entry of `.mcp.json` lists tools that include `get_best_practices`, `search_documentation` and `list_projects`.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-class "*AiKitTests" --filter-class "*LayoutTests"`
Expected: FAIL on the missing files.

- [ ] **Step 3: Write `AGENTS.md`, `CLAUDE.md`, `.mcp.json`, `StdioMcpClient` and `SkillFrontMatter`**

- [ ] **Step 4: Run the tests**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-class "*AiKitTests" --filter-class "*LayoutTests" --filter-class "*NamingTests" --filter-class "*SampleOptionTests"`
Expected: PASS; the naming and sample tests prove that the new Markdown files carry no source name form and no sample reference.

- [ ] **Step 5: Commit**

```bash
git add templates/cloudground/AGENTS.md templates/cloudground/CLAUDE.md templates/cloudground/.mcp.json tests/CloudGround.Templates.Tests
git commit -m "feat: agent rules and angular mcp server in generated solutions"
```

### Task 5: Architecture decision records and the README of a generated solution

**Files:**
- Create: `templates/cloudground/docs/adr/README.md` and one file per decision, `templates/cloudground/docs/adr/NNNN-<slug>.md`
- Modify: `templates/cloudground/README.md`, `templates/cloudground/.template.config/template.json` (only if Markdown conditionals need an operation, see Step 3)
- Create: `tests/CloudGround.Templates.Tests/DocumentationTests.cs`, `tests/CloudGround.Templates.Tests/MarkdownDocuments.cs`

**Interfaces:**
- Produces: `MarkdownDocuments.BrokenReferences(string root, IEnumerable<string> markdownFiles)` returning the list of `file: reference` strings whose target does not exist, and `MarkdownDocuments.AdrViolations(string adrDirectory)` returning the ADRs that lack a section or are missing from the index. Task 6 uses both for the repository.

References are relative Markdown links (not `http`, `https`, `mailto` or `#`), resolved against the file's folder, and inline code spans outside fenced blocks that start with `src/`, `tests/`, `docs/`, `.github/`, `.claude/`, `.devcontainer/` or `.config/` and contain no `<`, `*`, `/bin/` or `/obj/`, resolved against the root. Every Markdown file of the solution except `CHANGELOG.md` is checked.

ADR format:

```markdown
# 0001. <Decision>

Status: Accepted

## Context
## Decision
## Alternatives
- **<Alternative>**: why it lost
## Consequences
```

The ADRs, in this order: modular monolith with feature slices; Minimal APIs with handlers called directly; Aspire for orchestration; Microsoft libraries first; PostgreSQL and EF Core with a schema per module; migrations through Aspire and migration bundles; `HybridCache` on Garnet; RabbitMQ and Wolverine with a transactional outbox; backend for frontend with cookie sessions and YARP; Keycloak locally and any OpenID Connect provider in operation; Angular with server-side rendering per route; Mapperly; architecture tests with ArchUnitNET; the test stack (xUnit v3, FakeItEasy, `Aspire.Hosting.Testing`, Playwright); OpenTelemetry; container images (SDK container publishing on Ubuntu chiseled images, Dockerfile for the web app); Docker Compose from Aspire's publish pipeline; versioning with Conventional Commits, release-please and MinVer; version policy and Dependabot; the devcontainer; the AI collaboration kit. ADRs describe the decision without naming the sample modules, so they hold in both variants.

README changes:
- "Architecture" starts with "System context" (person, the solution, identity provider, OTLP backend) and "Containers" (browser, BFF, web app, API, PostgreSQL, Garnet, RabbitMQ, Keycloak, migrations), both Mermaid `flowchart` diagrams in C4 notation, and a "Module map": with the sample, a diagram of the API host, both modules, their contracts project, `IProjectCatalog` and `ProjectArchived`; without it, one sentence that the solution has no modules yet and that the add-module skill creates the first.
- New sections "Add a module" and "Add a feature" that point to the skill files, which are readable without Claude Code.
- New section "Working with AI agents": `AGENTS.md`, `CLAUDE.md`, the skills, the MCP server, and the guardrails CI enforces.
- New section "Decisions" that links `docs/adr/README.md`.
- Paths of the web app are given from the solution root, such as `src/CloudGround.App.Web/src/app/app.routes.server.ts`; the sentences about the sample's routes and pages are conditional on `sample`.

- [ ] **Step 1: Write the failing tests**

`DocumentationTests`:
- `Documented_paths_exist_in_the_generated_solution(string sample)` for `true` and `false`: `BrokenReferences` is empty.
- `Decision_records_name_their_alternatives(string sample)` for `true` and `false`: `AdrViolations` is empty and `docs/adr` holds at least 20 ADRs.
- `Readme_shows_the_c4_diagrams`: the README has the headings `### System context` and `### Containers`, each followed by a `mermaid` block.
- `Module_map_names_every_module(string sample)` for `true` and `false`: the "Module map" section names every folder under `src/Modules` that does not end with `.Contracts`, by module name; without the sample it links `.claude/skills/add-module/SKILL.md`.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-class "*DocumentationTests"`
Expected: FAIL: no `docs/adr`, no diagrams, and the README's web app paths (`src/app/...`, `src/instrumentation.mjs`) do not resolve from the solution root.

- [ ] **Step 3: Write the ADRs and the README changes**

If `<!--#if (sample) -->` in `README.md` is left in the generated file, add a `SpecialCustomOperations` entry for `**/*.md` with the conditional operation in `template.json`; `SampleOptionTests.Generated_solutions_contain_no_template_directives` shows it.

- [ ] **Step 4: Run the documentation, sample and naming tests**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-class "*DocumentationTests" --filter-class "*SampleOptionTests" --filter-class "*NamingTests"`
Expected: PASS.

- [ ] **Step 5: Render every Mermaid block**

Run `npx -y @mermaid-js/mermaid-cli` on each `mermaid` block of the generated README of both variants, with Playwright's Chromium as Puppeteer's browser.
Expected: an SVG per block and no `Syntax error` or `Parse error` in the output.

- [ ] **Step 6: Commit**

```bash
git add templates/cloudground/README.md templates/cloudground/docs templates/cloudground/.template.config tests/CloudGround.Templates.Tests
git commit -m "docs: decision records, c4 diagrams and module map in generated solutions"
```

### Task 6: The template repository's decisions, skill and README

**Files:**
- Create: `docs/adr/README.md` and `docs/adr/NNNN-<slug>.md`
- Create: `.claude/skills/change-template/SKILL.md`
- Modify: `README.md`, `AGENTS.md`, `CLAUDE.md`, `CONTRIBUTING.md`, `docs/nuget/README.md`, `docs/superpowers/specs/2026-10-27-cloudground-design.md`
- Modify: `tests/CloudGround.Templates.Tests/DocumentationTests.cs`, `tests/CloudGround.Templates.Tests/AiKitTests.cs`

**Interfaces:**
- Consumes: `MarkdownDocuments.BrokenReferences`, `MarkdownDocuments.AdrViolations` (Task 5), `SkillFrontMatter.Read` (Task 4).

Repository ADRs: the template content is a complete solution of its own (two solutions); the dotted source name `CloudGround.App` and its derived forms; template tests pack and install into an isolated template hive and build what they generate; CI runs the generated solution's own composite action; the sample is a template option with conditional content; design, plans and written rules steer the AI; releases with release-please, MinVer and a publish job on the release tag.

`change-template` skill (`name: change-template`): where a change belongs (template content, root tests, both), the placeholders and their forms, how to make content sample-only (`#if (sample)` in C#, `<!--#if (sample) -->` in XML and Markdown, `//#if (sample)` in TypeScript, the exclude list in `template.json`), generated output that must stay out of the package (`CloudGround.Templates.csproj` and `template.json`), Dependabot directories, and the two test commands that must pass.

README: within the first screen what CloudGround is, the quickstart (`dotnet new install` from source until the package is on NuGet, `dotnet new cloudground`, `dotnet run --project src/<Name>.AppHost`), the topology diagram from spec 8.1, and "How this was built with AI": the design with its decision markers, one plan per phase, written rules in `AGENTS.md`, tests written first, a separate review of every branch, CI on every pull request and commits with AI co-author trailers, each linked. The status note says that all seven phases are built. The feature list gains the AI collaboration kit and the ADRs.

`AGENTS.md` (repository) gains the ADR folder and the skill; `CLAUDE.md` names `/change-template`; `CONTRIBUTING.md` links the ADRs and the skill; `docs/nuget/README.md` names the AI kit and the ADRs. Spec: status line "Implemented in phases 1–7" and a **Builder** (2026-10-29) note in 8.9 for the bundled scaffolds, `WithModuleMigrations`, `.mcp.json` with the documented `npx` configuration, C4 diagrams as Mermaid flowcharts, and the ADR format.

- [ ] **Step 1: Write the failing tests**

- `DocumentationTests.Documented_paths_exist_in_the_repository`: `BrokenReferences` over every Markdown file of the repository outside `templates/`, `docs/superpowers/`, `artifacts/`, `node_modules/` and except `CHANGELOG.md` is empty.
- `DocumentationTests.Repository_decision_records_name_their_alternatives`: `AdrViolations` on `docs/adr` is empty and it holds at least 7 ADRs.
- `AiKitTests.Repository_skills_are_named_after_their_folder`: as in Task 4, for `.claude/skills` of the repository.
- `DocumentationTests.Repository_readme_links_the_evidence_of_ai_assisted_development`: the section `## How this was built with AI` links `docs/superpowers/specs/`, `docs/superpowers/plans/`, `docs/adr/` and `AGENTS.md`.

- [ ] **Step 2: Run them to see them fail**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-method "*Repository*"`
Expected: FAIL: no `docs/adr`, no repository skill, no such README section.

- [ ] **Step 3: Write the ADRs, the skill, and the document changes**

- [ ] **Step 4: Run the repository tests**

Run: `dotnet test --project tests/CloudGround.Templates.Tests --filter-method "*Repository*"`
Expected: PASS.

- [ ] **Step 5: Run both suites**

Run: `dotnet test --solution CloudGround.Templates.slnx` and `dotnet test --solution templates/cloudground/CloudGround.App.slnx`
Expected: both PASS.

- [ ] **Step 6: Commit**

```bash
git add docs/adr .claude README.md AGENTS.md CLAUDE.md CONTRIBUTING.md docs/nuget/README.md docs/superpowers/specs tests/CloudGround.Templates.Tests
git commit -m "docs: repository decisions, template skill and final readme"
```
