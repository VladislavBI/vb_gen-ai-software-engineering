# Banking Transaction Pipeline

**Author:** Vlad Bairak  
**Date:** 2026-07-08  
**Status:** Complete

## Overview

This project implements a **multi-agent transaction processing pipeline** that validates, scores for fraud risk, and performs compliance checks on banking transactions. The system processes transactions through three sequential processing agents (Validator, Fraud Detector, Compliance Checker), orchestrated by an Integrator, using a file-based JSON message bus and producing audit-trail results with complete decision records.

The pipeline demonstrates core principles of agentic AI: decomposition of complex business logic into specialized agents, asynchronous coordination via a shared message bus, and deterministic state progression. All transactions flow through validation → fraud detection → compliance checking, with intermediate and final results preserved at each stage for audit and debugging.

## Per-Agent Responsibilities

- **Transaction Validator** — Confirms required fields (transaction_id, amount, currency, timestamp), validates amounts using decimal arithmetic, checks ISO 4217 currency codes, and flags invalid transactions without blocking downstream processing.
- **Fraud Detector** — Scores transactions for fraud risk based on high amounts (≥$10K USD equivalent), off-hours timing (before 06:00 or after 22:00 UTC), cross-border transfers, and wire-transfer patterns. Produces a risk level (LOW/MEDIUM/HIGH/CRITICAL) and factor breakdown.
- **Compliance Checker** — Applies business rules: block-list checking, PII keyword detection in descriptions, regulatory holds for high-risk transactions, and validation error propagation. Issues a final APPROVED or HOLD_PENDING_REVIEW status.
- **Integrator** — Orchestrates the pipeline, loads sample transactions, creates the shared directory bus, and coordinates message flow through all three agents.

## System Architecture

```
┌─────────────────────────────────────────────────────────────────────┐
│  Banking Transaction Pipeline (Multi-Agent System)                  │
├─────────────────────────────────────────────────────────────────────┤
│                                                                       │
│  shared/input/ ──────┐                                              │
│  (raw transactions)  │                                              │
│                      ↓                                              │
│              ┌───────────────────┐                                  │
│              │    Integrator     │                                  │
│              │  (setup, queue)   │                                  │
│              └─────────┬─────────┘                                  │
│                        │                                            │
│              ↓ Deposit Messages                                     │
│              to shared/input/                                       │
│                        │                                            │
│       ┌────────────────┴────────────────┐                          │
│       │                                 │                          │
│       ↓                                 ↓                          │
│  ┌──────────────┐        Transaction Validator                    │
│  │ shared/      │        ──────────────────────                   │
│  │ input/       │        • Required fields                         │
│  │ (*.json)     │        • Amount validation (Decimal)            │
│  └──────────────┘        • ISO 4217 currency check                │
│                          • ISO 8601 timestamp                      │
│                          ↓ Write to shared/processing/            │
│                                                                     │
│  ┌──────────────┐        Fraud Detector                            │
│  │ shared/      │        ──────────────────                        │
│  │ processing/  │        • High-value threshold ($10K USD equiv)   │
│  │ (*.json)     │        • Off-hours detection (06:00–22:00 UTC)  │
│  └──────────────┘        • Cross-border flag (metadata.country)   │
│       ↑                  • Wire transfer risk                       │
│       └────────────────┐ • Risk score: LOW/MEDIUM/HIGH/CRITICAL   │
│                        │ ↓ Write to shared/output/                │
│                        │                                           │
│  ┌──────────────┐      │  Compliance Checker                       │
│  │ shared/      │      │  ──────────────────────                  │
│  │ output/      │      │  • Block-list check (ACC-9999, etc.)    │
│  │ (*.json)     │      │  • PII keyword detection (password, SSN) │
│  └──────────────┘      │  • Regulatory hold on HIGH/CRITICAL risk│
│       ↑                │  • Final status: APPROVED or             │
│       └────────────────┘    HOLD_PENDING_REVIEW                  │
│                        │   ↓ Write to shared/results/            │
│                        │                                           │
│  ┌──────────────┐      ↓                                           │
│  │ shared/      │ Final Audit Trail                               │
│  │ results/     │ ──────────────────                              │
│  │ (*.json)     │ • Complete transaction record                   │
│  │ Final        │ • All validation, fraud, compliance decisions  │
│  │ decisions    │ • ISO 8601 timestamps on all operations        │
│  └──────────────┘                                                  │
│                                                                     │
└─────────────────────────────────────────────────────────────────────┘

File-based message format (JSON envelope):
────────────────────────────────────────────
{
  "message_id": "UUID",
  "timestamp": "ISO 8601 UTC",
  "source_agent": "<agent_name>",
  "target_agent": "<next_agent>",
  "message_type": "transaction",
  "data": { <transaction + validation + fraud + compliance fields> }
}
```

## Tech Stack

| Component | Technology | Purpose |
|-----------|-----------|---------|
| **Language** | Python 3.12 | Core agent logic; builtin `decimal`, `json`, `uuid`, `datetime` modules |
| **Message Bus** | File-based JSON | Asynchronous coordination; shared/ directory with input, processing, output, results subdirs |
| **MCP Servers** | `mcp` package (FastMCP) + context7 | Tool-based integration; `get_transaction_status_tool`, `list_pipeline_results_tool`, context7 research queries |
| **Testing** | pytest + pytest-cov | Unit tests per agent; 1 integration test; coverage gate ≥80% (target ≥90%) |
| **Slash Commands** | 2 custom skills | `/run-pipeline` (end-to-end orchestration), `/validate-transactions` (single-agent validation) |
| **Coverage Enforcement** | Pre-push hook | `scripts/check_coverage.py --min 80` blocks commits below threshold |

## Getting Started

### Prerequisites
- Python 3.12 or later
- pip (Python package manager)
- PowerShell 5.1 (for running demo scripts and slash commands)

### Installation
1. Navigate to the `src/` directory
2. Install dependencies: `pip install -r requirements.txt`
3. Set up the pipeline: `python integrator.py --setup`
4. Run the pipeline: `python integrator.py`

### Output
After running the pipeline, check `src/shared/results/` for transaction results. Each file contains:
- Original transaction data
- Validation result (is_valid, errors)
- Fraud score (risk_level, score 0–100, factors)
- Compliance status (APPROVED or HOLD_PENDING_REVIEW with reasons)

## Slash Commands

**`/run-pipeline`** — Orchestrate end-to-end pipeline execution:
- Runs integrator setup and pipeline
- Parses results and displays a summary (count of approved vs. held transactions, fraud breakdown)

**`/validate-transactions`** — Validate transactions only (first agent, debugging):
- Runs only the Transaction Validator on queued messages
- Useful for testing the validation logic in isolation

## Verification and Testing

Run the test suite and coverage check:
```powershell
cd homework-6/src
python -m pytest --cov=. --cov-report=term-missing --cov-fail-under=80 -v
```

Coverage gate (blocks push if below 80%):
```powershell
cd homework-6
python scripts/check_coverage.py --min 80
```

## Documentation

- **`HOWTORUN.md`** — Step-by-step instructions for running the system
- **`specification.md`** — Technical specification (domain rules, agent tasks, validation rules)
- **`agents.md`** — Agent behavioral guidelines (PII constraints, currency rules, edge cases)
- **`research-notes.md`** — MCP server documentation (context7 queries)
- **`demo/`** — Runnable demo scripts and example requests
- **`docs/screenshots/`** — Screenshots of pipeline runs, test coverage, skill execution, and MCP interaction

## Files and Structure

```
homework-6/
├── README.md                          (this file)
├── HOWTORUN.md                        (step-by-step runbook)
├── specification.md                   (domain spec and task details)
├── agents.md                          (agent guidelines and edge cases)
├── sample-transactions.json           (input: 8 sample transactions)
├── research-notes.md                  (MCP context7 documentation)
├── mcp.json                           (MCP server configuration)
├── PLAN.md                            (homework plan and milestones)
├── .claude/
│   ├── commands/
│   │   ├── write-spec.md             (skill: regenerate specification)
│   │   ├── run-pipeline.md           (skill: end-to-end orchestration)
│   │   └── validate-transactions.md  (skill: single-agent validation)
│   └── settings.json                 (coverage gate hook)
├── scripts/
│   └── check_coverage.py             (coverage enforcement)
├── src/
│   ├── requirements.txt              (Python dependencies)
│   ├── integrator.py                 (orchestrator and setup)
│   ├── agents/
│   │   ├── __init__.py
│   │   ├── transaction_validator.py  (Agent 1)
│   │   ├── fraud_detector.py         (Agent 2)
│   │   └── compliance_checker.py     (Agent 3)
│   ├── pipeline/
│   │   ├── __init__.py
│   │   └── messaging.py              (JSON message helpers)
│   ├── pipeline_mcp/
│   │   ├── __init__.py
│   │   └── server.py                 (FastMCP server)
│   ├── tests/
│   │   ├── __init__.py
│   │   ├── test_transaction_validator.py
│   │   ├── test_fraud_detector.py
│   │   ├── test_compliance_checker.py
│   │   └── test_pipeline_integration.py
│   └── shared/                       (message bus, created at runtime)
│       ├── input/
│       ├── processing/
│       ├── output/
│       └── results/
└── demo/
    ├── run-demo.ps1                 (runnable demo script)
    └── sample-requests.md           (example MCP calls and payloads)
```

## Summary

This pipeline exemplifies agentic decomposition: each agent has a single responsibility, communication is asynchronous and stateless (via JSON files), and results are audited at every stage. The system is extensible — new agents can be added to the chain, and the file-based bus is language-agnostic.

For detailed implementation, testing, and verification instructions, see **`HOWTORUN.md`**.
