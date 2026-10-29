@AGENTS.md

## Claude Code

- `/add-module` creates a module and `/add-feature` adds a feature slice to one; both run the
  skills in `.claude/skills/`.
- `.mcp.json` configures the Angular CLI's MCP server as `angular-cli`. Claude Code asks once
  whether to use it. Its `get_best_practices` and `search_documentation` tools give current Angular
  guidance; use them before writing Angular code.
