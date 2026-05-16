# Testing Strategy

Default test stack for every homework: **xUnit + FluentAssertions + Moq + coverlet** in a single test project (`HomeworkN.Tests`) that mirrors the `Api/Bll/Dal` folder structure inside.

## Tooling

- **xUnit** (`dotnet new xunit`) — test runner.
- **FluentAssertions** — readable asserts (`result.Should().Be(...)`).
- **Moq** — mock at layer boundaries only (e.g., mock the BLL's repo interface when unit-testing a BLL service).
- **coverlet.collector** — coverage; included by the xUnit template.
- **Microsoft.AspNetCore.Mvc.Testing** — for API integration tests via `WebApplicationFactory<Program>`.

## What to test in each layer

| Layer | Test type | What to assert |
|---|---|---|
| API | Integration via `WebApplicationFactory` | HTTP status codes, response shape, validation errors (`ProblemDetails`), routing. Real BLL + DAL in-memory wired up. |
| BLL | Pure unit | Business rules, domain transitions, error paths. **Mock the DAL repo interface** with Moq. |
| DAL | Pure unit | In-memory store CRUD, isolation between calls, concurrency safety. No mocks. |
| Validators | Pure unit | Each FluentValidation rule with valid + invalid inputs. |

## Project layout (mirrors src)

```
HomeworkN.Tests/
├── Api/
│   ├── Endpoints/          # WebApplicationFactory integration tests
│   └── Validators/
├── Bll/
│   └── Services/
└── Dal/
    └── Repositories/
```

## Test isolation

- **No `static` fields anywhere in production code.** They survive across tests and silently break isolation. Use `ConcurrentDictionary<Guid, T>` registered as a singleton service — `WebApplicationFactory` then gives each fixture a fresh container.
- For API integration tests, override the DAL registration with a fresh in-memory instance per test class via `WebApplicationFactory<Program>.WithWebHostBuilder(b => b.ConfigureServices(...))`.
- Each BLL service test creates a fresh `Mock<IRepository>` — never share a mock across tests.

## Commands (PowerShell)

```powershell
# Full run
dotnet test

# Filter by namespace / class
dotnet test --filter "FullyQualifiedName~Validators"
dotnet test --filter "ClassName=TransactionsEndpointsTests"

# Coverage (HW2 requires >85%)
dotnet test --collect:"XPlat Code Coverage"

# CI-style: warnings as errors
dotnet build -warnaserror
```

For PowerShell shell quirks see `../Infrastructure/powershell-conventions.md`.

## Coverage targets

- HW2 specifies **>85% line coverage**. Other homeworks default to "high enough that BLL services and validators have no obvious gaps."
- When `TASKS.md` doesn't pin a number, document the achieved coverage in the homework's `README.md` Quality section.
