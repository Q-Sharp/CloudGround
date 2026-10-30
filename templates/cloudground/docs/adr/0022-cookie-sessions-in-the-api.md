# 0022. Cookie sessions in the API

Status: Accepted

## Context

The browser must never hold OAuth tokens, and it must see one origin for its pages and its API
calls. [0009](0009-backend-for-frontend.md) met both with a backend for frontend: a service of its
own that kept the session and the tokens, forwarded `/api/*` with the access token and served the
web app. That service and its hop to the API are code to build, test and run, while the API is the
only resource behind it.

## Decision

The API signs browsers in itself. It uses OpenID Connect against Keycloak, with the authorization
code flow and PKCE, through the client `cloudground-app-api`, and gives the browser an HttpOnly
session cookie. The session ticket with the tokens and the Data Protection keys live in Garnet, so
every API instance accepts every session. The session slides with activity. Whenever the cookie is
validated and the access token is due, the API refreshes it at Keycloak and replaces the session's
roles with those of the new ID token; when the refresh fails, because the user was blocked or the
Keycloak session ended, the API rejects the cookie and signs the user out. The API forwards no tokens: it authenticates the cookie directly.

A request with an `Authorization: Bearer` header is authenticated as a JWT bearer token, every other
request by the cookie. Unsafe requests with the cookie need the XSRF token that Angular sends by
itself: the API sets the `cloudground-app-xsrf` cookie and expects its value in the `X-XSRF-TOKEN`
header, or in the form field of the logout form. Every cookie name starts with the solution's name.
`GET /account/login`, `POST /account/logout` and `GET /account/user` take over from the backend for
frontend's endpoints.

Every request that no endpoint of the API handles goes through YARP to the Angular SSR server,
without the `Cookie` and `Authorization` headers, so the browser still sees one origin. Paths under
`/api` and `/account` never reach the web app. This supersedes
[0009](0009-backend-for-frontend.md).

## Alternatives

- **Keep the backend for frontend**: the API stays a plain resource server that validates access
  tokens and knows nothing about cookies or antiforgery; in exchange there is one more service in
  every request and a proxy to the API that has to attach the access token, to build, test and
  operate.
- **A fixed session without token refresh**: less code, but Keycloak could no longer end a session
  early, so a blocked user would keep access until the session expires.
- **An OpenID Connect client in Angular**: no session in the backend, but tokens live in the
  browser, and silent renewal is fragile.
- **Duende's backend for frontend framework**: complete and well documented, but most companies
  need a commercial license.

## Consequences

- One service less in the deployment and in every request.
- The API owns sessions, antiforgery, the token refresh and the forwarding of pages besides its
  endpoints, and it is the service that the deployment publishes.
- The API stands in front of the web app, so when the API is down, the whole site is down, public
  pages included.
- A session lasts as long as the identity provider's session. With the local realm it ends after
  30 minutes without requests and at the latest 10 hours after sign-in, also while someone is
  working; a role taken away in Keycloak ends, and a user blocked there loses access, within the
  access token's lifetime of 5 minutes.
- The identity provider must return an ID token with the roles on every refresh. A refresh without
  one signs the user out, because the session could not know the current roles.
  In operation the operator's identity provider sets these times, not the template. Logging out of
  the application ends both sessions.
- When Keycloak is unreachable for longer than an access token lives, the API signs the user out at
  the next request, and signing in fails until Keycloak is back.
- When Garnet is unreachable, requests with a session cookie count as anonymous: public pages and
  anonymous API calls keep working, endpoints that need a user answer 401, and the same cookie works
  again once Garnet is back with its data. Signing in and renewing a session fail while it is down.
  Logging out still removes the cookie, so a user can always leave a shared browser; the stored
  session then stays in Garnet until it expires, and the session at Keycloak may stay open.
- Parallel requests of one session may refresh with the same refresh token, so the realm keeps used
  refresh tokens valid.
- A wrong method on an existing path of the API answers 404 `ProblemDetails`, not 405, because the
  not-found endpoints under `/api` and `/account` accept every method.
- Clients without a browser still call the API with bearer tokens and need no XSRF token.
- The web app never sees the session cookie, and server rendering stays anonymous.
