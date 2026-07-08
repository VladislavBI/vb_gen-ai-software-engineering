# How to Run the Banking Transaction Pipeline

This document provides step-by-step instructions for running the multi-agent transaction processing pipeline on your local machine.

## Prerequisites

Before starting, ensure you have:
- **Python 3.12** or later installed (check with `python --version`)
- **pip** (Python package manager) installed
- **PowerShell 5.1** available (for running demo scripts and slash commands on Windows; bash/sh on Linux/macOS)
- At least **10 MB free disk space** for the pipeline's shared/ directory
- **Git** and the repository cloned locally

## Step 1: Navigate to the Source Directory

Open a terminal (PowerShell on Windows) and change to the homework-6 source directory:

```powershell
cd homework-6\src
```

All subsequent commands assume you are in this directory.

## Step 2: Install Python Dependencies

Install the required Python packages from `requirements.txt`:

```powershell
pip install -r requirements.txt
```

This installs:
- `mcp` — Multi-transport protocol for MCP servers (includes `mcp.server.fastmcp` submodule)
- `pytest` — Testing framework
- `pytest-cov` — Code coverage plugin for pytest

## Step 3: Set Up the Pipeline (Create Shared Directories and Queue Transactions)

Initialize the pipeline by creating the message bus directories and loading sample transactions:

```powershell
python integrator.py --setup
```

**Expected output:**
```
Starting setup mode...
Created/verified directory: shared\input
Created/verified directory: shared\processing
Created/verified directory: shared\output
Created/verified directory: shared\results
Loaded 8 transactions from <path-to-homework>/homework-6/sample-transactions.json
Queued transaction TXN001 (message_id=550e8400-e29b-41d4-a716-446655440000)
Queued transaction TXN002 (message_id=660f9511-f39c-52e5-b837-5f7766551111)
Queued transaction TXN003 (message_id=770g0622-g40d-63f6-c948-6g8877662222)
...
Successfully queued 8 messages to shared/input/
Setup complete.
```

Note: The full path to sample-transactions.json is printed (not relative); backslashes indicate Windows path format.

**What happens:**
- Creates four directories under `shared/`: `input/`, `processing/`, `output/`, `results/`
- Reads `sample-transactions.json` (8 sample transactions of varying amounts, currencies, and risk profiles)
- Wraps each transaction in a JSON message envelope (UUID + ISO 8601 timestamp)
- Deposits one JSON file per transaction into `shared/input/`
- Verifies all 8 transactions are queued

## Step 4: Run the Full Pipeline

Execute the pipeline to process all queued transactions through all three agents:

```powershell
python integrator.py
```

**Expected output:**
```
Starting pipeline mode...
Found 8 input messages to process
Processing message 550e8400-e29b-41d4-a716-446655440000...
  [OK] Message 550e8400-e29b-41d4-a716-446655440000 processed successfully
Processing message 660f9511-f39c-52e5-b837-5f7766551111...
  [OK] Message 660f9511-f39c-52e5-b837-5f7766551111 processed successfully
...
Pipeline complete: 8 processed, 0 failed
Results written to shared/results/: 8 files
Pipeline complete.
```

**What happens:**
1. Reads all `.json` files from `shared/input/`
2. **Validator agent** processes each message:
   - Checks required fields (transaction_id, amount, currency, timestamp)
   - Validates amount (positive for normal transactions, negative OK for refunds)
   - Confirms currency is ISO 4217 (USD, EUR, GBP, JPY, CAD, AUD, CHF, etc.)
   - Writes enriched message to `shared/processing/`
3. **Fraud Detector agent** processes each message from `shared/processing/`:
   - Scores based on: high amount (≥$10K USD equivalent), off-hours (before 06:00 or after 22:00 UTC), cross-border, wire transfer
   - Assigns risk level: LOW (0–30), MEDIUM (31–60), HIGH (61–85), CRITICAL (86–100)
   - Writes enriched message to `shared/output/`
4. **Compliance Checker agent** processes each message from `shared/output/`:
   - Checks block list (ACC-9999, ACC-BLOCKED)
   - Detects PII keywords (password, SSN, credit card, PIN, CVV)
   - Applies regulatory hold for HIGH or CRITICAL fraud risk
   - Writes final decision to `shared/results/`

## Step 5: Verify Results

Check the `shared/results/` directory to confirm all transactions were processed:

```powershell
Get-ChildItem shared\results -Filter *.json | Measure-Object
```

**Expected output:**
```
Count : 8
```

Inspect a sample result:

```powershell
$file = Get-ChildItem shared\results -Filter *.json | Select-Object -First 1
Get-Content $file.FullName | ConvertFrom-Json | ConvertTo-Json -Depth 10
```

**Expected structure in each result file:**
- `data.transaction_id` — Original transaction ID (TXN001, TXN002, etc.)
- `data.validation_result` — Validation outcome (is_valid: true/false, errors: list)
- `data.fraud_score` — Fraud assessment (risk_level: LOW/MEDIUM/HIGH/CRITICAL, score: 0–100, factors: dict)
- `data.compliance_status` — Compliance decision (status: APPROVED/HOLD_PENDING_REVIEW, hold_flag: true/false, reasons: list)

## Step 6: Run the Test Suite

Execute the unit and integration tests with coverage measurement:

```powershell
python -m pytest --cov=. --cov-report=term-missing --cov-fail-under=80 -v
```

**Expected output:**
```
test_transaction_validator.py::test_valid_transaction PASSED
test_transaction_validator.py::test_invalid_currency PASSED
...
test_fraud_detector.py::test_high_amount PASSED
test_fraud_detector.py::test_off_hours PASSED
...
test_compliance_checker.py::test_block_list_hit PASSED
...
test_pipeline_integration.py::test_full_pipeline PASSED

---------- coverage: ... ----------
agents\fraud_detector.py         87% coverage
agents\transaction_validator.py  92% coverage
agents\compliance_checker.py      89% coverage
...
TOTAL                            88% coverage (required: 80%)
```

**What to verify:**
- All tests pass (PASSED)
- Coverage is at or above 80% (gate minimum)
- Aim for 90% or higher

## Step 7: Verify Coverage Gate

Test the pre-push hook that enforces the coverage gate:

```powershell
cd ..
python scripts\check_coverage.py --min 80
```

**Expected output (if coverage ≥ 80%):**
```
Running: python -m pytest --cov=. --cov-fail-under=80 -q
<pytest test results and coverage summary>
Coverage gate passed: coverage >= 80%
```

Exit code: 0 (success; push allowed)

**Expected output (if coverage < 80%):**
```
Running: python -m pytest --cov=. --cov-fail-under=80 -q
<pytest test results showing coverage below threshold>
Coverage gate failed: coverage < 80% or tests failed
```

Exit code: 1 (non-zero, blocks push)

## Step 8: Run Slash Commands (Optional)

From the homework-6 root directory, invoke the custom slash commands:

### 8a. Run the Full Pipeline via `/run-pipeline` Skill

```powershell
# From homework-6/ root directory
```

Invoke the skill (if configured in Claude Code):
```
/run-pipeline
```

**What it does:**
- Orchestrates `integrator.py --setup` and `integrator.py` (run)
- Parses results and displays a summary:
  - Total transactions processed
  - Count of APPROVED vs. HOLD_PENDING_REVIEW
  - Breakdown by fraud risk level
  - Sample transaction details

### 8b. Validate Transactions Only via `/validate-transactions` Skill

```powershell
# From homework-6/ root directory
```

Invoke the skill (if configured in Claude Code):
```
/validate-transactions
```

**What it does:**
- Runs only the Transaction Validator agent
- Useful for debugging validation logic in isolation
- Outputs pass/fail counts and any validation errors

## Step 9: Run the Demo Script (Optional)

Execute the demo PowerShell script to showcase the pipeline:

```powershell
cd homework-6
.\demo\run-demo.ps1
```

**What it does:**
- Runs pipeline setup
- Executes the full pipeline
- Parses and displays results in a formatted summary
- Shows examples of transaction flow and decision outcomes

## Cleanup (Optional)

To reset the pipeline and start fresh:

```powershell
cd homework-6\src
Remove-Item shared -Recurse -Force
```

Then return to Step 3 to re-run setup.

## Troubleshooting

### Issue: `ModuleNotFoundError: No module named 'mcp'` or `No module named 'mcp.server.fastmcp'`

**Solution:** Re-run Step 2 (install dependencies).

```powershell
pip install -r requirements.txt
```

### Issue: `FileNotFoundError: shared/input/`

**Solution:** Run Step 3 setup before running the pipeline.

```powershell
python integrator.py --setup
```

### Issue: `Coverage below 80%`

**Solution:** Review the coverage report and add tests for uncovered lines:

```powershell
python -m pytest --cov=. --cov-report=html
# Open htmlcov/index.html in a browser to visualize coverage
```

### Issue: Tests fail with `json.JSONDecodeError`

**Solution:** Verify that result files in `shared/results/` are valid JSON:

```powershell
Get-ChildItem shared\results -Filter *.json | ForEach-Object {
    try { Get-Content $_.FullName | ConvertFrom-Json | Out-Null }
    catch { Write-Host "Invalid JSON: $($_.Name)" }
}
```

### Issue: `python: command not found` or `pip: command not found`

**Solution:** Ensure Python 3.12 is in your PATH. On Windows:

```powershell
python --version
```

If not recognized, install Python 3.12 from [python.org](https://www.python.org/downloads/) and ensure you check "Add Python to PATH" during installation.

## Summary

The banking transaction pipeline is now running! You have:

1. ✓ Set up the message bus (`shared/` directories)
2. ✓ Queued 8 sample transactions
3. ✓ Processed them through 3 agents (Validator → Fraud Detector → Compliance Checker)
4. ✓ Verified results in `shared/results/`
5. ✓ Confirmed test suite passes and coverage ≥ 80%
6. ✓ Tested the coverage gate hook

For more details on the system architecture, see **`README.md`**.  
For MCP server and context7 queries, see **`research-notes.md`**.  
For agent responsibilities and edge cases, see **`specification.md`** and **`agents.md`**.

Happy testing!
