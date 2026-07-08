# /run-pipeline

**Execute the multi-agent transaction processing pipeline end-to-end.**

Use this skill to run the full pipeline: clear the processing directories, invoke the integrator, and display a summary of results from the `shared/results/` directory.

---

## Overview

The transaction pipeline processes a batch of sample transactions through three cooperating agents (Transaction Validator, Fraud Detector, Compliance Checker), writing results to `shared/results/`. This skill orchestrates the run, captures output, and presents a summary of transaction outcomes.

---

## Steps

### 1. Setup Pipeline Directories and Load Sample Transactions

Initialize the pipeline by setting up directories and loading sample transactions:

```powershell
Push-Location homework-6/src
try {
  python integrator.py --setup
  if ($LASTEXITCODE -ne 0) {
    throw "Integrator setup failed with exit code $LASTEXITCODE"
  }
  Write-Host "Pipeline setup complete: shared/ directories created and sample transactions loaded."
} finally { Pop-Location }
```

### 2. Clear Processing Directories

Remove intermediate files from previous runs to ensure a clean pipeline run:

```powershell
Push-Location homework-6/src
try {
  if (Test-Path shared/processing) { Remove-Item shared/processing/* -Force -ErrorAction SilentlyContinue }
  if (Test-Path shared/output) { Remove-Item shared/output/* -Force -ErrorAction SilentlyContinue }
  if (Test-Path shared/results) { Remove-Item shared/results/* -Force -ErrorAction SilentlyContinue }
  Write-Host "Processing directories cleared."
} finally { Pop-Location }
```

### 3. Run the Integrator

Invoke the pipeline integrator, which reads from `shared/input/`, processes transactions through the three agents (Validator, Fraud Detector, Compliance Checker), and writes results to `shared/results/`:

```powershell
Push-Location homework-6/src
try {
  python integrator.py
  if ($LASTEXITCODE -ne 0) {
    throw "Integrator failed with exit code $LASTEXITCODE"
  }
  Write-Host "Pipeline executed successfully."
} finally { Pop-Location }
```

### 4. Parse and Display Results Summary

Read all result files from `shared/results/`, extract key fields from the message envelope (transaction_id, validation/fraud/compliance decisions), and display a summary table:

```powershell
Push-Location homework-6/src
try {
  $results = @()
  $resultFiles = Get-ChildItem shared/results -Filter *.json -ErrorAction SilentlyContinue
  
  if ($resultFiles.Count -eq 0) {
    Write-Host "No results found in shared/results/."
    throw "Pipeline produced no output files"
  }
  
  Write-Host "Processed $($resultFiles.Count) transactions."
  Write-Host ""
  Write-Host "Result Summary:"
  Write-Host "─────────────────────────────────────────────────────────────"
  
  $complianceCounts = @{}
  $holdReasons = @()
  
  foreach ($file in $resultFiles) {
    try {
      $content = Get-Content $file.FullName -Raw | ConvertFrom-Json
      $data = $content.data
      $txnId = $data.transaction_id
      if ($null -eq $txnId) { $txnId = "UNKNOWN" }
      
      # Extract compliance status (final decision)
      $complianceStatus = $data.compliance_status.status
      if ($null -eq $complianceStatus) { $complianceStatus = "UNKNOWN" }
      
      # Extract fraud risk level
      $fraudRisk = $data.fraud_score.risk_level
      if ($null -eq $fraudRisk) { $fraudRisk = "N/A" }
      
      # Extract hold reasons if present
      $reasons = $data.compliance_status.hold_reasons
      if ($reasons -and $reasons.Count -gt 0) {
        foreach ($reason in $reasons) {
          $holdReasons += $reason
        }
      }
      
      $complianceCounts[$complianceStatus] = ($complianceCounts[$complianceStatus] + 1)
      
      $results += @{
        transaction_id = $txnId
        compliance_status = $complianceStatus
        fraud_risk = $fraudRisk
      }
    } catch {
      Write-Host "Warning: Could not parse $($file.Name): $_"
    }
  }
  
  # Display compliance status distribution
  Write-Host ""
  Write-Host "Compliance Status Distribution:"
  foreach ($status in ($complianceCounts.Keys | Sort-Object)) {
    Write-Host "  $status : $($complianceCounts[$status])"
  }
  
  # Display hold reasons if any
  if ($holdReasons.Count -gt 0) {
    Write-Host ""
    Write-Host "Hold Reasons:"
    $holdReasons | Group-Object | ForEach-Object {
      Write-Host "  $($_.Name) : $($_.Count)"
    }
  }
  
  # Display full results table
  Write-Host ""
  Write-Host "Transaction Details:"
  Write-Host "─────────────────────────────────────────────────────────────"
  $results | Format-Table -AutoSize -Property transaction_id, compliance_status, fraud_risk
  
} finally { Pop-Location }
```

---

## Integration

This skill is designed to be run interactively to verify the pipeline is operating end-to-end. Output is displayed to the console for immediate feedback on transaction counts and outcome distribution.

---

## Related Skills

- `/validate-transactions` — Inspect the sample transaction input data.
