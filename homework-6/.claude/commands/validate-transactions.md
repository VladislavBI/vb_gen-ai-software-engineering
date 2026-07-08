# /validate-transactions

**Inspect and validate the sample transaction input data.**

Use this skill to load and display the sample transactions from `sample-transactions.json`, and run the Transaction Validator agent in isolation to verify its validation logic.

---

## Overview

The transaction pipeline begins with a set of sample transactions defined in `sample-transactions.json`. This skill loads those transactions, displays them in a structured format, and runs the Transaction Validator agent to show validation results before the full pipeline processes them.

---

## Helper Function

Define a PowerShell 5.1-compatible null-coalescing helper:

```powershell
function Get-OrDefault {
  param([object]$Value, [object]$Default)
  if ($null -eq $Value) { $Default } else { $Value }
}
```

---

## Steps

### 1. Load and Display Sample Transactions

Read `sample-transactions.json` and display the transactions in a table:

```powershell
Push-Location homework-6/src
try {
  if (-not (Test-Path sample-transactions.json)) {
    throw "sample-transactions.json not found"
  }
  
  $transactions = Get-Content sample-transactions.json -Raw | ConvertFrom-Json
  
  Write-Host "Sample Transactions Loaded"
  Write-Host "─────────────────────────────────────────────────────────────"
  Write-Host "Total transactions: $($transactions.Length)"
  Write-Host ""
  
  # Display transaction details in a table
  $txnDisplay = @()
  foreach ($txn in $transactions) {
    $txnDisplay += @{
      transaction_id = $txn.transaction_id
      amount = $txn.amount
      currency = $txn.currency
      timestamp = $txn.timestamp
    }
  }
  
  $txnDisplay | Format-Table -AutoSize -Property transaction_id, amount, currency, timestamp
  
} finally { Pop-Location }
```

### 2. Run Transaction Validator in Dry-Run Mode

Invoke the Transaction Validator agent to show validation results without processing through the full pipeline:

```powershell
Push-Location homework-6/src
try {
  Write-Host ""
  Write-Host "Running Validator Agent (Dry-Run Mode)"
  Write-Host "─────────────────────────────────────────────────────────────"
  
  # Import the validator and run it against sample transactions
  python -c @"
import json
from agents.transaction_validator import validate_transaction

with open('sample-transactions.json', 'r') as f:
    transactions = json.load(f)

print('Validation Results:')
print('-' * 60)

validation_counts = {'VALID': 0, 'INVALID': 0}
for txn in transactions:
    # Create a message envelope for the validator
    message = {
        'message_id': f"msg_{txn.get('transaction_id', 'unknown')}",
        'data': txn
    }
    
    # Call the validator
    result = validate_transaction(message)
    
    # Extract validation result from the message envelope
    validation_result = result.get('data', {}).get('validation_result', {})
    is_valid = validation_result.get('is_valid', False)
    errors = validation_result.get('errors', [])
    
    status = 'VALID' if is_valid else 'INVALID'
    validation_counts[status] = validation_counts.get(status, 0) + 1
    
    error_str = f" ({'; '.join(errors)})" if errors else ""
    print(f"  {txn.get('transaction_id', 'UNKNOWN'):12} : {status}{error_str}")

print('-' * 60)
print('Validation Summary:')
for status, count in sorted(validation_counts.items()):
    print(f"  {status}: {count}")
"@
  
  if ($LASTEXITCODE -ne 0) {
    Write-Host "Warning: Validator execution returned exit code $LASTEXITCODE"
  }
  
} finally { Pop-Location }
```

### 3. Display Validity Breakdown

Summarize the validation results:

```powershell
Write-Host ""
Write-Host "Validity Breakdown:"
Write-Host "─────────────────────────────────────────────────────────────"
Write-Host "Transactions marked VALID are ready to proceed through Fraud Detection and Compliance Checking."
Write-Host "Transactions marked INVALID should be reviewed for data quality issues."
```

---

## Integration

This skill is designed to be run before executing the full pipeline (`/run-pipeline`) to inspect the input data and verify the Validator agent's logic in isolation. The output helps troubleshoot data issues and validate agent behavior.

---

## Related Skills

- `/run-pipeline` — Execute the full multi-agent pipeline.
