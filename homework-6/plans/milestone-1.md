# Milestone 1: Agent 1 — Specification, agents.md, and /write-spec skill — Session Plan

**Started:** 2026-07-07
**Super-plan reference:** ../PLAN.md milestone 1

## Approach

The specification.md will be written to describe the *actual* system this homework builds: a banking transaction pipeline with three primary agents (Transaction Validator, Fraud Detector, Compliance Checker) that process transactions in sequence through a file-based JSON message bus. The spec will operationalize TASKS.md requirements by mapping High-Level Objective → Mid-Level Objectives → Implementation Notes → Context → Low-Level Tasks (one per agent with exact prompts and function signatures). 

The agents.md will establish behavioral guidelines for AI agents working on this project, covering tech stack assumptions (Python 3.12, FastMCP, file-based messaging), security guardrails (PII handling, decimal vs float for money), testing expectations, and how agents should handle edge cases. It will extend the pattern used in homework-3's agents.md, adapting domain rules from FinTech card lifecycle to transaction processing pipeline.

The /write-spec skill will be a slash command that documents the specification-generation template, allowing future regeneration or variation of the spec with different parameters (e.g., different agent roles or pipeline stages).

## Touch list

- **homework-6/specification.md**: Create with 5 required sections: High-Level Objective (one sentence describing the banking pipeline), Mid-Level Objectives (4–5 testable requirements covering validation, fraud detection, compliance, state transitions), Implementation Notes (guardrails for decimal money, ISO 4217 currencies, JSON message format, timestamps), Context (beginning state: sample-transactions.json; ending state: results in shared/results/), Low-Level Tasks (three entries: Validator, Fraud Detector, Compliance Checker, each with Task/Prompt/File to CREATE/Function to CREATE/Details fields).

- **homework-6/agents.md**: Create with sections covering tech stack (Python 3.12, FastMCP), domain rules (transaction states, agent responsibilities, message format), security constraints (PII redaction, no full account numbers in logs), code conventions (money as Decimal, ISO 4217, idempotency via message_id), testing expectations, and edge-case handling for failed transactions.

- **homework-6/.claude/commands/write-spec.md**: Create as a slash command (markdown file) that describes how to regenerate or customize a specification from a template, including the template structure and key parameters (project name, agent list, message format).

## Review focus

- **Section completeness**: Verify all 5 spec sections are present and correctly named (case-sensitive match in the Verify command).
- **Low-Level Tasks format**: Each agent entry must include all five fields (Task, Prompt, File to CREATE, Function to CREATE, Details). Prompts should be executable instructions (word-for-word what you'd give to Claude Code).
- **agents.md applicability**: Check that guardrails are specific to this transaction pipeline (not generic finance), emphasizing file-based messaging, JSON handoff between agents, and the three-agent sequence.
- **Skill completeness**: The /write-spec.md file should reference the specification and provide enough context that the skill could regenerate a variant spec without ambiguity.
- **Consistency across artifacts**: The tech stack stated in agents.md (Python 3.12, file-based bus, JSON messages) must align with the Low-Level Tasks' function signatures and file names.

## Notes

(Empty at start; appended during execution.)
