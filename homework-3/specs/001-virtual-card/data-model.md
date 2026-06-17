# Phase 1 Data Model: Virtual Card Lifecycle

Conceptual model only (no storage schema prescribed). Money is always integer minor units + ISO-4217.

## Entities

### Card
| Field | Type | Notes |
|---|---|---|
| `card_id` | opaque id | Stable external reference; not the PAN. |
| `pan_last_four` | string(4) | Display only. |
| `network_token` | token | Replaces PAN for all operations. **Full PAN/CVV never stored.** |
| `currency` | ISO-4217 | Fixed at creation; single-currency per card. |
| `state` | enum | `ACTIVE` \| `FROZEN` \| `CLOSED`. |
| `owner_ref` | id | The end-user who owns the card. |
| `funding_account_ref` | id | Eligible account the card draws on. |
| `compliance_hold` | bool | When true, owner cannot unfreeze. |
| `created_at` / `updated_at` | UTC timestamp | |

**Invariants**: state ∈ legal set; `CLOSED` is terminal; `compliance_hold ⇒ state = FROZEN`;
`pan_last_four` and `network_token` are the only card-secret-derived fields ever exposed.

### SpendingLimit
| Field | Type | Notes |
|---|---|---|
| `card_id` | id | Owning card. |
| `per_transaction` | minor units | ≥ 0; currency = card currency. |
| `periodic_amount` | minor units | ≥ 0. |
| `period` | enum | e.g., `DAILY` \| `MONTHLY`. |
| `requires_second_authorizer` | bool | True when a raise crosses the high-value threshold. |

**Invariants**: amounts non-negative integers in the card's currency; a raise above the configured
threshold is not effective until a second authorizer approves.

### Transaction *(read-only in this feature)*
| Field | Type | Notes |
|---|---|---|
| `transaction_id` | id | |
| `card_id` | id | |
| `amount` | minor units | |
| `currency` | ISO-4217 | |
| `merchant_descriptor` | string | Masked/cleaned for display. |
| `status` | enum | e.g., `AUTHORIZED` \| `SETTLED` \| `DECLINED`. |
| `occurred_at` | UTC timestamp | Ordering key (newest-first). |

### AuditEvent *(append-only, immutable)*
| Field | Type | Notes |
|---|---|---|
| `event_id` | id | |
| `card_id` | id | |
| `actor_ref` | id | Who acted. |
| `actor_role` | enum | `end-user` \| `ops` \| `compliance`. |
| `action` | enum | `CREATE` \| `FREEZE` \| `UNFREEZE` \| `SET_LIMIT` \| `CLOSE` \| `READ_SENSITIVE`. |
| `before` / `after` | snapshot | Redacted; no PAN/CVV. |
| `reason` | string? | Required for compliance force-freeze. |
| `correlation_id` | id | Ties to the originating request. |
| `occurred_at` | UTC timestamp | |

**Invariants**: never updated or deleted; always written in the same logical unit as the action it records.

## State Machine

```text
            freeze                       close
  ACTIVE ───────────▶ FROZEN ───────────────────▶ CLOSED
    ▲                   │                            ▲
    └──── unfreeze ─────┘            close ──────────┘
   (owner unfreeze blocked while compliance_hold = true)

Invalid from CLOSED: freeze, unfreeze, set_limit  → rejected + audited
```

## Relationships

- `Card 1—1 SpendingLimit` (current effective limit) ; `Card 1—* Transaction` ; `Card 1—* AuditEvent`.
- `Actor` is external (identity provider); referenced by `owner_ref` / `actor_ref`.
