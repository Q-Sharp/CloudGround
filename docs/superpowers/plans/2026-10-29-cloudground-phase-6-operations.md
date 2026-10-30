# CloudGround Phase 6: Operations Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generated solutions can run outside development: Aspire publishes a Docker Compose deployment with migration bundles and no development Keycloak, every service has container images tagged with the solution version and health endpoints, the API ships pre-generated Wolverine code, CI builds the images and validates the Compose file, and a devcontainer runs the whole quickstart.

**Architecture:** The AppHost gains a Docker Compose environment. In publish mode it swaps the Keycloak container for an external service whose URL is a parameter, publishes each EF migration as a bundle container that the API waits for, tags every image it builds with the AppHost's MinVer version and gives long-running services a restart policy. .NET images come from SDK container publishing on Ubuntu 26.04 chiseled images; the web image keeps its own Dockerfile. The API generates Wolverine handler code during `dotnet publish` and loads it statically in production. The Aspire CLI is a local dotnet tool, so CI and adopters run `dotnet aspire …` without a global install.

**Tech Stack:** `Aspire.Hosting.Docker` 13.6.4, `Aspire.Cli` 13.6.4 (local tool), `Aspire.Hosting.EntityFrameworkCore` 13.6.4-preview (`PublishAsMigrationBundle`), WolverineFx 6.56.0 (`codegen write`, `TypeLoadMode.Static`), .NET 10 images `10.0-resolute-chiseled-extra`, devcontainer `mcr.microsoft.com/devcontainers/base:3-resolute` with the dotnet, node, docker-in-docker and powershell features, `devcontainers/ci@v0.3`.

**Spec:** `docs/superpowers/specs/2026-10-27-cloudground-design.md` (7.4, 8.1, 8.2 Wolverine and migrations, 8.4, 8.6, 8.7, 8.10, 8.11, 8.12)

## Global Constraints

- Everything in the Global Constraints of the phase 1 to 5 plans still holds.
- ".NET services build images with SDK container publishing, no Dockerfiles." "Aspire's publish step produces a Docker Compose deployment … a production baseline, not a demo: the OIDC authority, TLS termination and secrets come from configuration that the adopter supplies, and the README documents them. Keycloak with the demo realm stays a development concern of the AppHost. CI checks that the generated Compose file is valid. It does not run a login flow against it." (spec 8.6)
- "No deployment and no image push to a registry." (spec 8.7)
- "CI computes the version once and applies it to every artifact: … the tags of all container images, including the Angular SSR image." (spec 8.10)
- "Wolverine compiles handler code at runtime in development; container images use pre-generated code." "Migrations … in operation … are published as a migration bundle." (spec 8.2)
- Devcontainer: ".NET 10 SDK, Node and the Aspire tooling, and container access so the AppHost can start its dependencies from inside it … works in VS Code and in GitHub Codespaces … one command runs everything." Verify with Docker as the host runtime, check Podman, document the result. (spec 8.11)
- Container base images on the newest LTS of their distribution (spec 7.4): .NET services on `mcr.microsoft.com/dotnet/aspnet:10.0-resolute-chiseled-extra` (Ubuntu 26.04; `-extra` keeps ICU and time zones), migration bundles on `mcr.microsoft.com/dotnet/aspnet:10.0-resolute` (a bundle starts through the API, which needs ASP.NET Core, and Aspire's bundle Dockerfile uses a shell-form entrypoint, which chiseled images cannot run), web on `node:26-trixie-slim`.
- Every `dotnet aspire` call in docs and CI names its step and passes `--non-interactive`; `aspire publish` and `aspire do` without a step run deployment steps.
- Aspire's experimental pipeline APIs (`ASPIREPIPELINES003`) are suppressed with `#pragma warning disable`/`restore` around the call only.

## Review Focus

1. **Regenerating the Compose file on another machine or in CI** must keep the database and cache data: volume names must not depend on the AppHost's path. Pinned in Task 2 by `Data_volumes_keep_their_names_across_machines`.
2. **A fresh host where PostgreSQL or RabbitMQ start slower than the API** must end with a running API, not a dead container. Pinned in Task 2 by `Long_running_services_restart_unless_stopped` and by the smoke run in Task 3.
3. **A page request that reaches the web container through the BFF** must not be rejected by Angular's host check. Pinned in Task 2 by `Web_runs_its_image_entrypoint_and_accepts_the_bff` and by the smoke run in Task 3.
4. **Generated Wolverine files left over from an earlier publish** must not break a normal build after a handler changes. Pinned in Task 3 by `Stale_generated_handlers_do_not_break_the_build`.
5. **A solution without the sample** (no migrations, no handlers) must still publish, build its images and pass the Compose check. Pinned by `ComposeFileTests` running in both generated variants and by the CI action in Task 4.

---

### Task 0: Branch, plan and spec

- [ ] **Step 1:** Branch `feat/operations` from an up-to-date `main` (phase 5 merged).
- [ ] **Step 2:** Spec: 7.1 Frontend marker reads "Author (Angular, newest stable major) / Default (rest)"; 7.4 base-image row names the images from the Global Constraints (**Builder**, 2026-10-29); 8.6 and 8.11 record the decisions of this plan (external `keycloak` service from the `keycloak-url` parameter, Aspire dashboard kept as the Compose OTLP backend, restart policy, stable volume names, Garnet persistence, Aspire CLI as local tool, devcontainer base image). Commit with the plan: `docs: plan phase 6`.

### Task 1: Health endpoints in every environment

**Files:**
- Modify: `src/CloudGround.App.ServiceDefaults/Extensions.cs` (`MapDefaultEndpoints` maps `/health` and `/alive` in every environment), `src/CloudGround.App.Web/src/server.ts` (`GET /health` → 200 `text/plain` `Healthy`, registered before the static files), `src/CloudGround.App.AppHost/AppHost.cs` (`web.WithHttpHealthCheck("/health")`)
- Create: `tests/CloudGround.App.Bff.Tests/HealthEndpointTests.cs`
- Modify: `tests/CloudGround.App.Integration.Tests/{WebTests,AppHostModelTests}.cs`

- [ ] **Step 1:** Failing tests: `Health_endpoints_answer_outside_development` (a `WebApplication` with `EnvironmentName = Production`, `AddServiceDefaults()`, `MapDefaultEndpoints()`, Kestrel on `http://127.0.0.1:0`; GET `/health` and `/alive` → 200); `Web_app_answers_health_probes` (GET `/health` on the `web` resource → 200, body `Healthy`); `Web_counts_as_healthy_only_when_its_health_endpoint_answers` (`web` has a `HealthCheckAnnotation`).
- [ ] **Step 2:** Run the template test command; expect the three to FAIL.
- [ ] **Step 3:** Implement. `ng serve` loads `server.ts`'s `reqHandler`, so the route works in development too; if it does not, rule and keep the health check for production only.
- [ ] **Step 4:** Run; expect PASS. Commit `feat: answer health probes in every environment`.

### Task 2: Compose deployment from the AppHost

**Files:**
- Modify: `Directory.Packages.props` (`Aspire.Hosting.Docker` 13.6.4), AppHost csproj, `.config/dotnet-tools.json` (`aspire.cli` 13.6.4, command `aspire`), `.gitignore` (`aspire-output/`, `.env.*`)
- Modify: `src/CloudGround.App.AppHost/AppHost.cs`:
  - `builder.AddDockerComposeEnvironment("compose").ConfigureComposeFile(ComposeDeployment.RestartLongRunningServices)`
  - PostgreSQL `WithDataVolume(VolumeName("postgres-data"))`, Garnet `WithDataVolume(VolumeName("cache-data"))` (persistence on), where `VolumeName(name)` returns `name` in publish mode and `null` in run mode
  - run mode: Keycloak container as today, referenced and awaited by `api` and `bff`; publish mode: `builder.AddExternalService("keycloak", builder.AddParameter("keycloak-url"))`, referenced by both
  - web: `AddJavaScriptApp("web", "../CloudGround.App.Web")` (default run script `dev`); `NODE_OPTIONS` and the `--port` arguments in run mode only; in publish mode `NG_ALLOWED_HOSTS` = the web endpoint's host
  - bff: `WithExternalHttpEndpoints()`
  - each EF migration resource: `PublishAsMigrationBundle(publishContainer: true)`
- Create: `src/CloudGround.App.AppHost/ComposeDeployment.cs` (`public static void RestartLongRunningServices(ComposeFile composeFile)`: every service without a `restart` value that no other service waits for with `service_completed_successfully` gets `unless-stopped`)
- Modify: `src/CloudGround.App.Web/package.json` (script `start` → `dev`)
- Create: `tests/CloudGround.App.Integration.Tests/{PublishedCompose,ComposeFileTests,MigrationComposeTests}.cs`; `template.json` excludes `MigrationComposeTests.cs` without the sample (and the E2E exclude line gets its indentation fixed)

**Interfaces:**
- Produces: `PublishedCompose` (collection fixture `PublishedDeployment`): runs the AppHost through `DistributedApplicationTestingBuilder` with `--operation publish --step publish --output-path <temp>` once, exposes `string OutputDirectory` and `string Service(string name)` (the YAML block of one service; empty when absent).

- [ ] **Step 1:** Failing tests in `ComposeFileTests`: `Identity_provider_is_configured_not_deployed` (no `keycloak` service; `api` and `bff` contain `services__keycloak__default__0: "${KEYCLOAK_URL}"`); `Only_the_bff_and_the_dashboard_publish_ports`; `Web_runs_its_image_entrypoint_and_accepts_the_bff` (no `command:`, no `NODE_OPTIONS`, `NG_ALLOWED_HOSTS: "web"`); `Long_running_services_restart_unless_stopped`; `Data_volumes_keep_their_names_across_machines` (`source: "postgres-data"`, `source: "cache-data"`). In `MigrationComposeTests`: `Migrations_run_once_before_the_api` (both migration services exist without `restart`, `api` depends on each with `service_completed_successfully`).
- [ ] **Step 2:** Run; expect FAIL.
- [ ] **Step 3:** Implement. `dotnet tool restore` then `dotnet aspire publish --non-interactive -o <temp>` in a scratch copy produces the same file.
- [ ] **Step 4:** Run the template test command (run-mode tests still pass, including Keycloak's volume test); expect PASS. Commit `feat: publish a Docker Compose deployment`.

### Task 3: Container images

**Files:**
- Modify: `Directory.Build.props` (`ContainerFamily` `resolute-chiseled-extra`), AppHost (`PublishAsMigrationBundle(publishContainer: true, baseImage: "mcr.microsoft.com/dotnet/aspnet:10.0-resolute")`, `builder.TagImagesWithVersion()` right before `Build()`)
- Create: `src/CloudGround.App.AppHost/ContainerImages.cs` (`public static IDistributedApplicationBuilder TagImagesWithVersion(this IDistributedApplicationBuilder builder)`: the version is the AppHost assembly's informational version up to `+`; every `IComputeResource` with `RequiresImageBuild()` gets `WithRemoteImageTag(version)`)
- Modify: `src/CloudGround.App.Api/Program.cs` (`return await app.RunJasperFxCommands(args);`), `Messaging.cs` (`CritterStackDefaults(defaults => defaults.Production.GeneratedCodeMode = TypeLoadMode.Static)`), Api csproj (`Compile Remove="Internal/Generated/**"` unless `WolverineCodeGenerated` is `true`; target `GenerateWolverineCode` after `Build` when `_IsPublishing` is `true`: remove `Internal/Generated`, run the built API with `codegen write`, build again with `WolverineCodeGenerated=true`), `.gitignore` (`Internal/Generated/`), `CloudGround.Templates.csproj` and `template.json` (exclude `**/Internal/Generated/**` and `**/aspire-output/**`)
- Modify: `tests/CloudGround.App.Integration.Tests/ComposeFileTests.cs`; Create: `tests/CloudGround.Templates.Tests/ContainerImageTests.cs`; Modify: `PackageContentTests.cs`

- [ ] **Step 1:** Failing tests: `Every_built_image_is_tagged_with_the_solution_version` (publish-mode model; each image-building compute resource's push options give the test assembly's own version up to `+`); `Migration_bundles_run_on_the_ubuntu_aspnet_image` (sample: the bundle Dockerfiles start `FROM mcr.microsoft.com/dotnet/aspnet:10.0-resolute`); root `Services_build_on_ubuntu_lts_chiseled_images` (`dotnet msbuild` on the generated API and BFF reports `ContainerFamily` `resolute-chiseled-extra`); root `Published_api_loads_pregenerated_handlers` (sample: `dotnet publish -c Release`, then the published DLL with `ASPNETCORE_ENVIRONMENT=Production` and unreachable connection strings logs `Using pre-generated Wolverine HandlerRegistry` and no `MissingPreBuiltTypesException`; the process is stopped after that line or 60 s); root `Stale_generated_handlers_do_not_break_the_build` (a file with a compile error in `src/<Api>/Internal/Generated/` and `dotnet build` still succeeds); `Package_contains_no_generated_output`.
- [ ] **Step 2:** Run; expect FAIL.
- [ ] **Step 3:** Implement. `ProcessRunner` gains a variant that stops a process once a line matches.
- [ ] **Step 4:** Run both test commands; expect PASS.
- [ ] **Step 5: Smoke run with Podman** in a scratch copy (`DOCKER_HOST` on the Podman socket): `dotnet aspire do push` and `dotnet aspire do prepare-compose` with `--Parameters:keycloak-url=https://keycloak.invalid`, then `docker compose --env-file .env.Production up -d`. Expected and ledgered: migration services exit 0; `api` logs the pre-generated registry; `bff` answers `/health` 200 and `/explore` 200 through its published port; all images carry the version tag. Then `docker compose down -v` and remove the images. Commit `feat: build versioned container images with pre-generated handlers`.

### Task 4: CI builds the images and checks the Compose file

**Files:**
- Modify: `.github/actions/build-and-test/action.yml` (after the tests: `dotnet tool restore`; `dotnet aspire do push --non-interactive -o artifacts/compose -- --Parameters:keycloak-url=https://keycloak.example` with the step name "Build and tag the container images"; `dotnet aspire do prepare-compose` with the same arguments; `docker compose --env-file artifacts/compose/.env.Production -f artifacts/compose/docker-compose.yaml config --quiet`)

- [ ] **Step 1:** Run the three commands by hand on a scratch copy of each variant with Podman; expect exit 0 and the version-tagged images. Without a container registry in the AppHost the push step only tags locally; the README says so (Task 7).
- [ ] **Step 2:** Commit `ci: build the images and check the Compose file`. The pull request's CI is the test.

### Task 5: Devcontainer

**Files:**
- Create: `.devcontainer/devcontainer.json`: `name` `CloudGround.App`; `image` `mcr.microsoft.com/devcontainers/base:3-resolute`; features `ghcr.io/devcontainers/features/dotnet:2` (`version` `10.0`), `node:2` (`version` `26`), `docker-in-docker:4`, `powershell:2`; `containerEnv` `SSL_CERT_DIR=/usr/lib/ssl/certs:/home/vscode/.aspnet/dev-certs/trust`; `postCreateCommand` `dotnet tool restore && dotnet restore && npm ci --prefix src/CloudGround.App.Web`; `postStartCommand` `dotnet aspire certs trust --non-interactive`; VS Code extensions C# Dev Kit, Angular Language Service, ESLint
- Modify: root `.github/workflows/ci.yml` (job `devcontainer`: pack, install, generate `Contoso.Shop`, then `devcontainers/ci@v0.3` with `subFolder: generated/contoso-shop`, `push: never`, `runCmd: dotnet test --solution Contoso.Shop.slnx --filter-not-trait Category=EndToEnd`), `LayoutTests.cs`, web `.gitignore` (drop the `.vscode` leftovers)

- [ ] **Step 1:** Failing root test: `Generated_solution_has_root_file` gains `.devcontainer/devcontainer.json`.
- [ ] **Step 2:** Run; expect FAIL. Implement; run; expect PASS.
- [ ] **Step 3:** Podman check: `npx @devcontainers/cli@0.90.0 up --docker-path podman --workspace-folder <generated solution>`, then `exec` `dotnet test --solution … --filter-not-trait Category=EndToEnd`. Ledger what works; the README states the result (Task 7).
- [ ] **Step 4:** Commit `feat: add a devcontainer`. The `devcontainer` CI job is the Docker verification.

### Task 6: Dependabot for npm, Docker and devcontainers

**Files:**
- Modify: template and root `dependabot.yml` (ecosystems `npm` and `docker` for the web app's directory, `devcontainers` for the solution root, one group each, weekly; ignore major updates of `@types/node` in npm and `node` in docker), `src/CloudGround.App.Web/package.json` and lock (`@types/node` ^26)
- Modify: `tests/CloudGround.Templates.Tests/DependabotConfigurationTests.cs`

- [ ] **Step 1:** Failing tests: `Npm_directories_contain_a_package_lock`, `Docker_directories_contain_a_dockerfile`, `Devcontainers_directories_contain_a_devcontainer` (both configurations); `Dependabot_ignores_major_updates_of_lts_bound_packages` becomes a theory over ecosystem and expected list (`nuget` as today, `npm` `@types/node`, `docker` `node`).
- [ ] **Step 2:** Run; expect FAIL. Implement; run; expect PASS. `npm run build` and `npm test` still pass. Commit `build: let dependabot update npm, docker and devcontainer dependencies`.

### Task 7: Docs, review and pull request

- [ ] **Step 1:** Generated README: "Deploy with Docker Compose" (the two `dotnet aspire do` commands, what lands in the output folder, the parameters `keycloak-url`, `bff-client-secret` and the generated passwords, image names and tags, the push step tagging locally without a registry, TLS termination in front of the BFF with the original `Host` and `X-Forwarded-Proto`, the realm and clients the adopter's Keycloak needs, OTLP to the bundled dashboard or another backend, health endpoints); "Devcontainer" with the Docker and Podman results; `npm run dev` instead of `npm start`; the web image's standalone example names `services__api__http__0`.
- [ ] **Step 2:** Root README (status phases 1–6, bullets for images, Compose and devcontainer), `docs/nuget/README.md`, `CONTRIBUTING.md` if prerequisites change.
- [ ] **Step 3:** Both test commands; expect PASS. Commit `docs: describe deployment and the devcontainer`.
- [ ] **Step 4:** Final review (fresh reviewer, most capable model), fix pass, scan for private information, push, pull request `feat: container deployment and devcontainer`, CI green, squash-merge. Remove the smoke-run images, the scratch copies and the Aspire deployment state they created.
