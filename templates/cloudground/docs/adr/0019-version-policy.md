# 0019. Newest LTS versions and Dependabot

Status: Accepted

## Context

A solution ages quickly, and outdated dependencies are a security and support risk.

## Decision

Every component is on its newest LTS release, or on its newest stable release where it has no LTS
line; Angular is always on its newest stable major. Dependabot updates NuGet, npm, container images,
devcontainer features and GitHub Actions in grouped weekly pull requests and proposes no major
updates for components that follow an LTS line. Angular and TypeScript update in a group of their
own, so the other npm packages keep updating when an Angular update fails. TypeScript stays in the
range the current Angular release supports, so Dependabot proposes only its patch updates.
Dependabot's pull requests are `chore(deps)` commits.

## Alternatives

- **Pinning and updating by hand**: predictable, but updates get forgotten.
- **Renovate**: more configurable, but a third-party service; Dependabot is built into GitHub.
- **Always the newest major, including non-LTS releases**: the newest features, but shorter support
  windows.

## Consequences

- Moving to a new LTS major is a deliberate pull request with an ADR.
- Dependabot's pull request for a new Angular major fails, because it keeps the old TypeScript.
  It is replaced by a pull request that runs `ng update`, which moves Angular and TypeScript together
  and migrates the code.
- Preview packages are exceptions and are recorded where they are used.
