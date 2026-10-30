# Working rules

These rules apply to every change in this repository, by humans and AI agents alike.

## Repository

- `CloudGround.Templates.csproj` packs `templates/` into the `CloudGround.Templates` NuGet package.
- `templates/cloudground/` is the template content and a complete solution of its own
  (`CloudGround.App.slnx`).
- `tests/CloudGround.Templates.Tests/` packs and installs the template into an isolated hive,
  generates solutions and builds and tests them.
- `docs/superpowers/specs/` holds the design, `docs/superpowers/plans/` the implementation plans.
- `docs/adr/` records the repository's decisions; `templates/cloudground/docs/adr/` those of the
  generated solutions.
- `.claude/skills/change-template/SKILL.md` describes how to change the template: placeholders,
  sample conditions, package exclusions and the tests to run.

## Commands

```bash
dotnet test --solution CloudGround.Templates.slnx
dotnet test --solution templates/cloudground/CloudGround.App.slnx
```

Tests use xUnit v3 on Microsoft.Testing.Platform. Filter with `--filter-class` or
`--filter-method`.

## Rules

- C# on .NET 10. Always the newest LTS version of a component, or the newest stable release where
  there is no LTS line.
- HTTP endpoints are Minimal APIs; no controllers.
- Microsoft libraries first. Every third-party package is named in the design.
- No explanatory comments in code or configuration, including YAML, `.editorconfig` and
  `.gitignore`. Rewrite unclear code instead. Tool directives without explanation are allowed.
- Every .NET project keeps all its namespace imports in one `GlobalUsings.cs` at its root, and no
  other C# file has a `using` directive, except generated files such as EF Core migrations; tests
  check this for the generated solutions and for `tests/CloudGround.Templates.Tests`.
- Warnings are errors.
- Everything in English.
- Inside the template content, `CloudGround.App` and `CloudGround_App` are placeholders for the
  solution name; never write a concrete solution name there.
- Commit messages and pull request titles follow Conventional Commits. Commits made with an AI
  agent end with its `Co-Authored-By` trailer. Commit messages and pull request descriptions
  contain no links to agent sessions.
- No personal information in the repository.
- Write the failing test first, watch it fail, then make it pass.
- A significant decision gets an ADR, in `docs/adr/` for the repository and in
  `templates/cloudground/docs/adr/` for the generated solutions.
