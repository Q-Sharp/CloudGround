# 0006. Design, plans and written rules steer the AI

Status: Accepted

## Context

This repository is built with an AI coding agent. Left alone, an agent makes many small decisions
that nobody wrote down, and afterwards nobody can tell why the code looks as it does.

## Decision

Work follows written artifacts. The design in `docs/superpowers/specs/` marks every decision as the
author's, approved, a default or left to the builder. Every phase has a plan in
`docs/superpowers/plans/` with its tasks, the tests that prove them and the exact values from the
design. `AGENTS.md` holds the rules for every change.

The agent writes each test first and watches it fail. Every phase ends with a review of the whole
branch by a separate model and lands as a pull request with CI. Decisions the agent takes while
executing a plan are reported to the author and recorded in the design. Commits carry the agent's
co-author trailer.

## Alternatives

- **Prompting without written plans**: fast for small changes, but the decisions disappear in chat
  histories.
- **AI for code completion only**: less risk, but no help with design, tests and documentation.
- **No AI**: slower, and the repository could not show how AI-assisted development is kept under
  control.

## Consequences

- The specs, the plans and the commit history show how every part was built.
- Generated solutions get their own rules, skills and tools for agents.
