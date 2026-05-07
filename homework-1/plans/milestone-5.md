# Milestone 5: Transaction history filters — Session Plan

**Started:** 2026-05-10
**Super-plan reference:** ../PLAN.md milestone 5

## Approach

The milestone extends `GET /transactions` to support optional query parameters for filtering: `accountId`, `type`, `from`, and `to` (as dates). The filters are optional and combinable, returning all rows matching all non-null filters.

**Strategy:**
1. Add overload signature to `ListTransactions` endpoint that accepts optional `string? accountId`, `string? type`, `DateOnly? from`, `DateOnly? to` query parameters.
2. Add a `ListAsync(filters)` method to `ITransactionRepository` that applies client-side filtering in the DAL. The in-memory repository will iterate through stored transactions and apply each non-null filter predicate.
3. Call the new repository method from `TransactionService.ListAsync` with the filter values.
4. The endpoint maps query parameters to the service call directly.

**Why this approach:** It localizes filter logic to the DAL where the data lives, keeps the service thin, and allows the endpoint to delegate parameter binding to ASP.NET Core's built-in model-binding. Date parsing is handled by the `DateOnly` type and ASP.NET's defaults. The in-memory repository can use LINQ to filter synchronously, then return the result as a sorted list.

**Alternative considered:** Adding filter logic directly in the endpoint layer. Rejected because it duplicates logic across endpoints if similar filtering is needed elsewhere, and violates separation of concerns (endpoint should orchestrate, not filter).

## Touch list

- **ITransactionRepository.cs**: Add `Task<IReadOnlyList<StoredTransaction>> ListAsync(string? accountId, string? type, DateOnly? from, DateOnly? to)` overload. The existing `ListAsync()` becomes a no-args convenience method that calls the new one with all nulls.
- **InMemoryTransactionRepository.cs**: Implement the new `ListAsync(...)` overload that filters in-memory data using LINQ predicates for each non-null filter. Keep the same ordering (descending by CreatedAt).
- **TransactionService.cs**: Add an overload `ListAsync(string? accountId, string? type, DateOnly? from, DateOnly? to)` that calls the repository method with the same parameters. The existing `ListAsync()` no-args method remains for backward compatibility.
- **TransactionsEndpoints.cs**: Modify the `ListTransactions` endpoint handler to accept optional query parameters and forward them to the service. The endpoint signature becomes `ListTransactions(TransactionService service, string? accountId = null, string? type = null, DateOnly? from = null, DateOnly? to = null)`.

## Review focus

- **Parameter binding**: Ensure ASP.NET Core's model binding correctly deserializes `DateOnly` from `from=YYYY-MM-DD` and `to=YYYY-MM-DD` strings without custom converters.
- **Filter logic correctness**: Verify that filters for `accountId` check both `fromAccount` and `toAccount` (the verify block tests `accountId=ACC-AAAAA` which should match both sender and receiver roles).
- **Empty/null handling**: All query parameters are optional; verify that null filters are skipped without breaking the chain.
- **Ordering**: Ensure results remain sorted descending by `CreatedAt` even after filtering.
- **No breaking changes**: Existing endpoints that call `ListAsync()` with no arguments should continue to work without modification.

## Notes

- Code review completed: all five review-focus criteria satisfied.
  - DateOnly binding supported natively by ASP.NET Core minimal APIs
  - accountId filter correctly checks both fromAccount and toAccount
  - Null/empty checks skip filters without breaking the chain
  - Ordering (DESC by CreatedAt) preserved after filtering
  - Backward compatibility maintained: no-args ListAsync() delegates to filtered version
- Removed unused `fromDateTime` variable that would have been flagged by .editorconfig analysis.
