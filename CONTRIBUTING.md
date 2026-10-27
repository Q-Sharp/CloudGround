# Contributing

## Build and test

```bash
dotnet test --solution CloudGround.Templates.slnx
dotnet test --solution templates/cloudground/CloudGround.App.slnx
```

The first command packs the template, installs it into an isolated template hive, generates
solutions and builds and tests them. The second builds and tests the template content in place.

## Pull requests

- Pull request titles follow [Conventional Commits](https://www.conventionalcommits.org/):
  `feat:` for new template features, `fix:` for corrections, `docs:`, `ci:`, `test:`,
  `refactor:` and `chore:` for everything else. `feat!:` marks a breaking change.
- Pull requests are squash-merged; the title becomes the commit message and drives the next
  version.
- Every change comes with tests.

## Code style

- Code and configuration contain no explanatory comments. Names, small functions and tests carry
  the meaning, so nothing can silently go out of date. Tool directives such as `#pragma` stay.
- Microsoft libraries come first; a third-party package needs a reason in the design.
- Warnings are errors.

## Template naming

Inside `templates/cloudground/`, `CloudGround.App` stands for the name passed with `--name`
(namespace form) and `CloudGround_App` for its identifier form, as Aspire uses it in the
generated `Projects.*` types. Use these placeholders wherever the solution name appears.
