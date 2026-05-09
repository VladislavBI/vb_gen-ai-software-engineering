# Milestone 2: Create and list transactions (storage + endpoints) — Session Plan

**Started:** 2026-05-09
**Super-plan reference:** ../PLAN.md milestone 2

## Approach

Build the minimal three-layer stack for reading and writing transactions:

1. **Domain (`Bll/Domain/`)**: Declare a `Transaction` record holding `Id` (Guid), `FromAccount`, `ToAccount`, `Amount` (decimal), `Currency`, `Type`, `CreatedAt` (DateTimeOffset). Use record for structural equality and immutability.

2. **Repository abstraction (`Bll/Abstractions/`)**: Define `ITransactionRepository` with two methods: `CreateAsync(Transaction) → Task<Guid>` (returns the created ID) and `ListAsync() → Task<IReadOnlyList<Transaction>>` (returns all stored transactions).

3. **DAL storage (`Dal/Repositories/`)**: Implement `InMemoryTransactionRepository` backed by a `ConcurrentDictionary<Guid, TransactionEntity>` singleton. Map between the BLL `Transaction` domain type and a DAL-owned `TransactionEntity` internal type (adding `DateTimeOffset CreatedAt` for audit trailing). The entity is created inside the repository at Create time; the domain record (without the ID) is passed in, the entity is stored with a generated ID, and the ID is returned.

4. **BLL service (`Bll/Services/`)**: Wire `TransactionService` injecting `ITransactionRepository`. Expose `CreateAsync(Transaction)` and `ListAsync()` methods that delegate to the repository.

5. **API endpoints (`Api/Endpoints/`)**: Map `POST /transactions` (request: camelCase JSON matching TASKS.md schema; response: 201 with created transaction including the ID) and `GET /transactions` (request: none; response: 200 JSON array of transactions). Wire both into `Program.cs` via an extension method `MapTransactions(this IEndpointRouteBuilder)`.

Register `TransactionService` and `InMemoryTransactionRepository` in DI in `Program.cs` under a scope where the singleton dictionary is reachable (exactly once across the app lifetime).

Wire JSON serialization with camelCase naming policy in `Program.cs` using `System.Text.Json` options.

Alternatives considered and rejected:
- **Entity Framework**: TASKS.md mandates in-memory storage; EF adds ceremony without benefit.
- **Static dictionary**: A static field in the DAL would couple the storage to the DAL class and prevent testing/re-seeding; a singleton service dependency is cleaner.
- **Returning Transaction from POST instead of 201 with Location header**: RFC 7807 conventions use `Results.Created` which sets the Location header; the TASKS.md shape example shows the created transaction body, so we return both.

## Touch list

- `homework-1/src/Homework1.Bll/Domain/Transaction.cs` — new domain record with Id, FromAccount, ToAccount, Amount, Currency, Type, CreatedAt.
- `homework-1/src/Homework1.Bll/Abstractions/ITransactionRepository.cs` — new interface: `CreateAsync(Transaction)` and `ListAsync()`.
- `homework-1/src/Homework1.Dal/Repositories/InMemoryTransactionRepository.cs` — new class implementing ITransactionRepository; holds `ConcurrentDictionary<Guid, TransactionEntity>` with an internal TransactionEntity type; maps Transaction ↔ TransactionEntity.
- `homework-1/src/Homework1.Bll/Services/TransactionService.cs` — new service injecting ITransactionRepository; delegates to repository methods.
- `homework-1/src/Homework1.Api/Endpoints/TransactionsEndpoints.cs` — new file: `MapTransactions(this IEndpointRouteBuilder)` extension wiring POST and GET.
- `homework-1/src/Homework1.Api/Program.cs` — register DI bindings (singleton dictionary, scoped service, optional logger); call `MapTransactions(app)`; configure `System.Text.Json` with camelCase naming.

## Review focus

- **Type discipline**: Verify `Transaction` is a `record`, `TransactionEntity` is internal DAL-owned, no type leaks across layers.
- **Dependency direction**: Api depends on Bll; Bll depends on abstractions in `Bll/Abstractions/`; DAL implements interfaces from Bll; no circular or backward refs.
- **Async/await**: Both Create and List are async (`Task<T>`) per common patterns for future DB swaps.
- **Guid generation**: ID is generated inside the repository's Create method, not in the BLL or API.
- **JSON camelCase**: Verify `PropertyNamingPolicy.CamelCase` is wired in `Program.cs` and the created response uses camelCase keys (e.g., `createdAt` not `CreatedAt`).
- **No validation**: This milestone deliberately skips validation (Task 2 is M4). Ensure the endpoint accepts any malformed request without 400s.
- **Singleton scope**: The `ConcurrentDictionary` is registered as singleton so state persists across requests within a single app run; confirm the DI binding is correct.

## Notes

- Implementation completed without deviations from the session plan.
- Domain model split into `Transaction` (for requests, without ID) and `StoredTransaction` (with ID and CreatedAt timestamp) to maintain clean separation between creation input and returned data.
- DAL entity type `TransactionEntity` is internal to the repository class, not exposed across layers.
- Dependency graph verified: Api → Bll → Dal (one-way). Bll has no reference to Dal (only Abstractions). BLL.csproj had a forward reference that was removed.
- JSON camelCase serialization configured in Program.cs using System.Text.Json's built-in JsonNamingPolicy.CamelCase.
- Singleton ConcurrentDictionary registration ensures state persists across requests within a single app run.
- All endpoints async (Task<T>) per convention.
- Guid generated inside repository's CreateAsync, not in API layer.
- Code review focused on: type discipline, dependency direction, async patterns, ID generation scope, JSON serialization, no validation (deferred to M4), singleton scope. All criteria satisfied.
- Verify block: build succeeded (0 errors), app started on port 5080, POST /transactions created a transaction with returned ID, amount round-tripped correctly, GET /transactions returned non-empty list. ✓

