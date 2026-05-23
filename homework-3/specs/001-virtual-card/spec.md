# Feature Specification: Virtual Card Lifecycle

**Feature Branch**: `001-virtual-card`

**Created**: 2026-05-23

**Status**: Draft

**Input**: User description: "Virtual card lifecycle for a regulated FinTech app — create a card, freeze/unfreeze it, set and adjust spending limits, and view its transactions. Stakeholders: end-users and internal ops/compliance. Regulated environment: auditability, security, clear boundaries for sensitive data."

## Clarifications

### Session 2026-05-23

- **Q**: Which card states and transitions are in scope? → **A**: `ACTIVE`, `FROZEN`, `CLOSED`;
  legal transitions `ACTIVE↔FROZEN`, `ACTIVE→CLOSED`, `FROZEN→CLOSED`. `CLOSED` is terminal.
- **Q**: Can ops/compliance override an end-user? → **A**: Yes — a compliance *force-freeze* takes
  precedence; the owner cannot unfreeze while a compliance hold stands. Both actions are audited.
- **Q**: How is sensitive card data handled? → **A**: Only last-four + network token are ever exposed
  or stored; full PAN/CVV never appear in responses, logs, audit payloads, or storage (PCI-aligned).
- **Q**: What guards limit increases? → **A**: Raises above a configurable high-value threshold require
  a second authorizer; values are validated as integer minor units in the card's currency.
- **Q**: What consistency does a read-after-write expect? → **A**: State changes are visible within an
  assumed ≤2s window; freezes block new authorizations within that window (see Success Criteria).

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Create a virtual card (Priority: P1)

An authenticated end-user requests a new virtual card on one of their eligible funding accounts. The
system issues a card in `ACTIVE` state with a default spending limit, returns the card reference and the
last four digits (never the full PAN), and records who created it and when.

**Why this priority**: Without issuance there is no card to manage; this is the minimum viable slice and
every other story depends on a card existing.

**Independent Test**: Issue a card against an eligible account and confirm an `ACTIVE` card is returned
with a masked identifier, a default limit, and a corresponding audit event — without any other story.

**Acceptance Scenarios**:

1. **Given** an authenticated user with an eligible funding account, **When** they request a new card,
   **Then** an `ACTIVE` card is created with a default limit and only the last four digits are exposed.
2. **Given** the same create request is retried with the same idempotency key, **When** it is received
   again, **Then** the original card is returned and no second card or duplicate audit event is created.
3. **Given** a user who has reached their per-user active-card cap, **When** they request another card,
   **Then** issuance is refused with a clear reason and an audit event records the denied attempt.

---

### User Story 2 - Freeze and unfreeze a card (Priority: P1)

An end-user (or ops/compliance) freezes an active card to immediately block new authorizations, and later
unfreezes it to restore spending. State transitions are constrained and every transition is audited.

**Why this priority**: Freeze is the primary risk-control action users reach for when a card is lost or
suspicious; it is the highest-value safety lever after issuance.

**Independent Test**: Freeze an `ACTIVE` card and confirm it moves to `FROZEN` and blocks new spend;
unfreeze it and confirm it returns to `ACTIVE` — each transition producing an audit event.

**Acceptance Scenarios**:

1. **Given** an `ACTIVE` card, **When** the owner freezes it, **Then** it becomes `FROZEN` and new
   authorizations are declined while the freeze persists.
2. **Given** a `FROZEN` card, **When** the owner unfreezes it, **Then** it returns to `ACTIVE`.
3. **Given** a `CLOSED` card, **When** any actor attempts to freeze or unfreeze it, **Then** the action
   is rejected as an invalid transition and audited.
4. **Given** an already-`FROZEN` card, **When** a freeze is requested again, **Then** the result is the
   same `FROZEN` state (idempotent) with no duplicate side effects.

---

### User Story 3 - Set and adjust spending limits (Priority: P2)

An end-user sets or changes the spending limit on a card (per-transaction and/or periodic). Limits are
validated against policy bounds; raises beyond a configured threshold require elevated authorization.

**Why this priority**: Limits are core to controlling exposure but are only meaningful once a card exists
and can be frozen; they refine rather than establish the lifecycle.

**Independent Test**: Set a valid limit on a card and confirm it is applied and audited; attempt an
out-of-policy value and confirm rejection with a clear reason.

**Acceptance Scenarios**:

1. **Given** an `ACTIVE` card, **When** the owner sets a limit within policy bounds, **Then** the limit
   is updated, applied to subsequent authorizations, and the change is audited with old→new values.
2. **Given** a requested limit above the high-value threshold, **When** an owner without elevated rights
   submits it, **Then** the change is held/declined pending a second authorizer and is audited.
3. **Given** an invalid limit (negative, zero where disallowed, wrong currency, non-minor-unit), **When**
   submitted, **Then** it is rejected with a precise validation error and no partial update occurs.

---

### User Story 4 - View card transactions (Priority: P2)

An end-user views a paginated, reverse-chronological list of a card's transactions with status and masked
card reference. Ops/compliance can view the same data for any card within their remit.

**Why this priority**: Visibility is essential for trust and dispute readiness, but it observes state the
earlier stories produce rather than creating new lifecycle capability.

**Independent Test**: Request a card's transactions and confirm a stable, paginated, ordered list is
returned with sensitive fields masked, including the empty-state case.

**Acceptance Scenarios**:

1. **Given** a card with transactions, **When** the owner lists them, **Then** results are paginated,
   newest-first, with stable ordering across pages and only masked card data shown.
2. **Given** a card with no transactions, **When** the owner lists them, **Then** an explicit empty
   result (not an error) is returned.
3. **Given** a user requesting a card they do not own, **When** they list its transactions, **Then**
   access is denied and the attempt is audited.

---

### User Story 5 - Ops/compliance oversight & intervention (Priority: P3)

Ops/compliance review a card's full audit trail and may force-freeze a card under investigation. Their
elevated actions are themselves audited and bounded by least-privilege (no access to secrets they do not
need).

**Why this priority**: Oversight is required for a regulated system but builds on the audit and freeze
capabilities established earlier; it is additive rather than foundational.

**Independent Test**: As an ops/compliance role, retrieve a card's audit history and force-freeze it;
confirm the intervention is applied and recorded with the operator's identity and reason.

**Acceptance Scenarios**:

1. **Given** an ops/compliance operator, **When** they open a card's audit trail, **Then** the full
   ordered event history is returned without exposing full PAN/CVV.
2. **Given** a card flagged for investigation, **When** an operator force-freezes it with a reason,
   **Then** the card becomes `FROZEN`, the owner cannot unfreeze it while the hold stands, and the action
   is audited with operator identity and reason.

---

### Edge Cases

- **Concurrent transitions**: two freeze/limit requests race on the same card → last-writer-wins is
  unacceptable; the system MUST serialize per-card so outcomes are deterministic and each is audited.
- **Idempotency-key reuse with different payloads**: same key, different body → reject as a conflict
  rather than silently returning the old result.
- **Stale read after write**: a client reads card state immediately after a change → state MUST reflect
  the write within the stated consistency window, or clearly indicate pending.
- **Limit currency mismatch**: limit currency ≠ card currency → reject with a precise error.
- **Owner action on operator hold**: owner tries to unfreeze a compliance force-freeze → denied and audited.
- **Closed/terminal card**: any mutating action on a `CLOSED` card → invalid-transition error, audited.
- **Empty / not-found**: list/view of a non-existent or not-owned card → not-found or denied (do not leak
  existence to non-owners), audited.
- **Permission boundary**: end-user attempts an ops-only action → denied, audited, no state change.
- **Fraud-ish pattern**: rapid issue→spend→freeze cycling beyond rate limits → throttle and flag.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: System MUST allow an authenticated user to create a virtual card against an eligible funding
  account, returning an `ACTIVE` card with a default spending limit.
- **FR-002**: System MUST expose only the last four digits and a network token for any card; it MUST NEVER
  return, log, or store the full PAN or CVV.
- **FR-003**: System MUST support card states `ACTIVE`, `FROZEN`, and `CLOSED`, and MUST enforce the legal
  transitions: `ACTIVE↔FROZEN`, `ACTIVE→CLOSED`, `FROZEN→CLOSED`; all others MUST be rejected.
- **FR-004**: Users MUST be able to freeze and unfreeze their own `ACTIVE`/`FROZEN` cards; ops/compliance
  MUST be able to force-freeze any card within remit with a recorded reason.
- **FR-005**: System MUST allow setting/adjusting per-transaction and periodic spending limits within
  policy bounds, applying changes to subsequent authorizations.
- **FR-006**: System MUST require elevated/second-authorizer approval for limit raises above a configured
  high-value threshold.
- **FR-007**: Users MUST be able to view a paginated, reverse-chronological list of a card's transactions
  with stable ordering and masked card data.
- **FR-008**: System MUST make all create/freeze/unfreeze/limit-change/close operations idempotent under a
  client idempotency key, returning the original outcome on retry.
- **FR-009**: System MUST emit an immutable, append-only audit event for every state-changing action and
  every sensitive-data read, capturing actor, role, UTC timestamp, action, before/after, correlation id.
- **FR-010**: System MUST make authorization decisions per operation and MUST deny + audit any action
  outside the actor's role scope.
- **FR-011**: System MUST represent all monetary values as integer minor units with an explicit ISO-4217
  currency and MUST reject mismatched or malformed amounts.
- **FR-012**: System MUST serialize conflicting mutations on the same card so concurrent requests yield a
  deterministic, audited outcome.
- **FR-013**: System MUST return an explicit empty result (not an error) when listing transactions for a
  card that has none.
- **FR-014**: System MUST NOT reveal the existence of a card to a non-owner/non-authorized actor (return
  not-found rather than forbidden where disclosure would leak existence).

### Key Entities *(include if feature involves data)*

- **Card**: a virtual card. Attributes: card reference (opaque id), masked PAN (last four), network token,
  currency, state (`ACTIVE`/`FROZEN`/`CLOSED`), owner reference, funding-account reference, limits,
  created/updated timestamps. Never holds full PAN/CVV.
- **SpendingLimit**: per-transaction and periodic caps. Attributes: amount (minor units), currency, period,
  effective range. Belongs to a Card.
- **Transaction**: an authorization/settlement record observed for a card. Attributes: id, amount (minor
  units), currency, merchant descriptor, status, occurred-at. Read-only in this feature.
- **AuditEvent**: immutable record of an action or sensitive read. Attributes: id, actor, role, action,
  before/after snapshot, reason (optional), correlation id, UTC timestamp. Append-only.
- **Actor/Role**: authenticated principal with a role claim (`end-user`, `ops`, `compliance`); determines
  per-operation authorization.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: A user can create a usable card and see its masked details in under 30 seconds end-to-end.
- **SC-002**: Freezing a card blocks new authorizations within 2 seconds of the freeze being confirmed.
- **SC-003**: 100% of state-changing actions and sensitive reads produce a retrievable audit event (no
  unaudited mutations in any test scenario).
- **SC-004**: 0 occurrences of full PAN/CVV in any log, audit payload, response body, or stored record
  across all test fixtures.
- **SC-005**: Retrying any write with an unchanged idempotency key produces exactly one effect (no
  duplicates) in 100% of retry tests.
- **SC-006**: Transaction listing returns the first page in under 1 second at the assumed scale and keeps
  stable ordering across pages.
- **SC-007**: 100% of out-of-policy or invalid lifecycle requests are rejected with a precise,
  user-actionable reason and leave no partial state change.

## Assumptions

- Authentication is provided by an external identity system; this feature receives an authenticated
  principal with a role claim and does not implement login.
- Card issuance and authorization are backed by an external card-network/processor; this feature models
  lifecycle and oversight, not settlement.
- A funding account and eligibility check already exist and can be queried.
- Default per-user active-card cap and the high-value limit threshold are policy-configurable; assumed
  defaults (e.g., 5 active cards, high-value threshold at a configurable amount) are stated as targets.
- Audit retention follows a regulatory minimum (assumed 7 years) and audit storage is append-only.
- All amounts are single-currency per card; cross-currency conversion is out of scope.
