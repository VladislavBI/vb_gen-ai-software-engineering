# Homework 1 — Claude guidance

This file overrides the root `CLAUDE.md` for `homework-1/` scope. Anything not specialized here falls through to the root.

## Scope freeze (locked at planning, approved by user 2026-05-08)

`PLAN.md` decomposes TASKS.md Tasks 1–4 into 8 milestones. The non-obvious scope choices were approved by the user during `/homework 1 plan` and must not be re-litigated without an explicit re-plan:

- **Task 4** says *"Choose at least 1"* — we ship **two** options bundled into milestone 6:
  - **Option A** — `GET /accounts/{accountId}/summary` (totals + count + most-recent timestamp). Reuses existing repository data with no new infrastructure.
  - **Option D** — Per-IP fixed-window rate limiting (100 req/min/IP) via `Microsoft.AspNetCore.RateLimiting`. The middleware ships in `Microsoft.AspNetCore.App` — no extra NuGet package.
- **NBomber load-test project** (`Homework1.LoadTests`) is added beyond the required xUnit `Homework1.Tests` and bundled with docs/demo into milestone 8. NBomber exists to exercise the rate limiter under concurrency; the deterministic xUnit Tests milestone (M7) stays free of timing-sensitive scenarios so grading is reproducible.
- **Milestone 2 ships 5 files** (`Transaction.cs`, `ITransactionRepository.cs`, `InMemoryTransactionRepository.cs`, `TransactionService.cs`, `TransactionsEndpoints.cs`), one over the 1–4 sizing heuristic. Splitting storage from the first endpoint pair would leave a milestone with no behavioral verify; the trade-off is recorded in PLAN.md and was accepted.

If a future re-plan reduces or expands this scope, update both `PLAN.md` and this file.

## Port assignments

| Port | Use | Where |
|---|---|---|
| **5080** | Primary API for every Verify block | `--urls http://localhost:5080` in milestones 1–7 |
| **5081** | Parallel API for NBomber to hit | Milestone 8 only — main API may still be on 5080 in a separate Verify run |

These deviate from `TASKS.md`'s example port 3000. The 5080/5081 pair is documented in `.claude/docs/Infrastructure/powershell-conventions.md#port-discipline`.

## Stack specializations

- **Storage** — `ConcurrentDictionary<Guid, TransactionEntity>` registered as a singleton in DAL (per `common-rules.md#storage-dal`). Entity type is DAL-owned; BLL maps to/from the `Transaction` domain record at the repository call boundary.
- **Validation** — FluentValidation surfaced as RFC 7807 `ProblemDetails` with the TASKS.md error shape `{ error, details: [{field, message}] }`. Account-id format `^ACC-[A-Z0-9]+$`; amount must be `> 0` with at most 2 decimal places; currency must be a valid ISO 4217 code.
- **Rate limiting** — built-in `Microsoft.AspNetCore.RateLimiting` middleware, fixed-window policy (100 requests / 1 minute / partition-key = remote IP). Returns HTTP 429 when exceeded.
- **Solution file** — `Homework1.sln` (classic format). Created with `dotnet new sln --format sln` to override .NET 10's default `.slnx`. See `.claude/docs/Architecture/project-architecture.md#scaffold-powershell` for the full scaffold sequence.

## Files to leave alone

- `homework-1/TASKS.md` — instructor-authored spec, read-only.
- Any milestone with `Done: [x]` in `PLAN.md` — frozen evidence per the planning process. Re-plans rewrite only `[ ]`/`[~]`/`[!]` sections.
- Session plans under `homework-1/plans/` for completed milestones — frozen with the milestone.
