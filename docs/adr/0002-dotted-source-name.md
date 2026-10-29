# 0002. The source name CloudGround.App and its derived forms

Status: Accepted

## Context

`dotnet new cloudground --name Contoso.Shop` must rename namespaces, projects, folders, the realm,
resource names and the `Projects.*` types that Aspire generates, consistently, also for names that
are no valid identifiers, such as `my-shop`.

## Decision

The source name is the dotted `CloudGround.App`. `template.json` derives three forms from the given
name: `CloudGround.App` becomes the name with characters that are invalid in a namespace replaced,
`CloudGround_App` the identifier form that Aspire uses in `Projects.CloudGround_App_AppHost`, and
`cloudground-app` the lowercase kebab form for the realm, its clients and the Compose project.

## Alternatives

- **`CloudGround` as the source name**: the template's own name would be replaced everywhere,
  including the links to this repository.
- **A single word such as `MyApp`**: its identifier form equals the plain form, so the template
  engine cannot replace the two separately.
- **The source name alone, without derived symbols**: `my-shop` would produce invalid namespaces and
  realm names that do not match the code.

## Consequences

- The template content uses the three placeholders wherever the solution name appears.
- The naming tests check that generated solutions contain none of the forms.
