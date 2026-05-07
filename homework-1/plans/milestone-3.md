# Milestone 3: Get transaction by id and account balance — Session Plan

**Started:** 2026-05-09
**Super-plan reference:** ../PLAN.md milestone 3

## Approach

Milestone 3 closes out Task 1's remaining two read endpoints:
1. `GET /transactions/{id}` — retrieves a single transaction by ID, returning the transaction if found or 404 if not.
2. `GET /accounts/{accountId}/balance` — computes account balance from completed transactions (treating "deposit"/"credit" as additions, "withdrawal"/"debit" as subtractions).

The implementation strategy:
- Extend `ITransactionRepository` with a `GetByIdAsync(Guid id)` method that returns `StoredTransaction?` (nullable).
- Implement the method in `InMemoryTransactionRepository` using `_store.TryGetValue()`.
- Add `GetByIdAsync(Guid id)` and `GetAccountBalanceAsync(string accountId)` methods to `TransactionService`. The balance method iterates over all completed transactions, filters by accountId (matching either `FromAccount` or `ToAccount`), and sums based on transaction type.
- Add a new `AccountsEndpoints.cs` file with a `MapAccounts` extension that registers the balance endpoint.
- Modify `TransactionsEndpoints.cs` to add the GET by ID route.
- Wire both endpoints into `Program.cs` alongside the existing `MapTransactions` call.

Alternative considered and rejected:
- Computing balance directly in the endpoint (no service method) — rejected because it couples the endpoint to storage details and violates separation of concerns; the service layer should own the domain logic.
- Using a separate `IAccountRepository` — rejected as over-engineering; accounts are derived from transaction data, not stored entities.

## Touch list

- `homework-1/src/Homework1.Bll/Abstractions/ITransactionRepository.cs`: Add `GetByIdAsync(Guid id)` method signature returning `Task<StoredTransaction?>`.
- `homework-1/src/Homework1.Dal/Repositories/InMemoryTransactionRepository.cs`: Implement `GetByIdAsync` using `_store.TryGetValue()`. Add internal `TransactionEntity` record if not already present (it is).
- `homework-1/src/Homework1.Bll/Services/TransactionService.cs`: Add `GetByIdAsync(Guid id)` and `GetAccountBalanceAsync(string accountId)` methods.
- `homework-1/src/Homework1.Api/Endpoints/TransactionsEndpoints.cs`: Add route `MapGet("/{id}", GetTransactionById)` to the transactions group. Add `GetTransactionById` handler that returns 404 via `Results.NotFound()` when the transaction is not found.
- `homework-1/src/Homework1.Api/Endpoints/AccountsEndpoints.cs`: Create new file with `MapAccounts` extension. Add route `MapGet("/{accountId}/balance", GetAccountBalance)` that calls `TransactionService.GetAccountBalanceAsync` and returns a balance response record.
- `homework-1/src/Homework1.Api/Program.cs`: Call `app.MapAccounts()` after `MapTransactions()`.

## Review focus

- **Null handling**: `GetByIdAsync` returns nullable `StoredTransaction?`. Verify the endpoint properly uses `Results.NotFound()` when null.
- **Balance calculation logic**: Confirm the balance correctly interprets transaction types (which fields are added vs. subtracted). The `type` field in `TransactionEntity` holds values like "deposit", "withdrawal", "transfer" — verify the logic matches the naming.
- **Account ID matching**: Balance sums transactions where the account is either `FromAccount` or `ToAccount`. Ensure both sides are checked and the logic is correct for the account's perspective (deposits add, withdrawals subtract).
- **Response shape**: New endpoints should return camelCase JSON via the existing `System.Text.Json` configuration in `Program.cs`.
- **No cross-layer leaks**: `TransactionsEndpoints` and `AccountsEndpoints` should not reference DAL types; all mapping happens at the service layer.

## Notes

- Implemented `GetByIdAsync(Guid id)` in both the repository interface and the in-memory implementation using `TryGetValue()`.
- Added corresponding service method that delegates to the repository.
- Added `GET /transactions/{id}` route to TransactionsEndpoints, returning 404 via `Results.NotFound()` when the transaction is not found.
- Implemented `GetAccountBalanceAsync(string accountId)` in TransactionService that iterates over all transactions and:
  - Adds to balance when account is `ToAccount` and type is "deposit"/"credit" or "transfer"
  - Subtracts from balance when account is `FromAccount` and type is "withdrawal"/"debit" or "transfer"
- Created new `AccountsEndpoints.cs` file with `MapAccounts` extension and `GET /accounts/{accountId}/balance` route.
- Wired `MapAccounts()` call into `Program.cs` after `MapTransactions()`.
- Fixed style errors: used explicit types instead of `var` for `IReadOnlyList<StoredTransaction>`, `StoredTransaction`, and `StoredTransaction transaction` per `.editorconfig` rules.
- Verify block passed: build succeeded, POST created a transaction, GET by ID returned the same transaction, 404 returned for unknown ID, and balance endpoint responded successfully.
