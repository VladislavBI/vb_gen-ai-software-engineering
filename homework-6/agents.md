# Agents Behavioral Guidelines

**Project:** Homework 6 — Banking Transaction Pipeline
**Last updated:** 2026-07-07
**Stack:** Python 3.12, FastMCP, file-based JSON message bus

---

## Tech Stack Assumptions

All AI agents working on this project must assume:

1. **Language and runtime**: Python 3.12 (not 3.10 or earlier).
2. **Concurrency model**: Single-threaded per agent; coordination via file-based message bus (no threads or async within agent logic unless explicitly stated).
3. **Messaging protocol**: JSON files exchanged through `shared/input/`, `shared/processing/`, `shared/output/`, `shared/results/` directories. One message = one file named `{message_id}.json`.
4. **Message envelope**: All messages follow the standard format with `message_id`, `timestamp`, `source_agent`, `target_agent`, `message_type`, and nested `data` object.
5. **Dependencies**: 
   - `decimal` (builtin) for monetary arithmetic.
   - `json` (builtin) for serialization.
   - `uuid` (builtin) for message IDs.
   - `datetime` (builtin) for ISO 8601 timestamps.
   - `pytest` + `pytest-cov` for testing.
   - `fastmcp` for MCP server (if implementing Task 4).

---

## Domain Rules: Transaction States and Agent Responsibilities

### Transaction State Machine

Each transaction flows through a four-stage pipeline:

1. **SUBMITTED** → Initial state, raw data from `sample-transactions.json`.
2. **VALIDATED** → After Validator confirms fields, amounts, currency. Marked valid or rejected.
3. **FRAUD_SCORED** → After Fraud Detector assigns risk level (LOW/MEDIUM/HIGH/CRITICAL).
4. **COMPLIANCE_CHECKED** → After Compliance Checker applies holds/blocks and determines final status (APPROVED or HOLD_PENDING_REVIEW; REJECTED is reserved, not currently produced).

### Agent Responsibilities

| Agent | Input State | Output State | Checks | Output File |
|-------|---|---|---|---|
| **Transaction Validator** | SUBMITTED | VALIDATED | Required fields, amount > 0 (or < 0 for refunds), ISO 4217 currency, ISO 8601 timestamp | data.validation_result |
| **Fraud Detector** | VALIDATED | FRAUD_SCORED | Amount ≥$10K equivalent, off-hours, cross-border, wire transfer type | data.fraud_score |
| **Compliance Checker** | FRAUD_SCORED | COMPLIANCE_CHECKED | Validation result, block list, PII keywords, hold rules based on fraud risk | data.compliance_status (APPROVED or HOLD_PENDING_REVIEW only), write to shared/results/ |

---

## Security Constraints

### PII and Account Number Handling

- **Never log full account numbers**: Use format `ACC-1*** **01` (first 6 chars + last 2 only).
- **Never log customer names** from transaction descriptions or metadata.
- **Never log passwords, PINs, or other credentials** if present in description fields.
- **Audit trail only**: Log action, timestamp, transaction_id, and outcome (pass/fail); never the raw sensitive data.
- **Exception: MCP server**: When implementing `get_transaction_status()`, the response may include limited account info (e.g., `ACC-1*** **01` format) if necessary for debugging.

### Sensitive Patterns in Descriptions

Compliance Checker flags these keywords (case-insensitive) in description or metadata as PII risk:
- `password`, `ssn`, `credit card`, `pin`, `cvv`, `security code`, `atm code`

---

## Code Conventions

### Monetary Arithmetic

```python
from decimal import Decimal, ROUND_HALF_UP

# Always use Decimal for amounts
amount = Decimal("1500.00")

# Never use float for currency
# ❌ WRONG: amount = 1500.00
# ✅ RIGHT: amount = Decimal("1500.00")

# Rounding: always use ROUND_HALF_UP
rounded = amount.quantize(Decimal("0.01"), rounding=ROUND_HALF_UP)
```

### ISO 4217 Currencies

Supported currencies (case-sensitive, uppercase):
- USD, EUR, GBP, JPY, CAD, AUD, CHF, CNY, INR, MXN, BRL, ZAR

Validation: if currency not in above list, mark transaction as INVALID.

### ISO 8601 Timestamps

All timestamps must be in UTC, format: `YYYY-MM-DDTHH:MM:SSZ` (no other timezone).

```python
from datetime import datetime, timezone

# Generate timestamp
now = datetime.now(timezone.utc).isoformat(timespec='seconds').replace('+00:00', 'Z')
# Result: "2026-03-16T10:00:00Z"

# Parse timestamp
parsed = datetime.fromisoformat(timestamp_str.replace('Z', '+00:00'))
```

### Message Idempotency

Every message must carry a `message_id` field (UUID4 format). This prevents duplicate processing if a message is reprocessed:

```python
import uuid

message_id = str(uuid.uuid4())  # e.g., "550e8400-e29b-41d4-a716-446655440000"
```

---

## Testing Expectations

### Unit Test Scope

Each agent must have a unit test (`test_<agent_name>.py`) covering:

1. **Happy path**: Valid transaction → correct output state.
2. **Validation failures**: Invalid amount, unsupported currency, missing fields → error message in output.
3. **Edge cases**:
   - Validator: negative amounts, empty strings, missing fields.
   - Fraud Detector: exactly $10,000 USD (boundary), off-hours edge (22:00 and 06:00), cross-border flag variations.
   - Compliance Checker: block-list hit, PII keyword match, high-risk transactions, hold vs. approved.
4. **Idempotency**: Reprocessing same message_id produces same output.

### Integration Test Scope

One integration test (`test_pipeline_integration.py`) must:

1. Set up a temporary `shared/` directory structure.
2. Deposit sample transactions (from `sample-transactions.json` or a test subset).
3. Run Integrator → Validator → Fraud Detector → Compliance Checker in sequence.
4. Assert all transactions reach `shared/results/`.
5. Assert each result JSON is valid and includes all four layers (validation_result, fraud_score, compliance_status).
6. Assert no PII is logged.
7. Clean up temporary directory after test.

### Coverage Gate

- **Minimum**: 80% line coverage (blocks push if below).
- **Target**: ≥ 90% line coverage.
- **Tool**: `pytest --cov=. --cov-report=term-missing --cov-fail-under=80`.
- **Enforcement**: Hook in `.claude/settings.json` prevents git push if coverage < 80%.

---

## Edge Case Handling

### Invalid Transactions

**Validator behavior on invalid input:**
- Missing required field → validation_result.is_valid = false, add error message to errors list, pass to Fraud Detector anyway (downstream agents filter).
- Negative amount → validation_result.is_valid = false, error: "amount must be positive".
- Invalid currency (e.g., "XYZ") → validation_result.is_valid = false, error: "unsupported currency".
- Invalid timestamp → validation_result.is_valid = false, error: "timestamp must be ISO 8601 UTC".

**Fraud Detector behavior on invalid transaction:**
- If validation_result.is_valid = false, still score for fraud (assume worst-case: CRITICAL risk if flagged invalid).
- Log warning: "Processing invalid transaction TXN_ID; fraud score may be inaccurate."

**Compliance Checker behavior on invalid or high-risk transaction:**
- If validation_result.is_valid = false OR fraud_score.risk_level in ["HIGH", "CRITICAL"], status = "HOLD_PENDING_REVIEW".
- Add reason to compliance_status.reasons: e.g., "Validation failed: [reason]" or "Fraud risk CRITICAL".
- Always write to `shared/results/` even if transaction is held or rejected (for audit trail).

### Refunds (Negative Amounts)

- **Validator rule**: Refunds are identified by `transaction_type == "refund"`. For refunds, a negative amount is **expected and valid** — do NOT fail validation on negative amount if transaction_type is "refund". For all other transaction types, negative amount fails validation with error "amount must be positive".
- Fraud Detector: refunds are typically lower fraud risk, but high-value refunds (e.g., -$10,000) should still be scored for unusual patterns (amount absolute value used for threshold comparison).
- Compliance Checker: refunds must still pass block list and PII checks.

### Cross-Border and High-Value Transactions

- **Cross-border**: If metadata.country != "US", flag as risky factor (+25 fraud score points).
- **High-value**: Decimal("10000.00") USD equivalent (convert EUR and GBP using fixed rates).
- **Wire transfers**: transaction_type == "wire_transfer" adds +15 fraud score points.

---

## Logging Standards

### Log Format

```
[TIMESTAMP] [AGENT_NAME] [TRANSACTION_ID] [ACTION] [OUTCOME] [DETAILS]
```

Example:
```
[2026-03-16T10:00:00Z] [transaction_validator] [TXN001] validate_transaction PASS [fields OK, amount valid, currency USD]
[2026-03-16T10:00:05Z] [fraud_detector] [TXN002] detect_fraud CRITICAL [amount $25000 >= $10000, wire transfer]
[2026-03-16T10:00:10Z] [compliance_checker] [TXN001] check_compliance APPROVED [no block list hit, no PII, no hold]
[2026-03-16T10:00:12Z] [compliance_checker] [TXN003] check_compliance HOLD_PENDING_REVIEW [fraud risk HIGH, triggering review hold]
```

### Logging Rules

- **Timestamp**: Use ISO 8601 UTC format (same as message timestamp format).
- **Agent name**: Exactly as defined (transaction_validator, fraud_detector, compliance_checker).
- **Transaction ID**: Always include for traceability.
- **Action**: The function or check being performed.
- **Outcome**: Validator: PASS or FAIL. Fraud Detector: LOW/MEDIUM/HIGH/CRITICAL. Compliance Checker: APPROVED or HOLD_PENDING_REVIEW (REJECTED is reserved, not currently produced).
- **Details**: Brief explanation (no PII, no raw account numbers, no sensitive data).

---

## File Organization

```
homework-6/
├── specification.md                 # This spec (Task 1)
├── agents.md                        # This file (domain rules + conventions)
├── sample-transactions.json         # Input data
├── .claude/
│   └── commands/
│       └── write-spec.md            # Skill to regenerate spec template
├── src/
│   ├── requirements.txt             # Python deps (decimal, pytest, etc.)
│   ├── integrator.py                # Main orchestrator + setup
│   ├── agents/
│   │   ├── __init__.py
│   │   ├── transaction_validator.py # Agent 1: Validator
│   │   ├── fraud_detector.py        # Agent 2: Fraud Detector
│   │   └── compliance_checker.py    # Agent 3: Compliance Checker
│   ├── pipeline/
│   │   ├── __init__.py
│   │   └── messaging.py             # JSON message helpers (load/save)
│   ├── mcp/
│   │   ├── __init__.py
│   │   └── server.py                # FastMCP server (Task 4)
│   ├── tests/
│   │   ├── __init__.py
│   │   ├── test_transaction_validator.py
│   │   ├── test_fraud_detector.py
│   │   ├── test_compliance_checker.py
│   │   └── test_pipeline_integration.py
│   └── shared/                      # Message bus (created at runtime)
│       ├── input/
│       ├── processing/
│       ├── output/
│       └── results/
├── scripts/
│   └── check_coverage.py            # Coverage gate script (Task 3)
├── mcp.json                         # MCP server config (Task 4)
├── research-notes.md                # context7 queries documentation (Task 4)
├── README.md                        # Documentation (Task 5)
├── HOWTORUN.md                      # Step-by-step instructions (Task 5)
└── demo/
    ├── run-demo.ps1                 # Runnable demo script
    └── sample-requests.md           # Example requests
```

---

## Reference: Common Errors and Fixes

| Error | Cause | Fix |
|-------|-------|-----|
| `float` rounding errors in amounts | Using `float` instead of `Decimal` | Replace `amount = 123.45` with `amount = Decimal("123.45")` |
| Missing message_id in output | Agent forgot to copy message_id from input | Always include `message_id` in message envelope when creating output |
| Timestamp zone confusion | Mixing timezone-aware and naive datetimes | Always use UTC, format `YYYY-MM-DDTHH:MM:SSZ` |
| PII in logs | Full account number logged | Use `ACC-1*** **01` format and review log output before commit |
| FileNotFoundError in shared/ | Directory not created by integrator | Run `python integrator.py --setup` before running pipeline |
| Invalid JSON in shared/results/ | Agent wrote malformed JSON | Test JSON serialization with `json.dumps()` and `json.loads()` before writing to file |

---

## Notes for Code-Generating AI

When implementing agents or the integrator:

1. **Always start with `specification.md`** — it defines the exact fields, validations, and error messages expected.
2. **Use this agents.md as the domain reference** — it contains rules on PII, currencies, amounts, edge cases.
3. **Test as you go** — don't implement all three agents and test at the end; test one at a time.
4. **Use Decimal from day one** — never use float for amounts, even in temporary variables.
5. **Write to shared/ carefully** — ensure `shared/input/`, `shared/processing/`, `shared/output/`, `shared/results/` exist before writing, and use unique message_id-based filenames.
6. **Log without PII** — review every log line to ensure no account numbers, names, or sensitive data are in plaintext.
7. **Validate ISO 8601 timestamps** — use `datetime.fromisoformat()` with UTC handling.
8. **Run integrator.py --setup first** — creates shared/ dirs and populates input/ with messages from sample-transactions.json.
