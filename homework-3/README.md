# Homework 3 — Specification-Driven Design: Virtual Card Lifecycle

> **Student**: Vlad Bairak
> **Date**: 2026-05-23
> **Method**: GitHub **Spec Kit** (Spec-Driven Development)
> **AI tools**: Claude Code (Opus) driving the Spec Kit workflow

## Student & task summary

This homework delivers a **specification package** (no code) for a regulated FinTech feature: the
**virtual card lifecycle** — create a card, freeze/unfreeze it, set/adjust spending limits, and view its
transactions — for two stakeholder groups, **end-users** and **internal ops/compliance**. The graded
artifact is the depth and traceability of the specification itself.

I produced it with **GitHub Spec Kit**, running the Spec-Driven Development workflow end to end
(constitution → specify → clarify → plan → tasks → checklist → analyze; `implement` intentionally
skipped because no code is required). The native Spec Kit artifacts are the working source of truth and
the AI-usage evidence; the four required deliverables are assembled from them.

### Deliverables

| Deliverable | File |
|---|---|
| 1. Layered specification | [`specification.md`](specification.md) |
| 2. Agent guidelines | [`agents.md`](agents.md) |
| 3. Editor/AI rules (Claude) | [`.claude/rules.md`](.claude/rules.md) |
| 4. This write-up | `README.md` |
| Runbook | [`HOWTORUN.md`](HOWTORUN.md) |

### Spec Kit artifacts (methodology + AI-usage evidence)

| Phase | Artifact |
|---|---|
| Constitution | [`.specify/memory/constitution.md`](.specify/memory/constitution.md) |
| Specify (+ clarify) | [`specs/001-virtual-card/spec.md`](specs/001-virtual-card/spec.md) |
| Plan (+ research/data-model/contracts/quickstart) | [`specs/001-virtual-card/plan.md`](specs/001-virtual-card/plan.md) |
| Tasks | [`specs/001-virtual-card/tasks.md`](specs/001-virtual-card/tasks.md) |
| Checklists | [`specs/001-virtual-card/checklists/`](specs/001-virtual-card/checklists/) |
| Analyze report | [`specs/001-virtual-card/analysis-report.md`](specs/001-virtual-card/analysis-report.md) |

> **Note on process choice**: per agreement, this homework uses the **Spec Kit workflow** in place of the
> course's `PLAN.md` / `plans/milestone-N.md` machinery. The Spec Kit artifacts above are the planning
> and AI-usage evidence. All other course conventions (PowerShell-first runbook, PR process) are honored.

## Rationale — why the specification is written this way

- **Layering for "execute without guessing"**: the spec separates *intent* (High-Level → Mid-Level
  Objectives) from *guardrails* (Implementation Notes), *workspace* (Beginning/Ending Context), and
  *executable slices* (Low-Level Tasks). This mirrors the course template but goes well beyond it with a
  dedicated **Non-Functional & Policy** section, an **Edge-Case/Failure-Mode table**, an integrated
  **Verification** section, and **labeled performance targets**.
- **Constitution first**: I started with a project constitution (five non-negotiable principles:
  audit-first, data protection, idempotency/money integrity, least privilege, verifiable quality). Every
  downstream artifact is checked against it (see the plan's *Constitution Check* gate), which is what
  keeps requirements traceable from goals to tasks.
- **How I chose performance targets**: there is no production telemetry, so every number is a **labeled
  assumed target** with a one-line justification (e.g., freeze must take effect ≤ 2 s because a
  risk-control action users reach for must feel instant). Rationale is captured in
  `specs/001-virtual-card/research.md` (R8) and surfaced in `specification.md` §3 and §8.
- **How I chose verification depth**: because this is a regulated feature, verification is treated as a
  first-class principle, not an afterthought. Each mid-level objective has a verification path
  (`specification.md` §6), many low-level tasks carry explicit **Acceptance** lines
  (`specs/001-virtual-card/tasks.md`), and a reconciliation check ties audit-event count to the number of
  state-changing operations.
- **Edge cases scoped to the feature**: rather than a generic security essay, the failure-mode table is
  specific to virtual cards (concurrent transitions, idempotency-key reuse, compliance-hold override,
  closed-card mutation, existence-leak prevention).

## Industry best practices — what I added and where it appears

| Best practice | Where it appears |
|---|---|
| **PCI DSS data minimization** (never store/log full PAN/CVV; token + last-four only) | Constitution Principle II; `specification.md` §3–§4; spec FR-002; `research.md` R3; checklist CHK001–004 |
| **Immutable, append-only audit trail** (actor/role/before-after/correlation id) | Constitution Principle I; `data-model.md` AuditEvent; spec FR-009; tasks T006; checklist CHK005–008 |
| **Idempotency keys** for safe retries | Constitution Principle III; `specification.md` §4; spec FR-008; `research.md` R2; tasks T007 |
| **Money as integer minor units + ISO-4217** (no floats) | Constitution Principle III; `specification.md` §4; spec FR-011; `research.md` R4; tasks T002 |
| **Least privilege & segregation of duties** (per-operation authz, compliance override) | Constitution Principle IV; spec FR-004/FR-010; `contracts/operations.md`; tasks T009 |
| **Dual control / maker-checker** on high-value changes | spec FR-006; `contracts/operations.md` OP-4; tasks T024; checklist CHK015 |
| **Fail-closed defaults** | Constitution Security section; `specification.md` §4; `agents.md` §3 |
| **Existence privacy** (not-found vs forbidden) | spec FR-014; `contracts/operations.md` OP-5; tasks T027 |
| **Explicit state machine** with guarded transitions | `data-model.md`; spec FR-003; tasks T005 |
| **Concurrency control** (per-card serialization) | Constitution Principle III; spec FR-012; `research.md` R6; tasks T008 |
| **SLOs as first-class, numeric, labeled-assumed targets** | `specification.md` §3/§8; `plan.md` Performance Goals; `research.md` R8 |
| **Defined data retention** (~7 years, regulatory) | Constitution Security section; spec Assumptions |

## Repository map

```text
homework-3/
├── specification.md          # Deliverable 1 — layered spec
├── agents.md                 # Deliverable 2 — agent guidelines
├── .claude/rules.md          # Deliverable 3 — editor/AI rules
├── README.md                 # Deliverable 4 — this file
├── HOWTORUN.md               # Spec Kit runbook
├── .specify/memory/constitution.md   # project principles
├── specs/001-virtual-card/   # Spec Kit artifacts (spec, plan, tasks, contracts, analyze)
├── docs/screenshots/         # AI-interaction + analyze evidence
└── demo/                     # the Spec Kit prompts/commands used
```
