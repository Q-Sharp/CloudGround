# 0016. Container images from the .NET SDK on Ubuntu chiseled images

Status: Accepted

## Context

Every service must run as a container outside development, built the same way locally and in CI.

## Decision

The .NET services are built with the .NET SDK's container publishing, without Dockerfiles, on Ubuntu
26.04 chiseled images with ICU and time zone data: no shell, no package manager, a non-root user.
Migration bundles use `aspnet:10.0-resolute`, because they need a shell. The web app has its own
Dockerfile on `node:26-trixie-slim`, because SDK container publishing covers only .NET. Every image
is tagged with the solution's version.

## Alternatives

- **Dockerfiles for the .NET services**: full control, but more files to maintain and keep patched.
- **Alpine-based .NET images**: small, but musl instead of glibc and no ICU by default.
- **Images from other distroless vendors**: similar security, but outside the images Microsoft
  supports for .NET.

## Consequences

- The service images have no shell; problems are inspected from outside the container.
- Base image updates arrive through Dependabot and the .NET SDK.
