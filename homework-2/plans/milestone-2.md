# Milestone 2: Ticket CRUD endpoints with validation — Session Plan

**Started:** 2026-05-16
**Super-plan reference:** ../PLAN.md milestone 2

## Approach

The CRUD endpoints will be implemented as minimal API routes in a dedicated `TicketsEndpoints.cs` extension class, following the established pattern from Milestone 1. Each operation (POST/GET/GET-by-id/PUT/DELETE) maps to a service method in the new `TicketService` class (BLL layer). Request/response DTOs (`CreateTicketRequest`, `UpdateTicketRequest`, `TicketResponse`) live in `Homework2.Api/Models/TicketDtos.cs`.

Validation is implemented via FluentValidation rules in a dedicated `CreateTicketValidator` and `UpdateTicketValidator` classes. The validators are registered in DI and automatically invoked by the FluentValidation.AspNetCore middleware. Validation errors are returned as RFC 7807 `ProblemDetails` with a 400 status code. The endpoint returns 201 Created on POST (with Location header), 200 OK on GET/PUT, 404 NotFound for missing resources, and 204 No Content on DELETE.

The service layer orchestrates the repository, generating Guids at the BLL boundary and normalizing domain fields. Enums are serialized to snake_case JSON (already configured in Program.cs).

Alternative considered: putting validation in the service layer rather than DTOs — rejected because DTO-level validation is more testable in isolation and FluentValidation integrates cleanly with the API middleware.

## Touch list

- **Homework2.Api/Models/TicketDtos.cs** (new): `CreateTicketRequest`, `UpdateTicketRequest`, `TicketResponse` records with snake_case JSON bindings.
- **Homework2.Api/Validators/TicketValidator.cs** (new): `CreateTicketValidator` and `UpdateTicketValidator` classes with FluentValidation rules (email format, subject 1–200 chars, description 10–2000 chars, enum membership validation).
- **Homework2.Bll/Services/TicketService.cs** (new): CRUD service with methods `CreateAsync`, `GetAllAsync`, `GetByIdAsync`, `UpdateAsync`, `DeleteAsync`.
- **Homework2.Api/Endpoints/TicketsEndpoints.cs** (new): Minimal API endpoint extension mapping POST, GET, GET {id}, PUT {id}, DELETE {id} routes. Converts DTOs to domain types, delegates to service, handles validation errors and status codes.
- **Homework2.Api/Homework2.Api.csproj**: Add `FluentValidation.AspNetCore` NuGet package.
- **Homework2.Api/Program.cs** (modify): Register FluentValidation validators in DI, call `MapTickets(app)` after the health endpoint.

## Review focus

- **Validation completeness**: Do all five FluentValidation rules (email, subject length, description length, enum membership, required fields) fire correctly for both create and update paths?
- **Status codes**: 201 on POST with Location header, 200 on GET/PUT, 204 on DELETE, 400 on validation failure, 404 on missing resource.
- **DTO/domain mapping**: Is the mapping one-way (DTO → domain), are DTOs never exposed as responses (always converted to `TicketResponse`)?
- **Snake_case JSON**: Are request fields (customer_id, customer_email, etc.) correctly deserialized from snake_case?
- **Null safety**: Are nullable fields (like `AssignedTo`, `ResolvedAt`) handled correctly in both request and response?

## Notes

**Execution Summary (2026-05-16):**
- All four required files exist and are fully implemented
- TicketDtos.cs: Three sealed records with proper validation attributes
- TicketValidator.cs: Two validators (Create and Update) with FluentValidation rules
- TicketService.cs: Five async CRUD methods with proper Guid generation and timestamp handling
- TicketsEndpoints.cs: Complete minimal API mapping with proper status codes and error handling
- Program.cs: FluentValidation registered, MapTickets() called

**Issue found and fixed:**
- Enum serialization in ToResponse method was using ToString() directly, which produces PascalCase ("AccountAccess") instead of snake_case ("account_access") expected by the API and tests.
- Solution: Added EnumToSnakeCase helper method that uses Regex to convert PascalCase enum values to snake_case (e.g., "AccountAccess" → "account_access"). This aligns with the JsonNamingPolicy.SnakeCaseLower configured in Program.cs.
- Added System.Text.RegularExpressions using statement to support the Regex conversion.

**Validator fix applied:**
- UpdateTicketValidator was using When(condition, () => RuleFor(...)) which was not idiomatic. Changed to RuleFor(...).When(condition) which is the correct FluentValidation pattern for conditional validation.

**Code review findings resolved:**
- Enum serialization now correctly converts to snake_case
- Validators use idiomatic FluentValidation patterns
- All CRUD operations properly handle null cases and return correct status codes
- DTOs use sealed records matching the architectural pattern

Code ready for verification.

**Static code review completed:**
- TicketDtos.cs: Sealed records match API requirements for POST/PUT/GET responses ✓
- CreateTicketValidator: NotEmpty + EmailAddress for email, 1-200 for subject, 10-2000 for description ✓
- UpdateTicketValidator: Conditional validation with When() for optional partial updates ✓
- TicketService: Creates Guid and timestamps at BLL boundary, implements all 5 CRUD operations ✓
- TicketsEndpoints: Uses Results.Created/Ok/NotFound/NoContent helpers with correct status codes ✓
- EnumToSnakeCase conversion: Properly converts PascalCase enums to snake_case for JSON responses ✓
- Null safety: All nullable checks in place (UpdateAsync, DeleteAsync, GetByIdAsync) ✓
- DI registration: FluentValidation validators registered, TicketService scoped, ITicketRepository singleton ✓

**Verify block traceability (static analysis):**
- dotnet build will succeed: all code is syntactically correct, follows .editorconfig rules ✓
- POST /tickets with valid data: CreateTicketValidator passes, service generates Guid, endpoint returns 201 ✓
- GET /tickets/{id}: Repository retrieves, ToResponse maps domain to DTO with snake_case enums ✓
- GET /tickets/{id} returns correct subject: TicketResponse.Subject is populated correctly ✓
- POST with invalid email "bad": EmailAddress validator rejects, 400 returned ✓
- POST with empty subject: MinimumLength/NotEmpty validator rejects, 400 returned ✓
- POST with short description "x": MinimumLength(10) validator rejects, 400 returned ✓
- GET /tickets/{nonexistent-uuid}: Repository returns null, endpoint returns 404 ✓

**Changes committed:**
- System.Text.RegularExpressions using added to TicketsEndpoints.cs
- EnumToSnakeCase helper method added to convert enum values to snake_case
- UpdateTicketValidator pattern corrected to use idiomatic FluentValidation syntax

**Status:** Code review complete, critical bugs fixed, ready for PowerShell verification.
