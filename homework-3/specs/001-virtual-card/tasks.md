---
description: "Task list for Virtual Card Lifecycle feature implementation"
---

# Tasks: Virtual Card Lifecycle

**Input**: Design documents from `specs/001-virtual-card/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/operations.md

**Tests**: Test tasks ARE included — a regulated feature treats verification as first-class (Constitution
Principle V). They are documented here as work items, not executed (spec-only homework).

**Organization**: Grouped by user story (US1–US5) for independent implementation/testing. Each `[Story]`
tag traces a task to a spec user story; `[P]` marks tasks that touch different artifacts and can parallelize.

## Format: `[ID] [P?] [Story] Description`

---

## Phase 1: Setup (Shared Infrastructure)

- [ ] T001 Establish the project skeleton (domain/services/api/tests folders) per plan.md Structure.
- [ ] T002 [P] Define money value type as integer minor units + ISO-4217; forbid float amounts.
      **Acceptance**: constructing an amount from a float or mismatched currency fails fast with a typed error.
- [ ] T003 [P] Define role/principal abstraction consuming an external authenticated identity + role claim.
- [ ] T004 [P] Configure structured logging with a redaction filter that drops PAN/CVV-shaped fields.
      **Acceptance**: a log call containing a 16-digit PAN-like value emits a redacted record (no full PAN).

## Phase 2: Foundational (Blocking Prerequisites)

**⚠️ CRITICAL**: No user story work begins until this phase is complete.

- [ ] T005 Implement the `Card` aggregate and the `ACTIVE/FROZEN/CLOSED` state machine with legal-transition guards.
      **Acceptance**: every illegal transition (e.g., `CLOSED→FROZEN`) raises a typed invalid-transition error.
- [ ] T006 Implement append-only `AuditEvent` writer (actor, role, action, before/after, reason, correlation id, UTC).
      **Acceptance**: audit records cannot be updated or deleted; a mutation and its audit write succeed/fail atomically.
- [ ] T007 [P] Implement idempotency middleware: key→result store; replay on retry; conflict on same-key/different-payload.
      **Acceptance**: identical retry yields one effect; divergent payload under the same key returns a conflict.
- [ ] T008 [P] Implement per-card serialization (lock or optimistic version) for mutations.
      **Acceptance**: two concurrent freezes on one card yield one deterministic outcome, both audited.
- [ ] T009 [P] Implement the per-operation authorization gate (fail-closed) with ownership + role checks.
      **Acceptance**: an unauthorized action mutates no state and writes a denial audit event.
- [ ] T010 Implement the sensitive-data boundary: only network token + last-four are persisted/returned.
      **Acceptance**: no code path can read or emit full PAN/CVV; a unit test asserts their absence.

**Checkpoint**: Foundation ready — user stories can proceed.

## Phase 3: User Story 1 — Create a virtual card (P1) 🎯 MVP

**Goal**: Issue an `ACTIVE` card with masked details and a default limit.
**Independent Test**: Create against an eligible account → `ACTIVE` masked card + `CREATE` audit.

- [ ] T011 [P] [US1] Contract test for OP-1 (create) per contracts/operations.md.
- [ ] T012 [P] [US1] Integration test: create emits exactly one `CREATE` audit event.
- [ ] T013 [US1] Implement eligibility + per-user active-card cap check.
      **Acceptance**: exceeding the cap refuses issuance with a clear reason and audits the denied attempt.
- [ ] T014 [US1] Implement create-card handler (default limit, masked output, idempotency key).
      **Acceptance**: returns `ACTIVE` card with last-four only; same-key retry returns the original card.
- [ ] T015 [US1] Validate create inputs (currency, funding account); reject malformed requests with precise errors.

**Checkpoint**: US1 independently functional.

## Phase 4: User Story 2 — Freeze / unfreeze (P1)

**Goal**: Reversible risk control with audited transitions.
**Independent Test**: Freeze → blocks new spend; unfreeze → restores; each audited.

- [ ] T016 [P] [US2] Contract tests for OP-2 (freeze) and OP-3 (unfreeze).
- [ ] T017 [P] [US2] Integration test: freeze blocks a new authorization within the ≤2s window.
- [ ] T018 [US2] Implement freeze handler (owner + ops/compliance force-freeze with reason → `compliance_hold`).
      **Acceptance**: `ACTIVE→FROZEN`; repeat freeze idempotent; force-freeze records operator + reason.
- [ ] T019 [US2] Implement unfreeze handler enforcing the compliance-hold rule.
      **Acceptance**: owner unfreeze against a compliance hold is denied + audited; compliance can release it.
- [ ] T020 [US2] Enforce invalid-transition rejection for freeze/unfreeze on `CLOSED` cards.

**Checkpoint**: US1 + US2 work independently.

## Phase 5: User Story 3 — Set / adjust limits (P2)

**Goal**: Policy-bounded limits with dual control on high-value raises.
**Independent Test**: In-policy set applies + audits; out-of-policy rejected.

- [ ] T021 [P] [US3] Contract test for OP-4 (set/adjust limit).
- [ ] T022 [US3] Implement limit validation (non-negative minor units, currency = card currency, period).
      **Acceptance**: invalid limit rejected with a precise error and no partial update.
- [ ] T023 [US3] Implement limit-change handler applying to subsequent authorizations, audited old→new.
- [ ] T024 [US3] Implement high-value-threshold dual control (`PENDING_SECOND_AUTHORIZER`).
      **Acceptance**: a raise above the threshold is not effective until a second authorizer approves; audited.

**Checkpoint**: US1–US3 work independently.

## Phase 6: User Story 4 — View transactions (P2)

**Goal**: Stable, paginated, masked transaction visibility.
**Independent Test**: List returns ordered paginated page; empty card returns empty page.

- [ ] T025 [P] [US4] Contract test for OP-5 (list transactions).
- [ ] T026 [US4] Implement reverse-chronological pagination (default 25, max 100) with a stable cursor.
      **Acceptance**: ordering is stable across pages; first page p95 < 1 s at assumed scale.
- [ ] T027 [US4] Implement non-owner not-found behavior (existence not leaked) + sensitive-read audit.
      **Acceptance**: a non-owner request returns not-found and writes a `READ_SENSITIVE` denial audit.
- [ ] T028 [US4] Implement explicit empty-state response for cards with no transactions.

## Phase 7: User Story 5 — Ops/compliance oversight (P3)

**Goal**: Audit-trail review + bounded intervention.
**Independent Test**: Compliance views audit trail and force-freezes with reason.

- [ ] T029 [P] [US5] Contract test for OP-7 (view audit trail) — privileged-only.
- [ ] T030 [US5] Implement audit-trail read for ops/compliance (redacted; the read is itself audited).
      **Acceptance**: non-privileged actor is denied + audited; privileged actor gets full ordered history.
- [ ] T031 [US5] Implement OP-6 (close card) terminal transition with idempotent already-closed handling.

## Phase 8: Polish & Cross-Cutting

- [ ] T032 [P] e2e tests covering the US1→US5 golden path from quickstart.md.
- [ ] T033 [P] Negative/edge-case suite: concurrency race, idempotency conflict, currency mismatch, closed-card mutation.
- [ ] T034 Compliance review checkpoint: confirm 0 PAN/CVV leakage and 100% mutation/sensitive-read audit coverage.
      **Acceptance**: reviewer signs off against Success Criteria SC-003 and SC-004.
- [ ] T035 [P] Load check: validate assumed SLOs (create p95 < 800 ms; list p95 < 1 s; freeze visible ≤ 2 s).
- [ ] T036 Reconciliation check: audit event count == count of state-changing operations in the test run.

---

## Dependencies & Execution Order

- **Setup (P1)** → **Foundational (P2)** blocks all stories.
- **US1 (P1)** is the MVP; **US2 (P1)** next; **US3/US4 (P2)** then **US5 (P3)**.
- Within a story: contract/integration tests first → validation → handler → edge cases.
- **Polish (P8)** depends on all targeted stories.

## Traceability (task → requirement)

| Story | Tasks | Requirements |
|---|---|---|
| US1 | T011–T015 | FR-001, FR-002, FR-008, FR-009 |
| US2 | T016–T020 | FR-003, FR-004, FR-008, FR-009, FR-012 |
| US3 | T021–T024 | FR-005, FR-006, FR-011, FR-009 |
| US4 | T025–T028 | FR-007, FR-013, FR-014 |
| US5 | T029–T031 | FR-003, FR-009, FR-010 |
| Cross | T001–T010, T032–T036 | FR-008, FR-009, FR-010, FR-011, FR-012, SC-003, SC-004 |

## Notes

- `[P]` = different artifacts, no dependency. `[Story]` = traceability tag.
- Tasks with **Acceptance** lines carry a checkable definition-of-done (Constitution Principle V).
- No code is produced in this homework; tasks document the executable decomposition.
