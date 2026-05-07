# Milestone 7: Tests — API integration + BLL/DAL/validator unit coverage — Session Plan

**Started:** 2026-05-10
**Super-plan reference:** ../PLAN.md milestone 7

## Approach

We implement a comprehensive test suite across all three layers plus validators, using the xUnit + FluentAssertions + Moq + WebApplicationFactory stack documented in `.claude/docs/Architecture/testing-strategy.md`.

The strategy is:
1. **API integration tests** via `WebApplicationFactory<Program>` to exercise every endpoint (POST/GET /transactions, GET /transactions/{id}, GET /accounts/{accountId}/balance, GET /accounts/{accountId}/summary) with happy-path and error cases, plus validation rejection paths. These tests wire the real BLL + DAL in-process (via a fresh in-memory store per test class).
2. **Validator unit tests** for `CreateTransactionRequestValidator` covering the amount (>0, at most 2 decimals), account-id format (ACC-[A-Z0-9]+), and currency (valid ISO 4217) rules with table-driven cases for both valid and invalid inputs.
3. **BLL service unit tests** for `TransactionService` methods (Create, List, GetById, GetAccountBalance, GetAccountSummary) with a mocked `ITransactionRepository` to isolate business logic from storage.
4. **DAL unit tests** for `InMemoryTransactionRepository` covering CRUD operations, filtering (accountId, type, date range), and ordering, with no mocks — testing the in-memory store directly.
5. **Rate-limit integration test** using a test-only override policy (3 requests/5 seconds window) to deterministically trigger a 429 response without needing 110+ requests, keeping the test fast and reproducible. We override via `WebApplicationFactory<Program>.WithWebHostBuilder()` to inject a test policy.

All tests follow the folder structure in `testing-strategy.md`: `Tests/Api/Endpoints/`, `Tests/Api/Validators/`, `Tests/Bll/Services/`, `Tests/Dal/Repositories/`.

## Touch list

- **Tests/Api/Endpoints/TransactionsEndpointsTests.cs** — WebApplicationFactory integration tests for POST /transactions (create, validation rejection), GET /transactions (list, filters), GET /transactions/{id} (found, not found).
- **Tests/Api/Endpoints/AccountsEndpointsTests.cs** — WebApplicationFactory integration tests for GET /accounts/{accountId}/balance and GET /accounts/{accountId}/summary.
- **Tests/Api/Endpoints/RateLimitingIntegrationTests.cs** — WebApplicationFactory test with a small test-only rate-limit policy (3 requests/5 sec) to verify HTTP 429 is returned when exceeded.
- **Tests/Api/Validators/CreateTransactionRequestValidatorTests.cs** — Pure unit tests for amount, account-id, and currency validation rules with table-driven valid/invalid cases.
- **Tests/Bll/Services/TransactionServiceTests.cs** — Unit tests for CreateAsync, ListAsync (no filter, with filters), GetByIdAsync, GetAccountBalanceAsync, GetAccountSummaryAsync, each with a fresh Mock<ITransactionRepository>.
- **Tests/Dal/Repositories/InMemoryTransactionRepositoryTests.cs** — Unit tests for CreateAsync, ListAsync, GetByIdAsync, and filter combinations, exercising the ConcurrentDictionary store directly.

## Review focus

- **Test isolation**: Each API integration test class creates a fresh WebApplicationFactory<Program> instance with a clean in-memory store per test method (no shared state across tests).
- **Mocking boundaries**: BLL service tests mock only ITransactionRepository; no mocking of lower-level types or the store itself.
- **Validation coverage**: The validator test suite covers all rules (amount range + decimals, account-id format, currency) with both passing and failing cases.
- **Rate-limit test determinism**: The rate-limit test uses a small window (3 req / 5 sec) configurable via test policy override, not the production 100 req/min, so it completes quickly without timing sensitivity.
- **Async/await consistency**: All test methods and service calls properly use async/await; no blocking or `.Result` calls.
- **FluentAssertions idioms**: Assertions use `.Should().Be()`, `.Should().NotBeNull()`, `.Should().HaveCount()`, etc., for readability.

## Notes

(Appended during execution; empty at start.)
