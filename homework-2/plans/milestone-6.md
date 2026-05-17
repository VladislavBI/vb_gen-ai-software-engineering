# Milestone 6: Tests — API + BLL + DAL + parsers + classifier with ≥85% coverage — Session Plan

**Started:** 2026-05-17
**Super-plan reference:** ../PLAN.md milestone 6

## Approach

This milestone builds out the `Homework2.Tests` xUnit test suite to cover all functional code across API, BLL, DAL, and validator layers, using `WebApplicationFactory<Program>` for API integration tests, Moq for BLL unit tests (mocking `ITicketRepository`), pure unit tests for the DAL repository with the in-memory `ConcurrentDictionary`, and dedicated test classes for validators, parsers, and the classifier. The approach prioritizes test isolation (fresh in-memory store per integration-test fixture, fresh mocks per BLL test) and coverage breadth (hitting all three parser formats, all six category keywords, priority classification paths, update/filter/concurrency scenarios in the DAL). Given that Milestones 1–5 have already been implemented and verified, the test surface area is well-defined: the code is stable and we are not discovering new behavior, only asserting existing behavior. This allows tests to be written linearly without refactoring surprises. Coverage target of ≥85% line coverage is achievable because (1) the domain model and DTOs are simple records with no complex logic, (2) exception paths are minimal, and (3) the main logic concentrates in TicketService, TicketClassifier, and the three TicketParsers — each of which is straightforward and testable without mocks. The API endpoints layer has thin controller logic relying on services and validation, so integration tests via `WebApplicationFactory` can easily exceed 85% coverage when combined with BLL and DAL unit tests.

## Touch list

### API Integration Tests (`Homework2.Tests/Api/Endpoints/TicketsEndpointsTests.cs`)
- `POST /tickets` with valid request → 201 Created, response includes id, createdAt, status="New"
- `POST /tickets` with invalid email → 400 Bad Request with validation error for email field
- `POST /tickets` with short description → 400 Bad Request
- `POST /tickets` with subject > 200 chars → 400 Bad Request
- `POST /tickets` with autoClassify=true → 201 with classification applied
- `GET /tickets` → 200 with empty array when no tickets
- `GET /tickets` → 200 with array of all tickets after multiple creates
- `GET /tickets?priority=urgent` → filters to only urgent tickets
- `GET /tickets?category=account_access` → filters to only account_access category
- `GET /tickets?category=account_access&priority=urgent` → combined filter (AND semantics)
- `GET /tickets/{id}` with valid id → 200 with correct ticket
- `GET /tickets/{id}` with nonexistent id → 404 Not Found
- `PUT /tickets/{id}` with valid update → 200 with updated fields
- `PUT /tickets/{id}` with invalid description length → 400 Bad Request
- `PUT /tickets/{id}` with nonexistent id → 404 Not Found
- `DELETE /tickets/{id}` with valid id → 204 No Content
- `DELETE /tickets/{id}` with nonexistent id → 404 Not Found
- Verify response Content-Type is `application/json`
- Verify response body shape matches TicketResponse schema (all fields present, none null unless expected)

### API Validators Tests (`Homework2.Tests/Api/Validators/TicketValidatorsTests.cs`)
- `CreateTicketValidator`: valid request passes
- `CreateTicketValidator`: empty customer_id fails
- `CreateTicketValidator`: invalid email format fails
- `CreateTicketValidator`: empty customer_name fails
- `CreateTicketValidator`: subject < 1 char fails
- `CreateTicketValidator`: subject > 200 chars fails
- `CreateTicketValidator`: description < 10 chars fails
- `CreateTicketValidator`: description > 2000 chars fails
- `UpdateTicketValidator`: null subject is allowed
- `UpdateTicketValidator`: invalid subject length fails only when subject is non-null
- `UpdateTicketValidator`: invalid description length fails only when description is non-null

### BLL Services Tests (`Homework2.Tests/Bll/Services/TicketServiceTests.cs`)
- `CreateAsync`: creates ticket with new Guid, correct customer fields, default category=Other, priority=Medium, status=New
- `CreateAsync`: created ticket has metadata.Source="api"
- `CreateAsync`: created timestamp is recent (within 1 second)
- `GetAllAsync`: returns empty list when repository is empty
- `GetAllAsync`: returns all tickets when no filter
- `GetAllAsync`: filters by category (only tickets with matching category)
- `GetAllAsync`: filters by priority (only tickets with matching priority)
- `GetAllAsync`: filters by status (only tickets with matching status)
- `GetAllAsync`: combined filters use AND semantics (all conditions must match)
- `GetByIdAsync`: returns ticket when id exists
- `GetByIdAsync`: returns null when id does not exist
- `UpdateAsync`: updates subject, description, category, priority, status fields independently
- `UpdateAsync`: preserves fields not provided in update (null coalescing)
- `UpdateAsync`: returns null when ticket does not exist
- `UpdateAsync`: updates UpdatedAt timestamp
- `DeleteAsync`: deletes and returns true when ticket exists
- `DeleteAsync`: returns false when ticket does not exist
- **Mocking strategy**: Mock `ITicketRepository` for all tests; each test method creates a fresh mock; verify mock calls where appropriate (e.g., CreateAsync was called with expected Ticket)

### BLL Classifier Tests (`Homework2.Tests/Bll/Services/TicketClassifierTests.cs`)
- `Classify`: "cannot access account" → category=AccountAccess, priority=Medium or higher (depends on keywords)
- `Classify`: "critical production down, cannot access" → priority=Urgent, category=AccountAccess
- `Classify`: "minor cosmetic suggestion" → priority=Low, category=Other
- `Classify`: "payment invoice refund billing" → category=BillingQuestion
- `Classify`: "bug crash exception error" → category=TechnicalIssue
- `Classify`: "defect regression reproduce" → category=BugReport
- `Classify`: "feature enhancement request" → category=FeatureRequest
- `Classify`: confidence is 0.0–1.0 (ratio of matched keywords to category keywords)
- `Classify`: keywordsFound list contains only keywords that matched in the ticket text
- `Classify`: reasoning includes category name and confidence percentage
- `Classify`: when multiple categories have matching keywords, highest confidence wins
- `Classify`: priority keywords are case-insensitive
- `Classify`: category keywords are case-insensitive
- `Classify`: Category.Other gets default reasoning about no confident keywords

### BLL Parsers Tests (`Homework2.Tests/Bll/Services/TicketParsersTests.cs`)
- **CSV Parser**:
  - Valid CSV with header row and one data row → one RawTicketImport with all fields
  - CSV with multiple rows → each row becomes one import record
  - CSV with empty customer_id field → customer_id is empty string (validation happens later)
  - CSV with missing columns → field is empty string
  - CSV with whitespace trimming → leading/trailing spaces removed
  - Empty CSV (header only, no data rows) → empty list

- **JSON Parser**:
  - Valid JSON array with one object → one RawTicketImport
  - Valid JSON array with multiple objects → each object becomes one import record
  - JSON with camelCase property names (customer_id, customer_email, etc.) → fields mapped correctly
  - Invalid JSON syntax → throws exception (not caught by parser)
  - Empty JSON array `[]` → empty list
  - JSON with extra fields → extra fields ignored

- **XML Parser**:
  - Valid XML with root `<tickets>` and one `<ticket>` child → one RawTicketImport
  - Valid XML with multiple `<ticket>` children → each ticket becomes one import record
  - XML with missing elements → element value is empty string
  - XML with nested text nodes → extracts text content
  - Malformed XML (missing root) → empty list
  - Empty root `<tickets/>` → empty list

### DAL Repository Tests (`Homework2.Tests/Dal/Repositories/InMemoryTicketRepositoryTests.cs`)
- `GetAllAsync`: returns empty list initially
- `GetAllAsync`: returns all tickets after multiple creates (in insertion order or unordered—verify consistency)
- `GetByIdAsync`: returns ticket when id exists
- `GetByIdAsync`: returns null when id does not exist
- `CreateAsync`: creates ticket and returns it
- `CreateAsync`: multiple creates each persist independently
- `CreateAsync`: tickets are isolated (modifying one does not affect others)
- `UpdateAsync`: updates ticket and returns updated ticket
- `UpdateAsync`: returns null when id does not exist
- `UpdateAsync`: update does not affect other tickets
- `DeleteAsync`: deletes ticket and returns true
- `DeleteAsync`: deleted ticket is no longer retrievable
- `DeleteAsync`: returns false when id does not exist
- **Concurrency tests** (using `ConcurrentDictionary` thread-safety):
  - Parallel creates (10 concurrent `Task.WhenAll` CreateAsync calls) → all succeed, all persisted
  - Parallel deletes (create 10, delete all concurrently) → all deletions succeed
  - Mixed operations (5 creates, 5 updates, 5 deletes in parallel) → final state is consistent
  - Verify no race conditions or lost updates using concurrent stress tests

## Review focus

1. **Test isolation:** Verify that no test leaks state into another. API integration tests must reset the in-memory store between test classes (or ideally per test method via `WebApplicationFactory` `WithWebHostBuilder` override). BLL unit tests must create fresh mocks per test method. DAL unit tests should each start with a fresh repository instance.

2. **Assertion quality:** Tests must assert not just HTTP status codes but also response body fields (e.g., `response.id` matches the created ticket id, `response.status` is exactly "New", timestamps are recent, tags are empty list not null). Validators must test each rule in isolation with specific field-level error messages, not just "validation failed."

3. **Concurrency determinism:** Concurrent DAL tests must be repeatable and not flaky. Use `Task.WhenAll` with specific thread counts; verify final state is deterministic (e.g., count of remaining tickets equals expected count). Avoid `Thread.Sleep` for synchronization; use task completion guarantees.

4. **Coverage gap risks:** Enum conversions (Category/Priority/Status to/from strings in DTOs), null-handling in optional update fields, edge cases in parser whitespace trimming, keyword case-insensitivity in classifier, and the response shape transformation from domain Ticket to TicketResponse DTO could be missed if tests focus only on happy paths. Tests should include at least one negative test per major code path.

5. **WebApplicationFactory setup:** Ensure `Program.cs` is testable (registrations are public) and that the test fixture properly wires up dependencies. If any service or repository is registered as a singleton (as `ITicketRepository` is), fresh instances per test must be forced using `WithWebHostBuilder` to override DI configuration.

## Notes

**Serialization mismatch (request side):** `PostAsJsonAsync` / `PutAsJsonAsync` use default `System.Text.Json` options (PascalCase), but the API expects snake_case property names. Fixed by passing a `JsonSerializerOptions { PropertyNamingPolicy = SnakeCaseLower, Converters = [JsonStringEnumConverter(SnakeCaseLower)] }` to every `PostAsJsonAsync`, `PutAsJsonAsync`, and `ReadFromJsonAsync` call in `TicketsEndpointsTests.cs`.

**Enum filter bug revealed by tests (scope widening):** `GetAllTickets` in `TicketsEndpoints.cs` (Milestone 5 file) used `Enum.TryParse(value, ignoreCase: true)`, which silently fails for snake_case values like `account_access` (since "account_access" ≠ "accountaccess" even case-insensitively). Fixed with `.Replace("_", "")` before parsing. One new integration test `GetTickets_FilterByStatus_SnakeCaseValueParsedCorrectly` added to cover the multi-word case (`in_progress`). This widened the Files scope beyond the session plan; the deviation is justified because the test correctly revealed a shipping bug.

**Dead-code concurrency delete loop:** The mixed concurrency test had `for (int i = 5; i < 10 && i < ticketIds.Count; i++)` which never executed (ticketIds.Count = 5). Fixed to `for (int i = 2; i < 5; i++)` to actually delete 3 tickets, with assertion updated to `HaveCount(7)`.

**Review iterations:** 2 rounds with `code-review-advisor`. Round 1 returned 2 blockers (missing status filter test + dead delete loop). Both fixed before round 2, which gave sign-off with no blocking findings.

**Final test count:** 114 tests (113 from original plan + 1 added for status filter coverage). All pass. Build: 0 warnings, 0 errors.

**Post-commit fixup pass (3rd review iteration):** A re-review of files added during the milestone-runner's coverage-driven auto-retry (`TicketImportServiceTests.cs`, slimmed-down `TicketParsersTests.cs`) revealed three blockers that had bypassed the original review loop:

1. **`ParallelReadWrite_Consistency` was a false-positive test** — reads were awaited inside the loop, so `Task.WhenAll` operated on already-completed tasks. Rewritten to queue read/write tasks without awaiting until the final `Task.WhenAll`, making concurrency genuine.
2. **`TicketParsersTests.cs` covered only 6 of 18 mandated scenarios** — expanded to 14 tests covering CSV whitespace trimming, multi-row variants, JSON extra-fields/invalid-syntax/multiple-objects, XML multiple tickets, missing elements, and malformed XML throwing `XmlException` (production code does not catch — test pins the actual contract).
3. **`MixedConcurrentOperations` dead-code delete loop** — initially fixed, then accidentally reverted by the runner's retry; re-applied (delete `ticketIds[2..4]`, assert `HaveCount(7)`).

Also tightened `ImportAsync_WithMultipleErrors_ListsAllErrors` to assert `description` is in the error message plus `Total`/`Successful`. Final count: **140 tests, 97.21% coverage**, reviewer sign-off after this fixup pass.
