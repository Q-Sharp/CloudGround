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
validated and the access token is due, the API refreshes it at Keycloak; when the refresh fails,
because the user was blocked or the Keycloak session ended, the API rejects the cookie and signs the
user out. The API forwards no tokens: it authenticates the cookie directly.

A request with an `Authorization: Bearer` header is authenticated as a JWT bearer token, every other
request by the cookie. Unsafe requests with the cookie need the XSRF token that Angular sends by
itself: the API sets the `XSRF-TOKEN` cookie and expects its value in the `X-XSRF-TOKEN` header, or
in the form field of the logout form. `GET /account/login`, `POST /account/logout` and
`GET /account/user` take over from the backend for frontend's endpoints.

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
  working; a user blocked in Keycloak loses access within the access token's lifetime of 5 minutes.
  In operation the operator's identity provider sets these times, not the template. Logging out of
  the application ends both sessions.
- Parallel requests of one session may refresh with the same refresh token, so the realm keeps used
  refresh tokens valid.
- Clients without a browser still call the API with bearer tokens and need no XSRF token.
- The web app never sees the session cookie, and server rendering stays anonymous.
