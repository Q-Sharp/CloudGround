# 0018. Versions from Conventional Commits, release-please and MinVer

Status: Accepted

## Context

Versions should follow semantic versioning without anyone editing a version number.

## Decision

Conventional Commits decide the next version; pull request titles are checked in CI and
squash-merged. release-please keeps a release pull request open with the changelog, and merging it
tags `vX.Y.Z`. MinVer reads the version from the latest tag at build time and applies it to the
assemblies and the image tags.

## Alternatives

- **Version numbers in files**: easy to forget and a source of merge conflicts.
- **GitVersion**: computes versions from branches and tags with many modes; MinVer is smaller and
  enough with tags.
- **semantic-release**: publishes on every merge; release-please adds a deliberate release step.

## Consequences

- Pull request titles matter, and CI rejects titles that are not Conventional Commits.
- Builds between releases get pre-release versions.
