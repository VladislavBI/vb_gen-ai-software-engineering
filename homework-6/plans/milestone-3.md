# Milestone 3: Agent 2b — Three cooperating agents run end-to-end — Session Plan

**Started:** 2026-07-07
**Super-plan reference:** ../PLAN.md milestone 3

## Approach

This milestone implements the three core processing agents (Transaction Validator, Fraud Detector, Compliance Checker) and modifies the integrator to orchestrate them in sequence without the `--setup` flag. The strategy is to build each agent as a pure function (`validate_transaction()`, `detect_fraud()`, `check_compliance()`) that accepts a message envelope, enriches it with its output layer, and returns it. The integrator's no-flag mode will read all `.json` files from `shared/input/`, pipe each through the three agents in sequence, and collect final results in `shared/results/`. This design keeps agents stateless and composable — each can be tested independently and chained by the integrator without side effects.

Alternatives considered:
- **Agent as class** (vs. function): Function-based is simpler, testable, and matches the specification's `process_message`-style contract.
- **Async orchestration** (vs. sequential file-based): Sequential file I/O is simpler to debug and aligns with the homework's file-bus design; the spec mandates no async within agent logic.
- **In-memory message bus** (vs. file-based): Rejected; milestone 2 already committed to file-based and the spec uses it for audit trails.

## Touch list

- **homework-6/src/agents/__init__.py**: Create empty module file to mark agents as a package.
- **homework-6/src/agents/transaction_validator.py**: Create `validate_transaction(message: dict) -> dict` function that validates transaction fields (required fields present, amount > 0 except refunds, ISO 4217 currency, ISO 8601 timestamp), adds `data.validation_result` dict with `is_valid`, `errors`, `timestamp` fields, and logs the outcome.
- **homework-6/src/agents/fraud_detector.py**: Create `detect_fraud(message: dict) -> dict` function that scores for fraud risk regardless of validation status, calculates risk from amount (≥$10K USD equiv: 40%), off-hours (< 06:00 or > 22:00: 20%), cross-border (non-US: 25%), wire transfer (15%), assigns risk_level (LOW/MEDIUM/HIGH/CRITICAL), adds `data.fraud_score` dict, and logs HIGH/CRITICAL detections.
- **homework-6/src/agents/compliance_checker.py**: Create `check_compliance(message: dict) -> dict` function that checks validation result, block list (["ACC-9999", "ACC-BLOCKED"]), PII keywords (case-insensitive: password, ssn, credit card, pin, cvv), and high-risk fraud scores, sets hold_flag and compliance status (APPROVED or HOLD_PENDING_REVIEW), adds `data.compliance_status` dict, writes enriched message to `shared/results/{message_id}.json`, and logs HOLD_PENDING_REVIEW outcomes.
- **homework-6/src/integrator.py**: Modify `main()` to support pipeline orchestration when no `--setup` flag is given: read all `.json` files from `shared/input/`, sequentially process each through validator → fraud_detector → compliance_checker, and verify all results reach `shared/results/`.

## Review focus

- **Decimal vs. float**: Verify all amount comparisons and conversions use `Decimal` (not `float`). Check USD-equivalent conversion for EUR and GBP (0.92 and 1.27 rates) uses Decimal arithmetic.
- **ISO 8601 timestamp parsing**: Validate that timestamp parsing handles 'Z' suffix correctly and that off-hours boundary (strictly after 22:00 or before 06:00) is correctly implemented (22:00:00 and 06:00:00 exactly are normal hours).
- **Refund handling**: Verify that negative amounts are allowed only when `transaction_type == "refund"`, and Fraud Detector uses absolute value for amount threshold comparison.
- **Message envelope preservation**: Ensure `message_id`, `source_agent`, `target_agent`, and all prior `data.*` fields are preserved as each agent enriches the message (no accidental overwrites).
- **PII redaction**: Confirm that logs never output full account numbers (must be `ACC-1*** **01` format), customer names, or raw sensitive data. Check that PII detection in compliance checker uses case-insensitive keyword matching.
- **File I/O and error handling**: Verify that integrator creates the required shared directories, handles missing or malformed JSON files gracefully, and that agents don't crash on edge cases (empty descriptions, missing optional fields, invalid ISO 8601 strings).

## Notes

- Milestone 2's session plan flagged a path-handling assumption: `shared/` is cwd-relative (from src/) and `sample-transactions.json` is anchored via `Path(__file__).parent.parent`. Keeping this consistent in the no-flag orchestration mode.
- The Fraud Detector must process all transactions including invalid ones (per specification: "never filter, always score").
- The pipeline implements the 4-stage file-bus design: input → processing → output → results. Each agent writes its enriched message to the next stage directory (Validator to processing/, Fraud Detector to output/, Compliance Checker to results/).
- PII redaction uses a shared helper in `pipeline/messaging.py` with format "first 6 chars + last 2 only" (e.g., ACC-1*** **01) to ensure consistent redaction across all agents.
- Fraud Detector factors are tracked as explicit booleans during evaluation (not back-derived from risk_score) to ensure correctness across all factor combinations.
- Fraud score is an int (not float) per specification.

