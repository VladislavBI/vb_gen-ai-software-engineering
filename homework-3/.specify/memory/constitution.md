<!--
SYNC IMPACT REPORT
Version change: (template) → 1.0.0
Bump rationale: MINOR-from-zero — initial ratification of a complete principle set
  for the Virtual Card Lifecycle specification project.
Modified principles: all five instantiated from template placeholders.
Added sections: Security & Compliance Requirements; Development Workflow & Quality Gates.
Removed sections: none.
Templates requiring updates:
  - .specify/templates/plan-template.md      ✅ Constitution Check gate aligns (audit, data-protection, idempotency)
  - .specify/templates/spec-template.md       ✅ mandatory Success Criteria / Edge Cases satisfy Principle V
  - .specify/templates/tasks-template.md      ✅ task categories cover audit/verification/security cross-cutting work
Deferred TODOs: none.
-->

# Virtual Card Lifecycle Constitution
<!-- Governs the specification package for a regulated virtual-card feature: create, freeze/unfreeze, set limits, view transactions. -->

## Core Principles

### I. Audit-First & Traceability (NON-NEGOTIABLE)
Every state-changing action (card created, frozen, unfrozen, limit changed, closed) MUST emit an
immutable, append-only audit event recording actor identity, role, timestamp (UTC), action,
before/after state, and a correlation/request id. Audit records MUST be queryable by ops/compliance
and MUST NOT be editable or deletable by any actor. **Rationale**: regulated financial systems are
reconstructed and examined after the fact; if an action cannot be evidenced, it is treated as not
having occurred. No requirement, task, or design decision may remove or weaken auditability.

### II. Data Protection & Least Disclosure
Sensitive data MUST be minimized, masked, and never logged. The full PAN and CVV MUST NEVER appear in
logs, audit payloads, error messages, analytics, or non-PCI stores; only a network token and the last
four digits may be persisted or displayed. Access to sensitive fields MUST be role-gated and every read
of sensitive data MUST itself be auditable. **Rationale**: PCI DSS and privacy law make disclosure the
highest-severity failure mode; defaults must fail closed (redact unless explicitly permitted).

### III. Idempotency & Money Integrity
All write operations MUST be idempotent under client-supplied idempotency keys: a retried request with
the same key MUST return the original result and MUST NOT create duplicate cards, duplicate audit
events, or double-apply a limit change. Monetary amounts MUST be represented as integer minor units
(or fixed-point decimal) with explicit ISO-4217 currency — never floating point. **Rationale**: networks
retry; without idempotency, retries become financial defects, and float rounding silently corrupts money.

### IV. Least-Privilege Access & Segregation of Duties
Capabilities MUST be scoped to the smallest role that needs them. End-users may act only on their own
cards; ops/compliance get read/oversight and constrained intervention powers but MUST NOT be able to
view secrets they don't need; sensitive actions (e.g., raising a limit above a threshold) MAY require a
second authorizer. Permission boundaries MUST be expressed per-operation, not assumed. **Rationale**:
segregation of duties limits both fraud and blast radius, and is an explicit regulator expectation.

### V. Verifiable Quality (Acceptance Criteria & SLOs as First-Class)
Every mid-level objective MUST state how it will be verified, and a substantial share of low-level tasks
MUST end with checkable acceptance criteria / definition-of-done. Non-functional expectations
(latency percentiles, pagination/rate limits, read-after-write consistency) MUST be stated as numeric
targets or ranges — never vague language — and labeled as *assumed targets* with justification when
hypothetical. **Rationale**: a spec an AI agent can execute "without guessing" requires testability and
measurable targets to be designed in, not bolted on.

## Security & Compliance Requirements

- Scope assumes a **regulated environment**: PCI DSS for card data, plus auditability and data-residency
  expectations typical of banking. Sensitive data boundaries MUST be explicit in the spec (which fields,
  who may read, where stored).
- Authentication is assumed externally provided; this feature consumes an authenticated principal with a
  role claim. Authorization decisions MUST be made per-operation and MUST be auditable.
- Data retention: audit/event records retained per regulatory minimum (assumed 7 years); transient PII
  minimized and tokenized at rest. Any deviation MUST be called out as an assumption.
- Failure posture is **fail-closed**: when authorization, validation, or downstream dependency state is
  uncertain, the system MUST deny the action and emit an audit event rather than proceed optimistically.

## Development Workflow & Quality Gates

- This project produces **specification artifacts only** (no application code). The Spec Kit flow is the
  workflow: constitution → specify → clarify → plan → tasks → checklist → analyze. `implement` is out of
  scope.
- The Constitution Check in `plan.md` MUST confirm the design upholds Principles I–V before tasks are
  generated; violations MUST be recorded in Complexity Tracking with justification or the design revised.
- `analyze` MUST report cross-artifact consistency (spec ↔ plan ↔ tasks) and traceability from goals to
  tasks before the package is considered done.

## Governance

This constitution supersedes ad-hoc preferences for the virtual-card spec package. Amendments MUST be
recorded in the Sync Impact Report above with a semantic version bump (MAJOR: principle removal/redefinition;
MINOR: principle/section addition; PATCH: clarifications). Every spec, plan, and task set MUST be reviewable
against these principles; any exception MUST be explicit and justified in writing within the affected
artifact. Compliance review is a release gate, not an afterthought.

**Version**: 1.0.0 | **Ratified**: 2026-05-23 | **Last Amended**: 2026-05-23
