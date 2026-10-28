# CloudGround Phase 5: Angular SSR Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Generated solutions get an Angular SSR frontend behind the BFF: a public, server-rendered page that shows data from the API's anonymous endpoints, and a client-rendered area for signed-in users, with lint, unit tests, OpenTelemetry in Node, a Dockerfile and Playwright end-to-end tests.

**Architecture:** `src/CloudGround.App.Web` is an Angular CLI workspace (newest LTS major) with SSR and per-route render modes. The AppHost runs it as a JavaScript app; the BFF forwards every path that is not `/api` or `/bff` to it, so the browser still talks only to the BFF. The SSR server calls the API directly through service discovery for public data; the browser calls `/api` through the BFF with Angular's built-in XSRF support. Node is instrumented with the OpenTelemetry Node SDK. Playwright for .NET drives the whole stack through `Aspire.Hosting.Testing`.

**Tech Stack:** Angular 21 (newest LTS major on 2026-10-28: `v21-lts` → 21.2.x; Angular 22 is the active major), Angular Material, angular-eslint, the CLI's default unit test runner, Node.js 24 (newest Active LTS), OpenTelemetry Node SDK, `Aspire.Hosting.JavaScript` 13.6.4, `Microsoft.Playwright.Xunit.v3` 1.64.1.

**Spec:** `docs/superpowers/specs/2026-10-27-cloudground-design.md` (7.1 Frontend and Rendering, 7.4, 8.1, 8.2 Sample, 8.3, 8.4, 8.5, 8.7, 8.12)

## Global Constraints

- Everything in the Global Constraints of the phase 1 to 4 plans still holds.
- Versions (2026-10-28): Angular 21 (`@angular/*` 21.2.x, CLI 21.2.x), Node.js 24 (`.nvmrc` `24`), `Aspire.Hosting.JavaScript` 13.6.4, `Microsoft.Playwright.Xunit.v3` 1.64.1; npm packages at their newest stable versions compatible with Angular 21.
- "Angular CLI project with the current defaults (standalone, zoneless, signals, built-in control flow, the CLI's default unit test runner)." "Route-level render modes as in 7.1." "Calls go to the same origin (`/api/...`, `/bff/...`); no tokens in the browser." "ESLint via angular-eslint; the build fails on lint errors." "Containerized with its own Dockerfile." (spec 8.3)
- "SSR renders public pages anonymously … the SSR server calls the API's anonymous public endpoints directly, using Aspire service discovery. Routes that need data are server-rendered at request time, not prerendered." (spec 8.1)
- Generated files from `ng new`/`ng add` lose their comments (user rule: comments come out of one-off scaffolds); `package-lock.json` is committed.
- Placeholders: the workspace folder is `src/CloudGround.App.Web`; the npm package and Angular project are `cloudground-app-web`.
- `node_modules`, `dist` and `.angular` never enter the template package or a generated solution.
- With `--sample false`, the web app, its shell, login/logout and the user display stay; the project pages go.

## Review Focus

1. **A public page with JavaScript disabled** must still show its data, because it was rendered on the server. Pinned in Task 6 by `Public_page_shows_projects_without_javascript`.
2. **A signed-in write from the browser** must carry the XSRF header that the BFF expects, without custom code. Pinned in Task 6 by `Alice_creates_a_project_in_the_browser`.
3. **Bob (no editor role)** must not see a create form that fails, or must get a clear message. Pinned in Task 6 by `Bob_cannot_create_projects`.
4. **The generated web app** must install, lint, build and test for any solution name. Pinned in Task 1 by `Generated_web_app_lints_builds_and_tests`.
5. **Deep links** (`/projects` reloaded in the browser) must be served through the BFF, not answered 404. Pinned in Task 2 by `Deep_links_are_served_by_the_web_app`.

---

### Task 0: Branch and spec

- [ ] **Step 1:** `git switch -c feat/angular-ssr` from an up-to-date `main` (phase 4 merged).
- [ ] **Step 2:** Spec 7.4 "At the time of writing": Angular 21, Node.js 24 (**Builder**, 2026-10-28). Commit with the plan: `docs: plan phase 5`.

### Task 1: Angular workspace

**Files:**
- Create: `src/CloudGround.App.Web/` from `ng new cloudground-app-web --ssr --style=scss --skip-git` with Angular CLI 21, then `ng add @angular/material` and `ng add angular-eslint`; `.nvmrc` with `24`; comments removed from generated files
- Modify: `CloudGround.Templates.csproj` and `.template.config/template.json` (exclude `**/node_modules/**`, `**/dist/**`, `**/.angular/**`), `tests/CloudGround.Templates.Tests/{LayoutTests,WebAppTests}.cs`

**Interfaces:**
- Produces: npm scripts `start` (dev server), `build` (runs lint first, then `ng build`), `lint`, `test` (CLI default runner, single run), `serve:ssr` (`node dist/cloudground-app-web/server/server.mjs`); the built server listens on `PORT`.

- [ ] **Step 1:** Failing root tests: `Generated_solution_has_root_file` gains `src/Contoso.Shop.Web/package.json`, `src/Contoso.Shop.Web/angular.json`, `src/Contoso.Shop.Web/.nvmrc`; `Generated_web_app_lints_builds_and_tests` runs `npm ci`, `npm run build` and `npm test` in a generated solution and expects exit code 0 each; `Package_contains_no_node_modules` (the `.nupkg` has no `node_modules/`, `dist/` or `.angular/` entry).
- [ ] **Step 2:** Run; expect FAIL.
- [ ] **Step 3:** Scaffold, remove comments, set the scripts, make `build` lint first.
- [ ] **Step 4:** Run; expect PASS. Commit `feat: add the Angular workspace`.

### Task 2: The AppHost runs the web app and the BFF serves it

**Files:**
- Modify: AppHost (`AddJavaScriptApp("web", "../CloudGround.App.Web", "start")` with npm install, an HTTP endpoint passed to the dev server as `--port`, `WithReference(api)`, OTLP), BFF (`ApiProxy.cs` → `Proxy/Routes.cs` with a second route `web` for `/{**path}` with lower priority than `/api` and `/bff`, cluster `http://web`), AppHost csproj, `Directory.Packages.props`
- Create: `tests/CloudGround.App.Integration.Tests/WebTests.cs`

**Interfaces:**
- Produces: AppHost resource `web`; BFF route `web` (order after `api`); BFF unit test `Pages_are_forwarded_without_tokens` (the `web` route strips `Cookie` and `Authorization` like `api` and adds no token).

- [ ] **Step 1:** Failing tests: `Home_page_is_server_rendered_through_the_bff` (GET `/` on the BFF → 200 `text/html`, body contains `ng-server-context="ssr"`), `Deep_links_are_served_by_the_web_app` (GET `/projects` → 200 `text/html`), `Pages_are_forwarded_without_tokens`.
- [ ] **Step 2:** Run; expect FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run both test commands; expect PASS. Commit `feat: serve the Angular app through the BFF`.

### Task 3: Shell, user and login

**Files:**
- Create in the web app: `src/app/session/session.service.ts` (signal `user` from `GET /bff/user`, `logIn()` navigates to `/bff/login?returnUrl=<current path>`, `logOut()` posts `/bff/logout`), `src/app/shell/shell.ts` (Material toolbar with the user's name and a login or logout button), unit tests next to them
- Modify: `app.config.ts` (`provideHttpClient(withFetch(), withXsrfConfiguration({ cookieName: 'XSRF-TOKEN', headerName: 'X-XSRF-TOKEN' }))`), `app.routes.ts`, `app.routes.server.ts` (`''` → `RenderMode.Server`, `**` → `RenderMode.Server`)

- [ ] **Step 1:** Failing unit tests: `shows the login button when nobody is signed in`, `shows the user's name and a logout button when signed in` (HTTP testing controller for `/bff/user`), `logout posts to /bff/logout`.
- [ ] **Step 2:** Run `npm test`; expect FAIL.
- [ ] **Step 3:** Implement. On the server, `SessionService` does not call `/bff/user` (SSR is anonymous, spec 8.1); the browser loads the user after hydration.
- [ ] **Step 4:** Run `npm test` and `npm run build`; expect PASS. Commit `feat: show the signed-in user in the Angular shell`.

### Task 4: Public server-rendered page (sample)

**Files:**
- Create in the web app: `src/app/projects/public-projects.ts` (lists projects), `src/app/projects/projects.api.ts` (`list(): Observable<Project[]>`, `create(name: string, description?: string)`), `src/app/api-base-url.ts` (injection token; on the server the API's address from `services__api__http__0`, in the browser `''`), unit tests
- Modify: `app.routes.ts` and `app.routes.server.ts` with `#if (sample)` blocks in the template's JavaScript comment syntax; `template.json` excludes `src/CloudGround.App.Web/src/app/projects/**` without the sample
- Modify: `tests/CloudGround.App.Integration.Tests/ProjectsWebTests.cs` (sample only)

- [ ] **Step 1:** Failing tests: unit `lists the projects it receives`; integration `Home_page_lists_projects_rendered_on_the_server` (create a project through the API as alice, then GET `/` through the BFF; the HTML contains its name).
- [ ] **Step 2:** Run; expect FAIL.
- [ ] **Step 3:** Implement with the HTTP transfer cache so the browser does not fetch again after hydration.
- [ ] **Step 4:** Run both test commands and `npm test`; expect PASS. Commit `feat: render the public project list on the server`.

### Task 5: Client-rendered area for signed-in users (sample) and Node telemetry

**Files:**
- Create in the web app: `src/app/projects/my-projects.ts` (route `projects`, `RenderMode.Client`; list plus a Material form to create a project, visible for the `editor` role only; a 403 shows a message), `src/instrumentation.mjs` (OpenTelemetry Node SDK with HTTP and Express instrumentation, OTLP exporter configured from the standard `OTEL_*` variables), unit tests
- Modify: npm scripts load the instrumentation (`node --import ./src/instrumentation.mjs …`); AppHost passes `NODE_OPTIONS=--import <path>` for the dev server
- Create: `tests/CloudGround.App.Integration.Tests/AppHostModelTests.cs` gains `Web_app_exports_telemetry` (the `web` resource has an OTLP exporter annotation)

- [ ] **Step 1:** Failing tests: unit `shows the create form for editors only`, `shows a message when creating is forbidden`; integration `Web_app_exports_telemetry`.
- [ ] **Step 2:** Run; expect FAIL.
- [ ] **Step 3:** Implement.
- [ ] **Step 4:** Run all test commands; check once in the dashboard that a page request shows one trace from the BFF through Node to the API, and ledger the observation. Commit `feat: add the signed-in projects area and Node telemetry`.

### Task 6: End-to-end tests and Dockerfile

**Files:**
- Create: `tests/CloudGround.App.EndToEnd.Tests/` (csproj with `Microsoft.Playwright.Xunit.v3` and `Aspire.Hosting.Testing`, `StackFixture.cs` that installs Chromium through `Microsoft.Playwright.Program.Main(["install", "chromium"])` and starts the AppHost without volumes, tests below, trait `Category=EndToEnd`)
- Create: `src/CloudGround.App.Web/Dockerfile` (multi-stage on `node:24` images of the newest Debian release, non-root runtime user, `PORT` 8080), `.dockerignore`
- Modify: `.slnx`, `template.json` (E2E project in both variants; sample-only tests excluded), root tests run generated solutions with `--filter-not-trait Category=EndToEnd`

- [ ] **Step 1:** Failing tests: `Public_page_shows_projects_without_javascript` (browser context with `JavaScriptEnabled = false`), `Alice_creates_a_project_in_the_browser` (log in through Keycloak's page, create a project, see it listed), `Bob_cannot_create_projects`, `Login_and_logout_round_trip` (both variants).
- [ ] **Step 2:** Run; expect FAIL.
- [ ] **Step 3:** Implement the fixture and tests; build the image once locally with Podman and ledger the result.
- [ ] **Step 4:** Run all test commands; expect PASS. Commit `test: drive the stack with Playwright`.

### Task 7: CI, docs and pull request

- [ ] **Step 1:** The composite action sets up Node from `.nvmrc`, runs `npm ci`, `npm run build` (lint included) and `npm test` in the web app, installs Playwright's system dependencies, and runs every .NET test level including end-to-end on every push.
- [ ] **Step 2:** Docs: generated README (frontend, render modes, how the BFF serves pages, running the web app alone, end-to-end tests), root README status (phase 5 of 7), NuGet README, `LayoutTests`.
- [ ] **Step 3:** All test commands; expect PASS. Commit `docs: describe the Angular frontend`.
- [ ] **Step 4:** Final review (fresh reviewer, most capable model), fix pass, push, pull request `feat: angular ssr`, CI green, squash-merge.
