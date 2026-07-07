# Homework 6 — Plan

**TASKS.md commit:** 1a8386c
**Created:** 2026-07-06
**Stack:** Python 3.12 (multi-agent pipeline + FastMCP; file-based JSON message passing). *Language-agnostic per TASKS.md — Python chosen because Task 4 hardcodes a FastMCP `mcp/server.py`, context7 tooling, and a file/JSON message bus, all of which Python fits cleanly. Not the repo's usual .NET default; the student may re-stack later.*

## Overview

Final capstone (TASKS.md Tasks 1–5): build **four meta-agents** whose output is a working multi-agent banking transaction pipeline. Deliverables are both the meta-agents and the resulting system: a Specification agent producing `specification.md` + `agents.md` + a `/write-spec` skill (Task 1); a code-generation agent producing an integrator plus at least 3 cooperating agents (Transaction Validator, Fraud Detector, Compliance Checker) that pass JSON messages through `shared/input|processing|output|results/` (Task 2); two slash commands and a coverage-gate push hook (Task 3); dual MCP integration — context7 documented in `research-notes.md` plus a custom FastMCP `mcp/server.py` wired together in `mcp.json` (Task 4); and a pytest suite (unit per agent + 1 integration, isolated from real `shared/`) with README/HOWTORUN/demo/screenshots (Task 5). Coverage gate is 80% (aim ≥90%). Code root is `homework-6/src/`; slash commands live at `homework-6/.claude/commands/`.

## Milestones

### Milestone 1: Agent 1 — Specification, agents.md, and /write-spec skill
- **Goal:** Produce `specification.md` (all 5 required sections with per-agent Low-Level Tasks), an extended `agents.md`, and a `/write-spec` slash command that regenerates a spec from the template.
- **Why this milestone:** TASKS.md Task 1 is "spec first, code second" and every later milestone's scope derives from it; the skill is a graded Agent-1 deliverable. Isolating it means the pipeline is built against an agreed contract rather than ad hoc.
- **Files:** homework-6/specification.md, homework-6/agents.md, homework-6/.claude/commands/write-spec.md
- **Depends on:** none
- **Parallel:** sequential
- **Verify:**
  ```powershell
  $spec = Get-Content homework-6/specification.md -Raw
  foreach ($h in 'High-Level Objective','Mid-Level Objectives','Implementation Notes','Context','Low-Level Tasks') { if ($spec -notmatch [regex]::Escape($h)) { throw "spec missing: $h" } }
  foreach ($a in 'Validator','Fraud','Compliance') { if ($spec -notmatch $a) { throw "Low-Level Tasks missing agent: $a" } }
  if (-not (Test-Path homework-6/agents.md)) { throw 'missing agents.md' }
  $cmd = Get-Content homework-6/.claude/commands/write-spec.md -Raw
  if ($cmd -notmatch 'specification') { throw 'write-spec skill does not reference the spec template' }
  ```
- **Done:** [x]

### Milestone 2: Agent 2a — Pipeline scaffold, message bus, and integrator setup
- **Goal:** Stand up the `src/` project (deps, package layout), the `shared/` directory bus, a JSON-message helper, and an integrator that on `--setup` creates the four `shared/` dirs and loads `sample-transactions.json` into `shared/input/`.
- **Why this milestone:** Task 2's file-based protocol and integrator are the substrate every agent depends on; verifying dir setup + message loading independently avoids debugging agents and plumbing at once.
- **Files:** homework-6/src/requirements.txt, homework-6/src/integrator.py, homework-6/src/pipeline/messaging.py, homework-6/src/pipeline/__init__.py
- **Depends on:** 1
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-6/src
  try {
    pip install -q -r requirements.txt
    python integrator.py --setup
    foreach ($d in 'input','processing','output','results') { if (-not (Test-Path "shared/$d")) { throw "missing shared/$d" } }
    $msgs = Get-ChildItem shared/input -Filter *.json
    if ($msgs.Count -lt 8) { throw "expected 8 input messages, got $($msgs.Count)" }
  } finally { Pop-Location }
  ```
- **Done:** [ ]

### Milestone 3: Agent 2b — Three cooperating agents run end-to-end
- **Goal:** Implement Transaction Validator, Fraud Detector, and Compliance Checker as `process_message`-style agents chained by the integrator so a full `python integrator.py` run writes one result per transaction to `shared/results/`.
- **Why this milestone:** This is the core of Task 2 (≥3 cooperating agents, valid JSON hand-off, all sample transactions reaching `results/`). It is the largest functional deliverable and the thing the deliverable-check command in TASKS.md exercises.
- **Files:** homework-6/src/agents/transaction_validator.py, homework-6/src/agents/fraud_detector.py, homework-6/src/agents/compliance_checker.py
- **Depends on:** 2
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-6/src
  try {
    python integrator.py
    $results = Get-ChildItem shared/results -Filter *.json
    if ($results.Count -lt 8) { throw "expected 8 results, got $($results.Count)" }
    $bad = $results | Where-Object { try { Get-Content $_.FullName -Raw | ConvertFrom-Json | Out-Null; $false } catch { $true } }
    if ($bad) { throw "invalid JSON in results: $($bad.Name -join ', ')" }
    $txt = ($results | ForEach-Object { Get-Content $_.FullName -Raw }) -join ' '
    if ($txt -notmatch 'XYZ' -or $txt -notmatch 'risk') { throw 'results missing fraud/compliance decision fields' }
  } finally { Pop-Location }
  ```
- **Done:** [ ]

### Milestone 4: Task 4 — Dual MCP: context7 research-notes + custom FastMCP server
- **Goal:** Configure both servers in `mcp.json`, document ≥2 context7 queries in `research-notes.md`, and build `src/mcp/server.py` exposing tools `get_transaction_status` + `list_pipeline_results` and resource `pipeline://summary`.
- **Why this milestone:** Task 4 requires both MCP servers wired together and the custom server must read real `shared/results/`, so it depends on the pipeline producing results. Documenting context7 queries is a graded Agent-2 requirement.
- **Files:** homework-6/mcp.json, homework-6/research-notes.md, homework-6/src/mcp/server.py, homework-6/src/mcp/__init__.py
- **Depends on:** 3
- **Parallel:** sequential
- **Verify:**
  ```powershell
  $m = Get-Content homework-6/mcp.json -Raw | ConvertFrom-Json
  if (-not $m.mcpServers.context7) { throw 'mcp.json missing context7' }
  if (-not ($m.mcpServers.PSObject.Properties.Name -contains 'pipeline-status')) { throw 'mcp.json missing pipeline-status' }
  $rn = Get-Content homework-6/research-notes.md -Raw
  if (([regex]::Matches($rn, '(?im)^##\s*Query')).Count -lt 2) { throw 'research-notes needs >=2 context7 queries' }
  Push-Location homework-6/src
  try {
    $out = python -c "from mcp.server import get_transaction_status, list_pipeline_results; print(len(list_pipeline_results())); print(get_transaction_status('TXN001'))"
    if ($LASTEXITCODE -ne 0) { throw 'FastMCP tool functions failed to execute' }
    if ($out -notmatch 'TXN001') { throw 'get_transaction_status did not return TXN001 status' }
  } finally { Pop-Location }
  ```
- **Done:** [ ]

### Milestone 5: Tests — per-agent units + full-pipeline integration with coverage gate
- **Goal:** Build a pytest suite in `src/tests/` with a unit test per agent plus one integration test of the full pipeline, isolated from real `shared/` via `tmp_path`, and reach ≥80% coverage (aim ≥90%).
- **Why this milestone:** TASKS.md Task 5 mandates unit-per-agent + 1 integration test and feeds the 80% coverage gate in milestone 6; the planning-process spec requires a dedicated Tests milestone asserting tests actually ran.
- **Files:** homework-6/src/tests/test_transaction_validator.py, homework-6/src/tests/test_fraud_detector.py, homework-6/src/tests/test_compliance_checker.py, homework-6/src/tests/test_pipeline_integration.py
- **Depends on:** 3, 4
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-6/src
  try {
    python -m pytest --cov=. --cov-report=term-missing --cov-fail-under=80 -q
    if ($LASTEXITCODE -ne 0) { throw "pytest failed or coverage < 80% (exit $LASTEXITCODE; exit 5 = no tests collected)" }
  } finally { Pop-Location }
  ```
- **Done:** [ ]

### Milestone 6: Agent 3 — /run-pipeline, /validate-transactions skills + coverage-gate push hook
- **Goal:** Add the two slash commands and a coverage-gate: `scripts/check_coverage.py --min <n>` that exits non-zero below threshold, wired as a push-blocking hook in `.claude/settings.json`.
- **Why this milestone:** Task 3 requires both skills and a hook that blocks push when coverage < 80%; the gate must run against the milestone-5 suite, so it comes after Tests. The gate's block/pass behavior is verifiable headlessly even though the screenshot is captured manually.
- **Files:** homework-6/.claude/commands/run-pipeline.md, homework-6/.claude/commands/validate-transactions.md, homework-6/scripts/check_coverage.py, homework-6/.claude/settings.json
- **Depends on:** 5
- **Parallel:** sequential
- **Verify:**
  ```powershell
  foreach ($c in 'run-pipeline','validate-transactions') { if (-not (Test-Path "homework-6/.claude/commands/$c.md")) { throw "missing skill: $c" } }
  $s = Get-Content homework-6/.claude/settings.json -Raw
  if ($s -notmatch 'check_coverage') { throw 'settings.json hook not wired to coverage gate' }
  Push-Location homework-6/src
  try {
    python ../scripts/check_coverage.py --min 999
    if ($LASTEXITCODE -eq 0) { throw 'gate must block (non-zero) when coverage below threshold' }
    python ../scripts/check_coverage.py --min 80
    if ($LASTEXITCODE -ne 0) { throw 'gate must pass (zero) at 80% threshold' }
  } finally { Pop-Location }
  ```
  *Manual follow-up: capture `docs/screenshots/skill-run-pipeline.png` and `hook-trigger.png` by running `/run-pipeline` and triggering the push hook interactively.*
- **Done:** [ ]

### Milestone 7: Agent 4 — Finalize documentation, demo, and screenshots
- **Goal:** Write `README.md` (author "Vlad Bairak", 1–2 paragraph overview, per-agent bullets, ASCII pipeline diagram, tech-stack table), `HOWTORUN.md` numbered steps, populate `demo/` with runnable scripts/sample requests, and collect the 5 required screenshots.
- **Why this milestone:** TASKS.md Task 5 + root CLAUDE.md require a complete doc set with the student's name and diagram before submission; it depends on every prior milestone so it documents the finished system. Screenshot capture is a manual follow-up (see note).
- **Files:** homework-6/README.md, homework-6/HOWTORUN.md, homework-6/demo/run-demo.ps1, homework-6/demo/sample-requests.md
- **Depends on:** 1, 2, 3, 4, 5, 6
- **Parallel:** sequential
- **Verify:**
  ```powershell
  $r = Get-Content homework-6/README.md -Raw
  if ($r -notmatch 'Vlad Bairak') { throw 'README missing author name' }
  if ($r -notmatch 'shared' -or $r -notmatch '─|->|\|') { throw 'README missing ASCII architecture diagram' }
  if ($r -notmatch '(?im)tech stack') { throw 'README missing tech stack table' }
  if (-not (Test-Path homework-6/HOWTORUN.md)) { throw 'missing HOWTORUN.md' }
  if (-not (Get-ChildItem homework-6/demo -ErrorAction SilentlyContinue)) { throw 'demo/ is empty' }
  foreach ($s in 'pipeline-run','test-coverage','skill-run-pipeline','hook-trigger','mcp-interaction') { if (-not (Test-Path "homework-6/docs/screenshots/$s.png")) { throw "missing screenshot: $s.png" } }
  ```
  *Manual follow-up: `pipeline-run.png`, `test-coverage.png`, `skill-run-pipeline.png`, `hook-trigger.png`, and `mcp-interaction.png` must be captured interactively and embedded in the PR description.*
- **Done:** [ ]
