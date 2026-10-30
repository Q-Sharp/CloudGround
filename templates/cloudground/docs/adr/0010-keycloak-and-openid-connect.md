# 0010. Keycloak locally, any OpenID Connect provider in operation

Status: Accepted

## Context

Sign-in must work out of the box with real users and roles, without a cloud account, and the
deployment must work with the identity provider an organization already runs.

## Decision

The AppHost runs Keycloak with the realm in `src/CloudGround.App.AppHost/Realms/realm.json`: demo
users, the `editor` role, the API's client and a client for the tests. The deployment leaves
Keycloak out; the API takes the authority from `Keycloak:Url`, and any OpenID Connect provider with
the same client, audience and role claims works.

## Alternatives

- **Microsoft Entra ID**: managed and common in organizations, but it needs a tenant and cannot run
  locally or in CI.
- **An identity server inside the solution, such as OpenIddict**: identity as code, which then
  becomes part of what the team must secure and operate.
- **Fake authentication in development**: fast, but it hides the real login flow until production.

## Consequences

- The realm is for development only: it accepts any redirect URI and has a client that gets tokens
  with a password, for the tests.
- Integration and end-to-end tests run the real login.
- `Aspire.Hosting.Keycloak` and `Aspire.Keycloak.Authentication` are previews.
