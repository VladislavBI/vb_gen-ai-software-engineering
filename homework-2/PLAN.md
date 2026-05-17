# Homework 2 — Plan

**TASKS.md commit:** 478ca86
**Created:** 2026-05-16
**Stack:** .NET 10 / ASP.NET Core (minimal API + 3-layer split)

## Overview

Homework 2 (TASKS.md Tasks 1–5) asks for an intelligent customer support ticket system: a REST API with CRUD plus bulk import from CSV/JSON/XML (Task 1), a keyword-based auto-classification engine producing category, priority, confidence, and reasoning (Task 2), an xUnit test suite exceeding 85% line coverage (Task 3), multi-level documentation including Mermaid diagrams (Task 4), and integration/concurrency tests (Task 5). The chosen stack is .NET 10 / ASP.NET Core minimal API with the standard `Api`/`Bll`/`Dal`/`Tests` 3-layer split, FluentValidation for DTO validation, in-memory `ConcurrentDictionary`-backed storage, and coverlet for coverage measurement.

## Milestones

### Milestone 1: Solution scaffold and Ticket domain
- **Goal:** Stand up the 4-project .NET solution (`Api`/`Bll`/`Dal`/`Tests`) with the Ticket domain model, `ITicketRepository`, an in-memory repo, and a `/health` endpoint that proves the wiring builds and runs.
- **Why this milestone:** Every later milestone depends on a compiling solution with the layered references in place. Putting the domain shape (TASKS.md ticket model — id, customer fields, category/priority/status enums, metadata) and the repo interface here lets feature milestones layer behavior on a stable foundation without touching scaffold files.
- **Files:** homework-2/src/Homework2.sln, homework-2/src/Homework2.Api/Program.cs, homework-2/src/Homework2.Bll/Domain/Ticket.cs, homework-2/src/Homework2.Dal/Repositories/InMemoryTicketRepository.cs
- **Depends on:** none
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-2\src
  dotnet build Homework2.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework2.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 5
      $r = Invoke-RestMethod -Uri http://localhost:5080/health -Method Get
      if (-not $r) { throw "health endpoint returned empty" }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [x]

### Milestone 2: Ticket CRUD endpoints with validation
- **Goal:** Implement `POST /tickets`, `GET /tickets`, `GET /tickets/{id}`, `PUT /tickets/{id}`, `DELETE /tickets/{id}` with FluentValidation rules (email format, subject 1–200, description 10–2000, enum membership) returning proper 201/200/400/404 status codes.
- **Why this milestone:** The core CRUD surface from TASKS.md Task 1 is the contract every later piece (import, classification, tests, demo) calls. Isolating CRUD + validation here keeps the bulk-import and classifier milestones focused on their own logic without re-litigating field rules.
- **Files:** homework-2/src/Homework2.Api/Endpoints/TicketsEndpoints.cs, homework-2/src/Homework2.Api/Validators/TicketValidator.cs, homework-2/src/Homework2.Bll/Services/TicketService.cs, homework-2/src/Homework2.Api/Models/TicketDtos.cs
- **Depends on:** 1
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-2\src
  dotnet build Homework2.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework2.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 5
      $body = @{ customer_id="C1"; customer_email="a@b.com"; customer_name="Vlad"; subject="Login fails"; description="I cannot log in to my account at all." } | ConvertTo-Json
      $created = Invoke-RestMethod -Uri http://localhost:5080/tickets -Method Post -ContentType 'application/json' -Body $body
      if (-not $created.id) { throw "POST did not return id" }
      $fetched = Invoke-RestMethod -Uri "http://localhost:5080/tickets/$($created.id)" -Method Get
      if ($fetched.subject -ne "Login fails") { throw "GET returned wrong subject" }
      try { Invoke-RestMethod -Uri http://localhost:5080/tickets -Method Post -ContentType 'application/json' -Body (@{ customer_email="bad"; subject=""; description="x" } | ConvertTo-Json); throw "should have 400'd" } catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw "expected 400, got $($_.Exception.Response.StatusCode.value__)" } }
      try { Invoke-RestMethod -Uri "http://localhost:5080/tickets/00000000-0000-0000-0000-000000000000" -Method Get; throw "should have 404'd" } catch { if ($_.Exception.Response.StatusCode.value__ -ne 404) { throw "expected 404" } }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [x]

### Milestone 3: Multi-format bulk import (CSV/JSON/XML)
- **Goal:** Add `POST /tickets/import` accepting multipart-form file uploads in CSV, JSON, or XML format, returning a summary `{ total, successful, failed, errors[] }` with per-row error messages for malformed input.
- **Why this milestone:** TASKS.md Task 1 explicitly requires three parsers and a structured failure summary. Splitting parsers out from CRUD lets each format be tested in isolation and keeps the controller small.
- **Files:** homework-2/src/Homework2.Api/Endpoints/TicketsImportEndpoint.cs, homework-2/src/Homework2.Bll/Services/TicketImportService.cs, homework-2/src/Homework2.Bll/Services/TicketParsers.cs
- **Depends on:** 2
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-2\src
  dotnet build Homework2.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework2.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 5
      $tmp = New-Item -ItemType Directory -Path ([System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), [System.Guid]::NewGuid().ToString())) -Force
      $csv = Join-Path $tmp.FullName "t.csv"
      "customer_id,customer_email,customer_name,subject,description`nC1,a@b.com,Vlad,Login,Cannot log in to account" | Set-Content -Path $csv -Encoding utf8
      $form = @{ file = Get-Item $csv }
      $r = Invoke-RestMethod -Uri http://localhost:5080/tickets/import -Method Post -Form $form
      if ($r.total -lt 1 -or $r.successful -lt 1) { throw "CSV import summary wrong: $($r | ConvertTo-Json)" }
      $json = Join-Path $tmp.FullName "t.json"
      '[{"customer_id":"C2","customer_email":"b@c.com","customer_name":"Sam","subject":"Bug","description":"Crash on save action."}]' | Set-Content -Path $json -Encoding utf8
      $r2 = Invoke-RestMethod -Uri http://localhost:5080/tickets/import -Method Post -Form @{ file = Get-Item $json }
      if ($r2.successful -lt 1) { throw "JSON import failed" }
      $xml = Join-Path $tmp.FullName "t.xml"
      '<tickets><ticket><customer_id>C3</customer_id><customer_email>c@d.com</customer_email><customer_name>X</customer_name><subject>Hi</subject><description>Need a refund for last month.</description></ticket></tickets>' | Set-Content -Path $xml -Encoding utf8
      $r3 = Invoke-RestMethod -Uri http://localhost:5080/tickets/import -Method Post -Form @{ file = Get-Item $xml }
      if ($r3.successful -lt 1) { throw "XML import failed" }
      $bad = Join-Path $tmp.FullName "bad.csv"
      "customer_id,customer_email,customer_name,subject,description`nC,bademail,n,s,short" | Set-Content -Path $bad -Encoding utf8
      $r4 = Invoke-RestMethod -Uri http://localhost:5080/tickets/import -Method Post -Form @{ file = Get-Item $bad }
      if ($r4.failed -lt 1 -or -not $r4.errors) { throw "expected failed>0 with errors on bad row" }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [x]

### Milestone 4: Auto-classification engine
- **Goal:** Implement keyword-based category + priority classification with `POST /tickets/{id}/auto-classify` returning `{ category, priority, confidence, reasoning, keywordsFound[] }`, plus an opt-in `?autoClassify=true` flag on `POST /tickets`.
- **Why this milestone:** TASKS.md Task 2 is a self-contained piece of business logic with explicit keyword rules (urgent/high/low) and the six category buckets. Keeping it separate from CRUD lets it be unit-tested with high signal and lets the classifier change without touching parsers or endpoints.
- **Files:** homework-2/src/Homework2.Bll/Services/TicketClassifier.cs, homework-2/src/Homework2.Bll/Domain/ClassificationResult.cs, homework-2/src/Homework2.Api/Endpoints/TicketsClassifyEndpoint.cs
- **Depends on:** 2
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-2\src
  dotnet build Homework2.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework2.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 5
      $body = @{ customer_id="C1"; customer_email="a@b.com"; customer_name="V"; subject="Cannot access account"; description="Production down, security incident, cannot access my account." } | ConvertTo-Json
      $created = Invoke-RestMethod -Uri http://localhost:5080/tickets -Method Post -ContentType 'application/json' -Body $body
      $cls = Invoke-RestMethod -Uri "http://localhost:5080/tickets/$($created.id)/auto-classify" -Method Post
      if ($cls.priority -ne "urgent") { throw "expected urgent priority, got $($cls.priority)" }
      if ($cls.category -ne "account_access") { throw "expected account_access category, got $($cls.category)" }
      if (-not $cls.keywordsFound -or $cls.keywordsFound.Count -lt 1) { throw "expected keywordsFound list" }
      if ($cls.confidence -le 0 -or $cls.confidence -gt 1) { throw "confidence out of range" }
      $auto = Invoke-RestMethod -Uri "http://localhost:5080/tickets?autoClassify=true" -Method Post -ContentType 'application/json' -Body (@{ customer_id="C2"; customer_email="b@c.com"; customer_name="S"; subject="Minor cosmetic suggestion"; description="A minor cosmetic suggestion about the button color." } | ConvertTo-Json)
      if ($auto.priority -ne "low") { throw "auto-classify on create did not apply low priority" }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [x]

### Milestone 5: Filtering and list query
- **Goal:** Extend `GET /tickets` with query-string filters for `category`, `priority`, `status`, and combined filtering (matches TASKS.md Task 5's "combined filtering by category and priority" integration scenario).
- **Why this milestone:** Filtering is a small but distinct behavior the integration tests in Task 5 depend on. Treating it as its own milestone lets it ship behind a clean verify (post some tickets, query with filters, assert subset) without bloating earlier CRUD/import milestones.
- **Files:** homework-2/src/Homework2.Api/Endpoints/TicketsEndpoints.cs, homework-2/src/Homework2.Bll/Services/TicketService.cs
- **Depends on:** 2, 4
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-2\src
  dotnet build Homework2.sln
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
  $proc = Start-Process -FilePath dotnet -ArgumentList 'run --project Homework2.Api --no-build --urls http://localhost:5080' -PassThru -WindowStyle Hidden
  try {
      Start-Sleep -Seconds 5
      Invoke-RestMethod -Uri "http://localhost:5080/tickets?autoClassify=true" -Method Post -ContentType 'application/json' -Body (@{ customer_id="C1"; customer_email="a@b.com"; customer_name="V"; subject="Cannot access"; description="cannot access security production down" } | ConvertTo-Json) | Out-Null
      Invoke-RestMethod -Uri "http://localhost:5080/tickets?autoClassify=true" -Method Post -ContentType 'application/json' -Body (@{ customer_id="C2"; customer_email="b@c.com"; customer_name="S"; subject="Refund"; description="please process my invoice refund for last billing cycle" } | ConvertTo-Json) | Out-Null
      $all = Invoke-RestMethod -Uri "http://localhost:5080/tickets" -Method Get
      if ($all.Count -lt 2) { throw "expected at least 2 tickets" }
      $urgent = Invoke-RestMethod -Uri "http://localhost:5080/tickets?priority=urgent" -Method Get
      if (-not ($urgent | Where-Object { $_.priority -eq "urgent" })) { throw "priority filter returned no urgent tickets" }
      $billing = Invoke-RestMethod -Uri "http://localhost:5080/tickets?category=billing_question" -Method Get
      if (-not ($billing | Where-Object { $_.category -eq "billing_question" })) { throw "category filter failed" }
      $combo = Invoke-RestMethod -Uri "http://localhost:5080/tickets?category=account_access&priority=urgent" -Method Get
      if (-not ($combo | Where-Object { $_.category -eq "account_access" -and $_.priority -eq "urgent" })) { throw "combined filter failed" }
  } finally {
      Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
      Pop-Location
  }
  ```
- **Done:** [x]

### Milestone 6: Tests — API + BLL + DAL + parsers + classifier with ≥85% coverage
- **Goal:** Build out `Homework2.Tests` covering API endpoints via `WebApplicationFactory`, BLL services with Moq, DAL repository, validators, all three parsers, the classifier, and end-to-end + concurrency integration scenarios; gate the run on `dotnet test` exit 0, test count > 0, and ≥85% line coverage.
- **Why this milestone:** TASKS.md Task 3 pins overall coverage at >85% and Task 5 mandates integration + concurrency scenarios. A single, dedicated Tests milestone (per the planning spec) ties verification to behavior produced by every prior milestone and produces the coverage artifact for the screenshot deliverable.
- **Files:** homework-2/src/Homework2.Tests/**
- **Depends on:** 1, 2, 3, 4, 5
- **Parallel:** sequential
- **Verify:**
  ```powershell
  Push-Location homework-2\src
  $results = "TestResults"
  if (Test-Path $results) { Remove-Item -Recurse -Force $results }
  dotnet test Homework2.sln --collect:"XPlat Code Coverage" --results-directory $results --logger "trx;LogFileName=test.trx"
  if ($LASTEXITCODE -ne 0) { Pop-Location; throw "dotnet test failed" }
  $trx = Get-ChildItem -Recurse -Filter "*.trx" -Path $results | Select-Object -First 1
  if (-not $trx) { Pop-Location; throw "no trx produced" }
  [xml]$x = Get-Content $trx.FullName
  $total = [int]$x.TestRun.ResultSummary.Counters.total
  if ($total -lt 1) { Pop-Location; throw "zero tests executed" }
  $cov = Get-ChildItem -Recurse -Filter "coverage.cobertura.xml" -Path $results | Select-Object -First 1
  if (-not $cov) { Pop-Location; throw "coverage file missing" }
  [xml]$c = Get-Content $cov.FullName
  $rate = [double]$c.coverage.'line-rate'
  if ($rate -lt 0.85) { Pop-Location; throw "coverage $([math]::Round($rate*100,2))% below 85% threshold" }
  Write-Host "Tests: $total run, coverage $([math]::Round($rate*100,2))%"
  Pop-Location
  ```
- **Done:** [ ]

### Milestone 7: Finalize documentation, demo, and sample data
- **Goal:** Produce the multi-level docs (`README.md`, `API_REFERENCE.md`, `ARCHITECTURE.md`, `TESTING_GUIDE.md` with at least 3 Mermaid diagrams across them), substitute every template variable, fill in `HOWTORUN.md` PowerShell-first, capture the test-coverage screenshot, and populate `demo/` with runnable scripts plus `sample_tickets.csv` (50), `sample_tickets.json` (20), `sample_tickets.xml` (30), and at least one invalid file for negative tests.
- **Why this milestone:** TASKS.md Task 4 and the Deliverables section require the four documentation files, sample data files, and the coverage screenshot before submission. Bundling them last guarantees every artifact (architecture, API reference, test commands) reflects the final code shape rather than a guess written at scaffold time.
- **Files:** homework-2/README.md, homework-2/HOWTORUN.md, homework-2/docs/screenshots/test_coverage.png, homework-2/demo/sample-requests.ps1, homework-2/demo/sample_tickets.csv, homework-2/demo/sample_tickets.json, homework-2/demo/sample_tickets.xml, homework-2/docs/API_REFERENCE.md, homework-2/docs/ARCHITECTURE.md, homework-2/docs/TESTING_GUIDE.md
- **Depends on:** 1, 2, 3, 4, 5, 6
- **Parallel:** sequential
- **Verify:**
  ```powershell
  $required = @(
      'homework-2/README.md',
      'homework-2/HOWTORUN.md',
      'homework-2/docs/API_REFERENCE.md',
      'homework-2/docs/ARCHITECTURE.md',
      'homework-2/docs/TESTING_GUIDE.md',
      'homework-2/docs/screenshots/test_coverage.png',
      'homework-2/demo/sample-requests.ps1',
      'homework-2/demo/sample_tickets.csv',
      'homework-2/demo/sample_tickets.json',
      'homework-2/demo/sample_tickets.xml'
  )
  foreach ($p in $required) { if (-not (Test-Path $p)) { throw "missing deliverable: $p" } }
  $csvLines = (Get-Content homework-2/demo/sample_tickets.csv).Count
  if ($csvLines -lt 51) { throw "sample_tickets.csv must have 50 data rows (got $($csvLines-1))" }
  $jsonCount = (Get-Content homework-2/demo/sample_tickets.json -Raw | ConvertFrom-Json).Count
  if ($jsonCount -lt 20) { throw "sample_tickets.json must have 20 entries (got $jsonCount)" }
  [xml]$xml = Get-Content homework-2/demo/sample_tickets.xml
  if ($xml.tickets.ticket.Count -lt 30) { throw "sample_tickets.xml must have 30 tickets" }
  $mermaid = 0
  foreach ($f in 'homework-2/README.md','homework-2/docs/ARCHITECTURE.md','homework-2/docs/TESTING_GUIDE.md') {
      $mermaid += (Select-String -Path $f -Pattern '```mermaid' -AllMatches).Matches.Count
  }
  if ($mermaid -lt 3) { throw "need at least 3 Mermaid diagrams across docs (found $mermaid)" }
  if (Select-String -Path homework-2/README.md -Pattern '\[Your Name\]|\[Date\]|YOUR_USERNAME' -Quiet) { throw "README.md still contains template placeholders" }
  ```
- **Done:** [ ]
