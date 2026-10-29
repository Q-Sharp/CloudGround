# 0013. Architecture tests with ArchUnitNET

Status: Accepted

## Context

Rules that are only written down erode. Module boundaries need automated checks.

## Decision

`tests/CloudGround.App.Architecture.Tests` uses ArchUnitNET with xUnit v3 and reflection. For every
module it finds, it checks that modules reference each other only through contracts, do not depend
on each other's types and expose only their module class, requests and message handlers; that
contracts and domain types are free of frameworks; that every `DbContext` has its own schema and
retries; and that there are no controllers.

## Alternatives

- **NetArchTest**: similar fluent rules; ArchUnitNET has an xUnit v3 integration and a richer model
  of dependencies.
- **Assemblies and `internal` alone**: the compiler enforces some boundaries, but not rules such as
  contracts without framework references.
- **Code review alone**: depends on attention and does not keep up with AI-generated changes.

## Consequences

- The rules apply to modules that do not exist yet; a new module is checked automatically.
- Each rule is a test that names the offending types.
