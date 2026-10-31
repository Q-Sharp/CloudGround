# 0007. Releases with release-please, MinVer and a publish job

Status: Accepted

## Context

The template package needs versions and releases without hand-edited version numbers, and an
ordinary push must never publish anything.

## Decision

The template uses the same mechanism as the solutions it generates: Conventional Commit pull request
titles, release-please's release pull request with the changelog, and MinVer reading the version
from the tag. When a release pull request is merged, a job packs the template and pushes it to NuGet
with Trusted Publishing: it trades a GitHub identity token for a key that lives one hour, under a
policy on nuget.org. Only this job may request the identity token. The repository secret
`NUGET_USER` holds the nuget.org profile name.

## Alternatives

- **Publishing on every push to the main branch**: no deliberate release step.
- **Running `dotnet nuget push` by hand**: depends on one machine and its credentials.
- **A long-lived API key as a repository secret**: nuget.org discourages it for automated
  publishing, and a leaked key stays valid until someone revokes it.

## Consequences

- Merging the release pull request is the release.
- Without the policy or the secret the publish job fails, and nothing is published.
- The policy names the workflow file, so renaming `release-please.yml` means changing the policy.
