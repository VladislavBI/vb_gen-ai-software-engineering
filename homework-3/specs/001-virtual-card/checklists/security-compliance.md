# Security & Compliance Checklist: Virtual Card Lifecycle

**Purpose**: Validate that the spec encodes FinTech security, compliance, and verification expectations
before the package is considered complete.
**Created**: 2026-05-23
**Feature**: [spec.md](../spec.md)

## Data Protection (Constitution II)

- [x] CHK001 Full PAN/CVV are never stored, returned, or logged (FR-002, R3, T010).
- [x] CHK002 Only network token + last-four are exposed (data-model Card invariants).
- [x] CHK003 Sensitive reads are audited (FR-009, OP-7, T027/T030).
- [x] CHK004 Logging has a redaction filter for PAN/CVV-shaped fields (T004).

## Auditability (Constitution I)

- [x] CHK005 Every state-changing action emits an append-only audit event (FR-009, OP-1..6, T006).
- [x] CHK006 Audit records are immutable (no update/delete) (data-model AuditEvent invariants).
- [x] CHK007 Mutation + audit write are atomic (T006 acceptance).
- [x] CHK008 Reconciliation: audit count == state-changing op count (T036, SC-003).

## Integrity & Idempotency (Constitution III)

- [x] CHK009 All mutations are idempotent under a client key (FR-008, R2, T007).
- [x] CHK010 Same key + different payload → conflict (contracts cross-cutting, T007 acceptance).
- [x] CHK011 Money is integer minor units + ISO-4217; floats rejected (FR-011, R4, T002).
- [x] CHK012 Concurrent per-card mutations are serialized deterministically (FR-012, R6, T008).

## Access Control (Constitution IV)

- [x] CHK013 Authorization is per-operation and fail-closed (FR-010, R5, T009).
- [x] CHK014 Compliance force-freeze overrides owner; owner cannot unfreeze a hold (FR-004, OP-2/3).
- [x] CHK015 High-value limit raises require a second authorizer (FR-006, OP-4, T024).
- [x] CHK016 Non-owner card access returns not-found (existence not leaked) (FR-014, OP-5, T027).

## Verification & Performance (Constitution V)

- [x] CHK017 Each mid-level objective has a verification path (spec Success Criteria, quickstart).
- [x] CHK018 Several tasks carry explicit acceptance criteria (tasks.md Acceptance lines).
- [x] CHK019 Performance targets are numeric and labeled assumed (plan Performance Goals, R8).
- [x] CHK020 Edge cases enumerate expected behavior + audit implication (spec Edge Cases).

## Notes

- All items verified against spec.md, plan.md, research.md, data-model.md, contracts/operations.md,
  and tasks.md as of 2026-05-23. No open items.
