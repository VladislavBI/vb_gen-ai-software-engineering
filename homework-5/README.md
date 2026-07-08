# Homework 5 — MCP Servers (GitHub, Filesystem, Jira, Custom FastMCP)

**Author:** Vlad Bairak  
**GitHub:** [VladislavBI](https://github.com/VladislavBI)  
**Date:** 2026-06-30

---

## Overview

This homework configures **three external MCP servers** (GitHub, Filesystem, Jira) and builds **one
custom MCP server** with [FastMCP](https://github.com/jlowin/fastmcp). All four are registered in a
single [`mcp.json`](./mcp.json). The custom server exposes a lorem-ipsum source through both a
**Resource** (a URI Claude reads) and a **Tool** (an action Claude calls), each honoring a
`word_count` parameter (default 30).

See [HOWTORUN.md](./HOWTORUN.md) to install, run, connect, and test everything, and
[docs/resources-vs-tools.md](./docs/resources-vs-tools.md) for the Resources-vs-Tools explanation.

---

## Configured MCP servers

All four are registered under `mcpServers` in [`mcp.json`](./mcp.json):

| Server | Type | Launch | Credentials / args |
|---|---|---|---|
| **github** | remote HTTP | `https://api.githubcopilot.com/mcp/` | `Authorization: Bearer ${GITHUB_PAT}` |
| **filesystem** | stdio (npx) | `npx -y @modelcontextprotocol/server-filesystem ${HOMEWORK_5_DIR}` | directory path via `${HOMEWORK_5_DIR}` |
| **jira** | stdio (uvx) | `uvx mcp-atlassian` | `${JIRA_URL}`, `${JIRA_USERNAME}`, `${JIRA_API_TOKEN}` |
| **custom** | stdio (python) | `python custom-mcp-server/server.py` | none |

Credentials are referenced as `${...}` environment-variable placeholders — **no secrets are committed**.
Set them in your MCP client's environment (see HOWTORUN).

---

## Custom FastMCP server

Location: [`custom-mcp-server/`](./custom-mcp-server/)

| File | Role |
|---|---|
| `server.py` | FastMCP server: the `read` Tool + the `lorem://words` Resources |
| `lorem-ipsum.md` | source text the resource/tool read from (69 words) |
| `requirements.txt` | dependencies — includes `fastmcp` (and `pytest` for tests) |
| `test_server.py` | pytest suite driving the `read` tool over FastMCP's in-memory client |

### Behavior

- **Tool `read(word_count=30)`** — returns the first `word_count` words of `lorem-ipsum.md`.
- **Resource `lorem://words`** — the default 30 words.
- **Resource `lorem://words/{word_count}`** — an explicit count from the URI.

All three delegate to a shared `_read_words` helper, so the word-limiting logic is exercised by one
code path (directly and through the in-memory client in the tests). Out-of-range input degrades
gracefully: `word_count <= 0` returns an empty string; a count larger than the file returns every word.

Example — `read` with `word_count=10`:

> Lorem ipsum dolor sit amet consectetur adipiscing elit sed do

---

## Deliverables

| Deliverable | Location |
|---|---|
| MCP configuration (4 servers) | [`mcp.json`](./mcp.json) |
| Custom MCP server | [`custom-mcp-server/server.py`](./custom-mcp-server/server.py) |
| Dependencies (incl. `fastmcp`) | [`custom-mcp-server/requirements.txt`](./custom-mcp-server/requirements.txt) |
| Lorem-ipsum source | [`custom-mcp-server/lorem-ipsum.md`](./custom-mcp-server/lorem-ipsum.md) |
| Tests | [`custom-mcp-server/test_server.py`](./custom-mcp-server/test_server.py) |
| Resources-vs-Tools explanation | [`docs/resources-vs-tools.md`](./docs/resources-vs-tools.md) |
| Screenshots (one per server) | [`docs/screenshots/`](./docs/screenshots/) |
| Demo | [`demo/sample-read-requests.ps1`](./demo/sample-read-requests.ps1) |
| Runbook | [`HOWTORUN.md`](./HOWTORUN.md) |

### Screenshots

| File | Shows |
|---|---|
| `docs/screenshots/github-mcp-result.png` | GitHub MCP interaction (e.g. recent PRs/commits) |
| `docs/screenshots/filesystem-mcp-result.png` | Filesystem MCP listing/reading the configured directory |
| `docs/screenshots/jira-or-notion-mcp-result.png` | Jira MCP answering "last 5 bugs" (ticket keys only) |
| `docs/screenshots/custom-mcp-read-tool-result.png` | the custom `read` tool returning word-limited output |

---

## How AI tools were used

This homework was produced with **Claude Code** via the repository's `/homework` workflow:

- The **homework-planner** decomposed `TASKS.md` into the 6-milestone [`PLAN.md`](./PLAN.md).
- Each milestone was implemented through an **edit → review → apply → verify** loop, with the
  **code-review-advisor** agent reviewing every diff against milestone-specific criteria before a
  PowerShell `Verify` command proved the behavior.
- Per-milestone session plans live under [`plans/`](./plans/) and record the approach, review focus,
  and notes (including how the initially-planned placeholder MCP package names were corrected to real,
  runnable specs, and how the mandatory Tests milestone was adapted from .NET/xUnit to Python/pytest).

`PLAN.md` and `plans/` are checked-in AI-usage evidence.

---

## Architecture notes

- **Single config, four servers.** `mcp.json` is the contract the client uses to launch every server;
  it is intentionally credential-free (env-var placeholders) so it is portable and safe to commit.
- **Shared helper.** Keeping `_read_words` as the one place word-limiting happens means the Resource,
  the Tool, and the tests can never disagree about the result.
- **CWD-independent file read.** The server resolves `lorem-ipsum.md` via `Path(__file__).parent`, so it
  works regardless of the directory the MCP client launches it from.
