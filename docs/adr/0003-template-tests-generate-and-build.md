# 0003. Template tests pack, install, generate and build

Status: Accepted

## Context

A template is correct only if what it generates builds and passes its tests. The content can work in
place while generation breaks it, through names, conditions or excluded files.

## Decision

`tests/CloudGround.Templates.Tests` packs the template, installs it into an isolated template hive
with `--debug:custom-hive` and generates solutions with and without the sample and with unusual
names. It builds them, runs their tests and the web app's build, applies the module and feature
skills, starts the Angular CLI's MCP server and checks the package's content and every path in the
documentation.

## Alternatives

- **Testing only the content in place**: misses everything that generation changes.
- **Snapshot tests of the generated files**: catch every change but prove nothing about whether the
  output works, and every intended change rewrites the snapshots.
- **Installing into the user's template hive**: leaves state on the machine and conflicts with an
  installed release.

## Consequences

- The suite takes many minutes and needs Docker or Podman.
- Generated solutions live in a temporary folder that is deleted afterwards.
