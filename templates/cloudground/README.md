# CloudGround.App

Created with the [CloudGround](https://github.com/Q-Sharp/CloudGround) template.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Docker or Podman for the containers the AppHost starts

## Run

```bash
dotnet run --project src/CloudGround.App.AppHost
```

The Aspire dashboard opens in the browser and shows every resource, its logs and its traces.

## Test

```bash
dotnet test --solution CloudGround.App.slnx
```

## Versioning and releases

Versions follow semantic versioning and come from Git tags through MinVer. Pull request titles
follow [Conventional Commits](https://www.conventionalcommits.org/); they decide the next
version. release-please keeps a release pull request open with the changelog. Merging it tags the
release.

## GitHub setup

release-please opens its release pull request with the workflow's `GITHUB_TOKEN`. Allow that
once per repository, either under *Settings → Actions → General → Workflow permissions → Allow
GitHub Actions to create and approve pull requests*, or with:

```bash
gh api -X PUT repos/<owner>/<repo>/actions/permissions/workflow \
  -f default_workflow_permissions=read -F can_approve_pull_request_reviews=true
```

Use squash merges with the pull request title as the commit message, so the Conventional Commit
title reaches the main branch.
