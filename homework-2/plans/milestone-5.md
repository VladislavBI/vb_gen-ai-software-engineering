# Milestone 5: Filtering and list query — Session Plan

**Started:** 2026-05-17
**Super-plan reference:** ../PLAN.md milestone 5

## Approach

The `GET /tickets` endpoint currently returns all tickets without filtering. We will extend it to accept optional query-string parameters for `category`, `priority`, and `status`, plus support for combined filtering (e.g., `?category=account_access&priority=urgent`). The filtering will be applied in the service layer (`TicketService.GetAllAsync()`) which will accept optional filter parameters and return a filtered subset of tickets. The endpoint will extract query strings, pass them to the service, and return the filtered list. No changes to the Ticket domain or repository interface are needed — filtering is purely a query-time projection concern. This approach keeps filtering logic reusable and testable in isolation from HTTP concerns.

Alternative considered: filtering in the repository layer. Rejected because the in-memory repository is simple enough that applying filters in the service is clearer and more maintainable than adding a query-builder abstraction.

## Touch list

- **TicketsEndpoints.cs**: Modify the `GetAllTickets` handler to extract optional `category`, `priority`, and `status` query parameters (null by default) and pass them to the service.
- **TicketService.cs**: Add a new overload or extend `GetAllAsync()` to accept optional Category?, Priority?, and Status? parameters, then filter the full ticket list in-memory before returning.

## Review focus

- **Null-safety**: Query parameters are optional; ensure null values are handled correctly (treat as "no filter for this field").
- **Filtering logic**: Verify combined filters work (all specified filters must match AND semantics, not OR).
- **Enum case sensitivity**: Query strings are typically lowercase (`priority=urgent`); validate that comparison works correctly (likely via `string.Equals(..., StringComparison.OrdinalIgnoreCase)` if comparing string query values to enum names).
- **Return type consistency**: The endpoint still returns `IReadOnlyList<TicketResponse>`, matching the existing contract.
- **No scope creep**: Filtering is applied in the service layer only; no changes to the domain, repository, or request/response models.

## Notes

**2026-05-17 Implementation Complete:**
- Added optional query parameters (`category`, `priority`, `status`) to `GetAllTickets` endpoint
- Parameters are extracted from query strings and parsed using `Enum.TryParse<T>(..., ignoreCase: true, ...)`
- Invalid/unparseable values are silently ignored (treated as no filter)
- Service layer overload of `GetAllAsync()` accepts nullable enum filters
- Filters applied with AND semantics via sequential `Where` clauses
- Return type `IReadOnlyList<Ticket>` maintained via `.ToList().AsReadOnly()` (per codebase convention)
- Code passes all `.editorconfig` rules: explicit types for `out` params, proper `IReadOnly` handling
- Build clean with zero warnings/errors
- Ready for Verify block execution

**2026-05-17 Verify Complete:**
- Build succeeded with zero warnings/errors
- API started successfully on port 5080
- Created two test tickets with auto-classification applied
  - Ticket 1: "Cannot access" → account_access category, urgent priority
  - Ticket 2: "Refund" → billing_question category, low priority
- All filter tests passed:
  - `GET /tickets` returned ≥2 tickets
  - `?priority=urgent` filter returned urgent-priority tickets
  - `?category=billing_question` filter returned billing-category tickets
  - Combined filter `?category=account_access&priority=urgent` returned matching subset
- Milestone 5 verified successfully; ready for commit
