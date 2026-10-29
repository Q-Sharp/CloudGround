# 0005. The sample as a template option

Status: Accepted

## Context

The sample shows every layer at work, but a team that starts its own domain should not have to
delete it first.

## Decision

`--sample`, `true` by default, controls the *Projects* and *Tasks* modules and everything that
depends on them: `#if (sample)` in C#, `<!--#if (sample) -->` in XML and Markdown, `//#if (sample)`
in TypeScript, and an exclude list in `template.json` for files that belong to the sample. The
content defines `sample` for its own builds, so it always builds with the sample.

## Alternatives

- **Two templates**: duplicated content and two packages to keep in step.
- **A separate sample repository**: the template would ship no example, and the architecture tests
  would have nothing to check.

## Consequences

- Every line that belongs to the sample needs a condition.
- Tests generate both variants and check that the one without the sample builds, passes its tests
  and mentions no sample type.
