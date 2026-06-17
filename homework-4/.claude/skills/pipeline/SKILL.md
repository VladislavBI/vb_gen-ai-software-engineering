---
name: pipeline
description: "Run the homework-4 4-agent bug pipeline end-to-end with one command. Performs the Bug Researcher and Bug Planner stages inline, then dispatches research-verifier, bug-fixer, and (in parallel) security-verifier + unit-test-generator. Invoke as /pipeline [bug-id]."
argument-hint: "[bug-id]"
---

# Bug Pipeline Orchestrator

You are the orchestrator for the homework-4 **4-agent bug pipeline**. One invocation drives the
full run order from `TASKS.md`:

```
Bug Researcher → Bug Research Verifier → Bug Planner → Bug Fixer → (Security Verifier ∥ Unit Test Generator)
```

The four `*-verifier` / `*-fixer` / `*-generator` stages are real subagents in
`.claude/agents/`; the **Researcher** and **Planner** stages have no agent, so **you perform them
inline**. Drive every stage to completion in order — do not ask the user to invoke agents
manually between steps.

## Prerequisites (already enforced)

A `PreToolUse` hook (`.claude/skills/pipeline/check-prereqs.ps1`) verifies **both** the four agent
files and a non-empty `src/` before this skill can start. **If you are reading this, those
prerequisites passed** — you may assume
`.claude/agents/{research-verifier,bug-fixer,security-verifier,unit-test-generator}.md` and a
populated `src/` are present. Do not re-check them.

## Conventions

- **Bug id:** use the argument if given, else default to `001`.
- **Artifact root:** `context/bugs/<id>/`. Create the directory tree as needed.
- **Skills load automatically:** `research-verifier` loads `research-quality-measurement` and
  `unit-test-generator` loads `unit-tests-FIRST` from inside their own agent definitions — you do
  not need to pass or load those skills yourself.
- **Stop-on-failure:** at each gate below, if the stage failed, stop the pipeline and report what
  failed and where, rather than continuing to a stage whose inputs are invalid.

## Stages

### Stage 1 — Bug Researcher (inline)
Survey `src/` (and `tests/` if present). Identify the seeded bugs and security issue. Write
`context/bugs/<id>/research/codebase-research.md` documenting each finding with a precise
`file:line` reference and the relevant source snippet, plus a short root-cause hypothesis per item.

### Stage 2 — Bug Research Verifier (agent)
Dispatch the **`research-verifier`** subagent (via the Agent tool, `subagent_type: research-verifier`).
It reads `codebase-research.md`, checks every `file:line` and snippet against real source, rates
research quality with the `research-quality-measurement` skill, and writes
`context/bugs/<id>/research/verified-research.md`.

**Gate:** read `verified-research.md`. If its Research Quality is below the skill's pass threshold,
or unresolved discrepancies remain, stop and report — do not plan against unverified research.

### Stage 3 — Bug Planner (inline)
From the **verified** research, write `context/bugs/<id>/implementation-plan.md`: for each fix, the
target file, before/after code, and the exact test command to run. Keep it scoped to the verified
findings only.

### Stage 4 — Bug Fixer (agent)
Dispatch the **`bug-fixer`** subagent (`subagent_type: bug-fixer`). It reads
`implementation-plan.md`, applies each change, runs tests after each, and writes
`context/bugs/<id>/fix-summary.md`.

**Gate:** read `fix-summary.md`. If Overall Status is not green (tests failing / a change could not
be applied), stop and report. Stage 5 depends on a clean fix-summary.

### Stage 5 — Security Verifier ∥ Unit Test Generator (parallel)
Both stages depend only on `fix-summary.md` and the changed files, so dispatch them
**in a single message** (two Agent tool calls together) to run concurrently:

- **`security-verifier`** (`subagent_type: security-verifier`) → reads `fix-summary.md` + changed
  files, scans for vulnerabilities, rates each CRITICAL/HIGH/MEDIUM/LOW/INFO with `file:line` +
  remediation, writes `context/bugs/<id>/security-report.md` (report only, no code edits).
- **`unit-test-generator`** (`subagent_type: unit-test-generator`) → generates tests for the
  changed code only (FIRST-compliant via the `unit-tests-FIRST` skill), runs them, and writes
  `context/bugs/<id>/test-report.md` plus the test files.

## Final summary

After Stage 5 completes, print a table of every artifact and each stage's outcome:

| Stage | Agent | Artifact | Status |
|-------|-------|----------|--------|
| 1 Researcher | (inline) | `context/bugs/<id>/research/codebase-research.md` | … |
| 2 Research Verifier | research-verifier | `…/research/verified-research.md` | … |
| 3 Planner | (inline) | `…/implementation-plan.md` | … |
| 4 Bug Fixer | bug-fixer | `…/fix-summary.md` | … |
| 5 Security Verifier | security-verifier | `…/security-report.md` | … |
| 5 Unit Test Generator | unit-test-generator | `…/test-report.md` + tests | … |

Call out any gate that stopped the run and the single next action to unblock it.
