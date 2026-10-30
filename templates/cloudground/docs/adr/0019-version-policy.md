# 0019. Newest LTS versions and Dependabot

Status: Accepted

## Context

A solution ages quickly, and outdated dependencies are a security and support risk.

## Decision

Every component is on its newest LTS release, or on its newest stable release where it has no LTS
line; Angular is always on its newest stable major. Dependabot updates NuGet, npm, container images,
devcontainer features and GitHub Actions in grouped weekly pull requests and proposes no major
updates for components that follow an LTS line. Angular, majors included, updates in a group with
TypeScript apart from the other npm packages; TypeScript stays in the range the current Angular
release supports, so Dependabot proposes only its patch updates. Dependabot's pull requests are
`chore(deps)` or `chore(deps-dev)` commits.

## Alternatives

- **Pinning and updating by hand**: predictable, but updates get forgotten.
- **Renovate**: more configurable, but a third-party service; Dependabot is built into GitHub.
- **Always the newest major, including non-LTS releases**: the newest features, but shorter support
  windows.

## Consequences

- Moving to a new LTS major is a deliberate pull request with an ADR.
- A new Angular major arrives as a Dependabot pull request that fails, because it keeps the old
  TypeScript. A pull request that runs `ng update`, which moves Angular and TypeScript together and
  migrates the code, replaces it. Until then Dependabot proposes no fixes for the old major.
- Preview packages are exceptions and are recorded where they are used.
