#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Demonstrates the banking transaction pipeline end-to-end.

.DESCRIPTION
    This script orchestrates the pipeline setup and execution, then displays
    a summary of the results. It showcases the flow of transactions through
    the Validator, Fraud Detector, and Compliance Checker agents.

.EXAMPLE
    .\run-demo.ps1
#>

# Set error action for cleaner output
$ErrorActionPreference = "Stop"

function Write-ColorOutput {
    param(
        [string]$Message,
        [string]$ColorName = "Green"
    )
    Write-Host $Message -ForegroundColor $ColorName
}

function Main {
    Write-Host ""
    Write-ColorOutput "═══════════════════════════════════════════════════════════════" "Blue"
    Write-ColorOutput "  Banking Transaction Pipeline Demo" "Blue"
    Write-ColorOutput "═══════════════════════════════════════════════════════════════" "Blue"
    Write-Host ""

    # Navigate to src directory
    $scriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
    $srcDir = Join-Path (Split-Path -Parent $scriptDir) "src"

    if (-not (Test-Path $srcDir)) {
        Write-Host "ERROR: src directory not found at $srcDir" -ForegroundColor Red
        exit 1
    }

    Push-Location $srcDir

    try {
        # Step 1: Setup
        Write-Host ""
        Write-ColorOutput "Step 1: Setting up pipeline (creating shared directories)" "Blue"
        Write-Host "───────────────────────────────────────────────────────────────"
        $setupOutput = python integrator.py --setup 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Setup failed with exit code $LASTEXITCODE" -ForegroundColor Red
            Write-Host $setupOutput
            exit 1
        }
        Write-Host $setupOutput -ForegroundColor Green

        # Step 2: Run pipeline
        Write-Host ""
        Write-ColorOutput "Step 2: Running pipeline (processing transactions)" "Blue"
        Write-Host "───────────────────────────────────────────────────────────────"
        $pipelineOutput = python integrator.py 2>&1
        if ($LASTEXITCODE -ne 0) {
            Write-Host "Pipeline failed with exit code $LASTEXITCODE" -ForegroundColor Red
            Write-Host $pipelineOutput
            exit 1
        }
        Write-Host $pipelineOutput -ForegroundColor Green

        # Step 3: Analyze results
        Write-Host ""
        Write-ColorOutput "Step 3: Analyzing results" "Blue"
        Write-Host "───────────────────────────────────────────────────────────────"

        $resultsDir = Join-Path (Get-Location) "shared\results"

        if (-not (Test-Path $resultsDir)) {
            Write-Host "Results directory not found: $resultsDir" -ForegroundColor Red
            exit 1
        }

        $resultFiles = @(Get-ChildItem $resultsDir -Filter "*.json" -ErrorAction SilentlyContinue)
        Write-Host "Total transactions processed: $($resultFiles.Count)"

        if ($resultFiles.Count -eq 0) {
            Write-Host "No results found in $resultsDir" -ForegroundColor Yellow
            exit 1
        }

        # Parse and summarize results
        $approved = 0
        $holdPending = 0
        $fraudCounts = @{ LOW = 0; MEDIUM = 0; HIGH = 0; CRITICAL = 0 }
        $sampleTransactions = @()

        foreach ($file in $resultFiles) {
            try {
                $content = Get-Content $file.FullName -Raw | ConvertFrom-Json
                $txnId = $content.data.transaction_id
                $complianceStatus = $content.data.compliance_status.status
                $fraudLevel = $content.data.fraud_score.risk_level
                $fraudScore = $content.data.fraud_score.score
                $validationStatus = if ($content.data.validation_result.is_valid) { "Valid" } else { "Invalid" }

                # Count compliance statuses
                if ($complianceStatus -eq "APPROVED") {
                    $approved++
                }
                else {
                    if ($complianceStatus -eq "HOLD_PENDING_REVIEW") {
                        $holdPending++
                    }
                }

                # Count fraud levels
                if ($null -ne $fraudLevel) {
                    $fraudCounts[$fraudLevel]++
                }

                # Store sample for display
                if ($sampleTransactions.Count -lt 3) {
                    $sampleTransactions += @{
                        TxnId = $txnId
                        Validation = $validationStatus
                        FraudScore = "$fraudScore ($fraudLevel)"
                        Compliance = $complianceStatus
                    }
                }
            }
            catch {
                Write-Host "Error parsing $($file.Name): $_" -ForegroundColor Yellow
            }
        }

        # Display summary
        Write-Host ""
        Write-ColorOutput "─── Compliance Summary ───" "Green"
        Write-Host "  APPROVED transactions:           $approved"
        Write-Host "  HOLD_PENDING_REVIEW:             $holdPending"

        Write-Host ""
        Write-ColorOutput "─── Fraud Risk Breakdown ───" "Green"
        Write-Host "  LOW risk:                        $($fraudCounts['LOW'])"
        Write-Host "  MEDIUM risk:                     $($fraudCounts['MEDIUM'])"
        Write-Host "  HIGH risk:                       $($fraudCounts['HIGH'])"
        Write-Host "  CRITICAL risk:                   $($fraudCounts['CRITICAL'])"

        # Display sample transactions
        Write-Host ""
        Write-ColorOutput "─── Sample Results (first 3 transactions) ───" "Green"
        foreach ($tx in $sampleTransactions) {
            Write-Host ""
            Write-Host "  Transaction: $($tx.TxnId)"
            Write-Host "    Validation:      $($tx.Validation)"
            Write-Host "    Fraud Score:     $($tx.FraudScore)"
            Write-Host "    Compliance:      $($tx.Compliance)"
        }

        Write-Host ""
        Write-ColorOutput "═══════════════════════════════════════════════════════════════" "Blue"
        Write-ColorOutput "  Demo Complete! Results saved to: shared\results\" "Blue"
        Write-ColorOutput "═══════════════════════════════════════════════════════════════" "Blue"
        Write-Host ""

    }
    catch {
        Write-Host "ERROR: $($_.Exception.Message)" -ForegroundColor Red
        exit 1
    }
    finally {
        Pop-Location
    }
}

# Run main
Main
