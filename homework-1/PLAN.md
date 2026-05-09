# Homework 1 — Plan

**TASKS.md commit:** 6d862a4bd1542df262749a488ee39d083cb0c34f
**Created:** 2026-05-08
**Stack:** .NET 10 / ASP.NET Core (minimal API, in-memory storage)

## Overview

Homework 1 (TASKS.md Tasks 1–4) asks for a REST API for banking transactions with `POST /transactions`, `GET /transactions` (with filters from Task 3), `GET /transactions/:id`, `GET /accounts/:accountId/balance`, plus at least one extra Task 4 feature. We translate the Node/Python example to ASP.NET Core minimal APIs using the repo's standard API + BLL + DAL + Tests three-layer split with a `ConcurrentDictionary`-backed in-memory store. We pick **two** Task 4 features bundled into a single milestone: **Option A (Transaction Summary)** because it reuses existing repository data with no new infrastructure, and **Option D (Rate Limiting)** which leverages ASP.NET Core's native `Microsoft.AspNetCore.RateLimiting` middleware. Validation (Task 2) is implemented with FluentValidation surfaced as RFC 7807 `ProblemDetails`. Beyond the required xUnit Tests milestone, we add a separate NBomber load-test project to exercise the rate limiter and core endpoints under concurrency.

## Milestones

### Milestone 1: Scaffold solution and DI skeleton
- **Goal:** Stand up the four-project .NET solution (`Homework1.Api`, `Homework1.Bll`, `Homework1.Dal`, `Homework1.Tests`) under `homework-1/src/` with project references, the static `.editorconfig` + `Directory.Build.props`, and a `Program.cs` that boots the API and exposes a `GET /health` endpoint.
- **Why this milestone:** Establishes the build skeleton every other milestone depends on, including the test project that Tests will fill in. Scaffolding earns its own step because `dotnet new` + project-reference wiring + the static analyzer files is a self-contained deliverable that must compile before any feature work; if it slips, every later `dotnet build` fails for the wrong reason.
- **Files:** homework-1/src/Homework1.sln, homework-1/src/.editorconfig, homework-1/src/Directory.Build.props, homework-1/src/Homework1.Api/Program.cs
- **Depends on:** none
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-1\src
  dotnet build Homework1.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework1.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 4
      $health = Invoke-RestMethod -Uri http://localhost:5080/health -Method Get
      if (-not $health) { throw "no health response" }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [x]

### Milestone 2: Create and list transactions (storage + endpoints)
- **Goal:** Implement the `Transaction` domain record, the `ITransactionRepository` abstraction, the `ConcurrentDictionary`-backed in-memory implementation, a `TransactionService` for create/list, and the `POST /transactions` + `GET /transactions` endpoints (no validation or filtering yet) returning camelCase JSON with the TASKS.md transaction shape.
- **Why this milestone:** This is the smallest slice that yields a behaviorally verifiable HTTP path through all three layers (API → BLL → DAL), satisfying the planning-process rule that each milestone exercise behavior. Splitting the storage layer into its own milestone would leave it with only existence-style verifies; bundling it with the first write/read endpoint pair earns a real `Invoke-RestMethod` round-trip. This milestone is one file over the 1–4 soft heuristic (5 files) for that reason.
- **Files:** homework-1/src/Homework1.Bll/Domain/Transaction.cs, homework-1/src/Homework1.Bll/Abstractions/ITransactionRepository.cs, homework-1/src/Homework1.Dal/Repositories/InMemoryTransactionRepository.cs, homework-1/src/Homework1.Bll/Services/TransactionService.cs, homework-1/src/Homework1.Api/Endpoints/TransactionsEndpoints.cs
- **Depends on:** 1
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-1\src
  dotnet build Homework1.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework1.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 4
      $body = @{ fromAccount = 'ACC-12345'; toAccount = 'ACC-67890'; amount = 100.50; currency = 'USD'; type = 'transfer' } | ConvertTo-Json
      $created = Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $body
      if (-not $created.id) { throw "POST did not return id" }
      if ($created.amount -ne 100.50) { throw "amount round-trip wrong" }
      $list = Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Get
      if ($list.Count -lt 1) { throw "GET list returned no rows" }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [x]



### Milestone 3: Get transaction by id and account balance
- **Goal:** Add `GET /transactions/{id}` (404 when missing) and `GET /accounts/{accountId}/balance` (computed from completed credits minus debits per account), wiring through `TransactionService` and a new `AccountsEndpoints` map.
- **Why this milestone:** Closes out Task 1's remaining two read endpoints and exercises the 404 path that POST + LIST do not. Splits cleanly from M2 because it adds new endpoint files and a new service method without rewriting existing storage; isolated verify can confirm both happy and not-found paths.
- **Files:** homework-1/src/Homework1.Api/Endpoints/AccountsEndpoints.cs, homework-1/src/Homework1.Api/Endpoints/TransactionsEndpoints.cs, homework-1/src/Homework1.Bll/Services/TransactionService.cs
- **Depends on:** 2
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-1\src
  dotnet build Homework1.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework1.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 4
      $body = @{ fromAccount = 'ACC-12345'; toAccount = 'ACC-67890'; amount = 50.00; currency = 'USD'; type = 'transfer' } | ConvertTo-Json
      $created = Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $body
      $byId = Invoke-RestMethod -Uri "http://localhost:5080/transactions/$($created.id)" -Method Get
      if ($byId.id -ne $created.id) { throw "GET by id did not match" }
      try {
          Invoke-RestMethod -Uri 'http://localhost:5080/transactions/00000000-0000-0000-0000-000000000000' -Method Get | Out-Null
          throw "expected 404 for unknown id"
      } catch {
          if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "expected 404, got $($_.Exception.Response.StatusCode.value__)" }
      }
      $balance = Invoke-RestMethod -Uri 'http://localhost:5080/accounts/ACC-67890/balance' -Method Get
      if ($null -eq $balance) { throw "balance endpoint returned nothing" }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [x]

### Milestone 4: Request validation (FluentValidation + ProblemDetails)
- **Goal:** Reject `POST /transactions` requests where `amount` is non-positive or has more than 2 decimal places, where `fromAccount`/`toAccount` does not match `^ACC-[A-Z0-9]+$`, or where `currency` is not a valid ISO 4217 code, returning HTTP 400 with the TASKS.md error shape (`{ error, details: [{field, message}] }`).
- **Why this milestone:** Implements TASKS.md Task 2 in full and isolates regex/format rules from storage so they can be re-verified deterministically without touching M2/M3 endpoints. A separate milestone makes the validator a clear unit-of-test boundary that the Tests milestone (M7) can exercise with table-driven cases.
- **Files:** homework-1/src/Homework1.Api/Validators/CreateTransactionRequestValidator.cs, homework-1/src/Homework1.Api/Endpoints/TransactionsEndpoints.cs, homework-1/src/Homework1.Api/Program.cs
- **Depends on:** 2
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-1\src
  dotnet build Homework1.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework1.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 4
      $bad = @{ fromAccount = 'bad-id'; toAccount = 'ACC-67890'; amount = -5; currency = 'ZZZ'; type = 'transfer' } | ConvertTo-Json
      try {
          Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $bad | Out-Null
          throw "expected 400 for invalid request"
      } catch {
          if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "expected 400, got $($_.Exception.Response.StatusCode.value__)" }
      }
      $good = @{ fromAccount = 'ACC-12345'; toAccount = 'ACC-67890'; amount = 12.50; currency = 'USD'; type = 'transfer' } | ConvertTo-Json
      $created = Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $good
      if (-not $created.id) { throw "valid request was rejected" }
      $threeDp = @{ fromAccount = 'ACC-12345'; toAccount = 'ACC-67890'; amount = 12.555; currency = 'USD'; type = 'transfer' } | ConvertTo-Json
      try {
          Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $threeDp | Out-Null
          throw "expected 400 for 3-decimal amount"
      } catch {
          if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "expected 400 for 3-decimal amount, got $($_.Exception.Response.StatusCode.value__)" }
      }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [ ]

### Milestone 5: Transaction history filters
- **Goal:** Extend `GET /transactions` to honour `?accountId=`, `?type=`, `?from=YYYY-MM-DD`, and `?to=YYYY-MM-DD` query parameters, combinable, returning only matching rows.
- **Why this milestone:** Implements TASKS.md Task 3 as a focused step: the change touches the endpoint mapping and a single service method, so the diff stays small and the verify can post seed rows with distinct accounts/types/timestamps and assert each filter independently before exercising a combined query.
- **Files:** homework-1/src/Homework1.Api/Endpoints/TransactionsEndpoints.cs, homework-1/src/Homework1.Bll/Services/TransactionService.cs, homework-1/src/Homework1.Bll/Abstractions/ITransactionRepository.cs
- **Depends on:** 3, 4
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-1\src
  dotnet build Homework1.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework1.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 4
      $tx1 = @{ fromAccount = 'ACC-AAAAA'; toAccount = 'ACC-BBBBB'; amount = 10; currency = 'USD'; type = 'transfer' } | ConvertTo-Json
      $tx2 = @{ fromAccount = 'ACC-AAAAA'; toAccount = 'ACC-CCCCC'; amount = 20; currency = 'USD'; type = 'deposit' } | ConvertTo-Json
      $tx3 = @{ fromAccount = 'ACC-XXXXX'; toAccount = 'ACC-YYYYY'; amount = 30; currency = 'EUR'; type = 'withdrawal' } | ConvertTo-Json
      Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $tx1 | Out-Null
      Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $tx2 | Out-Null
      Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $tx3 | Out-Null
      $byAccount = Invoke-RestMethod -Uri 'http://localhost:5080/transactions?accountId=ACC-AAAAA' -Method Get
      if ($byAccount.Count -ne 2) { throw "accountId filter returned $($byAccount.Count) (expected 2)" }
      $byType = Invoke-RestMethod -Uri 'http://localhost:5080/transactions?type=deposit' -Method Get
      if ($byType.Count -ne 1) { throw "type filter returned $($byType.Count) (expected 1)" }
      $combined = Invoke-RestMethod -Uri 'http://localhost:5080/transactions?accountId=ACC-AAAAA&type=transfer' -Method Get
      if ($combined.Count -ne 1) { throw "combined filter returned $($combined.Count) (expected 1)" }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [ ]

### Milestone 6: Task 4 — Account summary (Option A) and per-IP rate limiting (Option D)
- **Goal:** Implement `GET /accounts/{accountId}/summary` returning total deposits, total withdrawals, transaction count, and most-recent transaction timestamp; and add per-IP rate limiting using ASP.NET Core's native `Microsoft.AspNetCore.RateLimiting` middleware (fixed-window: 100 requests/minute/IP, returning HTTP 429 when exceeded).
- **Why this milestone:** Satisfies the TASKS.md Task 4 "choose at least one" requirement with two complementary additions. Option A reuses the existing in-memory store with no new infrastructure; Option D adds a thin middleware layer that ASP.NET Core supports natively (no extra package beyond what `Microsoft.AspNetCore.App` ships) and creates throughput surface for the NBomber milestone (M8) to actually hit. The two changes touch disjoint files within budget (4) and are verifiable in one combined HTTP run.
- **Files:** homework-1/src/Homework1.Api/Endpoints/AccountsEndpoints.cs, homework-1/src/Homework1.Bll/Services/TransactionService.cs, homework-1/src/Homework1.Api/Program.cs, homework-1/src/Homework1.Api/RateLimiting/RateLimitingPolicies.cs
- **Depends on:** 3
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-1\src
  dotnet build Homework1.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework1.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 4
      # Summary check
      $dep = @{ fromAccount = 'ACC-SUM01'; toAccount = 'ACC-SUM01'; amount = 100; currency = 'USD'; type = 'deposit' } | ConvertTo-Json
      $wd  = @{ fromAccount = 'ACC-SUM01'; toAccount = 'ACC-SUM01'; amount = 25;  currency = 'USD'; type = 'withdrawal' } | ConvertTo-Json
      Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $dep | Out-Null
      Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post -ContentType 'application/json' -Body $wd  | Out-Null
      $summary = Invoke-RestMethod -Uri 'http://localhost:5080/accounts/ACC-SUM01/summary' -Method Get
      if ($summary.totalDeposits -ne 100) { throw "totalDeposits = $($summary.totalDeposits) (expected 100)" }
      if ($summary.totalWithdrawals -ne 25) { throw "totalWithdrawals = $($summary.totalWithdrawals) (expected 25)" }
      if ($summary.transactionCount -ne 2) { throw "transactionCount = $($summary.transactionCount) (expected 2)" }
      if (-not $summary.mostRecentTransactionAt) { throw "mostRecentTransactionAt missing" }
      # Rate-limit check: burst 110 requests, expect at least one 429
      $got429 = $false
      for ($i = 0; $i -lt 110; $i++) {
          try { Invoke-WebRequest -Uri 'http://localhost:5080/transactions' -Method Get -UseBasicParsing | Out-Null }
          catch {
              if ($_.Exception.Response.StatusCode.value__ -eq 429) { $got429 = $true; break }
              else { throw "unexpected status $($_.Exception.Response.StatusCode.value__) during rate-limit burst" }
          }
      }
      if (-not $got429) { throw "expected at least one 429 within 110-request burst" }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [ ]

### Milestone 7: Tests — API integration + BLL/DAL/validator unit coverage
- **Goal:** Populate `Homework1.Tests` with xUnit + FluentAssertions + Moq + `Microsoft.AspNetCore.Mvc.Testing` tests covering: API integration via `WebApplicationFactory<Program>` for every endpoint introduced in M2/M3/M5/M6 and validation rejection paths from M4; BLL `TransactionService` unit tests against a mocked `ITransactionRepository`; DAL unit tests for the in-memory repo; validator unit tests for amount, account-id, and currency rules; a focused integration test asserting that exceeding the rate-limit window returns HTTP 429 (using a test-only policy with a smaller window so the test stays fast).
- **Why this milestone:** Required by the planning-process spec (every PLAN.md must include a Tests milestone with `dotnet test` exit 0 and tests-run > 0). Consolidating tests into one step lets reviewers see total functional coverage at a glance and avoids interleaving test scaffolding with feature work earlier. Sequential because it consumes the surface area of every preceding functional milestone.
- **Files:** homework-1/src/Homework1.Tests/**
- **Depends on:** 1, 2, 3, 4, 5, 6
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-1\src
  $logPath = Join-Path $env:TEMP "hw1-dotnet-test.log"
  if (Test-Path $logPath) { Remove-Item $logPath -Force }
  dotnet test Homework1.sln --nologo | Tee-Object -FilePath $logPath
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "dotnet test exited $LASTEXITCODE" }
  $log = Get-Content $logPath -Raw
  $passed = 0
  if ($log -match 'Passed:\s*(\d+)')          { $passed = [int]$matches[1] }
  elseif ($log -match 'Total tests:\s*(\d+)') { $passed = [int]$matches[1] }
  if ($passed -lt 1) { Pop-Location; throw "dotnet test reported zero tests run (output: see $logPath)" }
  Pop-Location
  ```
- **Done:** [ ]

### Milestone 8: NBomber load test + docs/screenshots/demo bundle
- **Goal:** Add a separate `Homework1.LoadTests` console project using NBomber that runs a short, low-load scenario against `POST /transactions` and `GET /transactions` to demonstrate throughput and observe rate-limiter behaviour under concurrency; finalize `homework-1/README.md` (overview, features, AI-tools usage, architecture notes, NBomber report summary), `homework-1/HOWTORUN.md` (PowerShell run + load-test steps), drop the required AI-interaction + running-app + sample-request screenshots into `homework-1/docs/screenshots/`, and ship `homework-1/demo/run.ps1` plus `homework-1/demo/sample-requests.http`.
- **Why this milestone:** NBomber complements xUnit by exercising concurrency and rate-limiter behaviour that unit/integration tests do not realistically reproduce, while staying out of the required Tests milestone (M7) which must be deterministic for grading. Bundling NBomber with docs/demo keeps the final milestone count at 8 and pairs the load-test artefact with the README narrative that references its results. Kept separate from M7 because docs/screenshots are graded artifacts (10% screenshots + 25% AI usage docs) with different review criteria than functional tests.
- **Files:** homework-1/src/Homework1.LoadTests/**, homework-1/README.md, homework-1/HOWTORUN.md, homework-1/demo/run.ps1, homework-1/demo/sample-requests.http
- **Depends on:** 1, 2, 3, 4, 5, 6, 7
- **Parallel:** sequential
- **Verify:**
  ```powershell
  # Docs + demo deliverables
  if (-not (Test-Path homework-1\README.md))                 { throw "README.md missing" }
  if ((Get-Item homework-1\README.md).Length -lt 500)        { throw "README.md too short — likely still placeholder" }
  if (-not (Test-Path homework-1\HOWTORUN.md))               { throw "HOWTORUN.md missing" }
  if ((Get-Item homework-1\HOWTORUN.md).Length -lt 200)      { throw "HOWTORUN.md too short — likely still placeholder" }
  if (-not (Test-Path homework-1\demo\run.ps1))              { throw "demo/run.ps1 missing" }
  if (-not (Test-Path homework-1\demo\sample-requests.http)) { throw "demo/sample-requests.http missing" }
  $shots = Get-ChildItem homework-1\docs\screenshots -Filter *.png -ErrorAction SilentlyContinue
  if ($shots.Count -lt 3) { throw "expected >=3 screenshots in docs/screenshots/, found $($shots.Count)" }
  $readme = Get-Content homework-1\README.md -Raw
  if ($readme -notmatch '(?i)ai')       { throw "README.md does not mention AI usage" }
  if ($readme -notmatch '(?i)nbomber')  { throw "README.md does not reference NBomber load-test results" }
  # NBomber project: must build and run a smoke scenario end-to-end against a live API
  Push-Location homework-1\src
  dotnet build Homework1.LoadTests\Homework1.LoadTests.csproj
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "NBomber project build failed" }
  $api = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework1.Api --no-build --urls http://localhost:5081' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 4
      $env:HOMEWORK1_API_BASEURL = 'http://localhost:5081'
      dotnet run --project Homework1.LoadTests --no-build --configuration Release
      if ($LASTEXITCODE -ne 0) { throw "NBomber run exited $LASTEXITCODE" }
      $report = Get-ChildItem -Path Homework1.LoadTests -Recurse -Filter "*.html" -ErrorAction SilentlyContinue | Select-Object -First 1
      if (-not $report) { throw "NBomber did not produce an HTML report" }
  } finally {
      Stop-Process -Id $api.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [ ]
