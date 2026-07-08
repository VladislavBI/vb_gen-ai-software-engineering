# /write-spec

**Regenerate the transaction pipeline specification from a template.**

Use this skill to regenerate the `specification.md` file with different parameters (agent list, pipeline stages, business rules) without manually rewriting all 5 sections.

---

## Overview

The specification template defines the structure of a multi-agent transaction pipeline:

1. **High-Level Objective** — One sentence describing what the pipeline does.
2. **Mid-Level Objectives** — 4–5 testable requirements (validation, fraud detection, compliance, state transitions, audit trail).
3. **Implementation Notes** — Guardrails for monetary arithmetic, currency handling, message format, logging, PII protection.
4. **Context** — Beginning state (sample-transactions.json) and ending state (results in shared/results/).
5. **Low-Level Tasks** — Three entries (one per agent), each with Task name, Prompt, Files to create, Functions to create, and Details.

---

## Template Parameters

When regenerating the spec, you can customize:

### Pipeline Configuration

| Parameter | Values | Default | Notes |
|-----------|--------|---------|-------|
| `agent_list` | List of agent names | `["Transaction Validator", "Fraud Detector", "Compliance Checker"]` | Minimum 3 agents |
| `pipeline_stages` | State names | `["SUBMITTED", "VALIDATED", "FRAUD_SCORED", "COMPLIANCE_CHECKED"]` | One more than agents (raw + processed stages) |
| `message_bus_type` | "file" \| "queue" \| "streaming" | "file" | Type of inter-agent communication |
| `message_format` | "json" \| "protobuf" \| "avro" | "json" | Message envelope format |

### Business Rules

| Parameter | Format | Default | Notes |
|-----------|--------|---------|-------|
| `high_value_threshold` | Currency + amount | "USD 10000" | For fraud scoring high-value transactions |
| `off_hours_range` | "HH:MM-HH:MM" | "22:00-06:00" | Time range flagged as risky for fraud detection |
| `currency_list` | Comma-separated ISO 4217 | "USD,EUR,GBP,JPY,CAD,AUD,CHF" | Supported currency codes |
| `compliance_block_list` | Account prefixes or IDs | "ACC-9999,ACC-BLOCKED" | Accounts to flag/block |
| `pii_keywords` | Comma-separated strings | "password,ssn,credit card,pin,cvv" | Keywords that trigger PII detection |

### Technical Stack

| Parameter | Values | Default | Notes |
|-----------|--------|---------|-------|
| `runtime` | "python3.12" \| "node18" \| "java17" \| "golang1.20" | "python3.12" | Runtime environment |
| `agent_framework` | "fastmcp" \| "anthropic" \| "autogen" \| "crewai" | "fastmcp" | Framework for agent implementation |
| `logging_format` | "structured" \| "plaintext" | "plaintext" | Log output format |
| `coverage_threshold` | "80" \| "85" \| "90" | "80" | Minimum test coverage percentage |

---

## Usage

### Basic: Regenerate with default parameters

The specification for the banking transaction pipeline uses these defaults:

```
High-Level Objective: "Build a multi-agent transaction processing pipeline that validates, scores for fraud risk, and performs compliance checks on incoming banking transactions, passing results through a file-based JSON message bus with audit trails and precise decimal arithmetic."

Agents: [Transaction Validator, Fraud Detector, Compliance Checker]

Pipeline Stages: SUBMITTED → VALIDATED → FRAUD_SCORED → COMPLIANCE_CHECKED

Message Format: JSON with message_id (UUID), timestamp (ISO 8601 UTC), source_agent, target_agent, message_type, and nested data object

Currency Support: USD, EUR, GBP, JPY, CAD, AUD, CHF (ISO 4217)

High-Value Threshold: USD $10,000 equivalent

Off-Hours Detection: 22:00 - 06:00 UTC

Compliance Block List: ACC-9999, ACC-BLOCKED

PII Keywords: password, ssn, credit card, pin, cvv, security code, atm code

Runtime: Python 3.12

Framework: FastMCP for MCP server implementation

Logging: Plaintext with agent name, transaction ID, timestamp, and outcome (no PII)

Test Coverage Gate: 80% (target 90%)
```

### Customization: Change agent roles or pipeline stages

To regenerate the spec with different agents or business rules:

1. Identify which template parameters you want to customize (from the **Template Parameters** table above).
2. Provide new values for those parameters.
3. The specification will be regenerated with:
   - Updated **High-Level Objective** to reflect new agent roles.
   - Updated **Mid-Level Objectives** with requirements matching the new agents.
   - Updated **Low-Level Tasks** section with one entry per agent (Task name, Prompt template, Files, Functions).
   - Updated **Implementation Notes** with new business rules (thresholds, currencies, keywords).
   - All other sections preserved from the default template.

### Example: Add a fourth agent (Settlement Processor)

To extend the pipeline with a settlement agent:

```
agent_list: ["Transaction Validator", "Fraud Detector", "Compliance Checker", "Settlement Processor"]
pipeline_stages: ["SUBMITTED", "VALIDATED", "FRAUD_SCORED", "COMPLIANCE_CHECKED", "SETTLED"]
```

The regenerated spec would include:

- Updated High-Level Objective: "...and processes settlement for approved transactions..."
- Updated Mid-Level Objectives: Add requirement for settlement state machine.
- Updated Low-Level Tasks: Add Task 4 entry for Settlement Processor (Prompt, Files, Functions).

### Example: Change runtime to Node.js

To regenerate for Node.js instead of Python:

```
runtime: "node18"
agent_framework: "fastmcp" (or equivalent Node MCP framework)
```

The regenerated spec would include:

- Updated Implementation Notes: Replace `python decimal.Decimal` with Node.js `decimal.js` library.
- Updated Low-Level Tasks: Prompts would reference JavaScript/TypeScript, not Python.
- Updated Context: Commands and file paths would be `npm run`, not `python integrator.py`.

---

## Output: Generated Sections

When you regenerate the specification with custom parameters, the output includes all 5 sections:

1. **High-Level Objective** — Updated to reflect new agent roles and pipeline purpose.
2. **Mid-Level Objectives** — Testable requirements for each new agent and stage.
3. **Implementation Notes** — Updated guardrails, thresholds, currencies, logging format.
4. **Context** — Beginning state (sample input data) and ending state (expected outputs).
5. **Low-Level Tasks** — One task entry per agent, with executable Prompt templates.

Each task entry follows this format:

```
Task: [Agent Name]
Prompt: "[Exact prompt for Claude Code or code-generating AI]"
File to CREATE: [Path/to/agent_file.py or equivalent]
Function to CREATE: [function_name(input: Type) -> Type]
Details: [What the agent checks, transforms, or decides]
```

---

## Integration with Code Generation (Agent 2)

When **Agent 2** (code-generation agent) receives the specification:

1. It reads the **Low-Level Tasks** section.
2. For each task, it extracts the **Prompt** field (verbatim).
3. It invokes a code-generating AI (Claude Code, Copilot, etc.) with the prompt.
4. The AI generates the agent implementation (e.g., `transaction_validator.py`).
5. The AI creates the required Functions (e.g., `validate_transaction(message: dict) -> dict`).

This decoupling means:

- The specification is the contract between Task 1 (specification) and Task 2 (code).
- The prompts in **Low-Level Tasks** are copy-paste-ready.
- If the spec is regenerated with different agents, the new prompts are automatically available for Agent 2 to use.

---

## Preserving Existing Specs

If you want to preserve a previously generated `specification.md` while creating a variant:

1. Save the current spec as `specification-v1.md`.
2. Run `/write-spec` with new parameters to regenerate `specification.md`.
3. The tool does not overwrite previous specs; you control versioning.

---

## When to Use This Skill

Use `/write-spec` when:

- **You want to add or remove agents** from the pipeline (e.g., add Settlement Processor, remove Compliance Checker).
- **You want to change business rules** (e.g., raise high-value threshold to $25K, add new currency support).
- **You want to retarget a different runtime** (e.g., Node.js instead of Python).
- **You want to document a variant pipeline** for a different homework or course module.

Do not use `/write-spec` when:

- You only need to modify implementation details within an existing agent (edit `agents.md` instead).
- You want to change a single business rule in an already-generated spec (edit `specification.md` directly).

---

## Reference: Specification Template Structure

The specification is organized around five required sections:

### 1. High-Level Objective
**Purpose**: One-sentence summary of what the pipeline does.
**Template**: "Build a [type] transaction [action] pipeline that [validates/scores/checks/processes] [subject], [mechanism/constraint]."
**Example**: "Build a multi-agent transaction processing pipeline that validates, scores for fraud risk, and performs compliance checks on incoming banking transactions, passing results through a file-based JSON message bus with audit trails and precise decimal arithmetic."

### 2. Mid-Level Objectives
**Purpose**: 4–5 testable requirements, one per major agent or capability.
**Template per objective**: "[Agent] [action]: [check/transform], [output location/state], [measurement/condition]."
**Example**: "Transaction validation: The Validator agent checks required fields (transaction_id, amount, currency, timestamp), confirms amounts are positive and precise (decimal), validates ISO 4217 currency codes, and logs validation results with transaction IDs and timestamps to `shared/processing/`."

### 3. Implementation Notes
**Purpose**: Guardrails for money, currency, messages, logging, PII.
**Subsections**:
- **Monetary Arithmetic**: language-specific Decimal type, rounding rule, JSON representation.
- **Currency and Localization**: ISO 4217 codes, thresholds, conversion rates.
- **Message Format and Naming**: JSON envelope schema, idempotency (message_id), timestamps, file naming.
- **Logging and PII Protection**: log format, PII redaction rules, audit level.
- **Directory Structure**: `shared/input/`, `shared/processing/`, `shared/output/`, `shared/results/`.

### 4. Context
**Purpose**: Beginning state (input data) and ending state (expected outputs).
**Beginning State**: 
- Input file (e.g., `sample-transactions.json`).
- Initial action (wrapping, enveloping).
- Edge cases in sample data.
**Ending State**:
- Output directory and format (e.g., `shared/results/` with final records).
- Coverage goal (e.g., ≥ 90%).
- Documentation deliverables (README, HOWTORUN, demo, screenshots).

### 5. Low-Level Tasks
**Purpose**: Per-agent implementation briefs; each with executable Prompt.
**Structure per task**:
- **Task**: Agent name.
- **Prompt**: Exact prompt to give Claude Code or code-generating AI (copy-paste ready).
- **File to CREATE**: Path (e.g., `src/agents/fraud_detector.py`).
- **Function to CREATE**: Signature (e.g., `detect_fraud(message: dict) -> dict`).
- **Details**: What the agent checks, transforms, or decides (1–2 paragraphs).

---

## Skill Limitations

- The `/write-spec` skill regenerates the specification; it does **not** execute against the pipeline or generate code.
- Agent 2 (code-generation) reads the regenerated spec and uses its prompts to generate the actual agent code.
- If you need to validate that a regenerated spec is correct, review the **Low-Level Tasks** section and ensure each Prompt is actionable and each Files/Functions path is consistent with your project layout.
