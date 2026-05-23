# Claude Code Project Rules — Virtual Card Lifecycle

Editor/AI rules that steer how Claude (and any AI partner) works in this project. These are the terse,
enforceable defaults; the fuller rationale lives in [`agents.md`](../agents.md) and
[`.specify/memory/constitution.md`](../.specify/memory/constitution.md).

## Hard rules (never break)

- **NEVER** output, log, store, or include in examples a full PAN or CVV. Use a network token + last-four.
- **NEVER** represent money as a float. Use integer minor units + ISO-4217 currency, always paired.
- **NEVER** allow an unaudited state change or sensitive read — audit is append-only and immutable.
- **NEVER** let an owner override a `compliance_hold`; ops/compliance force-freeze wins.
- **NEVER** use last-writer-wins on card state — serialize mutations per `card_id`.
- **NEVER** run `/speckit-implement` or generate application code — this homework is specification-only.

## Defaults (do this unless told otherwise)

- **Fail closed**: when authorization/validation/dependency state is uncertain → deny + audit.
- **Idempotency**: thread a client idempotency key on every write; same key + different payload → conflict.
- **Existence privacy**: non-owner access → return *not-found*, not *forbidden*.
- **Dual control**: high-value limit raises require a second authorizer.
- **Assumed targets**: label any performance/SLO number as *assumed* with a one-line justification.

## Naming & patterns

- Domain-first names: `Card`, `SpendingLimit`, `Transaction`, `AuditEvent`; roles `end-user`/`ops`/`compliance`.
- Lifecycle verbs: `create`, `freeze`, `unfreeze`, `setLimit`, `close`, `viewAudit`.
- States: `ACTIVE`, `FROZEN`, `CLOSED` only; transitions `ACTIVE↔FROZEN`, `ACTIVE→CLOSED`, `FROZEN→CLOSED`.
- Errors: explicit and typed (`InvalidTransition`, `OutOfPolicyLimit`, `IdempotencyConflict`,
  `Unauthorized`) with precise, user-actionable messages and **no partial updates**.

## What to avoid

- Vague requirements ("should be fast") — replace with numeric, labeled targets.
- Generic security essays — keep edge cases scoped to the virtual-card feature.
- Widening scope into auth/settlement/UI — those are external/out of scope.
- Editing instructor files: `TASKS.md`, `specification-TEMPLATE-example.md` are read-only.

## Workflow

- Spec Kit phases only: constitution → specify → clarify → plan → tasks → checklist → analyze.
- After any change to `specs/`, re-check cross-artifact consistency and update `analysis-report.md`.
- Keep goals→tasks traceability intact (see `specification.md` §10).
