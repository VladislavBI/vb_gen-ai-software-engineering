# Milestone 2: Agent 2a — Pipeline scaffold, message bus, and integrator setup — Session Plan

**Started:** 2026-07-07
**Super-plan reference:** ../PLAN.md milestone 2

## Approach

This milestone establishes the foundational infrastructure for the multi-agent pipeline. We will create a minimal Python 3.12 project structure with a file-based message bus (four shared/ subdirectories) and a messaging helper that wraps transactions in the standard JSON envelope format defined in specification.md. The integrator.py script will accept a `--setup` flag that orchestrates directory creation and loads sample-transactions.json, converting each transaction into a properly-enveloped JSON message file (one per transaction, named by message_id.json) deposited into shared/input/. This approach defers agent implementation to milestone 3, avoiding the complexity of debugging agents and plumbing together. Alternative: a single monolithic integrator.py with embedded transaction wrapping logic was rejected because separating the messaging protocol (pipeline/messaging.py) makes it reusable by all three agents later and testable independently.

## Touch list

- **homework-6/src/requirements.txt** — minimal Python dependencies: pytest and pytest-cov for testing framework (needed later in milestone 5); json, decimal, uuid, datetime are builtin.
- **homework-6/src/pipeline/__init__.py** — package marker; empty or minimal imports to establish the pipeline package.
- **homework-6/src/pipeline/messaging.py** — message envelope and I/O helpers:
  - `load_sample_transactions(path: str) -> list[dict]` — read and parse sample-transactions.json from homework root.
  - `create_message_envelope(transaction: dict, source_agent: str, target_agent: str, message_type: str) -> dict` — wrap a transaction in the standard envelope with UUID message_id and ISO 8601 UTC timestamp.
  - `save_message(message: dict, shared_dir: str) -> None` — write a message JSON file to shared/{dir}/ named {message_id}.json.
- **homework-6/src/integrator.py** — main orchestrator:
  - `--setup` mode: create shared/input/, shared/processing/, shared/output/, shared/results/; load all 8 sample transactions; wrap each in message envelope with source_agent="integrator" and target_agent="transaction_validator"; deposit to shared/input/.
  - Proper error handling if sample-transactions.json is missing or malformed.
  - Idempotent directory creation (mkdir -p equivalent).

## Review focus

- **Message envelope format conformance**: Every message must have message_id (UUID4 string), timestamp (ISO 8601 UTC in format YYYY-MM-DDTHH:MM:SSZ), source_agent ("integrator"), target_agent ("transaction_validator"), message_type ("transaction"), and nested data object containing the full original transaction.
- **Idempotency and robustness**: Running `python integrator.py --setup` twice must not fail or corrupt shared/input/; existing directories and files should be left intact (or safely overwritten).
- **File naming and uniqueness**: Each message file must be named {message_id}.json (lowercase, no collisions since UUID is unique).
- **Path handling**: Use pathlib.Path or os.path.join to avoid OS-specific separator issues; all relative paths must be relative to src/ directory (where integrator.py runs).
- **All 8 transactions loaded**: Verify that all transactions from sample-transactions.json are wrapped and deposited (no skipped or filtered transactions at this stage).

## Notes

**Reviewer observation (for future milestones):** The integrator.py uses `Path(__file__).parent.parent` to locate sample-transactions.json (anchored to src/), while shared/ directory paths are cwd-relative (e.g., `Path('shared') / 'input'`). This works correctly with the Verify command's `Push-Location homework-6/src` preamble, but milestone 6 (/run-pipeline skill) and milestone 7 (demo/ scripts) should validate path handling when they introduce new callers of integrator.py or when running from different working directories. No code change needed now; this is a forward-looking note for architectural consistency.
