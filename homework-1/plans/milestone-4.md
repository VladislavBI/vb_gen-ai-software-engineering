# Milestone 4: Request validation (FluentValidation + ProblemDetails) — Session Plan

**Started:** 2026-05-09
**Super-plan reference:** ../PLAN.md milestone 4

## Approach

We implement FluentValidation for the `CreateTransactionRequest` DTO with three rule categories: account-id format (`^ACC-[A-Z0-9]+$`), amount validation (non-positive rejection + 2-decimal-place limit), and ISO 4217 currency code validation. FluentValidation is already referenced in the `.csproj` via `FluentValidation.AspNetCore`. We create a dedicated validator class in `Homework1.Api/Validators/`, register it in the DI container in `Program.cs` using `AddValidatorsFromAssembly`, and wire the validation middleware so failed validation surfaces as RFC 7807 `ProblemDetails` with the shape `{ error, details: [{field, message}] }` matching the TASKS.md spec. The error response is built by mapping FluentValidation's `ValidationFailure` collection into `Results.ValidationProblem` with custom field/message extraction. This isolates validation from storage and allows Tests (M7) to verify validator behavior independently.

## Touch list

- **`Homework1.Api/Validators/CreateTransactionRequestValidator.cs`** (new file) — FluentValidation rule definitions for `CreateTransactionRequest`: account-id regex for `fromAccount` and `toAccount`, decimal amount > 0 with at most 2 places, currency ISO 4217 codes.
- **`Homework1.Api/Endpoints/TransactionsEndpoints.cs`** — wrap `CreateTransaction` handler with validation logic that invokes the validator and returns HTTP 400 with `ValidationProblem` if errors exist.
- **`Homework1.Api/Program.cs`** — register the validator in the DI container using `services.AddValidatorsFromAssembly(typeof(Program).Assembly)`.

## Review focus

- Validator rule syntax is declarative and matches the TASKS.md requirements exactly (account format, amount > 0, max 2 decimals, ISO 4217 codes).
- HTTP 400 error response shape is `{ error, details: [{field, message}] }` per TASKS.md, not the default `ProblemDetails` format.
- Account-id regex correctly rejects `bad-id` (lowercase letters, hyphen) and accepts `ACC-12345` and similar valid formats.
- Decimal validation rejects both non-positive amounts and amounts with > 2 decimal places (e.g., 12.555).
- Currency validation uses a known ISO 4217 code list (USD, EUR, etc.) and rejects unknown codes like `ZZZ`.
- Valid requests (e.g., `{ fromAccount: 'ACC-12345', toAccount: 'ACC-67890', amount: 12.50, currency: 'USD', type: 'transfer' }`) bypass validation and are created normally.

## Notes

- **Implementation review (2026-05-09)**: Validator implemented with FluentValidation. Rules: account-id regex `^ACC-[A-Z0-9]+$` for both fields, amount `> 0` with at most 2 decimals checked via `(amount * 100) == floor(amount * 100)`, currency validated against comprehensive ISO 4217 code list. DI registration in Program.cs via `AddValidatorsFromAssemblyContaining`. HTTP 400 response returns shape `{ error: "Validation failed", details: [{field, message}] }` per TASKS.md. All review focus items addressed: rules match spec, error shape correct, valid/invalid cases handled. Ready for verify.
