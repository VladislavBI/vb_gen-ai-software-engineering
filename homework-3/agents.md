# agents.md — AI Agent Guidelines for the Virtual Card Lifecycle Project

These guidelines tell any AI coding partner (Claude Code, Copilot, Cursor, etc.) how to behave
consistently in this regulated FinTech domain. They operationalize
[`.specify/memory/constitution.md`](.specify/memory/constitution.md) (v1.0.0). When a request conflicts
with these rules, **stop and ask** rather than violating them.

## 1. Tech stack assumptions

- **Nature of this project**: specification-only (no application code). Agents produce/maintain
  documents under `specs/`, the constitution, and the deliverable docs. Do **not** run
  `/speckit-implement` or generate source code for this homework.
- **Reference target for examples** (only to make the spec concrete): a strongly-typed, service-style
  backend (e.g., .NET / ASP.NET Core minimal API). Authentication and card-network settlement are
  **external** dependencies the feature consumes, not builds.
- **Workflow**: GitHub Spec Kit — constitution → specify → clarify → plan → tasks → checklist → analyze.

## 2. Domain rules (banking / cards)

- A card has exactly three states: `ACTIVE`, `FROZEN`, `CLOSED`. Legal transitions: `ACTIVE↔FROZEN`,
  `ACTIVE→CLOSED`, `FROZEN→CLOSED`. `CLOSED` is terminal. Treat any other transition as a typed error.
- A `compliance_hold` (ops/compliance force-freeze) **overrides** the owner; the owner must not be able
  to unfreeze while it stands.
- Limit raises above the configured high-value threshold require a **second authorizer**.
- Stakeholders: `end-user` (own cards only), `ops`, `compliance` (oversight + bounded intervention).

## 3. Security & compliance constraints (highest priority)

- **Never** write, log, store, return, or embed in examples a full PAN or CVV. Use only a network token
  and the last four digits. If asked to handle full PAN/CVV, refuse and explain.
- All sensitive reads must be auditable; treat audit as **non-negotiable** — every state change and
  sensitive read produces an immutable, append-only audit event (actor, role, action, before/after
  redacted, reason, correlation id, UTC).
- **Fail closed**: when authorization, validation, or a dependency's state is uncertain, deny and audit
  rather than proceeding.
- Respect least privilege and segregation of duties in every example and recommendation.

## 4. Code style & conventions (for when examples or future code are written)

- Money: integer minor units + explicit ISO-4217 currency. **Never** floats for money. Reject
  currency mismatches.
- Writes are idempotent under a client idempotency key; same key + different payload → conflict.
- Mutations on a single card are serialized; never last-writer-wins on financial state.
- Prefer explicit, typed errors over generic failures; precise, user-actionable messages; no partial
  updates on validation failure.
- Naming: domain-first (`Card`, `SpendingLimit`, `AuditEvent`, `Transaction`); actions named for the
  lifecycle verb (`freeze`, `unfreeze`, `setLimit`, `close`).

## 5. Testing & verification expectations

- Treat verification as first-class (Constitution Principle V). Every mid-level objective must have a
  verification path; many tasks must carry checkable acceptance criteria.
- Test categories to assume: **unit** (state machine, validation), **integration** (authorization +
  audit + idempotency), **e2e** (US1→US5 golden path), plus **reconciliation** (audit count == op count)
  and **manual compliance review** (0 PAN/CVV leakage).
- Performance targets are numeric and **labeled assumed**; do not invent tighter targets without
  justification.

## 6. How the agent should treat edge cases

- **Never log PAN/CVV** — redact by default; assume any 13–19 digit sequence may be a PAN.
- **Prefer idempotent writes** — always thread the idempotency key; surface conflicts, don't swallow them.
- **Concurrency** — assume requests race; design for per-card serialization and deterministic outcomes.
- **Existence privacy** — return *not-found* (not *forbidden*) to non-owners so card existence isn't leaked.
- **Terminal state** — reject mutations on `CLOSED` cards as invalid transitions, and audit the attempt.
- **Ambiguity** — make an informed, documented assumption (record it in the spec's Assumptions or
  Clarifications); escalate only scope/security/UX-significant ambiguities (max 3 at a time).

## 7. Working agreement

- Keep `specs/` artifacts internally consistent; after any change, re-run the equivalent of
  `/speckit-analyze` and update `analysis-report.md`.
- Preserve instructor files (`TASKS.md`, `specification-TEMPLATE-example.md`) read-only.
- Reflect changes back into the constitution's Sync Impact Report when principles change.
