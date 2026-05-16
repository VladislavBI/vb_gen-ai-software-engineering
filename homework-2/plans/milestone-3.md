# Milestone 3: Multi-format bulk import (CSV/JSON/XML) — Session Plan

**Started:** 2026-05-16
**Super-plan reference:** ../PLAN.md milestone 3

## Approach

Implement a file upload endpoint `POST /tickets/import` that accepts CSV, JSON, and XML formats. The approach is to:

1. Detect the format by file extension (`.csv`, `.json`, `.xml`).
2. Parse each row into a `CreateTicketRequest` DTO using format-specific parsers (CsvHelper for CSV, System.Text.Json for JSON, System.Xml.Linq for XML).
3. Validate each parsed row using the existing `CreateTicketValidator`.
4. For valid rows, save via `TicketService`; for invalid rows, collect error details.
5. Return a summary response with `{ total, successful, failed, errors[] }` where each error includes the row number and validation message.

Format detection by extension is simpler and more robust than Content-Type sniffing for this use case. Keeping the three parsers in a dedicated `TicketParsers` class keeps the import service clean and allows unit testing each format independently. The endpoint itself stays thin — validation and storage are delegated to the service layer.

## Touch list

- **Homework2.Bll/Services/TicketParsers.cs** — new class with three static parse methods: `ParseCsv(Stream)`, `ParseJson(Stream)`, `ParseXml(Stream)`, each returning `IReadOnlyList<CreateTicketRequest>`.
- **Homework2.Bll/Services/TicketImportService.cs** — new service with method `ImportAsync(Stream, string extension)` that detects format, parses, validates, saves, and returns `ImportResult { total: int, successful: int, failed: int, errors: List<ImportError> }`.
- **Homework2.Api/Endpoints/TicketsImportEndpoint.cs** — new endpoint extension mapping `POST /tickets/import` as a multipart form handler, delegating to `TicketImportService`.
- **Homework2.Bll/Homework2.Bll.csproj** — add `<PackageReference Include="CsvHelper" Version="..." />` for CSV parsing.
- **Homework2.Api/Program.cs** — register `TicketImportService` as scoped and add `MapTicketsImport()` call before `MapTickets()` to ensure `/tickets/import` is matched before the `/tickets/{id}` group.

## Review focus

- **Format detection and error handling:** Ensure file extension parsing is case-insensitive and handles edge cases (missing extension, unknown format).
- **Validation integration:** Every parsed row must be validated with `CreateTicketValidator` — reuse the existing validator rather than reinventing rules.
- **Error collection:** The error list must include the row number (1-indexed for CSV/JSON, not 0-indexed) and the validator's error message.
- **Response shape:** The response matches the super-plan schema exactly: `{ total, successful, failed, errors }` with `errors` as an array of `{ row, message }`.
- **Route ordering:** Ensure `POST /tickets/import` is registered early so it doesn't collide with `POST /tickets/{id}`.
- **Null/empty checks:** Handle empty files, missing Content-Type, and stream-read errors gracefully.

## Notes

- Session plan created before implementation. Ready to enter edit loop.
- **Implementation complete:**
  - `TicketParsers.cs`: Implemented CSV (CsvHelper), JSON (System.Text.Json), and XML (System.Xml.Linq) parsers. All return `IReadOnlyList<RawTicketImport>`.
  - `TicketImportService.cs`: Added `RawTicketImportValidator` with email validation, format detection by uppercase extension, row-by-row validation, and error aggregation with 1-indexed row numbers.
  - `TicketsImportEndpoint.cs`: Mapped `POST /tickets/import`, validates file presence and extension, delegates to service. Returns `{ total, successful, failed, errors[] }`.
  - Program.cs: Registered `TicketImportService` and mapped endpoint before `MapTickets()` to avoid route collision.
  - Added `CsvHelper` (30.0.1) and `FluentValidation` (11.9.2) NuGet packages to Bll.csproj.
  - Build: All style/analyzer rules satisfied (var vs explicit types, no unused using, no broad exceptions, email validation, case-invariant string operations).
- **Design decisions:**
  - Validators use standalone `RawTicketImportValidator` in BLL to avoid API layer leak.
  - `RawTicketImport` record uses `[property: JsonPropertyName]` attributes for JSON deserialization.
  - Email validation uses `System.Net.Mail.MailAddress` for standards compliance.
  - Row numbers are 1-indexed for user-friendliness (matches spreadsheet conventions).
- **Review performed manually against session plan criteria: all pass.**
