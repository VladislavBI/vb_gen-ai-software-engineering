# Phase 0 Research: Virtual Card Lifecycle

Decisions and rationale that resolve open questions before design. Each entry: **Decision**,
**Rationale**, **Alternatives considered**.

## R1 — Card state model

- **Decision**: Finite states `ACTIVE`, `FROZEN`, `CLOSED`; transitions `ACTIVE↔FROZEN`,
  `ACTIVE→CLOSED`, `FROZEN→CLOSED`. `CLOSED` is terminal.
- **Rationale**: Smallest set that covers issuance, reversible risk control, and irreversible
  termination; an explicit state machine makes invalid transitions a first-class, auditable error.
- **Alternatives**: Adding `PENDING`/`EXPIRED` — deferred; not needed for the chosen scope and would
  dilute the lifecycle without adding graded value.

## R2 — Idempotency strategy

- **Decision**: Client supplies an idempotency key on every mutating request; the service stores
  key→result and replays the original outcome on retry; same key + different payload → `409 Conflict`.
- **Rationale**: Card networks retry; idempotency converts retries from financial defects into safe
  no-ops (Principle III). Conflict-on-divergent-payload prevents silently masking client bugs.
- **Alternatives**: Natural-key dedup (fragile across fields); at-least-once with downstream dedup
  (pushes correctness to callers) — rejected.

## R3 — Sensitive data handling

- **Decision**: Persist/return only a network token + last-four. Full PAN/CVV never stored, logged, or
  returned. Sensitive reads are audited.
- **Rationale**: PCI DSS scope minimization; disclosure is the highest-severity failure (Principle II).
- **Alternatives**: Encrypt-and-store full PAN — rejected; expands PCI scope with no feature benefit.

## R4 — Money representation

- **Decision**: Integer minor units + explicit ISO-4217 currency on every amount; reject float and
  cross-currency limit/card mismatch.
- **Rationale**: Eliminates floating-point rounding corruption; currency-explicitness prevents silent
  mis-application (Principle III).
- **Alternatives**: Decimal strings (acceptable but heavier to validate); floats — rejected outright.

## R5 — Authorization & segregation of duties

- **Decision**: Per-operation authorization on an externally-authenticated principal with a role claim
  (`end-user`/`ops`/`compliance`). Compliance force-freeze overrides owner; high-value limit raises need
  a second authorizer.
- **Rationale**: Least privilege + segregation of duties are explicit regulator expectations (Principle IV).
- **Alternatives**: Coarse role gating at the API edge only — rejected; can't express per-card ownership
  or dual-control.

## R6 — Concurrency control

- **Decision**: Serialize mutations per card (e.g., per-card lock / optimistic version check) so racing
  freeze/limit requests produce deterministic, individually-audited outcomes.
- **Rationale**: Last-writer-wins on financial state is unacceptable; determinism is required for audit
  reconstruction (Principles I, III).
- **Alternatives**: No concurrency control (data races); global lock (needless contention) — rejected.

## R7 — Audit model

- **Decision**: Append-only `AuditEvent` per mutation and per sensitive read; immutable; queryable by
  ops/compliance; retained per regulatory minimum (assumed 7 years).
- **Rationale**: Auditability is non-negotiable (Principle I); append-only prevents tampering.
- **Alternatives**: Mutable activity log (tamperable) — rejected.

## R8 — Performance/SLO targets (assumed)

- **Decision**: card create p95 < 800 ms; freeze visible ≤ 2 s; list first page p95 < 1 s; default page
  size 25 (max 100); 10 mutations/card/min rate limit.
- **Rationale**: Aligns with typical FinTech UX expectations; conservative enough to be defensible while
  bounding pagination and abuse. Labeled **assumed** because no production telemetry exists yet.
- **Alternatives**: Tighter sub-200ms targets — rejected as unjustifiable without measurement.
