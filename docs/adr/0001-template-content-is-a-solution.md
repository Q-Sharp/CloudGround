# 0001. The template content is a complete solution

Status: Accepted

## Context

Template content has to be developed, built and tested like any solution. Content that compiles only
after `dotnet new` has run is slow to change and easy to break.

## Decision

`templates/cloudground/` is a complete solution with its own `CloudGround.App.slnx`,
`Directory.Build.props`, `Directory.Packages.props` and `global.json`. It builds, runs and passes
its tests in place. The root solution `CloudGround.Templates.slnx` holds only the packaging project
and the template tests. MSBuild uses the nearest `Directory.*.props`, so the two sets do not
interfere.

## Alternatives

- **Content with tokens that compile only after generation**: a smaller template, but no IDE
  support, no build, and every change needs a full generate-and-build cycle.
- **One solution for the packaging project and the content**: one file to open, but the content's
  package versions and build properties would mix with those of the packaging project.

## Consequences

- The content is worked on like any other solution.
- Output that the content produces while it builds and runs must be excluded from the package in
  both `CloudGround.Templates.csproj` and `templates/cloudground/.template.config/template.json`.
