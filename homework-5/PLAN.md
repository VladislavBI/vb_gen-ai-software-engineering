# Homework 5 — Plan

**TASKS.md commit:** 1a8386c
**Created:** 2026-06-30
**Stack:** Python 3.x / FastMCP (MCP servers) — not the repo-default .NET stack

## Overview
Homework 5 (TASKS.md Tasks 1–4) requires configuring three external MCP servers (GitHub, Filesystem, Jira-or-Notion) and building one custom FastMCP server that exposes a `lorem-ipsum.md` Resource plus a `read` Tool, both honoring a `word_count` parameter (default 30). The chosen approach: register all four servers in a single `mcp.json` (Task deliverables), implement the custom server in `custom-mcp-server/server.py` with a shared word-limiting helper so behavior is unit-testable in-process, add a pytest suite exercising the `read` tool over FastMCP's in-memory transport, capture one result screenshot per server (TASKS.md Deliverables/Expected Structure), and finalize README/HOWTORUN/demo plus the required Resources-vs-Tools explanation. Tasks 1–3 depend on external credentials and human-in-the-loop interaction, so those milestones verify configuration validity and screenshot evidence rather than live external API calls.

## Milestones

### Milestone 1: MCP client configuration (four servers)
- **Goal:** Produce a parseable `mcp.json` that registers the GitHub, Filesystem, Jira-or-Notion, and custom FastMCP servers.
- **Why this milestone:** The MCP config is the contract the client uses to launch every server (TASKS.md Deliverables: "mcp.json with all four servers registered"); isolating it lets us verify the registration shape before any server code or credentials exist, and it is the dependency every interaction screenshot relies on.
- **Files:** homework-5/mcp.json
- **Depends on:** none
- **Parallel:** safe
- **Verify:**
  ```powershell
  $cfg = Get-Content homework-5\mcp.json -Raw | ConvertFrom-Json
  $servers = $cfg.mcpServers
  foreach ($name in 'github','filesystem','custom') { if (-not $servers.$name) { throw "missing server: $name" } }
  if (-not ($servers.jira -or $servers.notion)) { throw 'missing jira or notion server' }
  $count = ($servers.PSObject.Properties | Measure-Object).Count
  if ($count -lt 4) { throw "expected >=4 servers, got $count" }
  Write-Host "mcp.json valid: $count servers"
  ```
- **Done:** [x]

### Milestone 2: Custom FastMCP server skeleton + dependencies
- **Goal:** Stand up an importable FastMCP server module with its `fastmcp` dependency declared and a `lorem-ipsum.md` source the resource will read.
- **Why this milestone:** TASKS.md Task 4 requires a separate `custom-mcp-server/` folder with `server.py`, a dependencies file that explicitly includes `fastmcp`, and the `lorem-ipsum.md` source; getting the module to import and the dependency to install proves the environment is reproducible before any tool/resource behavior is layered on.
- **Files:** homework-5/custom-mcp-server/server.py, homework-5/custom-mcp-server/requirements.txt, homework-5/custom-mcp-server/lorem-ipsum.md
- **Depends on:** none
- **Parallel:** safe
- **Verify:**
  ```powershell
  Push-Location homework-5\custom-mcp-server
  try {
    python -m pip install -r requirements.txt
    if ($LASTEXITCODE -ne 0) { throw 'pip install failed' }
    if (-not (Select-String -Path requirements.txt -Pattern 'fastmcp' -Quiet)) { throw 'fastmcp missing from requirements' }
    if ((Get-Content lorem-ipsum.md -Raw).Split() .Count -lt 40) { throw 'lorem-ipsum.md too short for 30+ word tests' }
    python -c "import importlib.util as u; s=u.spec_from_file_location('server','server.py'); m=u.module_from_spec(s); s.loader.exec_module(m); assert hasattr(m,'mcp'), 'no mcp object'; print('server import OK')"
    if ($LASTEXITCODE -ne 0) { throw 'server import failed' }
  } finally { Pop-Location }
  ```
- **Done:** [x]

### Milestone 3: Resource + `read` tool with word-count limiting
- **Goal:** Implement the `lorem-ipsum` Resource and the `read` Tool so both return exactly `word_count` words (default 30, configurable) via a shared helper.
- **Why this milestone:** This is the functional core of TASKS.md Task 4 — the Resource accepts `word_count` (default 30) and the `read` Tool takes an optional `word_count` and returns the resource content; isolating the word-limiting logic in `server.py` lets us verify the exact-count behavior directly without spinning up an MCP client.
- **Files:** homework-5/custom-mcp-server/server.py
- **Depends on:** 2
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-5\custom-mcp-server
  try {
    python -c "import importlib.util as u; s=u.spec_from_file_location('server','server.py'); m=u.module_from_spec(s); s.loader.exec_module(m); d=m._read_words(); assert len(d.split())==30, 'default not 30: '+str(len(d.split())); c=m._read_words(7); assert len(c.split())==7, 'custom not 7: '+str(len(c.split())); print('word-count OK')"
    if ($LASTEXITCODE -ne 0) { throw 'read word-count behavior failed' }
  } finally { Pop-Location }
  ```
- **Done:** [x]

### Milestone 4: Tests — `read` tool word-count coverage (pytest)
- **Goal:** Add a pytest suite that calls the `read` tool over FastMCP's in-memory client and asserts exactly 30 words by default and exactly N words for a custom `word_count`.
- **Why this milestone:** Per the planning spec a dedicated Tests milestone is mandatory; adapted here to Python/FastMCP because Task 4 produces executable code. Driving the actual `read` Tool through FastMCP's in-memory transport (not just the helper) verifies the tool wiring end-to-end, catching decorator/parameter regressions the Milestone-3 helper check cannot.
- **Files:** homework-5/custom-mcp-server/test_server.py, homework-5/custom-mcp-server/requirements.txt
- **Depends on:** 2, 3
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-5\custom-mcp-server
  try {
    python -m pip install -r requirements.txt
    if ($LASTEXITCODE -ne 0) { throw 'pip install failed' }
    python -m pytest test_server.py -q
    if ($LASTEXITCODE -ne 0) { throw "pytest failed or collected zero tests (exit $LASTEXITCODE; pytest exit 5 = no tests)" }
  } finally { Pop-Location }
  ```
- **Done:** [x]

### Milestone 5: MCP interaction screenshots (four servers)
- **Goal:** Capture one MCP-call result screenshot for each configured server: GitHub, Filesystem, Jira-or-Notion, and the custom `read` tool.
- **Why this milestone:** TASKS.md Tasks 1–4 success criteria each require screenshot evidence of a working interaction, and the Expected Structure pins four specific filenames under `docs/screenshots/`. These interactions need live credentials and human-in-the-loop prompting, so this milestone is verified by screenshot presence plus confirming the custom server is registered in the config.
- **Files:** homework-5/docs/screenshots/github-mcp-result.png, homework-5/docs/screenshots/filesystem-mcp-result.png, homework-5/docs/screenshots/jira-or-notion-mcp-result.png, homework-5/docs/screenshots/custom-mcp-read-tool-result.png
- **Depends on:** 1, 3
- **Parallel:** sequential
- **Verify:**
  ```powershell
  $shots = 'github-mcp-result.png','filesystem-mcp-result.png','jira-or-notion-mcp-result.png','custom-mcp-read-tool-result.png'
  foreach ($s in $shots) { $p = "homework-5\docs\screenshots\$s"; if (-not (Test-Path $p)) { throw "missing screenshot: $s" } }
  $cfg = Get-Content homework-5\mcp.json -Raw | ConvertFrom-Json
  if (-not $cfg.mcpServers.custom) { throw 'custom server not registered in mcp.json' }
  Write-Host 'all four MCP result screenshots present'
  ```
- **Done:** [~]

### Milestone 6: Finalize documentation (README, HOWTORUN, demo, Resources-vs-Tools)
- **Goal:** Complete `README.md` (author name, no template placeholders), `HOWTORUN.md` (install deps, run server, connect MCP config, use/test `read`), the Resources-vs-Tools explanation, and a runnable demo script.
- **Why this milestone:** TASKS.md Deliverables require a proper README with author name, a complete HOWTORUN, and the docs explanation distinguishing Resources (URIs Claude reads) from Tools (actions Claude calls); this final bundle makes the homework reproducible and submission-ready. No other milestone depends on it.
- **Files:** homework-5/README.md, homework-5/HOWTORUN.md, homework-5/docs/resources-vs-tools.md, homework-5/demo/sample-read-requests.ps1
- **Depends on:** 1, 2, 3, 4, 5
- **Parallel:** sequential
- **Verify:**
  ```powershell
  $readme = Get-Content homework-5\README.md -Raw
  if ($readme -match '\[Your Name\]|YOUR_USERNAME|\[Date\]') { throw 'unfilled template variables in README' }
  foreach ($f in 'homework-5\HOWTORUN.md','homework-5\docs\resources-vs-tools.md','homework-5\demo\sample-read-requests.ps1') { if (-not (Test-Path $f)) { throw "missing $f" } }
  if (-not (Select-String -Path homework-5\docs\resources-vs-tools.md -Pattern 'Resource' -Quiet)) { throw 'Resources-vs-Tools explanation missing Resource section' }
  if (-not (Select-String -Path homework-5\docs\resources-vs-tools.md -Pattern 'Tool' -Quiet)) { throw 'Resources-vs-Tools explanation missing Tool section' }
  if (-not (Select-String -Path homework-5\HOWTORUN.md -Pattern 'fastmcp' -Quiet)) { throw 'HOWTORUN missing install instructions' }
  Write-Host 'documentation finalized'
  ```
- **Done:** [~]
