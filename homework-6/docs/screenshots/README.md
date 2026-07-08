# Screenshots Documentation

This directory contains screenshots demonstrating the banking transaction pipeline system and its AI-driven integration.

## Required Screenshots (5 total)

Each screenshot must be captured manually by running the pipeline and associated commands in sequence. All PNG files must be saved in this directory (`homework-6/docs/screenshots/`).

### 1. `pipeline-run.png`

**Purpose:** Demonstrate a complete pipeline run from setup through results.

**How to capture:**
1. Open a PowerShell terminal
2. Navigate to `homework-6\src\`
3. Run: `python integrator.py --setup`
4. Run: `python integrator.py`
5. Take a screenshot showing:
   - The terminal output displaying all transaction processing steps
   - Evidence of successful setup (directory creation messages)
   - Evidence of pipeline execution (transaction processing logs)
   - Sample output lines showing transaction IDs and agent processing

**Expected content in screenshot:**
- Timestamps of pipeline execution
- Transaction IDs (TXN001, TXN002, etc.) being processed
- Agent names (transaction_validator, fraud_detector, compliance_checker)
- Outcome indicators (PASS, LOW/MEDIUM/HIGH/CRITICAL, APPROVED/HOLD_PENDING_REVIEW)

**File name:** `pipeline-run.png`

---

### 2. `test-coverage.png`

**Purpose:** Show the test suite running with coverage measurement (≥80%, aim ≥90%).

**How to capture:**
1. Open a PowerShell terminal
2. Navigate to `homework-6\src\`
3. Run: `python -m pytest --cov=. --cov-report=term-missing --cov-fail-under=80 -v`
4. Take a screenshot showing:
   - Individual test results (PASSED status for all tests)
   - Module names under coverage (fraud_detector, transaction_validator, compliance_checker)
   - Line-by-line coverage summary (% coverage per file)
   - Total coverage percentage (≥80%)
   - Exit code 0 (success)

**Expected content in screenshot:**
- Test names and PASSED indicators
- Coverage percentages (89%, 92%, 87%, etc.)
- Lines covered vs. lines total
- Terminal exit message indicating success

**File name:** `test-coverage.png`

---

### 3. `skill-run-pipeline.png`

**Purpose:** Demonstrate the `/run-pipeline` slash command executing end-to-end.

**How to capture:**
1. Open Claude Code in your IDE/editor where the homework-6 directory is configured
2. Ensure `.claude/commands/run-pipeline.md` is configured as a skill (it should be auto-registered)
3. Invoke the skill by typing: `/run-pipeline`
4. Wait for execution to complete
5. Take a screenshot showing:
   - The skill invocation text (visible in a chat or command interface)
   - The skill's output summarizing:
     - Total transactions processed
     - Count of APPROVED vs. HOLD_PENDING_REVIEW
     - Breakdown by fraud risk level (LOW, MEDIUM, HIGH, CRITICAL)
     - Sample transaction details with fraud and compliance outcomes

**Expected content in screenshot:**
- Slash command syntax and name
- Pipeline orchestration output (setup + run)
- Summary statistics
- Evidence of command completion

**File name:** `skill-run-pipeline.png`

---

### 4. `hook-trigger.png`

**Purpose:** Demonstrate the coverage-gate pre-push hook blocking a commit when coverage is insufficient.

**How to capture:**
1. This demonstrates the **coverage enforcement mechanism** from `.claude/settings.json` and `scripts/check_coverage.py`
2. Approach 1 (manual hook trigger):
   - Run: `cd homework-6\src`
   - Run: `pytest --cov=. --cov-fail-under=79 -q` (intentionally set threshold below actual coverage to test gate)
   - Run: `cd homework-6`
   - Run: `python scripts\check_coverage.py --min 999` (set impossibly high threshold)
   - Take screenshot of output showing gate **FAILURE** (exit code 1)
3. Approach 2 (integration with git push, if available):
   - Attempt `git push` after modifying a file to drop coverage artificially
   - The pre-push hook should trigger and block the push
   - Screenshot the hook output

4. Take a screenshot showing:
   - Command name and arguments (check_coverage.py --min <threshold>)
   - Coverage percentage (e.g., "88%")
   - Threshold comparison (e.g., "88% < 999%")
   - Exit code 1 (non-zero, indicating failure/block)
   - Error message explaining why gate blocked the operation

**Expected content in screenshot:**
- Hook or script name visible
- Coverage value and threshold comparison
- Failure/block indication
- Error message or gate-blocked status

**File name:** `hook-trigger.png`

---

### 5. `mcp-interaction.png`

**Purpose:** Demonstrate interaction with the custom FastMCP server and MCP tools.

**How to capture:**
1. Ensure the FastMCP server in `src/pipeline_mcp/server.py` is configured in `mcp.json`
2. In Claude Code or an MCP client, invoke the custom MCP tools:
   - Tool: `get_transaction_status_tool` with argument `transaction_id: "TXN001"`
   - Or tool: `list_pipeline_results_tool` (no arguments)
   - Or resource: `pipeline://summary`
3. Take a screenshot showing:
   - MCP tool/resource being called (method name, arguments visible)
   - Response from the server including:
     - Transaction details (ID, status, fraud score, compliance decision)
     - Multiple transaction entries (proof of list operation)
     - Or pipeline summary statistics
   - Timestamps and ISO 8601 format where present

**Expected content in screenshot:**
- MCP tool name or resource URI
- Request parameters (transaction_id, etc.)
- Response body with transaction/summary data
- Evidence of successful server response (no errors)

**File name:** `mcp-interaction.png`

---

## How to Submit Screenshots

1. **Capture each screenshot** following the instructions above
2. **Save as PNG files** in this directory:
   - `homework-6/docs/screenshots/pipeline-run.png`
   - `homework-6/docs/screenshots/test-coverage.png`
   - `homework-6/docs/screenshots/skill-run-pipeline.png`
   - `homework-6/docs/screenshots/hook-trigger.png`
   - `homework-6/docs/screenshots/mcp-interaction.png`
3. **Verify all 5 files exist** in the directory before submitting
4. **Include evidence in the PR description** — link to or embed key screenshots in the pull request body (see `.claude/docs/Infrastructure/pull-request-process.md`)

## Troubleshooting

### Screenshot Capture Tools

- **Windows (built-in):** `Win+Shift+S` (Snip & Sketch), then Save As PNG
- **Windows (PowerShell):** Use `Greenshot` (lightweight, recommended for terminal output)
- **VS Code / IDE:** Use built-in screenshot or third-party extension (e.g., CodeSnap)
- **macOS:** `Cmd+Shift+4` (region select) or `Cmd+Shift+3` (full screen)
- **Linux:** `gnome-screenshot`, `Flameshot`, or `Spectacle` (KDE)

### Quality Guidelines

- **Clarity:** Ensure text is readable (minimum 12pt font)
- **Content:** Include full command and output (not just partial results)
- **Framing:** Crop to show only the relevant terminal/IDE window (not entire desktop)
- **Format:** Save as PNG (no JPEG compression artifacts)

### If Screenshots Fail to Capture

If any of the above steps fail to produce output, document the issue:

1. Run the command and capture the error message
2. Note the exact step where it failed
3. Include this in the PR description's "Screenshots" section with a note like:
   ```
   [hook-trigger.png] — Coverage gate test manually verified via:
   `python scripts/check_coverage.py --min 999` returned exit code 1
   ```

---

## Notes

- All screenshots are graded as **Evidence of AI usage and system functionality** (10% of final grade)
- Each screenshot must clearly show **what** is being demonstrated (command, output, result)
- Timestamps and unique identifiers (transaction IDs, message IDs) help prove authenticity
- Do not edit screenshots after capture (no cropping, annotation, or content modification beyond framing)

For detailed instructions on running the pipeline, tests, and MCP server, see:
- **`HOWTORUN.md`** — Step-by-step runbook
- **`research-notes.md`** — MCP server and context7 query documentation
- **`README.md`** — System overview and architecture
