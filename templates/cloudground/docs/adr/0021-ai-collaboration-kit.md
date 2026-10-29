# 0021. Rules, skills and tools for AI agents in the repository

Status: Accepted

## Context

Coding agents write code fast, but they follow the patterns they have seen most, including outdated
ones. Without written rules and automated checks, every session drifts a little.

## Decision

The solution carries `AGENTS.md` with tool-neutral rules, `CLAUDE.md`, which imports `AGENTS.md` and
adds Claude Code specifics, two skills in `.claude/skills/` that create a module and add a feature
with example files, and `.mcp.json` for the Angular CLI's MCP server. Every rule that can be checked
is enforced in CI: warnings as errors and analyzers, the formatting check, Mapperly's diagnostics,
the architecture tests, ESLint, Conventional Commit titles and all test levels.

## Alternatives

- **No files for agents**: agents guess the conventions from the code and repeat its accidents.
- **One instruction file per tool**: files for each agent drift apart; many agents read `AGENTS.md`.
- **Code generators such as `dotnet new` item templates**: deterministic, but versioned apart from
  the solution; skill files travel with it and can be followed by hand.

## Consequences

- Humans and agents follow the same rules in one place.
- The example files in the skills can drift from the code; changes to the conventions update them
  too.
