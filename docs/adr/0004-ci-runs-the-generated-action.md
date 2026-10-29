# 0004. CI runs the generated solution's own composite action

Status: Accepted

## Context

The template's CI must prove that a generated solution passes its own CI, without a second copy of
those steps that could drift.

## Decision

Every generated solution ships a composite action that its own workflow uses; in this repository it
is `templates/cloudground/.github/actions/build-and-test`. The template's CI generates solutions
with and without the sample and runs that action from the generated folders. A second job builds the
generated devcontainer and runs the tests inside it.

## Alternatives

- **The same steps written into the template's workflow**: adopters would run something different
  from what the template checks.
- **The generated workflow as a reusable workflow**: reusable workflows must live in a repository's
  `.github/workflows` folder, not in a generated subfolder.

## Consequences

- Every pull request to the template tests the CI that adopters get.
- The composite action takes the solution folder as an input.
