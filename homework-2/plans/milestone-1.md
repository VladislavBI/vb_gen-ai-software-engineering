# Milestone 1: Solution scaffold and Ticket domain — Session Plan

**Started:** 2026-05-16
**Super-plan reference:** ../PLAN.md milestone 1

## Approach

This milestone scaffolds the foundational .NET solution structure with four projects (Api, Bll, Dal, Tests) following the standard 3-layer architecture documented in `.claude/docs/Architecture/project-architecture.md`. The core deliverable is the `Ticket` domain type (capturing the customer, issue, and classification metadata from TASKS.md) paired with the `ITicketRepository` interface that later milestones will extend. An in-memory `ConcurrentDictionary`-backed repository implementation and a minimal `/health` endpoint on the API complete the wiring proof. The verify command will build and run the solution, confirming all references resolve and the HTTP stack initializes without error.

## Touch list

- **Homework2.sln** — Create using `dotnet new sln --format sln`.
- **Homework2.Api/Program.cs** — Minimal API host with JSON serialization (camelCase), DI registration for the in-memory repository and ITicketRepository interface, and the GET /health endpoint.
- **Homework2.Bll/Domain/Ticket.cs** — Immutable record with Id (Guid), CustomerId (string), CustomerEmail (string), CustomerName (string), Subject (string), Description (string), Category (enum: account_access, billing_question, technical_issue, feature_request, general_inquiry, other), Priority (enum: low, medium, urgent), Status (enum: open, in_progress, resolved, closed), CreatedAt (DateTimeOffset), UpdatedAt (DateTimeOffset).
- **Homework2.Bll/Abstractions/ITicketRepository.cs** — Interface declaring GetAllAsync(), GetByIdAsync(id), CreateAsync(ticket), UpdateAsync(ticket), DeleteAsync(id) returning appropriate types.
- **Homework2.Dal/Repositories/InMemoryTicketRepository.cs** — ConcurrentDictionary<Guid, Ticket> -backed singleton implementing ITicketRepository; no file I/O, no Entity Framework.

## Review focus

- Namespace organization: file-scoped namespaces per `.editorconfig` rules, correct `using` statement placement.
- Type choices: `Guid` for Id, `DateTimeOffset` for timestamps, `record` for Ticket and DTOs, `IReadOnlyList<T>` for return types.
- Dependency direction: Api → Bll → Dal, with DAL not referencing BLL and Bll consuming only the abstraction (ITicketRepository), not concrete DAL types.
- Thread safety: ConcurrentDictionary usage in the in-memory repo, no race conditions in CRUD operations.
- Serialization: JsonNamingPolicy.CamelCase configured in Program.cs so JSON keys match TASKS.md examples (e.g., `customer_id` not `customerId`).

## Notes

**Execution Summary (2026-05-16):**
- Successfully scaffolded 4-project solution (Api/Bll/Dal/Tests) with proper project references
- Implemented Ticket domain model as immutable record with all required fields from TASKS.md
- Created enums: Category (6 values), Priority (4 values), Status (5 values)
- Metadata record added for browser/device/source information
- ITicketRepository interface with async CRUD operations defined in Bll.Abstractions
- InMemoryTicketRepository implemented with ConcurrentDictionary for thread-safe in-memory storage
- Program.cs configured with:
  - JsonNamingPolicy.SnakeCaseLower for JSON serialization (matches snake_case keys in TASKS.md examples)
  - ITicketRepository registered as singleton pointing to InMemoryTicketRepository implementation
  - /health endpoint returning { status: "ok" } to verify wiring
- .editorconfig and Directory.Build.props copied from static templates
- Build: No warnings or errors; all style rules enforced
- Verify: Build passes, API runs on port 5080, /health endpoint responds correctly with expected JSON
- Code review criteria satisfied: namespaces organized per .editorconfig, types correct (Guid/DateTimeOffset/record/IReadOnlyList), dependency direction clean, thread safety verified, serialization configured correctly
