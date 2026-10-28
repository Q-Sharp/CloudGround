# CloudGround Phase 4: BFF and Login Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** A browser talks only to a BFF that logs users in against Keycloak, keeps their session outside the process, and forwards `/api/*` with the user's access token; the API reads anonymously and writes only for users with the `editor` role.

**Architecture:** The AppHost starts Keycloak with an imported `cloudground` realm. A new `CloudGround.App.Bff` host uses cookie authentication with OpenID Connect, stores the authentication ticket and the Data Protection keys in Garnet, refreshes access tokens before they expire, exposes `/bff/login`, `/bff/logout` and `/bff/user`, and forwards `/api/*` to the API through YARP with a bearer token and Angular-compatible antiforgery. The API validates Keycloak JWTs; each module guards its write endpoints with its own policy that requires the `editor` realm role.

**Tech Stack:** Aspire.Hosting.Keycloak and Aspire.Keycloak.Authentication 13.6.4-preview.1.26523.11, YARP 2.3.0 with Microsoft.Extensions.ServiceDiscovery.Yarp 10.11.0, Microsoft.AspNetCore.DataProtection.StackExchangeRedis 10.0.14, Aspire.StackExchange.Redis.DistributedCaching 13.6.4, xUnit v3, Aspire.Hosting.Testing.

**Spec:** `docs/superpowers/specs/2026-10-27-cloudground-design.md` (7.1 Auth and reverse proxy, 8.1 Runtime topology, 8.2 Sample, 8.4 Observability, 8.5 Tests, 11 Left to the builder)

## Global Constraints

- Everything in the Global Constraints of the phase 1 to 3 plans still holds.
- Pinned versions (2026-10-28): `Aspire.Hosting.Keycloak` and `Aspire.Keycloak.Authentication` 13.6.4-preview.1.26523.11 (**Author** exception, 2026-10-28: "Beide Previews"), `Yarp.ReverseProxy` 2.3.0, `Microsoft.Extensions.ServiceDiscovery.Yarp` 10.11.0, `Microsoft.AspNetCore.DataProtection.StackExchangeRedis` 10.0.14.
- "The browser only ever talks to the BFF. Tokens never reach the browser." (spec 8.1)
- "The BFF owns login, logout and a user-info endpoint, attaches the access token when forwarding `/api/*`, and protects state-changing requests with antiforgery that works with Angular's built-in XSRF support." Angular reads cookie `XSRF-TOKEN` and sends header `X-XSRF-TOKEN`.
- BFF session state and Data Protection keys live in Garnet (builder pick for spec 8.1).
- Realm (builder pick for spec 11): name `cloudground`; realm role `editor`; users `alice`/`alice` (editor) and `bob`/`bob` (no editor role); clients `cloudground-bff` (confidential, authorization code), `cloudground-tests` (public, direct access grants, used only by the integration tests); client scope `cloudground-api` adds audience `cloudground-api`; a realm-role mapper writes the realm roles into a flat `roles` claim.
- Reads stay anonymous; every non-GET API endpoint requires the `editor` role through its module's policy.
- With `--sample false`, Keycloak, the BFF and API authentication stay.

## Review Focus

1. **A second BFF instance or a BFF restart** must keep a logged-in user logged in. Pinned in Task 5 by `Session_survives_a_bff_restart`.
2. **An access token that expires during a session** must be refreshed, not forwarded expired. Pinned in Task 3 by `Token_close_to_expiry_is_refreshed` and `Token_far_from_expiry_is_kept`.
3. **A cross-site POST through the BFF** without the XSRF header must be rejected before it reaches the API and must not change data. Pinned in Task 4 by `Write_without_xsrf_header_is_rejected`.
4. **`/bff/login?returnUrl=https://evil.example/`** must not redirect off-site after login. Pinned in Task 3 by `Login_ignores_foreign_return_urls`.
5. **Token leakage**: `/bff/user` returns no tokens and the BFF does not forward its own cookies to the API. Pinned in Task 3 by `User_endpoint_returns_name_and_roles_only` and in Task 4 by `Forwarded_requests_carry_no_bff_cookies`.

---

### Task 0: Branch and spec

- [ ] **Step 1:** `git switch -c feat/bff-login` from an up-to-date `main` (phase 3 merged).
- [ ] **Step 2:** Spec 7.4: add rows for `Aspire.Hosting.Keycloak` and `Aspire.Keycloak.Authentication` as preview with **Author** exception (2026-10-28). Spec 8.1: name Garnet as the session and key store (**Builder**, 2026-10-28). Commit `docs: record the Keycloak preview exception`.

### Task 1: Keycloak with the cloudground realm

**Files:**
- Create: `src/CloudGround.App.AppHost/Realms/cloudground-realm.json`
- Modify: `src/CloudGround.App.AppHost/AppHost.cs`, AppHost csproj, `Directory.Packages.props`, `.template.config/template.json` (port symbol `keycloakPort` replacing `8380`)
- Create: `tests/CloudGround.App.Integration.Tests/KeycloakTests.cs`, `tests/CloudGround.App.Integration.Tests/Tokens.cs`

**Interfaces:**
- Produces: AppHost resource `keycloak` (`AddKeycloak("keycloak", port: 8380).WithRealmImport("./Realms")`); constants in the realm as listed in Global Constraints. The `cloudground-bff` client's secret is the placeholder `${BFF_CLIENT_SECRET}`, filled from the generated secret AppHost parameter `bff-client-secret` through Keycloak's environment.
- Produces: `internal static class Tokens { Task<string> ForAsync(DistributedApplication app, string user, CancellationToken ct); }` — password grant against `{keycloak http}/realms/cloudground/protocol/openid-connect/token` with client `cloudground-tests`, password equal to the user name.

- [ ] **Step 1:** Failing tests: `Realm_publishes_its_openid_configuration` (GET `/realms/cloudground/.well-known/openid-configuration` → 200), `Alice_gets_a_token_with_the_editor_role_for_the_api` (decoded payload: `aud` contains `cloudground-api`, `roles` contains `editor`), `Bob_gets_a_token_without_the_editor_role`.
- [ ] **Step 2:** Run; expect FAIL (no `keycloak` resource).
- [ ] **Step 3:** Add Keycloak to the AppHost and write the realm file. The realm starts fresh on every run (no data volume), so edits to the file apply on the next start.
- [ ] **Step 4:** Run; expect PASS. Commit `feat: start Keycloak with a demo realm`.

### Task 2: The API authenticates and writes need the editor role

**Files:**
- Modify: Api `Program.cs`, Api csproj; `ProjectsModule.cs`, `TasksModule.cs` and the four write endpoints; `ProjectsApiTests.cs`, `TasksApiTests.cs`, `ProjectArchivedFlowTests.cs`, `AppHostFixture.cs`
- Create: `tests/CloudGround.App.Integration.Tests/AuthorizationTests.cs`

**Interfaces:**
- Consumes: `Tokens.ForAsync` (Task 1).
- Produces: API authentication `AddKeycloakJwtBearer("keycloak", realm: "cloudground")` with `Audience = "cloudground-api"`, `RoleClaimType = "roles"`, `NameClaimType = "preferred_username"`, HTTPS metadata only outside Development; policies `ProjectsPolicies.Edit = "projects:edit"` and `TasksPolicies.Edit = "tasks:edit"`, each `RequireRole("editor")`, registered by the module.
- Produces: `AppHostFixture.CreateApiClientAsync(string user)` returning an `HttpClient` with `Authorization: Bearer <token>`.

- [ ] **Step 1:** Failing tests in `AuthorizationTests`: `Every_write_endpoint_rejects_anonymous_callers` (reads `/openapi/v1.json`, sends every POST/PUT/PATCH/DELETE with GUIDs for route values and an empty JSON object, expects 401 `ProblemDetails` each), `Writes_need_the_editor_role` (bob → 403 `ProblemDetails` on `POST /api/projects`, and no project with that name exists), `Reads_stay_anonymous` (`GET /api/projects` without token → 200). Existing write tests switch to `CreateApiClientAsync("alice")`.
- [ ] **Step 2:** Run; expect the three new tests to FAIL.
- [ ] **Step 3:** Implement authentication, the module policies and `.RequireAuthorization(<Module>Policies.Edit)` on the write endpoints; `UseAuthentication`/`UseAuthorization` after the exception handler.
- [ ] **Step 4:** Run both test commands; expect PASS. Commit `feat: require the editor role for writes`.

### Task 3: BFF login, session and user endpoint

**Files:**
- Create: `src/CloudGround.App.Bff/` (csproj, `Program.cs`, `Properties/launchSettings.json`, `Authentication/BffAuthentication.cs`, `Authentication/DistributedTicketStore.cs`, `Authentication/AccessTokenRefresher.cs`, `Endpoints/BffEndpoints.cs`)
- Create: `tests/CloudGround.App.Bff.Tests/` (csproj, `AccessTokenRefresherTests.cs`), `tests/CloudGround.App.Integration.Tests/BffLogin.cs`, `tests/CloudGround.App.Integration.Tests/BffAuthenticationTests.cs`
- Modify: AppHost (`bff` project: references keycloak, cache, api; client secret parameter `bff-client-secret` passed to Keycloak as `BFF_CLIENT_SECRET` for the realm import and to the BFF), `.slnx`, `template.json` (ports `bffHttpsPort` 7411, `bffHttpPort` 5411), `PortTests.ProjectsWithLaunchSettings` gets `Bff`

**Interfaces:**
- Produces: `GET /bff/login?returnUrl=` (challenge; only local return URLs, otherwise `/`), `POST /bff/logout` (signs out of cookie and OIDC), `GET /bff/user` → 200 `{ "name": string, "roles": string[] }` or 401 `ProblemDetails`.
- Produces: `internal sealed class DistributedTicketStore(IDistributedCache cache, TimeProvider clock) : ITicketStore`, keys `bff/tickets/{guid}`, expiry from the ticket's `ExpiresUtc`.
- Produces: `internal sealed class AccessTokenRefresher(HttpClient tokenClient, TimeProvider clock)` with `Task<bool> RefreshIfCloseToExpiryAsync(AuthenticationProperties properties, OpenIdConnectOptions options, CancellationToken ct)`; refresh window 1 minute; updates `access_token`, `refresh_token`, `expires_at`; called from `CookieAuthenticationEvents.OnValidatePrincipal`, which renews the cookie or rejects the principal when the refresh fails.
- Produces: `internal sealed class BffSession : IDisposable { HttpClient Client; string? XsrfToken; }` and `internal static class BffLogin { Task<BffSession> LogInAsync(AppHostFixture fixture, string user, string? returnUrl = null) }` — follows the redirects to Keycloak's login form, posts user name and password, follows back; the client keeps the session cookie (`HttpClientHandler` with a `CookieContainer`, `AllowAutoRedirect = true`), `XsrfToken` reads the `XSRF-TOKEN` cookie from that container.

- [ ] **Step 1:** Failing unit tests: `Token_close_to_expiry_is_refreshed` (stub token endpoint returns new tokens; properties updated), `Token_far_from_expiry_is_kept` (no call to the endpoint), `Failed_refresh_reports_false`; `DistributedTicketStore` round trip `Stored_ticket_can_be_retrieved_and_removed` (in-memory `MemoryDistributedCache`).
- [ ] **Step 2:** Failing integration tests: `Anonymous_user_endpoint_returns_401`, `User_endpoint_returns_name_and_roles_only` (alice; body has exactly `name` and `roles`; no property containing `token`), `Login_ignores_foreign_return_urls` (after login with `returnUrl=https://evil.example/`, the final URL is the BFF's `/`), `Logout_ends_the_session` (`POST /bff/logout` with the XSRF header from Task 4 is not yet required here; afterwards `/bff/user` → 401).
- [ ] **Step 3:** Run both; expect FAIL.
- [ ] **Step 4:** Implement the BFF: ServiceDefaults, `AddRedisDistributedCache("cache")`, `AddDataProtection().SetApplicationName("CloudGround.App.Bff").PersistKeysToStackExchangeRedis(...)` on the cache connection, cookie scheme (`__Host-bff`, `SameSite=Strict`, `HttpOnly`, secure outside Development) with `SessionStore`, `AddKeycloakOpenIdConnect("keycloak", realm: "cloudground")` (code flow, `SaveTokens`, `MapInboundClaims = false`, scopes `openid profile`, role claim `roles`), the three endpoints, `ProblemDetails` for 401.
- [ ] **Step 5:** Run both test commands; expect PASS. Commit `feat: log users in through the BFF`.

### Task 4: Forward `/api/*` with the access token and antiforgery

**Files:**
- Create: `src/CloudGround.App.Bff/Proxy/ApiProxy.cs`, `src/CloudGround.App.Bff/Antiforgery/XsrfProtection.cs`
- Create: `tests/CloudGround.App.Integration.Tests/BffProxyTests.cs`, `tests/CloudGround.App.Bff.Tests/ApiProxyTests.cs`
- Modify: BFF `Program.cs`, csproj

**Interfaces:**
- Produces: YARP route `api` for `/api/{**rest}` to cluster `https+http://api` (service discovery destination resolver); transforms: remove `Cookie`, set `Authorization: Bearer <access_token>` when the user is signed in.
- Produces: antiforgery with `HeaderName = "X-XSRF-TOKEN"`; the BFF sets a readable `XSRF-TOKEN` cookie on every response to a signed-in user; POST/PUT/PATCH/DELETE to `/api/*` and `/bff/logout` are validated and answer 400 `ProblemDetails` when the token is missing or wrong.

- [ ] **Step 1:** Failing integration tests: `Anonymous_read_goes_through_the_bff`, `Signed_in_write_goes_through_the_bff` (alice, with header → 201), `Write_without_xsrf_header_is_rejected` (alice, no header → 400, and the project does not exist), `Anonymous_write_through_the_bff_returns_401`. Failing unit tests in `ApiProxyTests` against the transformer that `ApiProxy` builds (YARP `ITransformBuilder.Build` on the `api` route, then `TransformRequestAsync` on a `DefaultHttpContext` with a cookie and a signed-in user whose properties hold an access token): `Forwarded_requests_carry_no_bff_cookies`, `Forwarded_requests_carry_the_access_token`, `Anonymous_requests_carry_no_authorization_header`.
- [ ] **Step 2:** Run; expect FAIL.
- [ ] **Step 3:** Implement the proxy and the antiforgery middleware; `Logout_ends_the_session` now sends the header.
- [ ] **Step 4:** Run both test commands; expect PASS. Commit `feat: forward API calls through the BFF`.

### Task 5: Sessions outlive a BFF instance

**Files:**
- Create: `tests/CloudGround.App.Integration.Tests/BffSessionTests.cs`

- [ ] **Step 1:** Failing test `Session_survives_a_bff_restart`: log in as alice, restart the `bff` resource through `KnownResourceCommands.RestartCommand`, wait until it is healthy, then `/bff/user` still returns alice and a signed-in write still succeeds. If Task 3 already persisted everything, the test passes at once; then prove it by a mutation (ephemeral Data Protection keys or no `SessionStore`), observe the failure, revert, and ledger it.
- [ ] **Step 2:** Run; expect PASS (or the mutation's FAIL). Commit `test: keep sessions across BFF restarts`.

### Task 6: Sample option, docs and pull request

- [ ] **Step 1:** `--sample false`: Keycloak, BFF and API authentication stay; `AuthorizationTests` and BFF tests run in both variants (the anonymous-write rule simply finds no write endpoint without the sample). `SampleOptionTests` stay green.
- [ ] **Step 2:** Docs: generated README (login flow, demo users alice and bob, the BFF's endpoints, XSRF for Angular, where sessions and keys live, the test client and why it exists only in the demo realm), root README status (phase 4 of 7), NuGet README, `LayoutTests` for `src/<name>.Bff` and `tests/<name>.Bff.Tests`.
- [ ] **Step 3:** Both test commands; expect PASS. Commit `docs: describe the BFF and login`.
- [ ] **Step 4:** Final review (fresh reviewer, most capable model), fix pass, push, pull request `feat: bff and login`, CI green, squash-merge.
