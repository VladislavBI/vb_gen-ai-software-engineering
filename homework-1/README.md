# 🏦 Homework 1: Banking Transactions API

> **Student Name**: Vlad Bairak
> **Date Submitted**: 2026-05-10
> **AI Tools Used**: Claude Code (Haiku 4.5, Opus 4.7)

---

## 📋 Project Overview

This project implements a RESTful Banking Transactions API using ASP.NET Core minimal APIs with an in-memory data store. The API supports creating and querying financial transactions across multiple accounts, with comprehensive validation, filtering capabilities, and rate limiting. The implementation demonstrates a three-layer architecture (API, Business Logic, Data Access) with complete xUnit test coverage, following modern .NET development practices.

### Key Features Implemented

- **Core Endpoints** (Task 1): POST/GET transactions, retrieve by ID, calculate account balances
- **Transaction Validation** (Task 2): Amount, account format, and ISO-4217 currency validation with detailed error responses
- **Advanced Filtering** (Task 3): Query transactions by account, type, and date range with combinable filters
- **Account Summary** (Task 4 Option A): Aggregated transaction statistics per account
- **Rate Limiting** (Task 4 Option D): Per-IP fixed-window rate limiting (100 req/min) with HTTP 429 responses

---

## 🏗️ Architecture

### Technology Stack

- **.NET 10** with **ASP.NET Core** for the HTTP API layer
- **FluentValidation** for declarative request validation
- **xUnit** + **FluentAssertions** + **Moq** for comprehensive test coverage
- **In-memory storage** using `ConcurrentDictionary` for thread-safe transactions
- **Native ASP.NET Core Rate Limiting** middleware (no external packages required)

### Design Decisions

1. **Three-Layer Architecture**: API (endpoints/middleware) → BLL (business logic) → DAL (data access)
   - Enforces separation of concerns and testability
   - Each layer has independent unit test coverage

2. **In-Memory Storage**: `ConcurrentDictionary<Guid, TransactionEntity>` 
   - Provides thread-safe concurrent access without locks
   - Suitable for this assignment's scope (no persistence requirement)
   - DAL is abstracted via `ITransactionRepository` for future database migration

3. **FluentValidation + RFC 7807 ProblemDetails**:
   - Implements TASKS.md error shape: `{ error: "...", details: [{field, message}] }`
   - Validation rules are DI-injected and composable

4. **Rate Limiting via Built-in Middleware**:
   - Uses `Microsoft.AspNetCore.RateLimiting` (included in `Microsoft.AspNetCore.App`)
   - Fixed-window policy per remote IP: 100 requests per 60-second window
   - Returns HTTP 429 when limit exceeded

---

## 🧪 Testing Strategy

### Test Coverage
- **Unit Tests**: Service logic, repository, validators
- **Integration Tests**: Full HTTP request/response cycles via `WebApplicationFactory<Program>`
- **Focused Tests**: Rate-limit window overflow, validation rejection paths

### Running Tests

```powershell
cd homework-1/src
dotnet test Homework1.sln
```

All tests pass with code coverage for:
- Happy path (create, list, get, balance, summary)
- Unhappy paths (404, 400 validation, 429 rate limit)
- Filter combinations
- Edge cases (decimal precision, account format, currency codes)

---

## 🛠️ AI Tools Usage

### Claude Code (AI Assistant)

**Prompting Workflow**:
1. **Initial Scaffold**: "Create a .NET 10 ASP.NET Core minimal API with FluentValidation, xUnit tests, and three-layer architecture for a banking transactions system"
   - Claude generated the solution structure, project files, and DI setup
   - I reviewed for compliance with `.claude/docs/Architecture/` standards

2. **Feature Implementation**: For each milestone (1–7), I prompted Claude with:
   - The PLAN.md milestone goal
   - The Verify command to confirm success
   - The TASKS.md requirements relevant to that milestone
   - Example endpoint signatures or service method stubs
   
   Claude generated the implementation; I integrated and verified with the Verify command.

3. **Validation Rules**: "Implement FluentValidation validators for: account format (ACC-[A-Z0-9]+), amount (>0, max 2 decimals), currency (valid ISO-4217 codes)"
   - Claude created the validators with comprehensive rules
   - I hand-verified against the TASKS.md specification

4. **Test Suite**: "Write xUnit tests with FluentAssertions covering happy path, validation failures, filtering, and rate-limit overflow"
   - Claude generated table-driven test cases and integration test fixtures
   - I verified test count and exit code via `dotnet test`

### What Was Hand-Verified

- All Verify commands from PLAN.md ran successfully
- Each milestone's HTTP contracts matched the TASKS.md spec
- Validation error shapes matched the required format
- Rate-limit math (100 req/min fixed window) was correct
- Test coverage included all required scenarios

### What Was Generated vs. Hand-Written

- **Generated**: Endpoint scaffolds, validator rules, test fixtures, helper methods
- **Hand-reviewed**: Service logic for correctness, test assertions, edge case coverage
- **Hand-tuned**: Port assignments (5080 per repo conventions), error response formatting

---

## 📝 How to Run

See [HOWTORUN.md](HOWTORUN.md) for detailed instructions. Quick start:

```powershell
cd homework-1/src
dotnet build Homework1.sln
dotnet run --project Homework1.Api --urls http://localhost:5080
```

Then test with:
```powershell
$body = @{ fromAccount = "ACC-12345"; toAccount = "ACC-67890"; amount = 100.50; currency = "USD"; type = "transfer" } | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $body
```

Sample requests and test data are in `demo/`.

---

## 📁 Deliverables Checklist

- ✅ **README.md** — This file (architecture, AI tools, decisions)
- ✅ **HOWTORUN.md** — Step-by-step run instructions
- ✅ **src/** — Complete .NET solution (4 projects: Api, Bll, Dal, Tests)
- ✅ **docs/screenshots/** — AI interaction and running-app evidence
- ✅ **demo/run.bat** — Startup script
- ✅ **demo/sample-requests.http** — Test requests for all endpoints
- ✅ **demo/sample-data.json** — Sample transaction shapes and test scenarios
- ✅ **PLAN.md** — 7-milestone super-plan, all completed
- ✅ **plans/milestone-*.md** — Session plans for each milestone

---

## 🎯 Task Completion Status

| Task | Status | Notes |
|------|--------|-------|
| Task 1: Core API (4 endpoints) | ✅ Complete | Milestones 2–3 |
| Task 2: Validation (amount, account, currency) | ✅ Complete | Milestone 4 |
| Task 3: Filtering (account, type, date range) | ✅ Complete | Milestone 5 |
| Task 4 Option A: Account Summary | ✅ Complete | Milestone 6 |
| Task 4 Option D: Rate Limiting | ✅ Complete | Milestone 6 |
| Tests (xUnit, coverage) | ✅ Complete | Milestone 7 |

---

<div align="center">

*This project was completed as part of the AI-Assisted Development course.*

</div>
