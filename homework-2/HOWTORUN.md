# How to Run the Support Ticket Management System

This guide provides step-by-step instructions to build, test, and run the Support Ticket Management System API.

## Prerequisites

- **OS**: Windows 10 / Windows 11
- **.NET SDK**: 10.0 or 11.0 (download from [dot.net](https://dot.net))
- **PowerShell**: 5.1 (included with Windows)
- **Git**: (optional, for cloning)

## Step 1: Navigate to the Project Directory

```powershell
cd "D:\Work\learn\Courses\AI -set\lecture-1\vb_gen-ai-software-engineering\homework-2"
```

## Step 2: Restore NuGet Packages

Before building, restore all project dependencies:

```powershell
dotnet restore
```

**Expected Output:**
```
Determining projects to restore...
  Restored D:\...\homework-2\src\Homework2.Api\Homework2.Api.csproj (in XX ms)
  Restored D:\...\homework-2\src\Homework2.Bll\Homework2.Bll.csproj (in XX ms)
  Restored D:\...\homework-2\src\Homework2.Tests\Homework2.Tests.csproj (in XX ms)
```

## Step 3: Build the Solution

Build all projects to ensure compilation:

```powershell
dotnet build
```

**Expected Output:**
```
Build succeeded.
    0 Warning(s)
    0 Error(s)

Time Elapsed 00:00:XX.XXXXXX
```

## Step 4: Run Unit and Integration Tests

Execute the full test suite to verify the system:

```powershell
dotnet test --verbosity normal
```

**Expected Output:**
```
Test Run Successful.
Total tests: 120+
     Passed: 120+
     Failed: 0
     Skipped: 0
Time Elapsed 00:00:XX.XXXXXX
```

### Optional: Run Tests with Coverage Report

Generate a code coverage report:

```powershell
dotnet test /p:CollectCoverage=true /p:CoverageFormat=opencover /p:Threshold=80
```

This generates coverage statistics showing which code paths are tested.

### Optional: Run Specific Test Categories

```powershell
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"
```

## Step 5: Start the API Server

Navigate to the API project and run it:

```powershell
cd src\Homework2.Api
dotnet run
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
info: Microsoft.Hosting.Lifetime[0]
      Application started. Press Ctrl+C to stop.
```

The API is now listening on **http://localhost:5000**.

Leave this terminal open; the server will run until you press `Ctrl+C`.

## Step 6: Test the API (in a New Terminal)

Open a **new PowerShell window** (do not close the server) and navigate back to the project:

```powershell
cd "D:\Work\learn\Courses\AI -set\lecture-1\vb_gen-ai-software-engineering\homework-2"
```

### 6.1: Create a Ticket

```powershell
$uri = "http://localhost:5000/tickets"
$body = @{
    customerId = "CUST-001"
    customerEmail = "alice@example.com"
    customerName = "Alice Johnson"
    subject = "Cannot reset password"
    description = "I've tried resetting my password but haven't received the email."
    category = "account_access"
    priority = "high"
    tags = @("urgent", "account")
} | ConvertTo-Json

$response = Invoke-RestMethod -Uri $uri -Method POST `
    -ContentType "application/json" `
    -Body $body

Write-Host "Created ticket ID: $($response.Id)" -ForegroundColor Green
$response | ConvertTo-Json
```

**Expected Response:**
```json
{
  "id": "550e8400-e29b-41d4-a716-446655440000",
  "customerId": "CUST-001",
  "customerEmail": "alice@example.com",
  "customerName": "Alice Johnson",
  "subject": "Cannot reset password",
  "description": "I've tried resetting my password but haven't received the email.",
  "category": "account_access",
  "priority": "high",
  "status": "new",
  "createdAt": "2026-05-17T10:30:00Z",
  "updatedAt": "2026-05-17T10:30:00Z",
  "resolvedAt": null,
  "assignedTo": null,
  "tags": ["urgent", "account"]
}
```

### 6.2: List All Tickets

```powershell
$uri = "http://localhost:5000/tickets"
$tickets = Invoke-RestMethod -Uri $uri -Method GET

Write-Host "Found $($tickets.Count) tickets" -ForegroundColor Green
$tickets | Format-Table -Property Id, CustomerId, Subject, Priority, Status
```

### 6.3: Filter Tickets by Priority

```powershell
$uri = "http://localhost:5000/tickets?priority=high"
$tickets = Invoke-RestMethod -Uri $uri -Method GET

Write-Host "High-priority tickets: $($tickets.Count)" -ForegroundColor Green
$tickets | Format-Table -Property Id, Subject, Priority
```

### 6.4: Get a Single Ticket

```powershell
$ticketId = "<ticket-id-from-step-6.1>"
$uri = "http://localhost:5000/tickets/$ticketId"

$ticket = Invoke-RestMethod -Uri $uri -Method GET
$ticket | ConvertTo-Json
```

### 6.5: Update a Ticket

```powershell
$ticketId = "<ticket-id-from-step-6.1>"
$uri = "http://localhost:5000/tickets/$ticketId"
$body = @{
    subject = "Password reset issue - RESOLVED"
    status = "resolved"
    assignedTo = "support-team-1"
} | ConvertTo-Json

$updated = Invoke-RestMethod -Uri $uri -Method PUT `
    -ContentType "application/json" `
    -Body $body

Write-Host "Updated ticket status to: $($updated.status)" -ForegroundColor Green
```

### 6.6: Auto-Classify a Ticket

Automatically assign category and priority based on ticket description:

```powershell
$ticketId = "<ticket-id-from-step-6.1>"
$uri = "http://localhost:5000/tickets/$ticketId/auto-classify"

$classification = Invoke-RestMethod -Uri $uri -Method POST

Write-Host "Auto-classified as: Category=$($classification.category), Priority=$($classification.priority)" -ForegroundColor Green
Write-Host "Confidence: $($classification.confidence)"
Write-Host "Reasoning: $($classification.reasoning)"
Write-Host "Keywords Found: $($classification.keywordsFound -join ', ')"
```

### 6.7: Batch Import Tickets from CSV

```powershell
# First, create a CSV file or use the sample data
$sampleCsv = "demo\sample_tickets.csv"

# Upload the CSV
$uri = "http://localhost:5000/tickets/import"
$form = @{
    file = Get-Item -Path $sampleCsv
}

$result = Invoke-RestMethod -Uri $uri -Method POST `
    -Form $form

Write-Host "Imported $($result.successful) tickets" -ForegroundColor Green
Write-Host "Failed: $($result.failed)" -ForegroundColor Red
if ($result.failed -gt 0) {
    Write-Host "Errors:" -ForegroundColor Red
    $result.errors | ForEach-Object { Write-Host "  - $_" }
}
```

### 6.8: Batch Import Tickets from JSON

```powershell
$sampleJson = "demo\sample_tickets.json"

$uri = "http://localhost:5000/tickets/import"
$form = @{
    file = Get-Item -Path $sampleJson
}

$result = Invoke-RestMethod -Uri $uri -Method POST `
    -Form $form

Write-Host "Imported $($result.successful) JSON tickets" -ForegroundColor Green
```

### 6.9: Batch Import Tickets from XML

```powershell
$sampleXml = "demo\sample_tickets.xml"

$uri = "http://localhost:5000/tickets/import"
$form = @{
    file = Get-Item -Path $sampleXml
}

$result = Invoke-RestMethod -Uri $uri -Method POST `
    -Form $form

Write-Host "Imported $($result.successful) XML tickets" -ForegroundColor Green
```

### 6.10: Delete a Ticket

```powershell
$ticketId = "<ticket-id-from-step-6.1>"
$uri = "http://localhost:5000/tickets/$ticketId"

Invoke-RestMethod -Uri $uri -Method DELETE
Write-Host "Ticket deleted successfully" -ForegroundColor Green
```

## Step 7: Run the Demo Script

A full automated demo script is provided:

```powershell
cd demo
.\sample-requests.ps1
```

This script performs all CRUD operations, filtering, import, and classification in sequence.

**Expected Output:**
```
[Demo] Creating ticket: Cannot login...
[Demo] Ticket created with ID: 550e8400-...
[Demo] Listing all tickets...
[Demo] Filtering by priority: high
[Demo] Auto-classifying ticket...
[Demo] Importing CSV sample data...
[Demo] Imported 50 tickets
[Demo] Listing final ticket count...
[Demo] Demo complete!
```

## Step 8: Stop the API Server

In the terminal running the API, press **Ctrl+C** to stop:

```
Ctrl+C
```

**Expected Output:**
```
info: Microsoft.Hosting.Lifetime[13]
      Shutdown requested.
info: Microsoft.Hosting.Lifetime[7]
      Shutdown complete.
```

## Troubleshooting

### Issue: "dotnet: command not found"

**Solution**: Ensure .NET SDK is installed and added to PATH.

```powershell
dotnet --version
```

If not found, download from [dot.net](https://dot.net) and reinstall.

### Issue: "Address already in use" on port 5000

**Solution**: The port is in use by another process. Either:

1. Stop the other process using port 5000
2. Change the port in `src\Homework2.Api\Program.cs` and rebuild

### Issue: Tests fail with "Cannot open database"

**Solution**: Tests use in-memory database. If tests fail, ensure:

1. No other instance is running: `Get-Process dotnet | Stop-Process -Force`
2. Rebuild: `dotnet clean; dotnet build`
3. Re-run tests: `dotnet test`

### Issue: Import fails with "No file provided"

**Solution**: Ensure file path is absolute. Use:

```powershell
$form = @{
    file = Get-Item -Path (Resolve-Path "demo\sample_tickets.csv")
}
```

## Additional Resources

- **API Documentation**: See `docs/API_REFERENCE.md` for complete endpoint documentation
- **Architecture Guide**: See `docs/ARCHITECTURE.md` for system design details
- **Testing Guide**: See `docs/TESTING_GUIDE.md` for test strategy and coverage
- **Sample Data**: See `demo/` folder for CSV, JSON, XML examples

## Summary

You now have a working Support Ticket Management System! The API is ready to:
- ✅ Create, read, update, and delete tickets
- ✅ Filter tickets by category, priority, and status
- ✅ Import tickets from multiple formats (CSV/JSON/XML)
- ✅ Automatically classify tickets using keyword matching
- ✅ Validate all input with comprehensive error reporting

For any issues, refer to the Troubleshooting section or see the documentation files.
