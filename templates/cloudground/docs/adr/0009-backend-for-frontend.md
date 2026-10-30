# 0009. Backend for frontend with cookie sessions and YARP

Status: Superseded by [0022](0022-cookie-sessions-in-the-api.md)

## Context

A browser app that holds OAuth tokens exposes them to any script on the page. Current guidance for
browser-based apps recommends a backend that keeps the tokens.

## Decision

The BFF in `src/CloudGround.App.Bff` signs users in with OpenID Connect (authorization code flow
with PKCE), keeps the tokens in the server-side session and gives the browser an HttpOnly cookie. It
protects state-changing requests with an XSRF token that Angular sends by itself and forwards
`/api/*` with the access token through YARP. It also serves the web app, so the browser sees one
origin.

## Alternatives

- **An OpenID Connect client in Angular**: no extra service, but tokens live in the browser, and
  silent renewal is fragile.
- **Duende BFF**: complete and well documented, but most companies need a commercial license.
- **Cookie authentication in the API itself**: no proxy, but the API would need sessions and
  antiforgery, and it would no longer be a plain resource server.

## Consequences

- One more service in the request path; YARP keeps it thin.
- Sessions and keys live in Garnet, so several BFF instances share them.
- The API validates access tokens and knows nothing about cookies.
