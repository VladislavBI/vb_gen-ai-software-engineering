# Spec Kit Prompts Log — Virtual Card Lifecycle

The exact Spec-Driven Development prompts used to produce this package, in order. Reproducible: run them
from a Claude Code session rooted at `homework-3/` after `specify init` (see `../HOWTORUN.md`).

## 1. /speckit-constitution
```
FinTech/banking principles for a regulated virtual-card feature: audit-first & traceability
(non-negotiable), data protection & least disclosure (never log PAN/CVV, token + last-four only),
idempotency & money integrity (minor units + ISO-4217, idempotent writes), least-privilege access &
segregation of duties, and verifiable quality (acceptance criteria + numeric SLOs as first-class).
```

## 2. /speckit-specify
```
Virtual card lifecycle for a regulated FinTech app: create a card, freeze/unfreeze it, set/adjust
spending limits, and view its transactions. Stakeholders: end-users and internal ops/compliance.
Regulated environment: auditability, security, clear boundaries for sensitive data.
```

## 3. /speckit-clarify
```
Resolve ambiguities with informed FinTech defaults: card states ACTIVE/FROZEN/CLOSED and legal
transitions; compliance force-freeze overrides owner; sensitive-data handling (token + last-four);
dual control on high-value limit raises; read-after-write consistency window.
```

## 4. /speckit-plan
```
Add non-functional targets (latency percentiles, pagination/rate limits, read-after-write consistency
— labeled assumed with justification), data-handling guardrails, and beginning/ending context. Produce
research, data-model, contracts, and quickstart design docs.
```

## 5. /speckit-tasks
```
Decompose into many small, traceable tasks grouped by user story (US1–US5), with [Story] tags and
explicit acceptance criteria / definition-of-done on a substantial share of tasks.
```

## 6. /speckit-checklist
```
Generate a security & compliance quality checklist validating data protection, auditability,
idempotency/integrity, access control, and verification/performance coverage.
```

## 7. /speckit-analyze
```
Cross-artifact consistency & traceability: confirm every user story and functional requirement is
covered by tasks, every contract maps to a requirement, and each constitution principle is enforced.
```

> `/speckit-implement` is intentionally NOT run — this homework is specification-only.
