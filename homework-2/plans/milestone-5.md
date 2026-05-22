# Milestone 5: Filtering and list query — Session Plan

**Started:** 2026-05-22
**Super-plan reference:** ../PLAN.md milestone 5

## Approach

The current `GetAllTickets` endpoint in `TicketsEndpoints.cs` returns all tickets unconditionally. We will extend it to accept optional query-string parameters (`category`, `priority`, `status`) and apply filtering at the service layer. The `TicketService.GetAllAsync()` method will be modified to accept an optional filter object containing these three fields (each nullable), and the repository's `GetAllAsync()` will be enhanced to apply the filters when present. This keeps filtering logic in the BLL service layer (not the endpoint) and maintains separation of concerns. We considered a LINQ extension method but chose to inline the filtering logic in the service for clarity given the small number of filters. The endpoint will remain simple: bind the query parameters, construct a filter object, call the service, and return the result.

## Touch list

- **TicketsEndpoints.cs** — modify `GetAllTickets` handler to accept query parameters for `category`, `priority`, `status`; bind them and pass to service
- **TicketService.cs** — add an overload or modify `GetAllAsync()` to accept optional filter parameters (category, priority, status); apply filtering logic
- **InMemoryTicketRepository.cs** (implied but not listed in Files) — may need to add filtering support in `GetAllAsync()` if the service delegates filtering; however, since the endpoint's `Files` lists only the two files above, filtering logic should live in TicketService, not the repository

## Review focus

- Query parameter binding: ensure nullable enums (`category`, `priority`, `status`) are correctly parsed from query strings and map back to domain enums (the endpoint already does snake_case-to-PascalCase conversion for response DTOs; incoming filters must do the inverse)
- Filtering logic: confirm that all three filters work individually and in combination (e.g., `?priority=urgent&category=account_access` returns only tickets matching both)
- Edge cases: test with uppercase/lowercase mixed case query values, invalid enum values (should be silently ignored or return 400?), and empty/missing filters (should return all tickets)
- Response shape: the response is still a list of `TicketResponse` objects; filtering must not change the response type
- Performance: filtering a single concurrent dictionary is O(n) per call, which is acceptable for this homework scope

## Notes

**Completed successfully.** Implemented query-parameter filtering for `GET /tickets` endpoint:

- Extended `GetAllTickets()` method in `TicketsEndpoints.cs` to accept optional `category`, `priority`, `status` query parameters
- Added enum parsing validation that converts snake_case query values to PascalCase enum values
- Each parameter is validated independently; invalid enum values return 400 BadRequest
- Updated `TicketService.GetAllAsync()` to accept and apply nullable filter parameters
- Filtering logic correctly handles individual and combined filters (e.g., `?category=account_access&priority=urgent`)
- Verify passes all filter scenarios: single filters, combined filters, and all-tickets fallback when no filters provided

**Corrective edit during Verify:** Fixed SonarQube rule S1066 violations by merging nested if statements in parameter validation (lines 95–119) from `if-if` to `if-and` pattern to reduce code complexity.
