# Sample Requests and MCP Tool Calls

This document demonstrates example transaction payloads and MCP tool calls for interacting with the banking transaction pipeline.

## Transaction Message Format

All transactions flow through the pipeline as JSON message envelopes. Below are realistic examples from `sample-transactions.json`.

### Example 1: Normal Domestic Transfer

**Original transaction (from sample-transactions.json):**
```json
{
  "transaction_id": "TXN001",
  "timestamp": "2026-03-16T09:00:00Z",
  "source_account": "ACC-1001",
  "destination_account": "ACC-2001",
  "amount": "1500.00",
  "currency": "USD",
  "transaction_type": "transfer",
  "description": "Monthly rent payment",
  "metadata": {
    "channel": "online",
    "country": "US"
  }
}
```

**Message envelope (after integrator processing):**
```json
{
  "message_id": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-07-08T14:30:00Z",
  "source_agent": "integrator",
  "target_agent": "transaction_validator",
  "message_type": "transaction",
  "data": {
    "transaction_id": "TXN001",
    "timestamp": "2026-03-16T09:00:00Z",
    "source_account": "ACC-1001",
    "destination_account": "ACC-2001",
    "amount": "1500.00",
    "currency": "USD",
    "transaction_type": "transfer",
    "description": "Monthly rent payment",
    "metadata": {
      "channel": "online",
      "country": "US"
    }
  }
}
```

**Final result (after all agents, in shared/results/):**
```json
{
  "message_id": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-07-08T14:30:00Z",
  "source_agent": "integrator",
  "target_agent": "transaction_validator",
  "message_type": "transaction",
  "data": {
    "transaction_id": "TXN001",
    "timestamp": "2026-03-16T09:00:00Z",
    "source_account": "ACC-1001",
    "destination_account": "ACC-2001",
    "amount": "1500.00",
    "currency": "USD",
    "transaction_type": "transfer",
    "description": "Monthly rent payment",
    "metadata": {
      "channel": "online",
      "country": "US"
    },
    "validation_result": {
      "is_valid": true,
      "errors": [],
      "timestamp": "2026-07-08T14:30:05Z"
    },
    "fraud_score": {
      "risk_level": "LOW",
      "score": 5,
      "factors": {
        "high_amount": false,
        "off_hours": false,
        "cross_border": false,
        "wire_transfer": false
      },
      "timestamp": "2026-07-08T14:30:06Z"
    },
    "compliance_status": {
      "status": "APPROVED",
      "hold_flag": false,
      "hold_reasons": [],
      "timestamp": "2026-07-08T14:30:07Z"
    }
  }
}
```

### Example 2: High-Value Wire Transfer (HOLD_PENDING_REVIEW)

**Original transaction:**
```json
{
  "transaction_id": "TXN002",
  "timestamp": "2026-03-16T09:15:00Z",
  "source_account": "ACC-1002",
  "destination_account": "ACC-3001",
  "amount": "25000.00",
  "currency": "USD",
  "transaction_type": "wire_transfer",
  "description": "Equipment purchase",
  "metadata": {
    "channel": "branch",
    "country": "US"
  }
}
```

**Final result (with HOLD due to high fraud risk):**
```json
{
  "message_id": "660f9511-f39c-52e5-b837-5f7766551111",
  "timestamp": "2026-07-08T14:30:15Z",
  "source_agent": "integrator",
  "target_agent": "transaction_validator",
  "message_type": "transaction",
  "data": {
    "transaction_id": "TXN002",
    "timestamp": "2026-03-16T09:15:00Z",
    "source_account": "ACC-1002",
    "destination_account": "ACC-3001",
    "amount": "25000.00",
    "currency": "USD",
    "transaction_type": "wire_transfer",
    "description": "Equipment purchase",
    "metadata": {
      "channel": "branch",
      "country": "US"
    },
    "validation_result": {
      "is_valid": true,
      "errors": [],
      "timestamp": "2026-07-08T14:30:20Z"
    },
    "fraud_score": {
      "risk_level": "HIGH",
      "score": 55,
      "factors": {
        "high_amount": true,
        "off_hours": false,
        "cross_border": false,
        "wire_transfer": true
      },
      "timestamp": "2026-07-08T14:30:21Z"
    },
    "compliance_status": {
      "status": "HOLD_PENDING_REVIEW",
      "hold_flag": true,
      "hold_reasons": [
        "High fraud risk detected: HIGH"
      ],
      "timestamp": "2026-07-08T14:30:22Z"
    }
  }
}
```

### Example 3: Invalid Transaction (Unsupported Currency)

**Original transaction:**
```json
{
  "transaction_id": "TXN006",
  "timestamp": "2026-03-16T10:05:00Z",
  "source_account": "ACC-1006",
  "destination_account": "ACC-7700",
  "amount": "200.00",
  "currency": "XYZ",
  "transaction_type": "transfer",
  "description": "Test payment",
  "metadata": {
    "channel": "online",
    "country": "US"
  }
}
```

**Final result (with HOLD due to validation failure):**
```json
{
  "message_id": "770g0622-g40d-63f6-c948-6g8877662222",
  "timestamp": "2026-07-08T14:31:00Z",
  "source_agent": "integrator",
  "target_agent": "transaction_validator",
  "message_type": "transaction",
  "data": {
    "transaction_id": "TXN006",
    "timestamp": "2026-03-16T10:05:00Z",
    "source_account": "ACC-1006",
    "destination_account": "ACC-7700",
    "amount": "200.00",
    "currency": "XYZ",
    "transaction_type": "transfer",
    "description": "Test payment",
    "metadata": {
      "channel": "online",
      "country": "US"
    },
    "validation_result": {
      "is_valid": false,
      "errors": [
        "Unsupported currency: XYZ"
      ],
      "timestamp": "2026-07-08T14:31:05Z"
    },
    "fraud_score": {
      "risk_level": "CRITICAL",
      "score": 100,
      "factors": {
        "high_amount": false,
        "off_hours": false,
        "cross_border": false,
        "wire_transfer": false,
        "validation_failed": true
      },
      "timestamp": "2026-07-08T14:31:06Z"
    },
    "compliance_status": {
      "status": "HOLD_PENDING_REVIEW",
      "hold_flag": true,
      "hold_reasons": [
        "Validation failed: Unsupported currency: XYZ",
        "High fraud risk detected: CRITICAL"
      ],
      "timestamp": "2026-07-08T14:31:07Z"
    }
  }
}
```

### Example 4: Cross-Border Refund (Negative Amount)

**Original transaction:**
```json
{
  "transaction_id": "TXN007",
  "timestamp": "2026-03-16T10:10:00Z",
  "source_account": "ACC-1007",
  "destination_account": "ACC-8800",
  "amount": "-100.00",
  "currency": "GBP",
  "transaction_type": "refund",
  "description": "Refund for order #8821",
  "metadata": {
    "channel": "online",
    "country": "GB"
  }
}
```

**Final result (APPROVED — refund is valid and low-risk):**
```json
{
  "message_id": "880h1733-h51e-74g7-d059-7h9988773333",
  "timestamp": "2026-07-08T14:31:30Z",
  "source_agent": "integrator",
  "target_agent": "transaction_validator",
  "message_type": "transaction",
  "data": {
    "transaction_id": "TXN007",
    "timestamp": "2026-03-16T10:10:00Z",
    "source_account": "ACC-1007",
    "destination_account": "ACC-8800",
    "amount": "-100.00",
    "currency": "GBP",
    "transaction_type": "refund",
    "description": "Refund for order #8821",
    "metadata": {
      "channel": "online",
      "country": "GB"
    },
    "validation_result": {
      "is_valid": true,
      "errors": [],
      "timestamp": "2026-07-08T14:31:35Z"
    },
    "fraud_score": {
      "risk_level": "MEDIUM",
      "score": 25,
      "factors": {
        "high_amount": false,
        "off_hours": false,
        "cross_border": true,
        "wire_transfer": false
      },
      "timestamp": "2026-07-08T14:31:36Z"
    },
    "compliance_status": {
      "status": "APPROVED",
      "hold_flag": false,
      "hold_reasons": [],
      "timestamp": "2026-07-08T14:31:37Z"
    }
  }
}
```

---

## MCP Tool Calls and Expected Responses

The custom FastMCP server in `src/pipeline_mcp/server.py` exposes two primary tools:

### Tool 1: `get_transaction_status_tool`

**Purpose:** Retrieve the final status and details of a specific transaction by transaction_id.

**Request:**
```json
{
  "method": "tools/call",
  "params": {
    "name": "get_transaction_status_tool",
    "arguments": {
      "transaction_id": "TXN001"
    }
  }
}
```

**Expected Response:**
The tool returns a dictionary with the complete transaction record including all processing stages:
```json
{
  "message_id": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2026-07-08T14:30:00Z",
  "source_agent": "integrator",
  "target_agent": "transaction_validator",
  "message_type": "transaction",
  "transaction_id": "TXN001",
  "data": {
    "transaction_id": "TXN001",
    "amount": "1500.00",
    "currency": "USD",
    "timestamp": "2026-03-16T09:00:00Z",
    "source_account": "ACC-1001",
    "destination_account": "ACC-2001",
    "transaction_type": "transfer",
    "description": "Monthly rent payment",
    "metadata": {
      "channel": "online",
      "country": "US"
    },
    "validation_result": {
      "is_valid": true,
      "errors": [],
      "timestamp": "2026-07-08T14:30:05Z"
    },
    "fraud_score": {
      "risk_level": "LOW",
      "score": 5,
      "factors": {
        "high_amount": false,
        "off_hours": false,
        "cross_border": false,
        "wire_transfer": false
      },
      "timestamp": "2026-07-08T14:30:06Z"
    },
    "compliance_status": {
      "status": "APPROVED",
      "hold_flag": false,
      "hold_reasons": [],
      "timestamp": "2026-07-08T14:30:07Z"
    }
  }
}
```

### Tool 2: `list_pipeline_results_tool`

**Purpose:** List all transactions currently in `shared/results/` with their compliance status.

**Request:**
```json
{
  "method": "tools/call",
  "params": {
    "name": "list_pipeline_results_tool",
    "arguments": {}
  }
}
```

**Expected Response:**
The tool returns a list of transaction IDs (string list):
```json
[
  "TXN001",
  "TXN002",
  "TXN003",
  "TXN004",
  "TXN005",
  "TXN006",
  "TXN007",
  "TXN008"
]
```

To get detailed status for each transaction, use `get_transaction_status_tool` with each transaction_id.

### Resource: `pipeline://summary`

**Purpose:** Retrieve a high-level summary of pipeline execution and statistics.

**Request:**
```json
{
  "method": "resources/read",
  "params": {
    "uri": "pipeline://summary"
  }
}
```

**Expected Response:**
The resource returns a summary string:
```
Pipeline has 8 results; 4 approved, 4 held for review
```

This is a high-level overview. For detailed per-transaction breakdown, use `list_pipeline_results_tool` to enumerate all transaction IDs, then call `get_transaction_status_tool` for each ID.

---

## context7 Research Queries

The `mcp.json` configuration includes the context7 MCP server for knowledge research. Below are example queries used in `research-notes.md`:

### Query 1: ISO 4217 Currency Codes

**Context7 Tool:** `search`  
**Query:** `"ISO 4217 currency codes USD EUR GBP JPY CAD AUD CHF validation"`  
**Expected Response:** Documentation of ISO 4217 standard, supported currency codes, and validation rules used by the transaction validator.

### Query 2: PII (Personally Identifiable Information) Detection

**Context7 Tool:** `search`  
**Query:** `"PII detection keywords SSN password credit card PIN CVV compliance regulations"`  
**Expected Response:** Reference materials on PII categories, keywords to detect (password, SSN, credit card, PIN, CVV), and compliance requirements (GDPR, HIPAA, etc.) used by the compliance checker.

---

## Invoking MCP Tools

The custom FastMCP server in `src/pipeline_mcp/server.py` uses **stdio transport** (not HTTP), so HTTP-based invocations shown in some MCP documentation do not apply here.

### Recommended Invocation Methods

**Via Claude Code IDE (Recommended):** 
Use the built-in MCP tool-call interface to invoke:
- Tool: `get_transaction_status_tool` with argument `transaction_id: "TXN001"`
- Tool: `list_pipeline_results_tool` (no arguments)
- Resource: `pipeline://summary`

This is the simplest and most reliable approach for interactive debugging and testing.

**Via Python MCP Client SDK:** 
```python
import subprocess
from mcp.client.stdio import stdio_session

async def query_pipeline():
    async with stdio_session(
        subprocess.Popen(["python", "-m", "pipeline_mcp.server"])
    ) as session:
        result = await session.call_tool("list_pipeline_results_tool", {})
        print(result)
```

### Registered Tool Names and Signatures

| Tool Name | Arguments | Returns |
|-----------|-----------|---------|
| `get_transaction_status_tool` | `transaction_id: str` | Transaction status dict with validation, fraud, compliance data |
| `list_pipeline_results_tool` | (none) | List of transaction status strings |
| `pipeline://summary` (resource) | (none) | Summary text of pipeline state and statistics |

---

## Summary

- **Transaction envelopes** follow a standard JSON format with message_id, timestamps, and nested data
- **MCP tools** (`get_transaction_status`, `list_pipeline_results`) provide queries into pipeline results
- **MCP resources** (`pipeline://summary`) expose aggregated statistics
- **context7 queries** document domain knowledge (ISO 4217, PII, compliance)

For detailed instructions on running these requests, see **`HOWTORUN.md`** and the MCP configuration in **`mcp.json`**.
