# 0012. Mapperly for object mapping

Status: Accepted

## Context

Entities must not leak into API responses, and mapping code is repetitive and a common place for
silent mistakes.

## Decision

Mapperly, a source generator: mappers are partial classes, their code is generated at build time,
and a target member without a source is a build error. Queries map with generated projections, so
only the needed columns are read.

## Alternatives

- **AutoMapper**: maps by convention at runtime, so mistakes surface late; it moved to a commercial
  license in 2025.
- **Hand-written mapping**: no dependency, but boilerplate, and nothing notices a new member that
  was forgotten.
- **Mapster**: can generate code too, but maps at runtime by default.

## Consequences

- A new response member without a source fails the build, also when an agent wrote the code.
- The generated code can be read and debugged.
