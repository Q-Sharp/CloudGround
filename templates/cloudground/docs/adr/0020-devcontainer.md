# 0020. Devcontainer with its own Docker daemon

Status: Accepted

## Context

The quickstart should work the same way on every machine, including GitHub Codespaces.

## Decision

`.devcontainer/devcontainer.json` uses `mcr.microsoft.com/devcontainers/base` on Ubuntu 26.04 with
the features for .NET, Node, Docker in Docker and PowerShell, and it restores the solution's dotnet
tools when it is created.

## Alternatives

- **The .NET devcontainer image**: has no Ubuntu 26.04 variant for .NET 10.
- **The host's Docker socket in the container**: faster, but paths and permissions differ between
  hosts, especially with Podman.
- **No devcontainer**: one file less, but every machine needs every prerequisite installed.

## Consequences

- The container runs its own Docker daemon, so the AppHost starts its containers inside it.
- It works with Docker and with rootless Podman on the host.
