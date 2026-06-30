# Homework 4 — 4-Agent Bug Pipeline

**Author:** Vlad Bairak  
**GitHub:** [VladislavBI](https://github.com/VladislavBI)  
**Date:** 2026-06-17

---

## Overview

This homework implements a **4-agent agentic pipeline** that automatically finds, fixes, reviews, and
tests bugs in a small .NET application. A single `/pipeline` command drives all six stages:

```
Bug Researcher → Research Verifier → Bug Planner → Bug Fixer → Security Verifier ─┐
                                                                                    ├─ (parallel)
                                                                          Unit Test Generator ─┘
```

The pipeline operates on a seeded sample application (`src/SampleApp`) that contains 2 intentional
logic bugs and 1 intentional security issue, producing concrete before/after results.

---

## Sample Application

`src/SampleApp` is a .NET 10 console CLI with two subsystems:

| Subsystem | File | Role |
|---|---|---|
| Pricing | `Pricing/OrderCalculator.cs` | Discount + tax calculation |
| Auth | `Auth/TokenAuthenticator.cs` | Admin token verification |

### Seeded flaws (pre-pipeline)

| ID | Type | Location | Description |
|---|---|---|---|
| Bug 1 | Logic | `OrderCalculator.GetDiscountRate` | Off-by-one: `> 10` instead of `>= 10` — 10 units gets no discount |
| Bug 2 | Logic | `OrderCalculator.CalculateTotal` | Tax applied on pre-discount subtotal — customers overcharged |
| Sec 1 | Security | `TokenAuthenticator.IsAdmin` | Hardcoded secret + non-constant-time `==` + missing null guard |

---

## The 4 Agents

| Agent | File | Model | Rationale |
|---|---|---|---|
| **Research Verifier** | `.claude/agents/research-verifier.md` | `claude-opus-4-8` | Requires careful reasoning to verify every file:line reference and rate research quality accurately |
| **Bug Fixer** | `.claude/agents/bug-fixer.md` | `claude-sonnet-4-6` | Routine plan execution — reads implementation-plan.md and applies edits; speed matters more than depth |
| **Security Verifier** | `.claude/agents/security-verifier.md` | `claude-opus-4-8` | Security review demands broad vulnerability knowledge and precise severity reasoning |
| **Unit Test Generator** | `.claude/agents/unit-test-generator.md` | `claude-sonnet-4-6` | Test scaffolding is well-structured work; fast model keeps iteration cycles short |

---

## Skills

| Skill | File | Used by |
|---|---|---|
| Research Quality Measurement | `skills/research-quality-measurement/SKILL.md` | Research Verifier |
| FIRST Principles | `skills/unit-tests-first/SKILL.md` | Unit Test Generator |

---

## How to Run

See [HOWTORUN.md](HOWTORUN.md) for the full step-by-step runbook.

**Quick start:**
```powershell
# Build and verify tests pass (baseline — buggy code)
dotnet build src/SampleApp.slnx
dotnet test src/SampleApp.slnx

# Demonstrate a bug
dotnet run --project src/SampleApp -- total 10 100

# Run the entire pipeline on bug 001
/pipeline 001
```

---

## Pipeline Outputs

After `/pipeline 001` completes, artifacts appear under `context/bugs/001/`:

| File | Produced by |
|---|---|
| `research/codebase-research.md` | Bug Researcher (inline) |
| `research/verified-research.md` | Research Verifier agent |
| `implementation-plan.md` | Bug Planner (inline) |
| `fix-summary.md` | Bug Fixer agent |
| `security-report.md` | Security Verifier agent |
| `test-report.md` | Unit Test Generator agent |

---

## AI Tools Used

- **Claude Code** (Anthropic) — agent definitions, skills, pipeline orchestration, and this sample app were built with Claude Code using the `/pipeline` and agent-dispatch patterns.
- All four agents are defined in `.claude/agents/` and are invoked automatically by the `/pipeline` skill.
