# Phase 1 Quickstart: Validating the Virtual Card Lifecycle

How an implementing team (or AI agent) would confirm the feature is correctly built. This is a
**validation guide for the spec**, not runnable code (no implementation in this homework).

## Preconditions (beginning context)

- An authenticated principal with a role claim (`end-user` / `ops` / `compliance`) is available.
- An eligible funding account exists for the test end-user.
- An audit store (append-only) and a card state store are reachable.

## Golden-path walkthrough (maps to user stories)

1. **Create (US1)** — as end-user, create a card on the eligible account → expect `ACTIVE`, masked
   last-four, default limit, and a `CREATE` audit event. Re-send with the same idempotency key → same card.
2. **Freeze (US2)** — freeze the card → expect `FROZEN`; a new authorization attempt is declined within
   the consistency window; `FREEZE` audit event present.
3. **Unfreeze (US2)** — unfreeze → expect `ACTIVE`; `UNFREEZE` audit event present.
4. **Set limit (US3)** — set an in-policy limit → applied + `SET_LIMIT` audit with old→new. Attempt a
   raise above the high-value threshold → `PENDING_SECOND_AUTHORIZER`.
5. **List transactions (US4)** — list → paginated newest-first, masked card reference; empty card →
   explicit empty page.
6. **Oversight (US5)** — as compliance, view the audit trail and force-freeze with a reason → card
   `FROZEN` with `compliance_hold`; owner unfreeze attempt denied + audited.

## Failure-path checks (maps to Edge Cases)

- Mutate a `CLOSED` card → invalid-transition error, audited.
- Same idempotency key + different payload → conflict.
- Limit in wrong currency / negative → precise validation error, no partial update.
- Non-owner lists another user's card → not-found (existence not leaked), audited.

## Definition of done (feature-level)

- All acceptance scenarios in `spec.md` pass; all contracts in `contracts/operations.md` satisfied.
- 0 occurrences of full PAN/CVV anywhere (logs, responses, audit, storage).
- 100% of mutations and sensitive reads produce retrievable audit events.
- SLO targets met under the assumed scale (create p95 < 800 ms; freeze visible ≤ 2 s; list p95 < 1 s).
