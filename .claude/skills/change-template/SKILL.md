---
name: change-template
description: Changes the CloudGround template content or its packaging with the right placeholders, sample conditions, package exclusions and tests. Use for any change under templates/cloudground, to CloudGround.Templates.csproj or to the template tests.
---

# Change the template

## Where a change belongs

- Behavior of a generated solution (its code, configuration, tests and documentation) changes in
  `templates/cloudground/`. Test it inside the content, in the test project the change concerns.
- How the template generates, names and packs (`template.json`, `CloudGround.Templates.csproj`,
  symbols, conditions) is tested in `tests/CloudGround.Templates.Tests`, which packs, installs and
  generates.
- A change that only shows after generation needs both, for example a new file that must be renamed
  or left out without the sample.

## Placeholders

| Form | Placeholder | For `--name Contoso.Shop` |
|---|---|---|
| namespaces, projects, folders | `CloudGround.App` | `Contoso.Shop` |
| identifier, as in Aspire's `Projects.*` types | `CloudGround_App` | `Contoso_Shop` |
| lowercase kebab case: realm, clients, Compose project | `cloudground-app` | `contoso-shop` |

Never write a concrete solution name in the content.

## Content that belongs to the sample

| Where | Condition |
|---|---|
| C# | `#if (sample)` … `#endif` |
| XML, `.slnx`, `.csproj`, Markdown | `<!--#if (sample) -->` … `<!--#endif -->` |
| TypeScript | `//#if (sample)` … `//#endif` |
| whole files | the `!sample` exclude list in `templates/cloudground/.template.config/template.json` |

The content defines `sample` in `templates/cloudground/Directory.Build.props`, so it always builds
with the sample. A solution generated with `--sample false` must build, pass its tests and mention
no sample type.

Every project keeps its imports in the `GlobalUsings.cs` at its root. An import that only the sample
uses, including one that only files in the `!sample` exclude list use, stays in its sorted place
there, between `#if (sample)` and `#endif`.

## Generated output

Files that the content's build, tests or tools write must stay out of the package: add them to the
exclude lists in `CloudGround.Templates.csproj` and in
`templates/cloudground/.template.config/template.json`, and to `templates/cloudground/.gitignore`.
NuGet rejects a file whose installed path reaches 200 characters, so keep paths in the content
short.

## Dependencies

.NET package versions live in `templates/cloudground/Directory.Packages.props`, the web app's in
`templates/cloudground/src/CloudGround.App.Web/package.json`. A new folder with a manifest needs an
entry in both Dependabot files, `.github/dependabot.yml` and
`templates/cloudground/.github/dependabot.yml`; the Dependabot tests check them.

## Decisions

A significant decision about the template repository gets an ADR in `docs/adr/`; one about the
generated solutions gets an ADR in `templates/cloudground/docs/adr/`.

## Verify

```bash
dotnet test --solution templates/cloudground/CloudGround.App.slnx
dotnet test --solution CloudGround.Templates.slnx
```

The first runs the content's tests in place, end-to-end tests included. The second packs the
template, generates solutions with and without the sample and builds and tests them; it takes many
minutes and needs Docker or Podman.
