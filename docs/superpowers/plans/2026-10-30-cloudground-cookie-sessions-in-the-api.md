# CloudGround: Cookie Sessions in the API Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Remove the BFF. The API signs browsers in itself with a cookie session and still accepts bearer tokens, and it forwards every request it does not handle to the Angular SSR server, so the browser keeps seeing one origin.

**Architecture:** The API gets a policy scheme that picks JWT bearer when the request has an `Authorization: Bearer` header and the session cookie otherwise. The cookie scheme keeps its ticket in Garnet (`sessions/{guid}`), Data Protection keys live in Garnet (`data-protection-keys`), OpenID Connect against Keycloak (authorization code with PKCE) signs in through the client `cloudground-app-api`. The session keeps the tokens and slides; when the cookie is validated and the access token is due, the API refreshes it at Keycloak, and a failed refresh rejects the cookie and signs the user out (the BFF's `AccessTokenRefresher` and `RefreshingCookieEvents` move into the API). Tokens are never forwarded. XSRF is checked for unsafe methods on cookie-authenticated requests only. `/account/login`, `/account/logout` and `/account/user` replace `/bff/*`. A YARP route with the lowest order forwards every path outside `/api` and `/account` to the web app, without `Cookie` and `Authorization`.

**Tech Stack:** unchanged; the API takes over the BFF's packages (`Microsoft.AspNetCore.DataProtection.StackExchangeRedis`, `Yarp.ReverseProxy`, `Microsoft.Extensions.ServiceDiscovery.Yarp`).

**Spec:** `docs/superpowers/specs/2026-10-27-cloudground-design.md` (7.1 Auth, 8.1 Runtime topology, 8.3 Frontend, 8.4 Observability, 8.6 Deployment, 8.12 Folder layout); decision by the **Author**, 2026-10-30: cookie authentication in the API instead of a BFF.

## Global Constraints

- Everything in the Global Constraints of the phase 1 to 7 plans still holds.
- Realm client `cloudground-app-api` (confidential, standard flow, PKCE `S256`, roles in the ID token), secret `${API_CLIENT_SECRET}` from the AppHost parameter `api-client-secret`, passed to the API as `Keycloak:ClientSecret`. The test client `cloudground-app-tests` and the demo users stay.
- In the Compose file only the API and the dashboard publish ports, both on the loopback interface.
- **Author**, 2026-10-30, during Task 2: keep the token refresh instead of a fixed 8-hour session without refresh, so that Keycloak can still end a session early.
- **Author**, 2026-10-30, during Task 3: the realm states Keycloak's default lifetimes (`ssoSessionIdleTimeout` 1800, `ssoSessionMaxLifespan` 36000, `accessTokenLifespan` 300), pinned by `Realm_states_its_session_lifetimes`; the API stays in front of the web app, so the whole site is down while the API is down.

## Review Focus

1. **A cross-site POST with the session cookie** is rejected and changes nothing; a bearer request needs no XSRF token. Pinned by `Write_without_xsrf_header_is_rejected`, `Logout_without_xsrf_token_keeps_the_session` and `Bearer_write_needs_no_xsrf_token`.
2. **Unknown `/api` and `/account` paths** stay `ProblemDetails` from the API and never reach the web app. Pinned by `Unknown_route_returns_problem_details` and `Unknown_account_route_returns_problem_details`.
3. **The web app never sees the session.** Pinned by `Forwarded_requests_carry_no_cookie_or_authorization`.
4. **Sessions work on every API instance.** Pinned by `Session_works_on_every_api_instance` with two replicas.

---

### Task 1: The API signs browsers in

**Files:**
- Modify: Api `Authentication.cs`, `Program.cs`, csproj; AppHost `AppHost.cs`; `Realms/realm.json`; `.slnx`
- Create: Api `DistributedTicketStore.cs`, `XsrfProtection.cs`, `AccountEndpoints.cs`, `AccessTokenRefresher.cs`, `RefreshingCookieEvents.cs`, `TokenRefresh.cs`; `tests/CloudGround.App.Api.Tests/` (`AuthenticationTests`, `DistributedTicketStoreTests`, `HealthEndpointTests`, `AccessTokenRefresherTests`, `RefreshingCookieEventsTests`); Integration `BrowserLogin.cs`, `ApiInstances.cs`, `AccountTests.cs`, `SessionTests.cs`, `ProjectsInTheBrowserTests.cs`

- [ ] **Step 1:** Failing unit tests: bearer header selects the bearer scheme, anything else the cookie; the session slides and refreshes its access token, and a failed refresh signs the user out; both schemes take the authority from `Keycloak:Url`; ticket round trip under `sessions/`.
- [ ] **Step 2:** Failing integration tests against `api`: anonymous `/account/user` → 401; `/account/login` redirects to Keycloak with `code_challenge_method=S256`; a real sign-in returns name and roles only; foreign return URLs land on `/`; logout ends the session and needs the XSRF token; sessions and keys live in Garnet; ten requests on new connections succeed on two API instances; signed-in writes need the XSRF header, bearer writes do not.
- [ ] **Step 3:** Implement; run both; expect PASS. Commit `feat: sign browsers in at the api`.

### Task 2: The API serves the web app and the BFF goes

**Files:**
- Create: Api `WebProxy.cs`; `tests/CloudGround.App.Api.Tests/WebProxyTests.cs`
- Modify: AppHost (api external, api references web, no `bff`); Web `session.service.ts`, `shell.ts`, `shell.html`, `shell.spec.ts`; Integration `AppHostFixture`, `AppHostModelTests`, `ComposeFileTests`, `WebTests`, `ProjectsWebTests`, `ProblemDetailsTests`, `KeycloakTests`, `Tokens`; E2E `StackFixture`, `StackTest`, `LoginTests`, `ProjectsTests`; realm; `template.json`; root `ContainerImageTests`, `LayoutTests`, `NamingTests`, `PortTests`
- Delete: `src/CloudGround.App.Bff`, `tests/CloudGround.App.Bff.Tests`, Integration `Bff*.cs`, `ProjectsThroughBffTests.cs`

- [ ] **Step 1:** Failing tests: shell and session spec on `/account/*`; transforms strip `Cookie` and `Authorization`; the web route skips `/api` and `/account`; home page and 404 page through `api`; no `bff` resource and external endpoints on `api`; only `api` and the dashboard publish ports; clients `contoso-shop-api` and `contoso-shop-tests`.
- [ ] **Step 2:** Implement and delete; run the web app's tests, both .NET suites; expect PASS. Commit `refactor!: serve the web app from the api and remove the bff`.

### Task 3: Docs

- [ ] **Step 1:** ADR 0022 supersedes 0009; ADRs 0003, 0007, 0010, 0011, 0015 and the index; generated README (containers diagram, sign-in, frontend, deployment), `AGENTS.md`, root and NuGet READMEs, the spec.
- [ ] **Step 2:** Root suite (documentation tests); `git grep -i bff` shows only history. Commit `docs: record cookie sessions in the api`.
