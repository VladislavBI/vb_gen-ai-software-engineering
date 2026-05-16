# Milestone 4: Auto-classification engine — Session Plan

**Started:** 2026-05-16
**Super-plan reference:** ../PLAN.md milestone 4

## Approach

The classifier will be implemented as a keyword-matching engine that scans the ticket subject and description against predefined keyword lists for priorities and categories. The classifier returns a `ClassificationResult` record containing category, priority, confidence (as ratio of matched keywords to total keywords in the winning category), reasoning, and the list of keywords found.

Priority keywords are simple case-insensitive substring matches:
- **Urgent**: "can't access", "critical", "production down", "security"
- **High**: "important", "blocking", "asap"
- **Low**: "minor", "cosmetic", "suggestion"
- **Medium**: fallback when no priority keyword matches

Category keywords will be matched against both subject and description:
- `AccountAccess`: "login", "password", "2fa", "access", "account"
- `TechnicalIssue`: "bug", "error", "crash", "exception", "fail"
- `BillingQuestion`: "payment", "invoice", "refund", "billing", "charge"
- `FeatureRequest`: "feature", "enhancement", "improve", "request", "wish"
- `BugReport`: "defect", "reproduce", "steps to reproduce", "regression"
- `Other`: fallback

The confidence score is computed as the count of matched keywords divided by the total number of keywords defined for the winning category (0.0–1.0). The reasoning field will be a human-readable explanation of the classification (e.g., "Classified as account_access with 3 matching keywords: access, cannot access, production down").

The `TicketClassifier` service will be registered in `Program.cs` as `AddScoped<TicketClassifier>()` and used by:
1. A new `POST /tickets/{id}/auto-classify` endpoint that classifies an existing ticket, persists the result via `TicketService.UpdateAsync`, and returns the `ClassificationResult`.
2. The existing `POST /tickets` endpoint will accept an optional `?autoClassify=true` query parameter. When present, it will call the classifier after creating the ticket and apply the classification before returning the response.

The implementation will update `Program.cs` to register the classifier service and map the new classify endpoint.

## Touch list

- **ClassificationResult.cs** (new): Record type with properties `Category`, `Priority`, `Confidence`, `Reasoning`, `KeywordsFound` (IReadOnlyList<string>).
- **TicketClassifier.cs** (new): Service class with a public `Classify(Ticket ticket)` method that returns `ClassificationResult` based on keyword matching.
- **TicketsClassifyEndpoint.cs** (new): Endpoint mapper with `MapClassify(this WebApplication app)` exposing `POST /tickets/{id}/auto-classify`.
- **TicketsEndpoints.cs** (modify): Update `CreateTicket` to accept `autoClassify` query parameter and invoke the classifier when true.
- **Program.cs** (modify): Register `TicketClassifier` as scoped service and call `app.MapClassify()` after `app.MapTicketsImport()`.

## Review focus

- **Keyword matching logic**: Ensure case-insensitive matching and that multi-word keywords like "can't access" and "production down" are handled correctly.
- **Confidence calculation**: Verify the confidence score is computed correctly (matched count / total keywords in category).
- **Priority keyword precedence**: Urgent > High > Low > Medium (first matching keyword tier wins).
- **Category selection**: When multiple categories match, the one with the highest confidence wins; ties default to `Other`.
- **Auto-classify query parameter**: Ensure the flag is optional and doesn't break existing CRUD behavior when absent.
- **Persistence**: Verify that `auto-classify` endpoint actually updates the ticket via `TicketService.UpdateAsync` and returns the persisted result.
- **API response shape**: Ensure JSON field names match the verify block's expectations (snake_case: `keywords_found`, `confidence`, etc.).

## Notes

**Build check (first iteration):** Build passed successfully. All three files created (ClassificationResult.cs, TicketClassifier.cs, TicketsClassifyEndpoint.cs). Program.cs updated to register classifier and map endpoint. TicketsEndpoints.cs updated to accept autoClassify parameter.

**Code analysis before review:**
- ClassificationResult uses proper record with XML doc params (not inline comments on record properties).
- TicketClassifier uses case-insensitive StringComparison.OrdinalIgnoreCase for all keyword matching (no ToLowerInvariant needed).
- Confidence calculated as matched_keywords_count / total_keywords_in_category (0.0–1.0).
- Priority precedence: Urgent → High → Low → Medium (tried in order, first match wins).
- Category selection: wins by highest confidence; Other is fallback when no category keyword matched.
- ClassificationResultResponse sealed, snake_case field conversion via pragma-suppressed ToLowerInvariant calls.
- AutoClassify parameter in CreateTicket is optional (defaults to false), preserves backward compatibility.
- Auto-classify endpoint calls TicketService.UpdateAsync to persist changes, then returns ClassificationResult as JSON.
