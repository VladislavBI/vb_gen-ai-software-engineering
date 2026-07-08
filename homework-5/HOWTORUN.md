# How to Run — Homework 5 (MCP Servers)

PowerShell-first runbook (Windows, PowerShell 5.1). Covers: install dependencies, configure
credentials, run the custom server, connect the MCP configuration, and use/test the `read` tool.

All commands assume you start from the repository root
(`vb_gen-ai-software-engineering`). `python` here refers to **Python 3.x** (this stack uses
Python 3.13); if `python` maps to Python 2 on your system, use `py -3` instead.

---

## 1. Prerequisites

| Tool | Why | Check |
|---|---|---|
| Python 3.10+ | runs the custom FastMCP server and tests | `python --version` |
| Node.js + npx | launches the Filesystem MCP server | `npx --version` |
| uv / uvx | launches the Jira (`mcp-atlassian`) server | `uvx --version` |
| An MCP client | Claude Code / Claude Desktop / Copilot | — |

---

## 2. Install dependencies

The custom server depends on **`fastmcp`** (and `pytest` for the tests):

```powershell
python -m pip install -r homework-5\custom-mcp-server\requirements.txt
```

`requirements.txt` pins `fastmcp>=3.4,<4` and `pytest>=8`.

---

## 3. Configure credentials (environment variables)

[`mcp.json`](./mcp.json) references everything sensitive via `${...}` placeholders — nothing secret is
committed. Set these in the environment your MCP client launches from (PowerShell `$env:` shown; for a
persistent value use the client's own env config or `setx`):

```powershell
# GitHub MCP — a GitHub Personal Access Token with repo scope
$env:GITHUB_PAT = "<your-github-pat>"

# Filesystem MCP — absolute path to the directory to expose (adjust to your machine)
$env:HOMEWORK_5_DIR = "D:\Work\Courses\vb_gen-ai-software-engineering\homework-5"

# Jira MCP (mcp-atlassian)
$env:JIRA_URL = "https://your-domain.atlassian.net"
$env:JIRA_USERNAME = "<your-jira-email>"
$env:JIRA_API_TOKEN = "<your-jira-api-token>"
```

> The `custom` server needs no credentials.

---

## 4. Run the custom MCP server

```powershell
python homework-5\custom-mcp-server\server.py
```

This starts the FastMCP server over stdio (the transport the `custom` entry in `mcp.json` uses). It runs
until interrupted (Ctrl+C). Normally you do **not** start it by hand — the MCP client launches it for you
via `mcp.json` (next step). Run it directly only to confirm it starts without error.

---

## 5. Connect the MCP configuration to your client

The configuration is [`mcp.json`](./mcp.json), registering `github`, `filesystem`, `jira`, and `custom`.

- **Claude Code:** point it at this file, or copy the `mcpServers` block into your project `.mcp.json`.
- **Claude Desktop:** merge the `mcpServers` block into `claude_desktop_config.json`.
- **Copilot / other clients:** import the same `mcpServers` block.

The `custom` server uses a path relative to the config file
(`python custom-mcp-server/server.py`); MCP clients resolve `args` paths from the directory containing
`mcp.json`, so run the client with `homework-5/` as the config location (or make the path absolute).

After connecting, restart/reload the client so it picks up the four servers.

---

## 6. Use and test the `read` tool

### From the MCP client

Prompt your client, for example:

> Use the **read** tool to return 10 words.

Expected result (first 10 words of `lorem-ipsum.md`):

> Lorem ipsum dolor sit amet consectetur adipiscing elit sed do

Or read the Resource directly: `lorem://words` (default 30 words) or `lorem://words/7` (7 words).

### Reproduce without a client (demo script)

```powershell
powershell -ExecutionPolicy Bypass -File homework-5\demo\sample-read-requests.ps1
```

This prints the `read` output for word counts 30, 10, and 7.

### Run the tests

```powershell
Push-Location homework-5\custom-mcp-server
python -m pytest test_server.py -q
Pop-Location
```

All tests should pass (the suite drives the `read` tool and the resources through FastMCP's in-memory
client and asserts exact word counts).

---

## Troubleshooting

| Symptom | Fix |
|---|---|
| `ModuleNotFoundError: fastmcp` | Re-run step 2 (`pip install -r ...requirements.txt`). |
| Custom server `FileNotFoundError: lorem-ipsum.md` | The server reads relative to its own file; run it as shown — do not move `lorem-ipsum.md` out of `custom-mcp-server/`. |
| Filesystem server serves nothing | `HOMEWORK_5_DIR` is unset or wrong; set it to an existing absolute path. |
| Jira server errors on connect | Check `JIRA_URL`/`JIRA_USERNAME`/`JIRA_API_TOKEN`; the token must be a current Atlassian API token. |
| GitHub server 401 | `GITHUB_PAT` missing or lacks scope. |
