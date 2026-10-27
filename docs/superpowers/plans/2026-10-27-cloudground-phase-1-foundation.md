# CloudGround Phase 1: Foundation Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A packable `dotnet new cloudground` template whose generated solution (Aspire AppHost, ServiceDefaults, Minimal API host, integration test) builds, runs and passes its tests, with automatic SemVer, CI, Dependabot and community files, landed as the first pull request on the public `Q-Sharp/CloudGround` repository.

**Architecture:** The repository root holds a packaging project, a template test project and the repository's own GitHub setup. `templates/cloudground/` holds the template content, which is itself a complete solution with source name `CloudGround.App`. Template tests pack the template, install it into an isolated template hive, generate solutions and build them, so every test exercises what a user actually installs.

**Tech Stack:** .NET 10 SDK, Aspire 13.6.4, xUnit v3, MinVer, release-please, GitHub Actions, Dependabot.

**Spec:** `docs/superpowers/specs/2026-10-27-cloudground-design.md`

## Phase roadmap

The spec covers several subsystems that build on each other. Each phase gets its own plan when the previous phase has landed, and each lands as one pull request with working, tested software.

| Phase | Delivers |
|---|---|
| **1 Foundation (this plan)** | template package, generated Aspire solution skeleton, template tests, SemVer, CI, Dependabot, community files, public repository |
| 2 Modules and data | module registration, PostgreSQL + EF Core with a schema per module, `ProblemDetails`, built-in validation, Mapperly, `HybridCache` + Garnet, architecture tests, Projects module, `--sample` option |
| 3 Messaging | RabbitMQ + Wolverine with outbox and inbox, Tasks module, `ProjectArchived`, event integration test, tracing through messages |
| 4 BFF and login | Keycloak realm import, BFF with OIDC cookie session, YARP to the API, antiforgery, role checks, persisted session and Data Protection keys |
| 5 Frontend | Angular SSR (newest LTS), Angular Material, render modes, public page and authenticated area, Node OpenTelemetry, ESLint, Angular unit tests, Dockerfile, Playwright E2E |
| 6 Operations | SDK container images, Compose publish and validity check, versioned image tags in CI, devcontainer, Dependabot for npm and Docker |
| 7 AI kit and documentation | `AGENTS.md`/`CLAUDE.md` for generated solutions, `.claude/` skills, Angular MCP configuration, ADRs, C4 diagrams, final READMEs |

## Global Constraints

- C# on .NET 10. Newest LTS of everything; newest stable where a component has no LTS line (spec 7.4).
- Pinned versions for this phase (newest stable on 2026-10-27; re-check before starting): `Aspire.AppHost.Sdk`, `Aspire.Hosting.AppHost`, `Aspire.Hosting.Testing` 13.6.4; `Microsoft.Extensions.ServiceDiscovery`, `Microsoft.Extensions.Http.Resilience` 10.11.0; `OpenTelemetry.Exporter.OpenTelemetryProtocol`, `OpenTelemetry.Extensions.Hosting`, `OpenTelemetry.Instrumentation.AspNetCore`, `OpenTelemetry.Instrumentation.Http`, `OpenTelemetry.Instrumentation.Runtime` 1.20.1; `xunit.v3` 4.0.2; `xunit.runner.visualstudio` 4.0.1; `Microsoft.NET.Test.Sdk` 18.11.0; `MinVer` 8.0.0. Actions: `actions/checkout@v7`, `actions/setup-dotnet@v6`, `googleapis/release-please-action@v5`, `amannn/action-semantic-pull-request@v6`.
- HTTP endpoints are Minimal APIs; no controllers.
- .NET tests use xUnit v3. xUnit v3 test projects are executables (`<OutputType>Exe</OutputType>`).
- Microsoft libraries first; no third-party package beyond the ones named in this plan.
- No explanatory comments in code or configuration, including YAML, `.editorconfig` and `.gitignore`. Tool directives without explanation are allowed. Comments that `dotnet new` or Aspire scaffolding produces are removed when the file is created.
- Everything in English. MIT license. Author and copyright holder: `Q-Sharp`. No other personal information anywhere in the repository.
- Commit messages and pull request titles follow Conventional Commits. Every commit ends with the trailer `Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>` and contains no session links. Pull requests are squash-merged.
- Folder layout as in spec 8.12 and section 9. The template source name is `CloudGround.App`, so `-n Contoso.Shop` turns `CloudGround.App.Api` into `Contoso.Shop.Api` and `CloudGround_App_Api` into `Contoso_Shop_Api`.
- Warnings are errors everywhere. Must work on Linux with Docker or Podman; this phase needs no containers.

## Review Focus

1. **A freshly generated solution outside any git repository** (the state right after `dotnet new`, before `git init`) must build. MinVer warns `MINVER1001` there, and warnings are errors. Pinned in Task 4 by `Generated_solution_builds_without_git_and_gets_prerelease_version`.
2. **A lowercase, hyphenated name** like `-n my-shop` must produce valid C# namespaces and identifiers and build. Pinned in Task 3 by `Hyphenated_lowercase_name_builds`.
3. **Dotfiles in the package**: NuGet's default excludes drop `.gitignore`, `.editorconfig` and `.github/`, so they would be missing from generated solutions. Pinned in Task 1 by `Package_contains_template_dotfiles`, and the layout tests run against the installed package, not the folder.
4. **Leftover source-name forms** such as `Projects.CloudGround_App_Api` or `cloudground.app` in generated files or paths. Pinned in Task 3 by `Generated_solution_contains_no_source_name_form`.
5. **Build output leaking into the package** after someone has built the template content locally (`bin/`, `obj/`, `.vs/`, `*.user`). Pinned in Task 1 by `Package_contains_no_build_output`.

---

### Task 0: Branch

- [ ] **Step 1: Create the phase branch from `main`**

Run: `git -C ~/Sources/CloudGround switch -c feat/foundation`
Expected: `Switched to a new branch 'feat/foundation'`

### Task 1: Template package, test harness and root files

**Files:**
- Create: `global.json`, `Directory.Build.props`, `Directory.Packages.props`, `.editorconfig`, `.gitignore`, `CloudGround.Templates.csproj`, `CloudGround.Templates.slnx`
- Create: `templates/cloudground/.template.config/template.json`
- Create: `templates/cloudground/global.json`, `templates/cloudground/Directory.Build.props`, `templates/cloudground/Directory.Packages.props`, `templates/cloudground/.editorconfig`, `templates/cloudground/.gitignore`, `templates/cloudground/CloudGround.App.slnx`
- Create: `tests/CloudGround.Templates.Tests/CloudGround.Templates.Tests.csproj`, `DotnetCli.cs`, `RepositoryRoot.cs`, `TemplatePackage.cs`, `GeneratedSolution.cs`, `TemplateCollection.cs`, `PackageContentTests.cs`, `LayoutTests.cs`

**Interfaces:**
- Produces:
  - `sealed record CliResult(int ExitCode, string Output)`
  - `static class DotnetCli { static Task<CliResult> RunAsync(string workingDirectory, params string[] arguments); }`: runs `dotnet` with stdout and stderr merged into `Output`; fails the test with the output after a 10-minute timeout.
  - `static class RepositoryRoot { static string Find(); }`: walks up from `AppContext.BaseDirectory` to the directory containing `CloudGround.Templates.slnx`.
  - `sealed class TemplatePackage : IAsyncLifetime`: `string PackagePath`; `ValueTask<GeneratedSolution> GenerateAsync(string name)`. `InitializeAsync` packs `CloudGround.Templates.csproj` into a temporary directory and installs the package with `dotnet new install <nupkg> --debug:custom-hive <hive>`. `GenerateAsync` runs `dotnet new cloudground -n <name> -o <temp>/<name> --debug:custom-hive <hive>` in a fresh temporary directory outside any git repository. `DisposeAsync` deletes all temporary directories.
  - `sealed record GeneratedSolution(string Root, string Name) { IReadOnlyList<string> RelativeFiles(); }`: paths relative to `Root`, using `/`.
  - `[CollectionDefinition(TemplateCollection.Name)] sealed class TemplateCollection : ICollectionFixture<TemplatePackage> { public const string Name = "template"; }`. Every template test class is in this collection, so packing happens once and tests run sequentially.

- [ ] **Step 1: Create the root build files**

`global.json`: SDK `10.0.100` with `rollForward: latestFeature`; `msbuild-sdks` entry `Aspire.AppHost.Sdk: 13.6.4`. `Directory.Build.props`: `TargetFramework net10.0`, `Nullable enable`, `ImplicitUsings enable`, `TreatWarningsAsErrors true`, `AnalysisLevel latest-recommended`, `EnforceCodeStyleInBuild true`. `Directory.Packages.props`: `ManagePackageVersionsCentrally true` and the versions from Global Constraints that the root projects use. `.editorconfig` with `root = true` and the usual .NET conventions. `.gitignore` from `dotnet new gitignore`, comments removed. The same five files go into `templates/cloudground/`, with the template's `Directory.Packages.props` listing only what the template content uses.

- [ ] **Step 2: Create the test project with the harness from the Interfaces block and the failing tests**

`PackageContentTests` (collection `template`), reading the `.nupkg` as a zip archive:
```csharp
[Fact] public void Package_contains_template_dotfiles()
{
    var entries = ReadEntries(package.PackagePath);
    Assert.Contains(entries, e => e.EndsWith("cloudground/.gitignore"));
    Assert.Contains(entries, e => e.EndsWith("cloudground/.editorconfig"));
    Assert.Contains(entries, e => e.EndsWith("cloudground/.template.config/template.json"));
}

[Fact] public void Package_contains_no_build_output()
{
    var entries = ReadEntries(package.PackagePath);
    Assert.DoesNotContain(entries, e => e.Contains("/bin/") || e.Contains("/obj/") || e.Contains("/.vs/") || e.EndsWith(".user"));
}
```
`LayoutTests` (collection `template`), on `await package.GenerateAsync("Contoso.Shop")`:
```csharp
[Theory]
[InlineData("Contoso.Shop.slnx")]
[InlineData("global.json")]
[InlineData("Directory.Build.props")]
[InlineData("Directory.Packages.props")]
[InlineData(".editorconfig")]
[InlineData(".gitignore")]
public async Task Generated_solution_has_root_file(string relativePath)
```
asserting that `solution.RelativeFiles()` contains `relativePath`.

- [ ] **Step 3: Run the tests and confirm they fail**

Run: `dotnet test CloudGround.Templates.slnx`
Expected: FAIL, because `CloudGround.Templates.csproj` and `template.json` do not exist yet, so packing fails in `TemplatePackage.InitializeAsync`.

- [ ] **Step 4: Create `CloudGround.Templates.csproj`, `CloudGround.Templates.slnx` and `template.json`**

Packaging project: `PackageType Template`, `PackageId CloudGround.Templates`, `Authors Q-Sharp`, `PackageLicenseExpression MIT`, `IncludeBuildOutput false`, `IncludeContentInPack true`, `NoDefaultExcludes true` (keeps dotfiles), `EnableDefaultItems false` (the project sits at the root, so default globs would compile `tests/` and `templates/`), and content `templates/**/*` packed under `content`, excluding `**/bin/**`, `**/obj/**`, `**/.vs/**`, `**/*.user`. The root `.slnx` contains the packaging project and the test project.
`template.json`: `identity CloudGround.Templates.Solution`, `shortName cloudground`, `name CloudGround`, `author Q-Sharp`, `sourceName CloudGround.App`, `preferNameDirectory true`, `classifications [Web, Aspire, Cloud, Solution]`, `tags { language: C#, type: solution }`, and a `sources` modifier excluding `**/bin/**`, `**/obj/**`, `**/.vs/**`, `**/*.user`. The template's `CloudGround.App.slnx` is empty for now.

- [ ] **Step 5: Run the tests and confirm they pass**

Run: `dotnet test CloudGround.Templates.slnx`
Expected: PASS (8 tests).

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: add template package and template test harness" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 2: Aspire solution skeleton in the template content

**Files:**
- Create: `templates/cloudground/src/CloudGround.App.AppHost/CloudGround.App.AppHost.csproj`, `AppHost.cs`, `Properties/launchSettings.json`, `appsettings.json`
- Create: `templates/cloudground/src/CloudGround.App.ServiceDefaults/CloudGround.App.ServiceDefaults.csproj`, `Extensions.cs`
- Create: `templates/cloudground/src/CloudGround.App.Api/CloudGround.App.Api.csproj`, `Program.cs`, `Properties/launchSettings.json`, `appsettings.json`
- Create: `templates/cloudground/tests/CloudGround.App.Integration.Tests/CloudGround.App.Integration.Tests.csproj`, `ApiHealthTests.cs`
- Modify: `templates/cloudground/CloudGround.App.slnx`, `templates/cloudground/Directory.Packages.props`, `templates/cloudground/.template.config/template.json`

**Interfaces:**
- Produces: AppHost resource name `api` for `CloudGround.App.Api`; `IHostApplicationBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)` and `WebApplication MapDefaultEndpoints(this WebApplication app)` in `CloudGround.App.ServiceDefaults`, as Aspire's ServiceDefaults template defines them; health endpoint `/health` and liveness endpoint `/alive`.

- [ ] **Step 1: Scaffold with Aspire's templates, then trim**

Run in a scratch directory: `dotnet new install Aspire.ProjectTemplates::13.6.4`, then `dotnet new aspire-apphost` and `dotnet new aspire-servicedefaults` with `-n CloudGround.App.AppHost` and `-n CloudGround.App.ServiceDefaults`. Move the results into `templates/cloudground/src/`, remove every comment, move package versions into the template's `Directory.Packages.props`, and drop properties that `Directory.Build.props` already sets. The AppHost uses `<Project Sdk="Aspire.AppHost.Sdk">` with the SDK version taken from `global.json`. List the AppHost's `UserSecretsId` GUID under `guids` in `template.json`, so every generated solution gets a new one.

- [ ] **Step 2: Write the failing integration test**

```csharp
public sealed class ApiHealthTests
{
    [Fact]
    public async Task Api_reports_healthy()
    {
        var cancellation = TestContext.Current.CancellationToken;
        var appHost = await DistributedApplicationTestingBuilder.CreateAsync<Projects.CloudGround_App_AppHost>(cancellation);
        await using var app = await appHost.BuildAsync(cancellation);
        await app.StartAsync(cancellation);
        await app.ResourceNotifications.WaitForResourceHealthyAsync("api", cancellation);

        using var client = app.CreateHttpClient("api");
        using var response = await client.GetAsync("/health", cancellation);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(cancellation));
    }
}
```

- [ ] **Step 3: Run it and confirm it fails**

Run: `dotnet test templates/cloudground/CloudGround.App.slnx`
Expected: FAIL to compile, because `CloudGround.App.Api` does not exist and the AppHost registers no `api` resource.

- [ ] **Step 4: Create the API host and register it**

`CloudGround.App.Api` is a `Microsoft.NET.Sdk.Web` project referencing ServiceDefaults. Its `Program.cs` calls `builder.AddServiceDefaults()` and `app.MapDefaultEndpoints()` and nothing else. `AppHost.cs` adds `builder.AddProject<Projects.CloudGround_App_Api>("api")`. Add all projects to `CloudGround.App.slnx`.

- [ ] **Step 5: Run it and confirm it passes**

Run: `dotnet test templates/cloudground/CloudGround.App.slnx`
Expected: PASS (1 test).

- [ ] **Step 6: Extend `LayoutTests` with the project files and run the template tests**

Add `[InlineData]` rows for `src/Contoso.Shop.AppHost/Contoso.Shop.AppHost.csproj`, `src/Contoso.Shop.ServiceDefaults/Contoso.Shop.ServiceDefaults.csproj`, `src/Contoso.Shop.Api/Contoso.Shop.Api.csproj` and `tests/Contoso.Shop.Integration.Tests/Contoso.Shop.Integration.Tests.csproj`.
Run: `dotnet test CloudGround.Templates.slnx`
Expected: PASS.

- [ ] **Step 7: Commit**

```bash
git add -A
git commit -m "feat: add Aspire solution skeleton to the template" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 3: Name replacement that holds for every project name

**Files:**
- Create: `tests/CloudGround.Templates.Tests/NamingTests.cs`
- Modify: `templates/cloudground/.template.config/template.json`, and template content files where a test shows a wrong form

**Interfaces:**
- Consumes: `TemplatePackage.GenerateAsync`, `GeneratedSolution.RelativeFiles`, `DotnetCli.RunAsync` from Task 1.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public async Task Generated_solution_contains_no_source_name_form()
```
Generate `Contoso.Shop`. Assert that no relative path and no file content contains, case-insensitively, `cloudground.app` or `cloudground_app`.
```csharp
[Fact] public async Task Generated_solution_builds_and_its_tests_pass()
```
Generate `Contoso.Shop`. Assert `ExitCode == 0` for `dotnet build` and for `dotnet test` in `solution.Root`, and show `Output` in the assertion message.
```csharp
[Fact] public async Task Hyphenated_lowercase_name_builds()
```
Generate `my-shop`. Assert `ExitCode == 0` for `dotnet build`, and assert that `src/my-shop.Api/Program.cs` exists.

- [ ] **Step 2: Run them**

Run: `dotnet test CloudGround.Templates.slnx --filter "FullyQualifiedName~NamingTests"`
Expected: at least `Hyphenated_lowercase_name_builds` fails, because `my-shop` is no valid namespace when the plain name is used in code.

- [ ] **Step 3: Make the derived forms right**

In `template.json`, define the name forms so that file and project names use the plain name, C# namespaces use the `safe_namespace` form and identifiers use the `safe_name` form (see the dotnet/templating reference for `sourceName` and `forms`). Wherever the template content cannot express a form through replacement alone, add a derived symbol. The build must not depend on Aspire's `Projects.*` type names matching by chance.

- [ ] **Step 4: Run all template tests**

Run: `dotnet test CloudGround.Templates.slnx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "fix: replace every form of the source name in generated solutions" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 4: Automatic SemVer with MinVer and release-please

**Files:**
- Create: `tests/CloudGround.Templates.Tests/VersioningTests.cs`
- Create: `release-please-config.json`, `.release-please-manifest.json`, `templates/cloudground/release-please-config.json`, `templates/cloudground/.release-please-manifest.json`
- Modify: `Directory.Build.props`, `Directory.Packages.props`, `templates/cloudground/Directory.Build.props`, `templates/cloudground/Directory.Packages.props`, `templates/cloudground/.template.config/template.json`

**Interfaces:**
- Consumes: Task 1 harness.
- Produces: tags of the form `vX.Y.Z` (release-please default `include-v-in-tag`), read by MinVer with `MinVerTagPrefix v`.

- [ ] **Step 1: Write the failing tests**

```csharp
[Fact] public async Task Generated_solution_builds_without_git_and_gets_prerelease_version()
```
Generate `Untagged.App` and build it. Assert `ExitCode == 0` and that `FileVersionInfo.GetVersionInfo(<Root>/src/Untagged.App.Api/bin/Debug/net10.0/Untagged.App.Api.dll).ProductVersion` starts with `0.0.0-alpha.0`.
```csharp
[Fact] public async Task Generated_solution_takes_version_from_tag()
```
Generate `Tagged.App`, then run `git init`, `git add -A`, `git -c user.name=test -c user.email=test@example.invalid commit -m init` and `git tag v1.2.3` in its root, and build it. Assert `ExitCode == 0` and that the Api assembly's `ProductVersion` starts with `1.2.3`.

- [ ] **Step 2: Run them and confirm they fail**

Run: `dotnet test CloudGround.Templates.slnx --filter "FullyQualifiedName~VersioningTests"`
Expected: FAIL, because there is no MinVer yet, so both versions are `1.0.0`.

- [ ] **Step 3: Add MinVer to both `Directory.Build.props`**

Add `PackageReference MinVer` with `PrivateAssets all` (version in the matching `Directory.Packages.props`), `MinVerTagPrefix v`, and `MINVER1001` appended to `WarningsNotAsErrors`, so building outside git still works (Review Focus 1).

- [ ] **Step 4: Add release-please configuration to both**

`release-please-config.json`: a single package `.` with `release-type simple`, `include-component-in-tag false`, `bump-minor-pre-major true`, `changelog-path CHANGELOG.md`, and the `$schema` of release-please's config schema. `.release-please-manifest.json`: `{ ".": "0.0.0" }`. Exclude `CHANGELOG.md` and `version.txt` of the template repository from the template content in `template.json`.

- [ ] **Step 5: Run all template tests**

Run: `dotnet test CloudGround.Templates.slnx`
Expected: PASS.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "feat: derive versions from tags with MinVer and release-please" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 5: CI, pull request title check and release workflows

**Files:**
- Create: `templates/cloudground/.github/actions/build-and-test/action.yml`
- Create: `templates/cloudground/.github/workflows/ci.yml`, `pr-title.yml`, `release-please.yml`
- Create: `.github/workflows/ci.yml`, `.github/workflows/pr-title.yml`, `.github/workflows/release-please.yml`
- Modify: `tests/CloudGround.Templates.Tests/LayoutTests.cs`, `tests/CloudGround.Templates.Tests/PackageContentTests.cs`

**Interfaces:**
- Produces: composite action `build-and-test` with one input `working-directory` (default `.`). Steps: `actions/setup-dotnet` with `global-json-file: <working-directory>/global.json`; `dotnet restore`; `dotnet format --verify-no-changes --no-restore`; `dotnet build --no-restore -c Release`; `dotnet test --no-build -c Release`. Every `run` step uses `working-directory: ${{ inputs.working-directory }}` and `shell: bash`.

- [ ] **Step 1: Extend the tests and confirm they fail**

Add `LayoutTests` rows for `.github/actions/build-and-test/action.yml`, `.github/workflows/ci.yml`, `.github/workflows/pr-title.yml`, `.github/workflows/release-please.yml`, `release-please-config.json` and `.release-please-manifest.json`. Add to `Package_contains_template_dotfiles` an assertion for an entry ending in `cloudground/.github/workflows/ci.yml`.
Run: `dotnet test CloudGround.Templates.slnx`
Expected: FAIL on the new rows.

- [ ] **Step 2: Create the generated solution's workflows**

- `ci.yml`: runs on `push` to `main` and on `pull_request`, uses `actions/checkout` with `fetch-depth: 0` (MinVer needs tags), then `uses: ./.github/actions/build-and-test`.
- `pr-title.yml`: runs on `pull_request_target` (`opened`, `edited`, `synchronize`, `reopened`) with `permissions: pull-requests: read` and `amannn/action-semantic-pull-request`.
- `release-please.yml`: runs on `push` to `main` with `permissions: contents: write, pull-requests: write` and `googleapis/release-please-action`.

- [ ] **Step 3: Create the template repository's workflows**

- `ci.yml`: checkout with `fetch-depth: 0`, `actions/setup-dotnet` from `global.json`, then `dotnet test CloudGround.Templates.slnx`. After that `dotnet pack CloudGround.Templates.csproj -c Release -o artifacts`, `dotnet new install` of the produced package and `dotnet new cloudground -n Contoso.Shop -o generated/contoso-shop`. Finally `uses: ./generated/contoso-shop/.github/actions/build-and-test` with `working-directory: generated/contoso-shop`. The generated solution's own steps run unchanged.
- `pr-title.yml`: identical to the template's.
- `release-please.yml`: the release-please job exposes `release_created` and `tag_name`. A `publish` job with `needs` on it and `if: needs.release-please.outputs.release_created == 'true'` checks out `tag_name` with `fetch-depth: 0`, packs with `-c Release -o artifacts` and runs `dotnet nuget push artifacts/*.nupkg --api-key ${{ secrets.NUGET_API_KEY }} --source https://api.nuget.org/v3/index.json`. The publish job lives in the same workflow because a release created with `GITHUB_TOKEN` does not trigger other workflows.

- [ ] **Step 4: Lint the workflows and run the tests**

Run: `actionlint .github/workflows/*.yml templates/cloudground/.github/workflows/*.yml`
Expected: no output, exit code 0.
Run: `dotnet test CloudGround.Templates.slnx`
Expected: PASS.

- [ ] **Step 5: Commit**

```bash
git add -A
git commit -m "ci: add build, pull request title and release workflows" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 6: Dependabot, community files and first README

**Files:**
- Create: `.github/dependabot.yml`, `templates/cloudground/.github/dependabot.yml`
- Create: `.github/pull_request_template.md`, `templates/cloudground/.github/pull_request_template.md`
- Create: `.github/ISSUE_TEMPLATE/bug_report.yml`, `.github/ISSUE_TEMPLATE/feature_request.yml`, `.github/ISSUE_TEMPLATE/config.yml`
- Create: `LICENSE`, `CONTRIBUTING.md`, `CODE_OF_CONDUCT.md`, `SECURITY.md`, `README.md`, `AGENTS.md`, `CLAUDE.md`, `templates/cloudground/README.md`
- Modify: `tests/CloudGround.Templates.Tests/LayoutTests.cs`

- [ ] **Step 1: Extend `LayoutTests` and confirm the failure**

Add rows for `.github/dependabot.yml`, `.github/pull_request_template.md` and `README.md`, and a `[Fact] Generated_solution_has_no_license_file` that asserts `LICENSE` is absent (spec 8.12).
Run: `dotnet test CloudGround.Templates.slnx --filter "FullyQualifiedName~LayoutTests"`
Expected: FAIL on the new rows.

- [ ] **Step 2: Create the Dependabot configurations**

- **Shared settings for both files:** `version: 2`, weekly schedule, one group per ecosystem, and `ignore` entries with `update-types: ["version-update:semver-major"]` for `Microsoft.Extensions.*` and `Microsoft.AspNetCore.*` (they follow the .NET LTS line; spec 7.4).
- **Root file:** ecosystem `nuget` with `directories: ["/", "/templates/cloudground"]`, plus ecosystem `github-actions` with `directories: ["/", "/templates/cloudground", "/templates/cloudground/.github/actions/build-and-test"]`.
- **Template file:** ecosystem `nuget` with `directories: ["/"]`, plus ecosystem `github-actions` with `directories: ["/", "/.github/actions/build-and-test"]`.

- [ ] **Step 3: Write the community and documentation files**

- `LICENSE`: MIT, `Copyright (c) 2026 Q-Sharp`.
- `CODE_OF_CONDUCT.md`: Contributor Covenant 2.1, contact through the maintainer's GitHub profile.
- `SECURITY.md`: reports through GitHub private vulnerability reporting.
- `CONTRIBUTING.md`: build and test commands, Conventional Commits, squash merge, and the no-comments rule.
- Issue forms for bugs and feature requests; `config.yml` with `blank_issues_enabled: false`.
- Pull request template: checkboxes for tests, Conventional Commit title and documentation.
- Root `README.md`, in this order:
  1. a status line saying that this is work in progress at phase 1 of 7, linking to `docs/superpowers/plans/`
  2. what CloudGround is
  3. prerequisites: .NET 10 SDK, Docker or Podman
  4. installing from source: `dotnet pack` and `dotnet new install`
  5. generating a solution: `dotnet new cloudground -n Contoso.Shop`
  6. running it: `dotnet run --project src/Contoso.Shop.AppHost`
  7. testing it
- `templates/cloudground/README.md`: running and testing the generated solution.
- `AGENTS.md`: the repository's working rules from Global Constraints, the template test commands and the layout.
- `CLAUDE.md`: a single line `@AGENTS.md`.

- [ ] **Step 4: Run all template tests**

Run: `dotnet test CloudGround.Templates.slnx`
Expected: PASS.

- [ ] **Step 5: Check the repository for session links**

Run: `git grep -n -E 'claude\.ai/code/session'; git log --format=%B | grep -c -E 'claude\.ai/code/session'`
Expected: no `git grep` output and a count of `0`.

- [ ] **Step 6: Commit**

```bash
git add -A
git commit -m "docs: add community files, Dependabot and first README" -m "Co-Authored-By: Claude Opus 5.5 <noreply@anthropic.com>"
```

### Task 7: Public repository and first pull request

Creating the public repository is visible to everyone and permanent. Confirm with the author immediately before Step 1, and let the author run their personal-information check on the full history first.

- [ ] **Step 1: Create and configure the repository**

Run: `gh repo create Q-Sharp/CloudGround --public --description "dotnet new template for container-based .NET solutions with Aspire, Angular and a visible architecture" --source ~/Sources/CloudGround --remote origin`
Run: `gh api -X PATCH repos/Q-Sharp/CloudGround -F allow_merge_commit=false -F allow_rebase_merge=false -F allow_squash_merge=true -f squash_merge_commit_title=PR_TITLE -f squash_merge_commit_message=COMMIT_MESSAGES -F delete_branch_on_merge=true` (`COMMIT_MESSAGES` keeps the `Co-Authored-By` trailers of the squashed commits)
Run: `gh api -X PUT repos/Q-Sharp/CloudGround/private-vulnerability-reporting`
Expected: each command exits 0.

- [ ] **Step 2: Push `main` and the phase branch**

Run: `git push -u origin main && git push -u origin feat/foundation`
Expected: both branches on GitHub.

- [ ] **Step 3: Open the pull request**

Run: `gh pr create --base main --head feat/foundation --title "feat: foundation of the CloudGround template" --body-file <body>`. The body summarizes Tasks 1–6 and ends with `🤖 Generated with [Claude Code](https://claude.com/claude-code)`, with no session link.
Expected: PR URL printed. The PR title check does not run on this first pull request, because `pull_request_target` uses workflows from `main`, which has none yet.

- [ ] **Step 4: Watch CI**

Run: `gh pr checks --watch`
Expected: `ci` passes. The author merges the pull request.
