---
name: research-quality-measurement
description: Scale and procedure the research-verifier agent uses to rate the quality of a Bug Researcher's codebase-research.md (levels A–F) when writing verified-research.md. Private to that agent; not registered as a runtime skill.
---

# Skill: Research Quality Measurement

> **Scope:** Used **only** by the `research-verifier` agent when writing `verified-research.md`.
> It lives under `homework-4/skills/` (not `.claude/skills/`) so it is not loaded into the
> normal session flow — it is private to the agent that reads it.

## Purpose

Define a consistent, repeatable scale for rating the quality of a Bug Researcher's
`codebase-research.md`. The verifier assigns exactly one **Research Quality level** based on
how well the research holds up to verification (accurate references, matching snippets,
evidence-backed claims, coverage of the bug).

## Quality Levels

The verifier MUST select exactly one level and justify it with evidence.

| Level | Label | Criteria (all of the line's conditions must hold) |
|-------|-------|----------------------------------------------------|
| **A** | Excellent | Every file:line reference resolves and every snippet matches source verbatim; every claim is backed by cited evidence; root-cause hypothesis is specific and supported; no discrepancies found. |
| **B** | Good | ≥90% of references resolve and snippets match; all material claims are evidenced; at most minor/cosmetic discrepancies (e.g. an off-by-one line number) that do not change conclusions. |
| **C** | Adequate | ≥70% of references resolve; the core root-cause claim is supported, but some secondary claims are unverified or some snippets are paraphrased rather than exact; discrepancies exist but are documented and non-blocking. |
| **D** | Weak | <70% of references resolve, OR a central claim is unsupported/contradicted by source, OR snippets do not match. Research cannot be trusted by the Bug Planner without rework. |
| **F** | Failing | Research is largely fabricated, references do not exist, or the described symptom/root cause is contradicted by the actual code. |

## Scoring Procedure

1. **Enumerate every claim and reference** in `codebase-research.md`.
2. For each reference, open the cited file and confirm the line range and that the quoted
   snippet matches the source exactly. Record match / mismatch / not-found.
3. For each claim, confirm it is supported by cited evidence (not assertion).
4. Compute the reference-resolution rate and note any unsupported claims.
5. Map the results to the **highest level whose criteria are fully satisfied** — if any
   condition for a level fails, drop to the next lower level.

## How to Report

In `verified-research.md`, the **Research Quality Assessment** section must state:

- **Level:** the single letter + label (e.g. `B — Good`).
- **Reasoning:** 2–4 sentences citing the concrete evidence that pinned the level
  (resolution rate, which claims were/weren't supported, which discrepancies mattered).
- The **Verification Summary** section must restate this level next to the pass/fail verdict.

A `PASS` verdict requires level **C or higher** with no unresolved blocking discrepancies.
Levels **D**/**F** are an automatic `FAIL`.
