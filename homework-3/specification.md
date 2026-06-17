# Virtual Card Lifecycle — Specification

> Ingest the information in this file to plan and (in a future project) implement the Low-Level Tasks
> that satisfy the High- and Mid-Level Objectives. This spec is **specification-only**: it is designed so
> an engineering team or AI agent could execute it **without guessing**. It was produced with
> GitHub **Spec Kit** (Spec-Driven Development); the source artifacts live in
> [`specs/001-virtual-card/`](specs/001-virtual-card/) and the project principles in
> [`.specify/memory/constitution.md`](.specify/memory/constitution.md).

---

## 1. High-Level Objective

Enable an end-user of a regulated FinTech product to **own and control a virtual card across its full
lifecycle** — create it, freeze/unfreeze it, set and adjust its spending limits, and view its
transactions — while giving internal **ops/compliance** the oversight and intervention they need.

**Scope boundary (one sentence)**: This feature covers card *lifecycle and oversight* (state, limits,
visibility, audit); it does **not** implement authentication, card-network settlement, or any UI.

---

## 2. Mid-Level Objectives *(observable "what")*

Each objective states the observable change in the world and (in §6) how it is verified.

- **MO-1 Issuance**: A user can create an `ACTIVE` virtual card on an eligible funding account and
  immediately see its masked details (last-four only) and a default spending limit.
- **MO-2 Reversible risk control**: A user (or ops/compliance) can freeze a card to block new
  authorizations and later unfreeze it, with every transition constrained by a state machine.
- **MO-3 Exposure control**: A user can set/adjust per-transaction and periodic limits within policy;
  raises above a high-value threshold require a second authorizer.
- **MO-4 Visibility**: A user can view a paginated, reverse-chronological, masked list of a card's
  transactions, including an explicit empty state.
- **MO-5 Oversight**: Ops/compliance can review a card's full audit trail and force-freeze a card under
  investigation, bounded by least-privilege.
- **MO-6 Evidentiary integrity (cross-cutting)**: Every state change and sensitive read produces an
  immutable audit record; no mutation is unaudited.

---

## 3. Non-Functional & Policy Requirements *(how well / how safely)*

> Targets are **assumed** (no production telemetry yet) and chosen to be FinTech-reasonable; they bound
> UX and abuse without over-promising. See `specs/001-virtual-card/research.md` (R8) for justification.

| Concern | Requirement / Target |
|---|---|
| **Security (PCI)** | Full PAN/CVV **never** stored, returned, or logged; only network token + last-four. |
| **Privacy / least disclosure** | Sensitive fields role-gated; every sensitive read is audited; fail-closed on uncertainty. |
| **Auditability** | Append-only, immutable audit for 100% of mutations + sensitive reads; retained ~7 years (assumed regulatory minimum). |
| **Reliability / integrity** | Idempotent writes; per-card serialization; money as integer minor units + ISO-4217. |
| **Latency** | Card create p95 < 800 ms; transaction list first page p95 < 1 s. |
| **Consistency** | Read-after-write visible ≤ 2 s; freeze blocks new authorizations ≤ 2 s. |
| **Pagination** | Default page size 25, max 100; stable cursor ordering. |
| **Rate limiting** | Assumed 10 mutations/card/min to throttle fraud-ish cycling. |
| **Authorization** | Evaluated per operation; segregation of duties; dual control on high-value raises. |

---

## 4. Implementation Notes *(guardrails an agent must not violate)*

- **Money**: integer minor units + explicit ISO-4217 on every amount. Never floating point. Limit
  currency must equal card currency.
- **Sensitive data**: only `network_token` + `pan_last_four` are persistable/returnable. No code path may
  read or emit full PAN/CVV. Logging must run through a redaction filter.
- **Idempotency**: every mutating operation requires a client idempotency key. Retry with the same key
  replays the original result; **same key + different payload → conflict** (do not silently mask it).
- **State machine**: states `ACTIVE`/`FROZEN`/`CLOSED`; legal transitions `ACTIVE↔FROZEN`,
  `ACTIVE→CLOSED`, `FROZEN→CLOSED`. `CLOSED` is terminal. Illegal transitions are typed errors, audited.
- **Concurrency**: serialize mutations per `card_id` (lock or optimistic version) → deterministic,
  individually-audited outcomes. Never last-writer-wins on financial state.
- **Authorization**: per-operation, fail-closed. End-users act only on their own cards. A compliance
  `compliance_hold` overrides the owner (owner cannot unfreeze while it stands).
- **Audit**: the mutation and its audit write succeed or fail together (atomic). Audit records are never
  updated or deleted. Include actor, role, action, before/after (redacted), reason, correlation id, UTC.
- **Error semantics**: precise, user-actionable messages; no partial updates on validation failure;
  return *not-found* (not *forbidden*) to non-owners so existence is not leaked.

---

## 5. Context

### 5.1 Beginning context (what exists before work starts)

- An external **identity provider** issuing an authenticated principal with a role claim
  (`end-user` / `ops` / `compliance`).
- An external **card network/processor** that issues card credentials and reports authorizations.
- A **funding-account / eligibility** service that can be queried.
- An **append-only audit store** and a **card state store** are available.
- Specification artifacts already produced (this homework): the files under
  `specs/001-virtual-card/` and the constitution in `.specify/memory/`.

### 5.2 Ending context (what exists after the feature is built — hypothetical for this homework)

- A lifecycle service exposing the operations in `specs/001-virtual-card/contracts/operations.md`
  (create, freeze, unfreeze, set-limit, list-transactions, close, view-audit).
- A `Card` aggregate + state machine, `SpendingLimit`, read-only `Transaction` view, and immutable
  `AuditEvent` log (see `specs/001-virtual-card/data-model.md`).
- A test suite (unit/integration/e2e) plus compliance + reconciliation checkpoints proving the Success
  Criteria, and SLO measurements validating the assumed performance targets.

---

## 6. Verification *(how we know each objective is met)*

| Objective | Verification |
|---|---|
| MO-1 | Integration test: create → `ACTIVE` masked card + `CREATE` audit; same-key retry returns one card. |
| MO-2 | Integration test: freeze blocks a new authorization ≤ 2 s; unfreeze restores; transitions audited. |
| MO-3 | Unit test: in-policy limit applied + audited old→new; out-of-policy rejected; high-value raise held for second authorizer. |
| MO-4 | Integration test: paginated newest-first stable ordering; empty card → explicit empty page; first page p95 < 1 s. |
| MO-5 | Integration test: compliance reads full audit trail; force-freeze sets `compliance_hold`; owner unfreeze denied + audited. |
| MO-6 | Reconciliation: audit-event count == state-changing-op count; 0 PAN/CVV occurrences across all fixtures. |

Verification categories used: **unit** (validation, state machine), **integration** (authorization +
audit + idempotency), **e2e** (US1→US5 golden path, see `specs/001-virtual-card/quickstart.md`),
**reconciliation** (audit vs. operations), and **manual compliance review** (PAN/CVV leakage, audit
coverage). The full task list with checkable acceptance criteria is in
`specs/001-virtual-card/tasks.md`; the cross-artifact consistency proof is in
`specs/001-virtual-card/analysis-report.md`.

---

## 7. Edge Cases & Failure Modes

| # | Scenario | Expected behavior | Audit / compliance implication |
|---|---|---|---|
| E1 | Two freeze/limit requests race on one card | Serialized per card; deterministic outcome | Each attempt audited separately |
| E2 | Same idempotency key, different payload | Reject as conflict (no replay) | Conflict attempt audited |
| E3 | Read immediately after a write | Reflects the write within ≤ 2 s, or marked pending | n/a |
| E4 | Limit currency ≠ card currency | Reject with precise validation error; no partial update | Rejection audited |
| E5 | Owner tries to unfreeze a compliance hold | Denied | Denial audited with actor/role |
| E6 | Any mutating action on a `CLOSED` card | Invalid-transition error | Audited |
| E7 | List/view of non-existent or not-owned card | Return not-found (existence not leaked) | Sensitive-read denial audited |
| E8 | End-user attempts an ops-only action | Denied, no state change | Denial audited |
| E9 | Rapid issue→spend→freeze cycling beyond rate limit | Throttled and flagged | Flag recorded for fraud review |
| E10 | Limit set to negative / zero where disallowed / non-minor-unit | Rejected with precise error | Rejection audited |

---

## 8. Expected Performance *(assumed targets — labeled, with rationale)*

| Metric | Target | Why reasonable for FinTech |
|---|---|---|
| Card create | p95 < 800 ms | Issuance is interactive; sub-second keeps onboarding fluid. |
| Freeze → block new authorizations | ≤ 2 s | A risk-control action must take effect near-instantly to be trusted. |
| Transaction list (first page) | p95 < 1 s | List is a high-frequency read; >1 s feels broken. |
| Read-after-write visibility | ≤ 2 s | Users expect their just-made change to be reflected. |
| Pagination | 25 default / 100 max | Bounds payload size and protects the backing store. |
| Write rate limit | 10 mutations/card/min | Throttles abusive/fraud-ish cycling without hurting normal use. |

All numbers are **assumed targets** pending production telemetry; they are conservative and revisable
once real measurements exist (see `research.md` R8).

---

## 9. Low-Level Tasks

The complete, traceable task decomposition (T001–T036, grouped by user story, several ending with
explicit acceptance criteria / definition-of-done) is maintained as the Spec Kit Phase-2 artifact:

➡️ **[`specs/001-virtual-card/tasks.md`](specs/001-virtual-card/tasks.md)**

A condensed view of the executable slices:

1. **Setup** (T001–T004): skeleton; money value type; principal/role abstraction; redacted logging.
2. **Foundational** (T005–T010): state machine; append-only audit writer; idempotency; per-card
   serialization; per-operation authorization; sensitive-data boundary. *(Acceptance criteria attached.)*
3. **US1 Create** (T011–T015): eligibility + cap; create handler; input validation.
4. **US2 Freeze/Unfreeze** (T016–T020): freeze + force-freeze; unfreeze with hold rule; invalid-transition guard.
5. **US3 Limits** (T021–T024): validation; change handler; high-value dual control.
6. **US4 Transactions** (T025–T028): pagination; non-owner not-found + audit; empty state.
7. **US5 Oversight** (T029–T031): audit-trail read; close card.
8. **Polish** (T032–T036): e2e + edge-case suites; compliance review; load check; reconciliation.

Each task carries an `[Story]` traceability tag and several carry an **Acceptance** line so an
implementer can check them off (Constitution Principle V).

---

## 10. Traceability Summary

Goals → tasks → verification are linked end-to-end: High-Level Objective → Mid-Level Objectives (§2) →
Functional Requirements (`spec.md` FR-001..014) → Operation contracts (`contracts/operations.md`
OP-1..7) → Tasks (`tasks.md` T001..T036) → Verification (§6) and the consistency proof in
`analysis-report.md`. The README explains *why* these choices were made and where each industry practice
appears.
