# Banking Transaction Pipeline — Technical Specification

**Created:** 2026-07-07
**Author:** Vlad Bairak
**Stack:** Python 3.12, FastMCP, file-based JSON message bus

---

## High-Level Objective

Build a multi-agent transaction processing pipeline that validates, scores for fraud risk, and performs compliance checks on incoming banking transactions, passing results through a file-based JSON message bus with audit trails and precise decimal arithmetic.

---

## Mid-Level Objectives

1. **Transaction validation**: The Validator agent checks required fields (transaction_id, amount, currency, timestamp), confirms amounts are positive (with exception for refunds where negative is allowed) and precise (decimal), validates ISO 4217 currency codes, and logs validation results with transaction IDs and timestamps to `shared/processing/`.

2. **Fraud risk scoring**: The Fraud Detector agent receives validated transactions, scores them based on amount thresholds (≥$10,000 USD equivalent), unusual timing (off-hours), cross-border transfers, and transaction type patterns; assigns a risk level (LOW, MEDIUM, HIGH, CRITICAL) and reason to each transaction in `shared/processing/`.

3. **Compliance checking**: The Compliance Checker agent receives fraud-scored transactions, verifies they are not on a block list (configurable), checks for PII exposure in descriptions, and applies regulatory holds for high-risk transactions; outputs compliance status and hold flags to `shared/processing/`.

4. **State machine progression**: Each transaction progresses through states (SUBMITTED → VALIDATED → FRAUD_SCORED → COMPLIANCE_CHECKED) as messages flow through `shared/input/` → `shared/processing/` → `shared/output/` → `shared/results/`.

5. **Audit trail and completeness**: All transactions from `sample-transactions.json` appear in `shared/results/` as final JSON objects with all intermediate decisions (validation_result, fraud_score, compliance_status) and ISO 8601 timestamps on every operation.

---

## Implementation Notes

### Monetary Arithmetic
- **Use Python `decimal.Decimal`**, not `float`, for all currency amounts.
- **Rounding rule**: ROUND_HALF_UP for all conversions (e.g., multi-currency comparisons).
- **Internal representation**: store all amounts as strings in JSON, parse to Decimal on read, serialize back to strings on write.

### Currency and Localization
- **ISO 4217 support**: USD, EUR, GBP, JPY, CAD, AUD, CHF, and others; reject invalid codes (e.g., "XYZ").
- **High-value threshold**: USD $10,000 equivalent (use fixed rates for demo: EUR 0.92 to USD, GBP 1.27 to USD).
- **Cross-border detection**: compare source and destination `metadata.country` fields.

### Fraud Detection Boundaries
- **Off-hours time window**: Fraud Detector flags transactions with UTC hour `< 06:00` OR `> 22:00` as off-hours (risky). Boundary values: 22:00:00 and 06:00:00 exactly are considered normal business hours (not flagged); only times strictly after 22:00 or strictly before 06:00 trigger the risk flag.

### Message Format and Naming
**Standard JSON message envelope:**
```json
{
  "message_id": "uuid4-format-string",
  "timestamp": "2026-03-16T10:00:00Z",
  "source_agent": "transaction_validator",
  "target_agent": "fraud_detector",
  "message_type": "transaction",
  "data": {
    "transaction_id": "TXN001",
    "amount": "1500.00",
    "currency": "USD",
    "status": "validated",
    "validation_errors": []
  }
}
```
- **Idempotency**: `message_id` (UUID) ensures no duplicate processing if a message is reprocessed.
- **Timestamps**: All ISO 8601 UTC format (`YYYY-MM-DDTHH:MM:SSZ`).
- **File naming**: Use `{message_id}.json` in shared dirs to avoid collisions.

### Logging and PII Protection
- **Log format**: `[TIMESTAMP] [AGENT_NAME] [TXN_ID] [ACTION] [OUTCOME]`
- **PII rules**: Never log full account numbers (first 6 + last 2 only, e.g., `ACC-1*** **01`), no customer names in logs, no destination IP/location in plain text.
- **Audit level**: all validation passes/failures, all fraud scores, all compliance holds/releases logged.

### Directory Structure
```
shared/
├── input/       # Integrator deposits initial transaction messages here
├── processing/  # Agents move messages here while working
├── output/      # Agents write enriched messages here for next agent
└── results/     # Final outcomes after all agents complete
```

---

## Context

### Beginning State
- **Input file**: `sample-transactions.json` in the homework root, containing 8 sample transactions of varying amounts, currencies, and risk profiles.
- **Initial action**: Integrator reads `sample-transactions.json`, wraps each transaction in a JSON message envelope with a UUID and timestamp, and deposits one JSON file per transaction into `shared/input/`.
- **Sample transactions include edge cases**: invalid currency ("XYZ"), high-value transfer ($75K), cross-border (DE, GB), negative amount (refund), off-hours timestamp (02:47 UTC).

### Ending State
- **Output directory**: `shared/results/` contains one JSON file per original transaction (8 files total), each with all intermediate decisions preserved.
- **Final record format**: includes original transaction data, validation result, fraud score, compliance status, and all timestamps.
- **Coverage goal**: test suite reaches ≥90% code coverage (gate minimum: 80%).
- **Documentation deliverables**: README.md (with ASCII pipeline diagram and tech stack), HOWTORUN.md (numbered steps), demo scripts (runnable examples).

---

## Low-Level Tasks

### Task 1: Transaction Validator

**Task:** Transaction Validator

**Prompt:** 
```
Build a Python agent function `validate_transaction(message: dict) -> dict` in 
homework-6/src/agents/transaction_validator.py that:
1. Accepts a JSON message with data.transaction_id, data.amount, data.currency
2. Validates: amount is positive; currency is ISO 4217 (USD, EUR, GBP, JPY, CAD, AUD, CHF); 
   timestamp is valid ISO 8601; all required fields present
3. Returns enriched message with data.validation_result containing fields:
   - is_valid (bool)
   - errors (list of strings, empty if valid)
   - timestamp (ISO 8601 when validation ran)
4. Use Python Decimal for amount checks, never float
5. Log validation outcome to console/file with transaction_id and is_valid status
```

**File to CREATE:** `homework-6/src/agents/transaction_validator.py`

**Function to CREATE:** `validate_transaction(message: dict) -> dict`

**Details:**
- Checks all required fields: transaction_id, amount, currency, timestamp, source_account, destination_account.
- Validates amount: For transaction_type == "refund", negative amounts are allowed. For all other types, amount must be > 0. Use Decimal arithmetic, never float.
- Validates currency against hardcoded ISO 4217 list (USD, EUR, GBP, JPY, CAD, AUD, CHF, etc.); rejects unknown codes.
- Validates timestamp is ISO 8601 UTC format.
- Returns message with nested `data.validation_result` dict containing `is_valid`, `errors`, and `timestamp`.
- Logs outcome with transaction_id, validation_result status, and any errors (without PII).

---

### Task 2: Fraud Detector

**Task:** Fraud Detector

**Prompt:**
```
Build a Python agent function `detect_fraud(message: dict) -> dict` in 
homework-6/src/agents/fraud_detector.py that:
1. Accepts any transaction message (validated or invalid). If data.validation_result.is_valid == false, 
   treat as elevated risk and proceed with scoring anyway (never filter out invalid transactions).
2. Scores transaction for fraud risk based on:
   - Amount ≥ $10,000 USD equivalent (use Decimal; convert EUR 0.92/USD, GBP 1.27/USD)
   - Timestamp hour < 06:00 or > 22:00 UTC (off-hours)
   - Cross-border: metadata.country != "US" (unusual)
   - Transaction type "wire_transfer" (higher risk)
3. Assigns risk score: LOW (0-30), MEDIUM (31-60), HIGH (61-85), CRITICAL (86-100)
4. Calculates final score as sum of weighted risk factors (amount: 40%, timing: 20%, 
   cross-border: 25%, wire: 15%)
5. Returns enriched message with data.fraud_score containing:
   - risk_level (string: LOW/MEDIUM/HIGH/CRITICAL)
   - score (int 0-100)
   - factors (dict of applied factors and their contributions)
   - timestamp (when fraud check ran)
6. Log all HIGH and CRITICAL scores to console/file with transaction_id and risk_level
```

**File to CREATE:** `homework-6/src/agents/fraud_detector.py`

**Function to CREATE:** `detect_fraud(message: dict) -> dict`

**Details:**
- Processes all transactions regardless of validation result. If validation failed (is_valid == false), assume worst-case risk and score as CRITICAL or HIGH.
- High-value threshold: Decimal("10000") for USD; converts other currencies using fixed rates (EUR: 0.92, GBP: 1.27).
- Off-hours detection: parses ISO 8601 timestamp, extracts hour in UTC, flags timestamps strictly after 22:00 or strictly before 06:00 as off-hours (22:00:00 and 06:00:00 exactly are normal hours).
- Cross-border: compares metadata.country to "US"; non-US = risky.
- Wire transfer: transaction_type == "wire_transfer" adds 15 points.
- Weighted scoring: amount_risk (0-40 points) + timing_risk (0-20) + cross_border_risk (0-25) + wire_risk (0-15).
- Returns nested `data.fraud_score` dict with risk_level, score (int), factors (dict), and timestamp.
- Logs HIGH and CRITICAL detections with transaction_id and reason.

---

### Task 3: Compliance Checker

**Task:** Compliance Checker

**Prompt:**
```
Build a Python agent function `check_compliance(message: dict) -> dict` in 
homework-6/src/agents/compliance_checker.py that:
1. Accepts a fraud-scored transaction message (must have data.fraud_score.risk_level)
2. Checks validation result: if data.validation_result.is_valid == false, set hold_flag = true and note validation failures
3. Applies compliance rules:
   - Block list check: flag if destination_account in ["ACC-9999", "ACC-BLOCKED"] (demo block list)
   - PII check: flag if description contains known keywords like "password", "SSN", "credit card"
   - Hold rule: if fraud risk is HIGH or CRITICAL, apply regulatory hold (hold_flag = true)
4. Determines compliance status: APPROVED or HOLD_PENDING_REVIEW (see logic below; REJECTED reserved for future explicit block rules)
5. Returns enriched message with data.compliance_status containing:
   - status (APPROVED/HOLD_PENDING_REVIEW; REJECTED is reserved and not used currently)
   - hold_flag (bool, true if validation failed OR HIGH/CRITICAL risk OR PII detected OR block list hit)
   - reasons (list of strings explaining any holds/blocks/PII issues/validation failures)
   - timestamp (when compliance check ran)
6. Log all HOLD_PENDING_REVIEW transactions to console/file with transaction_id and reasons
7. Final result written to shared/results/ with all fields: original data + 
   validation_result + fraud_score + compliance_status
```

**File to CREATE:** `homework-6/src/agents/compliance_checker.py`

**Function to CREATE:** `check_compliance(message: dict) -> dict`

**Details:**
- Validation check: if data.validation_result.is_valid == false, set hold_flag = true and add each validation error to reasons list.
- Block list: hardcoded list of destination_account values that are flagged (["ACC-9999", "ACC-BLOCKED"]); if destination_account matches, set hold_flag = true.
- PII detection: searches description field for keywords (case-insensitive): "password", "ssn", "credit card", "pin", "cvv"; if found, set hold_flag = true.
- Fraud hold rule: if fraud_score.risk_level in ["HIGH", "CRITICAL"], set hold_flag = true.
- Status logic: if hold_flag == true, status = "HOLD_PENDING_REVIEW"; otherwise status = "APPROVED". REJECTED status is reserved for future explicit use.
- Returns nested `data.compliance_status` dict with status, hold_flag, reasons (list), and timestamp.
- Logs all HOLD_PENDING_REVIEW outcomes with transaction_id and reasons.
- Writes final enriched message to `shared/results/{message_id}.json` with all four layers of data (original + validation + fraud + compliance).
