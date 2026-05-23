# Phase 1 Operation Contracts: Virtual Card Lifecycle

Technology-agnostic operation contracts (no code, no transport prescribed). Every mutating operation
takes an **idempotency key** and emits an **audit event**. All amounts are integer minor units +
ISO-4217. Authorization is evaluated **per operation**; denials are audited and never mutate state.

## OP-1 Create Card → FR-001, FR-002, FR-008, FR-009

- **Actor**: end-user (own funding account).
- **Input**: `funding_account_ref`, `currency`, optional `initial_limit`, `idempotency_key`.
- **Output**: `card_id`, `pan_last_four`, `network_token`, `state=ACTIVE`, effective limit.
- **Errors**: ineligible account; per-user active-card cap reached; invalid currency.
- **Acceptance**: returns `ACTIVE` card with masked details; retry with same key returns the same card;
  audit event `CREATE` written. **Never** returns full PAN/CVV.

## OP-2 Freeze Card → FR-003, FR-004, FR-008, FR-009, FR-012

- **Actor**: owner, or ops/compliance (force-freeze with `reason`).
- **Input**: `card_id`, optional `reason`, `idempotency_key`.
- **Output**: `state=FROZEN` (`compliance_hold=true` when force-freeze).
- **Errors**: card `CLOSED` (invalid transition); not owner and not authorized.
- **Acceptance**: `ACTIVE→FROZEN`; new authorizations blocked ≤ 2 s; repeat freeze is idempotent; audit
  event `FREEZE` written with actor/role and reason when applicable.

## OP-3 Unfreeze Card → FR-003, FR-004, FR-009

- **Actor**: owner (only when no compliance hold), or compliance (to release its own hold).
- **Input**: `card_id`, `idempotency_key`.
- **Output**: `state=ACTIVE`.
- **Errors**: `compliance_hold=true` and actor is owner → denied; card `CLOSED` → invalid transition.
- **Acceptance**: `FROZEN→ACTIVE` only when permitted; owner attempt against a compliance hold is denied
  and audited; audit event `UNFREEZE` written.

## OP-4 Set / Adjust Limit → FR-005, FR-006, FR-011, FR-009

- **Actor**: owner; high-value raise requires a second authorizer (ops/compliance).
- **Input**: `card_id`, `per_transaction?`, `periodic_amount?`, `period?`, `idempotency_key`.
- **Output**: updated effective limit (or `PENDING_SECOND_AUTHORIZER` for high-value raises).
- **Errors**: negative/zero-where-disallowed; currency mismatch; non-minor-unit amount; card `CLOSED`.
- **Acceptance**: in-policy change applied to subsequent authorizations and audited with old→new; raise
  above threshold held pending second authorizer; invalid input rejected with no partial update.

## OP-5 List Transactions → FR-007, FR-013, FR-014

- **Actor**: owner, or ops/compliance within remit.
- **Input**: `card_id`, `page_size` (default 25, max 100), `cursor?`.
- **Output**: reverse-chronological page with stable ordering; masked card reference; next cursor.
- **Errors**: not owner/authorized → not-found (do not leak existence); unknown card → not-found.
- **Acceptance**: paginated newest-first with stable ordering across pages; empty card returns explicit
  empty page (not an error); first page p95 < 1 s; sensitive-read audited.

## OP-6 Close Card → FR-003, FR-008, FR-009

- **Actor**: owner or ops/compliance.
- **Input**: `card_id`, `idempotency_key`.
- **Output**: `state=CLOSED` (terminal).
- **Errors**: already `CLOSED` → idempotent no-op returning `CLOSED`.
- **Acceptance**: `ACTIVE/FROZEN→CLOSED`; subsequent mutating ops rejected as invalid transition; audit
  event `CLOSE` written.

## OP-7 View Audit Trail → FR-009, FR-010, Principle I/II

- **Actor**: ops/compliance only.
- **Input**: `card_id`, pagination.
- **Output**: ordered, immutable audit history; redacted (no PAN/CVV).
- **Errors**: non-privileged actor → denied + audited.
- **Acceptance**: full ordered history returned to authorized roles; the read itself is audited.

## Cross-cutting contract rules

- **Idempotency**: same key replays original result; same key + different payload → conflict.
- **Concurrency**: mutations on one `card_id` are serialized → deterministic, individually-audited outcomes.
- **Authorization**: evaluated per operation; failure → deny + audit + no state change (fail-closed).
- **Money**: integer minor units + ISO-4217 everywhere; mismatch rejected.
