#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Demonstrates all major API endpoints of the Support Ticket Management System.

.DESCRIPTION
    This script shows how to use PowerShell to interact with the ticket API.
    It includes examples of:
    - Creating tickets
    - Listing and filtering tickets
    - Updating tickets
    - Auto-classifying tickets
    - Batch importing from CSV/JSON/XML
    - Deleting tickets

.PREREQUISITES
    - API server running on http://localhost:5000
    - PowerShell 5.1 or higher

.EXAMPLE
    .\sample-requests.ps1

.NOTES
    Ensure the API is running before executing this script:
    cd src\Homework2.Api
    dotnet run
#>

param(
    [string]$BaseUrl = "http://localhost:5000",
    [int]$DelayMs = 500
)

# Utilities
function Write-Demo {
    param([string]$Message)
    Write-Host "[Demo] $Message" -ForegroundColor Cyan
}

function Write-Success {
    param([string]$Message)
    Write-Host "[✓] $Message" -ForegroundColor Green
}

function Write-Error-Message {
    param([string]$Message)
    Write-Host "[✗] ERROR: $Message" -ForegroundColor Red
}

function Start-Section {
    param([string]$Title)
    Write-Host ""
    Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
    Write-Host $Title -ForegroundColor Blue -NoNewline
    Write-Host " ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
    Write-Host ""
}

# Test connectivity
Write-Demo "Testing API connectivity..."
try {
    $health = Invoke-RestMethod -Uri "$BaseUrl/tickets" -Method GET -ErrorAction Stop
    Write-Success "API is responding"
} catch {
    Write-Error-Message "Cannot reach API at $BaseUrl"
    Write-Error-Message "Make sure the API is running: cd src\Homework2.Api && dotnet run"
    exit 1
}

# ============================================================================
# Section 1: Create Tickets
# ============================================================================
Start-Section "1. Creating Support Tickets"

Write-Demo "Creating ticket 1: Account access issue"
$ticket1Request = @{
    customerId = "DEMO-001"
    customerEmail = "alice@example.com"
    customerName = "Alice Johnson"
    subject = "Cannot login to my account"
    description = "I keep getting an 'Invalid credentials' error even though I'm using the correct password."
    category = "account_access"
    priority = "high"
    tags = @("urgent", "account", "login")
} | ConvertTo-Json

$ticket1 = Invoke-RestMethod -Uri "$BaseUrl/tickets" -Method POST `
    -ContentType "application/json" `
    -Body $ticket1Request

Write-Success "Created ticket: $($ticket1.id)"
Write-Host "  Status: $($ticket1.status)"
Write-Host "  Priority: $($ticket1.priority)"

Start-Sleep -Milliseconds $DelayMs

Write-Demo "Creating ticket 2: Billing issue"
$ticket2Request = @{
    customerId = "DEMO-002"
    customerEmail = "bob@example.com"
    customerName = "Bob Smith"
    subject = "Duplicate charge on my account"
    description = "I was charged twice for my monthly subscription. Please review and issue a refund."
    category = "billing_question"
    priority = "urgent"
    tags = @("billing", "refund", "urgent")
} | ConvertTo-Json

$ticket2 = Invoke-RestMethod -Uri "$BaseUrl/tickets" -Method POST `
    -ContentType "application/json" `
    -Body $ticket2Request

Write-Success "Created ticket: $($ticket2.id)"
Write-Host "  Status: $($ticket2.status)"
Write-Host "  Priority: $($ticket2.priority)"

Start-Sleep -Milliseconds $DelayMs

Write-Demo "Creating ticket 3: Bug report"
$ticket3Request = @{
    customerId = "DEMO-003"
    customerEmail = "carol@example.com"
    customerName = "Carol Williams"
    subject = "App crashes when uploading files"
    description = "The mobile app crashes immediately when I try to upload files larger than 10MB. Happens on both iOS and Android."
    category = "bug_report"
    priority = "high"
    tags = @("crash", "mobile", "file-upload")
} | ConvertTo-Json

$ticket3 = Invoke-RestMethod -Uri "$BaseUrl/tickets" -Method POST `
    -ContentType "application/json" `
    -Body $ticket3Request

Write-Success "Created ticket: $($ticket3.id)"
Write-Host "  Status: $($ticket3.status)"
Write-Host "  Priority: $($ticket3.priority)"

Start-Sleep -Milliseconds $DelayMs

Write-Demo "Creating ticket 4: Feature request"
$ticket4Request = @{
    customerId = "DEMO-004"
    customerEmail = "david@example.com"
    customerName = "David Brown"
    subject = "Feature request: Dark mode support"
    description = "Many users have requested dark mode support. Would be a great addition for reducing eye strain during evening hours."
    category = "feature_request"
    priority = "low"
    tags = @("feature", "ui", "dark-mode")
} | ConvertTo-Json

$ticket4 = Invoke-RestMethod -Uri "$BaseUrl/tickets" -Method POST `
    -ContentType "application/json" `
    -Body $ticket4Request

Write-Success "Created ticket: $($ticket4.id)"
Write-Host "  Status: $($ticket4.status)"
Write-Host "  Priority: $($ticket4.priority)"

# ============================================================================
# Section 2: List Tickets
# ============================================================================
Start-Section "2. Listing All Tickets"

Write-Demo "Fetching all tickets..."
$allTickets = Invoke-RestMethod -Uri "$BaseUrl/tickets" -Method GET
Write-Success "Retrieved $($allTickets.Count) tickets"
Write-Host ""
Write-Host "Tickets:" -ForegroundColor Gray
$allTickets | Format-Table -Property `
    @{Name="ID"; Expression={$_.id.ToString().Substring(0,8)}},
    @{Name="Subject"; Expression={$_.subject}},
    @{Name="Priority"; Expression={$_.priority}},
    @{Name="Status"; Expression={$_.status}} `
    -AutoSize

# ============================================================================
# Section 3: Filter Tickets
# ============================================================================
Start-Section "3. Filtering Tickets"

Write-Demo "Filter 1: Getting high-priority tickets..."
$highPriority = Invoke-RestMethod -Uri "$BaseUrl/tickets?priority=high" -Method GET
Write-Success "Found $($highPriority.Count) high-priority tickets"
$highPriority | Format-Table -Property `
    @{Name="ID"; Expression={$_.id.ToString().Substring(0,8)}},
    @{Name="Subject"; Expression={$_.subject}},
    @{Name="Priority"; Expression={$_.priority}} `
    -AutoSize

Start-Sleep -Milliseconds $DelayMs

Write-Demo "Filter 2: Getting billing-related tickets..."
$billingTickets = Invoke-RestMethod -Uri "$BaseUrl/tickets?category=billing_question" -Method GET
Write-Success "Found $($billingTickets.Count) billing tickets"
$billingTickets | Format-Table -Property `
    @{Name="ID"; Expression={$_.id.ToString().Substring(0,8)}},
    @{Name="Subject"; Expression={$_.subject}},
    @{Name="Category"; Expression={$_.category}} `
    -AutoSize

Start-Sleep -Milliseconds $DelayMs

Write-Demo "Filter 3: Getting urgent tickets with status new..."
$urgentNew = Invoke-RestMethod -Uri "$BaseUrl/tickets?priority=urgent&status=new" -Method GET
Write-Success "Found $($urgentNew.Count) urgent new tickets"

# ============================================================================
# Section 4: Get Single Ticket
# ============================================================================
Start-Section "4. Retrieving Single Ticket"

Write-Demo "Getting full details for ticket: $($ticket1.id)"
$singleTicket = Invoke-RestMethod -Uri "$BaseUrl/tickets/$($ticket1.id)" -Method GET
Write-Success "Retrieved ticket details"
Write-Host ""
Write-Host ($singleTicket | ConvertTo-Json | Out-String)

# ============================================================================
# Section 5: Update Ticket
# ============================================================================
Start-Section "5. Updating Ticket"

Write-Demo "Updating ticket 1: Changing status to in_progress and assigning agent..."
$updateRequest = @{
    status = "in_progress"
    assignedTo = "support-agent-1"
    tags = @("urgent", "account", "login", "in-progress")
} | ConvertTo-Json

$updated = Invoke-RestMethod -Uri "$BaseUrl/tickets/$($ticket1.id)" -Method PUT `
    -ContentType "application/json" `
    -Body $updateRequest

Write-Success "Ticket updated successfully"
Write-Host "  New Status: $($updated.status)"
Write-Host "  Assigned To: $($updated.assignedTo)"
Write-Host "  Updated At: $($updated.updatedAt)"

Start-Sleep -Milliseconds $DelayMs

Write-Demo "Updating ticket 2: Marking as resolved..."
$resolveRequest = @{
    status = "resolved"
} | ConvertTo-Json

$resolved = Invoke-RestMethod -Uri "$BaseUrl/tickets/$($ticket2.id)" -Method PUT `
    -ContentType "application/json" `
    -Body $resolveRequest

Write-Success "Ticket resolved"
Write-Host "  New Status: $($resolved.status)"

# ============================================================================
# Section 6: Auto-Classify Tickets
# ============================================================================
Start-Section "6. Auto-Classifying Tickets"

Write-Demo "Auto-classifying ticket 3 (app crash bug)..."
$classification3 = Invoke-RestMethod -Uri "$BaseUrl/tickets/$($ticket3.id)/auto-classify" -Method POST
Write-Success "Classification result:"
Write-Host "  Suggested Category: $($classification3.category)"
Write-Host "  Suggested Priority: $($classification3.priority)"
Write-Host "  Confidence: $($classification3.confidence * 100)%"
Write-Host "  Reasoning: $($classification3.reasoning)"
Write-Host "  Keywords Found: $($classification3.keywordsFound -join ', ')"

Start-Sleep -Milliseconds $DelayMs

Write-Demo "Auto-classifying ticket 4 (feature request)..."
$classification4 = Invoke-RestMethod -Uri "$BaseUrl/tickets/$($ticket4.id)/auto-classify" -Method POST
Write-Success "Classification result:"
Write-Host "  Suggested Category: $($classification4.category)"
Write-Host "  Suggested Priority: $($classification4.priority)"
Write-Host "  Confidence: $($classification4.confidence * 100)%"
Write-Host "  Reasoning: $($classification4.reasoning)"

# ============================================================================
# Section 7: Batch Import from CSV
# ============================================================================
Start-Section "7. Batch Import from CSV"

Write-Demo "Importing tickets from CSV file..."
$csvPath = Join-Path (Get-Item -Path ".").FullName "sample_tickets.csv"

if (-not (Test-Path $csvPath)) {
    Write-Error-Message "Sample CSV file not found at $csvPath"
} else {
    try {
        $csvForm = @{
            file = Get-Item -Path $csvPath
        }

        $importResult = Invoke-RestMethod -Uri "$BaseUrl/tickets/import" -Method POST `
            -Form $csvForm

        Write-Success "CSV import completed"
        Write-Host "  Successful imports: $($importResult.successful)"
        Write-Host "  Failed imports: $($importResult.failed)"

        if ($importResult.errors.Count -gt 0) {
            Write-Host "  Errors:" -ForegroundColor Yellow
            $importResult.errors | ForEach-Object {
                Write-Host "    - $_"
            }
        }
    } catch {
        Write-Error-Message "Failed to import CSV: $($_.Exception.Message)"
    }
}

Start-Sleep -Milliseconds $DelayMs

# ============================================================================
# Section 8: Batch Import from JSON
# ============================================================================
Start-Section "8. Batch Import from JSON"

Write-Demo "Importing tickets from JSON file..."
$jsonPath = Join-Path (Get-Item -Path ".").FullName "sample_tickets.json"

if (-not (Test-Path $jsonPath)) {
    Write-Error-Message "Sample JSON file not found at $jsonPath"
} else {
    try {
        $jsonForm = @{
            file = Get-Item -Path $jsonPath
        }

        $jsonResult = Invoke-RestMethod -Uri "$BaseUrl/tickets/import" -Method POST `
            -Form $jsonForm

        Write-Success "JSON import completed"
        Write-Host "  Successful imports: $($jsonResult.successful)"
        Write-Host "  Failed imports: $($jsonResult.failed)"
    } catch {
        Write-Error-Message "Failed to import JSON: $($_.Exception.Message)"
    }
}

Start-Sleep -Milliseconds $DelayMs

# ============================================================================
# Section 9: Batch Import from XML
# ============================================================================
Start-Section "9. Batch Import from XML"

Write-Demo "Importing tickets from XML file..."
$xmlPath = Join-Path (Get-Item -Path ".").FullName "sample_tickets.xml"

if (-not (Test-Path $xmlPath)) {
    Write-Error-Message "Sample XML file not found at $xmlPath"
} else {
    try {
        $xmlForm = @{
            file = Get-Item -Path $xmlPath
        }

        $xmlResult = Invoke-RestMethod -Uri "$BaseUrl/tickets/import" -Method POST `
            -Form $xmlForm

        Write-Success "XML import completed"
        Write-Host "  Successful imports: $($xmlResult.successful)"
        Write-Host "  Failed imports: $($xmlResult.failed)"
    } catch {
        Write-Error-Message "Failed to import XML: $($_.Exception.Message)"
    }
}

# ============================================================================
# Section 10: Final Statistics
# ============================================================================
Start-Section "10. Final Statistics"

Write-Demo "Getting final ticket count..."
$finalTickets = Invoke-RestMethod -Uri "$BaseUrl/tickets" -Method GET
Write-Success "Total tickets in system: $($finalTickets.Count)"

# Count by status
Write-Host ""
Write-Host "Tickets by Status:" -ForegroundColor Gray
$statusGroups = $finalTickets | Group-Object -Property status | Select-Object Name, Count
$statusGroups | Format-Table -Property @{Name="Status"; Expression={$_.Name}}, Count -AutoSize

# Count by priority
Write-Host ""
Write-Host "Tickets by Priority:" -ForegroundColor Gray
$priorityGroups = $finalTickets | Group-Object -Property priority | Select-Object Name, Count
$priorityGroups | Format-Table -Property @{Name="Priority"; Expression={$_.Name}}, Count -AutoSize

# Count by category
Write-Host ""
Write-Host "Tickets by Category:" -ForegroundColor Gray
$categoryGroups = $finalTickets | Group-Object -Property category | Select-Object Name, Count
$categoryGroups | Format-Table -Property @{Name="Category"; Expression={$_.Name}}, Count -AutoSize

# ============================================================================
# Section 11: Cleanup (Optional)
# ============================================================================
Start-Section "11. Demo Cleanup"

Write-Demo "Deleting demo tickets created in this session..."
$deletionCount = 0

@($ticket1.id, $ticket2.id, $ticket3.id, $ticket4.id) | ForEach-Object {
    try {
        Invoke-RestMethod -Uri "$BaseUrl/tickets/$_" -Method DELETE -ErrorAction Stop | Out-Null
        $deletionCount++
    } catch {
        # Ticket may have already been deleted
    }
}

Write-Success "Deleted $deletionCount demo tickets"

# ============================================================================
# Summary
# ============================================================================
Write-Host ""
Write-Host "━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
Write-Host "DEMO COMPLETE" -ForegroundColor Green -NoNewline
Write-Host " ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━" -ForegroundColor Blue
Write-Host ""
Write-Host "Summary of demonstrated operations:" -ForegroundColor Green
Write-Host "  ✓ Created 4 support tickets with various categories"
Write-Host "  ✓ Listed all tickets and displayed in table format"
Write-Host "  ✓ Filtered tickets by priority and category"
Write-Host "  ✓ Retrieved detailed view of single ticket"
Write-Host "  ✓ Updated ticket status and assignment"
Write-Host "  ✓ Auto-classified tickets based on content"
Write-Host "  ✓ Bulk imported tickets from CSV format (50 rows)"
Write-Host "  ✓ Bulk imported tickets from JSON format (20 entries)"
Write-Host "  ✓ Bulk imported tickets from XML format (30 entries)"
Write-Host "  ✓ Generated final statistics and summary"
Write-Host "  ✓ Cleaned up demo tickets"
Write-Host ""
Write-Host "For more information, see:" -ForegroundColor Cyan
Write-Host "  - docs/API_REFERENCE.md      (Complete API documentation)"
Write-Host "  - docs/ARCHITECTURE.md       (System design and architecture)"
Write-Host "  - docs/TESTING_GUIDE.md      (Testing strategy and coverage)"
Write-Host "  - HOWTORUN.md                (Step-by-step runbook)"
Write-Host ""
