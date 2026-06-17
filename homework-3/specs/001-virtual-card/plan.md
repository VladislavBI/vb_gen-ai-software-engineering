# Implementation Plan: Virtual Card Lifecycle

**Branch**: `001-virtual-card` | **Date**: 2026-05-23 | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `specs/001-virtual-card/spec.md`

## Summary

Specify the lifecycle of a virtual card — create, freeze/unfreeze, set/adjust limits, view transactions —
for a regulated FinTech context, with end-user and ops/compliance stakeholders. The technical approach
models a per-card state machine guarded by per-operation authorization, idempotent writes keyed by client
idempotency keys, money as integer minor units, and an append-only audit trail for every mutation and
sensitive read. **No application code is produced**; this plan defines the design context an engineering
team (or AI agent) would execute against.

## Technical Context

**Language/Version**: Not prescribed (spec-only). Reference target for implementers: a strongly-typed,
service-style backend (e.g., .NET / ASP.NET Core minimal API) — chosen only to make examples concrete.

**Primary Dependencies**: External identity provider (authN + role claim); external card
network/processor (issuance, authorization, settlement); funding-account/eligibility service.

**Storage**: Card + limit state store with per-card serialization; append-only audit store. Full
PAN/CVV are NOT stored (network token + last-four only).

**Testing**: Documentation-level test categories — unit (validation, state machine), integration
(authorization + audit), e2e (user journeys), plus reconciliation/compliance review checkpoints.

**Target Platform**: Server-side service in a regulated (PCI DSS) environment.

**Project Type**: Backend service feature (lifecycle + oversight), no UI in scope.

**Performance Goals** *(assumed targets — FinTech-reasonable, see Constraints)*: card create p95 < 800 ms;
freeze→authorization-block visible ≤ 2 s; transaction list first page p95 < 1 s.

**Constraints**: read-after-write visibility ≤ 2 s; transaction listing default page size 25 (max 100);
write rate-limit assumed 10 mutations/card/min; audit retention assumed 7 years; fail-closed on
authorization/validation uncertainty.

**Scale/Scope**: Assumed up to 5 active cards/user, tens of thousands of users, transaction history
paginated; figures labeled assumed and used to justify pagination/rate limits.

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | How this design upholds it | Status |
|---|---|---|
| I. Audit-First & Traceability | Every transition + sensitive read emits an append-only `AuditEvent` (actor, role, before/after, correlation id). | PASS |
| II. Data Protection & Least Disclosure | Only network token + last-four persisted/returned; PAN/CVV never logged or stored; sensitive reads audited. | PASS |
| III. Idempotency & Money Integrity | All writes keyed by idempotency key; amounts are integer minor units + ISO-4217. | PASS |
| IV. Least-Privilege & Segregation | Per-operation authorization; compliance force-freeze + second-authorizer for high-value raises. | PASS |
| V. Verifiable Quality | Mid-level objectives carry verification; tasks carry acceptance criteria; SLOs numeric (above). | PASS |

No violations → Complexity Tracking is empty.

## Project Structure

### Documentation (this feature)

```text
specs/001-virtual-card/
├── plan.md              # This file
├── research.md          # Phase 0 — decisions & rationale
├── data-model.md        # Phase 1 — entities, states, invariants
├── quickstart.md        # Phase 1 — how an implementer validates the feature
├── contracts/           # Phase 1 — per-operation contracts (no code)
│   └── operations.md
├── checklists/
│   └── requirements.md  # spec-quality checklist
└── tasks.md             # Phase 2 — /speckit-tasks output
```

### Source Code (repository root)

```text
# Illustrative target layout for an implementing team (NOT created by this homework):
src/
├── domain/            # Card, SpendingLimit, AuditEvent, state machine
├── services/          # lifecycle service, authorization, audit writer, idempotency
└── api/               # create/freeze/unfreeze/limit/list operation handlers

tests/
├── unit/              # state machine + validation
├── integration/       # authorization + audit + idempotency
└── e2e/               # user journeys US1–US5
```

**Structure Decision**: Documentation-only. The layout above is advisory context for an implementer so
the spec reads "without guessing"; this homework delivers the `specs/` artifacts, not `src/`.

## Complexity Tracking

> No Constitution Check violations — section intentionally empty.
