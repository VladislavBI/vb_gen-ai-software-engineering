# Milestone 5: Tests — per-agent units + full-pipeline integration with coverage gate — Session Plan

**Started:** 2026-07-08
**Super-plan reference:** ../PLAN.md milestone 5

## Approach

Build a pytest suite with **four test files**: one unit test per agent (`test_transaction_validator.py`, `test_fraud_detector.py`, `test_compliance_checker.py`) plus one integration test (`test_pipeline_integration.py`). 

**Unit test strategy:** Since all three agents are pure functions (message dict → dict, no file I/O at the unit level), each unit test will exercise the function directly with carefully constructed test messages, covering happy path, validation failures, boundary conditions (especially off-hours 22:00/06:00 exact times, $10K USD threshold), and edge cases (refunds, cross-border, block list, PII keywords). Each agent will have 8–12 test cases targeting ≥90% coverage of its logic.

**Integration test strategy:** Use `pytest`'s `tmp_path` fixture to create an isolated `shared/` directory structure (isolated from real homework-6/src/shared/), deposit test transaction messages into the isolated `shared/input/`, run `integrator.py`'s `run_pipeline()` function, and assert all transactions flow through all three agents and land in `shared/results/`. This avoids polluting the real shared/ directory during test runs and validates the full orchestration.

**Test isolation:** Unit tests operate on message dicts directly; integration test uses `tmp_path` to isolate file I/O. No mocking of agent functions — test them directly to ensure real behavior is tested, not mocked contracts.

**Coverage target:** Aim for ≥90% (gate minimum is 80%). Use `pytest --cov=. --cov-report=term-missing --cov-fail-under=80 -q` to verify.

## Touch list

**test_transaction_validator.py:**
- Test valid transaction → `is_valid=True`, empty errors list
- Test refund with negative amount → `is_valid=True` (refund carve-out)
- Test non-refund with negative amount → `is_valid=False`, error message added
- Test positive amount, non-refund → `is_valid=True`
- Test zero amount → `is_valid=False`
- Test missing required field → `is_valid=False`, error for each missing field
- Test invalid currency (e.g., "XYZ") → `is_valid=False`
- Test invalid ISO 8601 timestamp → `is_valid=False`
- Test valid ISO 8601 timestamps (with/without Z) → `is_valid=True`
- Test Decimal precision on amounts (no float rounding errors)

**test_fraud_detector.py:**
- Test LOW risk (amount < $10K, no other factors) → score < 20, risk_level="LOW"
- Test high-value transaction ($10K+ USD) → high_amount=True, score includes 40 points
- Test high-value with currency conversion (EUR, GBP) → correct USD equivalence applied
- Test off-hours boundary: 22:00:00 exactly → off_hours=False (normal hours boundary)
- Test off-hours boundary: 06:00:00 exactly → off_hours=False (normal hours boundary)
- Test off-hours: 22:00:01 → off_hours=True, score includes 20 points
- Test off-hours: 05:59:59 → off_hours=True, score includes 20 points
- Test cross-border (metadata.country != "US") → cross_border=True, score includes 25 points
- Test wire transfer (transaction_type contains "wire") → wire_transfer=True, score includes 15 points
- Test factor combinations don't bleed (high_amount alone should not flag off_hours, etc.) — regression test for milestone 3 factor bug
- Test invalid transaction (validation_result.is_valid=False) → still scores, no filtering
- Test refund high-value (negative amount, absolute value >= $10K) → high_amount applies to absolute value

**test_compliance_checker.py:**
- Test APPROVED (valid validation, no block list, no PII, no high fraud) → status="APPROVED", hold_flag=False
- Test HOLD_PENDING_REVIEW (validation failed) → status="HOLD_PENDING_REVIEW", hold_reasons includes validation errors
- Test HOLD_PENDING_REVIEW (source account on block list) → status="HOLD_PENDING_REVIEW"
- Test HOLD_PENDING_REVIEW (destination account on block list) → status="HOLD_PENDING_REVIEW"
- Test HOLD_PENDING_REVIEW (PII keyword in description) → status="HOLD_PENDING_REVIEW", hold_reasons includes "PII"
- Test HOLD_PENDING_REVIEW (fraud risk HIGH) → status="HOLD_PENDING_REVIEW", hold_reasons includes fraud risk
- Test HOLD_PENDING_REVIEW (fraud risk CRITICAL) → status="HOLD_PENDING_REVIEW"
- Test block list case-insensitivity (acc-9999 in lowercase) → matches block list
- Test PII keyword case-insensitivity → detects "Password", "PASSWORD", etc.
- Test write to shared/results/ → file created with correct message_id.json naming, valid JSON

**test_pipeline_integration.py:**
- Set up isolated tmp_path with shared/input, shared/processing, shared/output, shared/results
- Create test transactions (mix of valid, invalid, high-risk) in the isolated shared/input/
- Run integrator's run_pipeline() against isolated shared/
- Assert all transactions processed (count matches input count)
- Assert all results written to isolated shared/results/
- Assert each result JSON is valid JSON and includes all four layers (validation_result, fraud_score, compliance_status)
- Assert compliance_status.status is only "APPROVED" or "HOLD_PENDING_REVIEW" (no "REJECTED")
- Assert PII not logged to stdout/stderr (no full account numbers in print output)
- Clean up isolated shared/ after test

## Review focus

- **Factor breakdown correctness (regression)**: In `test_fraud_detector.py`, ensure that factors dict only marks factors that actually triggered (e.g., high_amount=True only if amount >= $10K), and combinations don't bleed (off_hours should not be marked if hour is within 06:00–22:00). This guards against the milestone 3 bug where factors were back-derived incorrectly.

- **Boundary precision on off-hours**: Test that 22:00:00 and 06:00:00 exactly are **not** flagged as off-hours (normal business hours), while 22:00:01 and 05:59:59 are. Off-hours is strictly `hour < 6` or `hour > 22`, not `<=` or `>=`.

- **Refund carve-out**: Validate that refunds can have negative amounts and pass validation, while non-refunds with negative amounts fail. This is a key spec difference.

- **Block list and PII case-insensitivity**: Ensure block list match is case-insensitive (ACC-9999 matches acc-9999), and PII keyword detection is case-insensitive.

- **Integration isolation**: The integration test must use `tmp_path` and not touch the real `homework-6/src/shared/` directory. Verify that after the test runs, isolated tmp_path is cleaned up and real shared/ is untouched.

- **Coverage gate behavior**: Verify that `pytest --cov=. --cov-fail-under=80 -q` exits 0 when coverage >= 80%, and exits non-zero when below 80%. Also verify that at least one test runs (exit code 5 = no tests collected, which is failure).

## Notes

**Review pass 1 (initial):** Reviewer approved the test suite structure; no blocking findings.

**Review pass 2 (after approval):** Code review approved; proceeding to Verify.

**Verify — first attempt (2026-07-08):** One test failed: `test_off_hours_after_22_00` expected off_hours=True at 22:00:01 but got False. Root cause: boundary logic in fraud_detector.py was checking `hour > 22`, which excludes hour 22 entirely. The intended business logic requires sub-second precision: normal hours are 06:00:00 through 22:00:00 inclusive, off-hours are < 06:00:00 OR > 22:00:00. Fixed by changing boundary check from `hour > NORMAL_HOURS_END` to `hour > NORMAL_HOURS_END or (hour == NORMAL_HOURS_END and (minute > 0 or second > 0))`. Re-ran Verify: all 76 tests pass, coverage 84.77% (exceeds 80% gate).
