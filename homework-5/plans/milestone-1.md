# Milestone 1: MCP client configuration (four servers) — Session Plan

**Started:** 2026-06-30
**Super-plan reference:** ../PLAN.md milestone 1

## Approach

MCP servers are registered in a JSON configuration file (`mcp.json` or `.mcp.json`) that lists each server with its command, arguments, and environment variables. The approach for this milestone:

1. **Choose GitHub MCP**: Use the Anthropic-published GitHub MCP server; register with `npx` command since it is Node.js-based and published to npm. Environment variable `GITHUB_TOKEN` will be required by the user but is not validated here — this milestone verifies the configuration shape only.

2. **Choose Filesystem MCP**: Use the Anthropic-published Filesystem MCP server; register with `npx` command. Argument will be the target directory path (e.g., `.` for the repo root or a specific folder). No credentials needed.

3. **Choose Jira MCP (per user decision)**: Use the Anthropic-published Jira MCP server; register with `npx` command. Arguments will include the Jira domain and require environment variables `JIRA_USERNAME` and `JIRA_API_TOKEN`. These are not validated at config time — the user provides them.

4. **Register custom FastMCP server**: Point to `custom-mcp-server/server.py` which will be implemented in Milestone 2. Use a Python command to run it (e.g., `python` or a direct path). Arguments will specify the server entry point.

5. **Create mcp.json**: Place the config at `homework-5/mcp.json` as a single JSON object with `mcpServers` key mapping server names to configuration objects. Each server entry includes `command`, `args`, and optional `env` for credentials.

This approach satisfies TASKS.md Tasks 1–4's requirement to "register all four servers" and lets Milestone 2 build the custom server code without modifying the config. We avoid hardcoding actual credentials (which would fail on first run without them) by relying on environment variables the user will supply in their actual MCP client setup.

## Touch list

- **homework-5/mcp.json**: Create new file with `mcpServers` object containing four server entries:
  - `github`: Anthropic GitHub MCP via `npx @anthropic-ai/github-mcp`, expecting `GITHUB_TOKEN` env var
  - `filesystem`: Anthropic Filesystem MCP via `npx @anthropic-ai/filesystem-mcp`, argument points to a directory (e.g., `"."`)
  - `jira`: Anthropic Jira MCP via `npx @anthropic-ai/jira-mcp`, expecting `JIRA_USERNAME` and `JIRA_API_TOKEN` env vars and Jira domain argument
  - `custom`: Custom FastMCP server via `python custom-mcp-server/server.py`, located in homework-5 directory so relative path works

## Review focus

- **JSON validity**: File parses correctly via `ConvertFrom-Json` in PowerShell; no syntax errors.
- **Server completeness**: All four servers (`github`, `filesystem`, `jira`, `custom`) are present as keys in `mcpServers`.
- **Command accuracy**: Each server command is realistic (e.g., `npx` for npm packages, `python` for custom server); no obvious typos.
- **Argument shape**: Arguments follow expected patterns (directory path for Filesystem, domain for Jira, module path for custom server).
- **Configuration keys**: Each server includes `command` at minimum; `args` and `env` present where needed (e.g., Jira has `env` for API token).

## Notes

- The original Touch-list described three non-existent npm packages (`@anthropic-ai/github-mcp`, `@anthropic-ai/filesystem-mcp`, `@anthropic-ai/jira-mcp`). At implementation time (per user direction to use real, runnable specs) these were corrected to:
  - **github** — official GitHub remote MCP endpoint `https://api.githubcopilot.com/mcp/` using `type: http` with an `Authorization: Bearer ${GITHUB_PAT}` header (no npm package).
  - **filesystem** — real npm package `@modelcontextprotocol/server-filesystem` via `npx -y`.
  - **jira** — real community Python package `mcp-atlassian` via `uvx` (user chose Jira over Notion). There is no official Anthropic Jira package.
  - **custom** — `python custom-mcp-server/server.py` (relative path; MCP clients resolve `args` paths from the config file's directory). Milestone 2 creates this file.
- Per code-review-advisor: replaced the machine-specific filesystem absolute path with the `${HOMEWORK_5_DIR}` env-var placeholder, and changed the literal `JIRA_URL` example to the `${JIRA_URL}` placeholder, for portability and consistency with the other credential placeholders. `HOMEWORK_5_DIR` is documented in HOWTORUN (Milestone 6).
- Kept `command: python` (not `python3`) to stay consistent with every Verify block and the other homework-5 commands on this Windows stack; the Python-version assumption is documented in HOWTORUN.
