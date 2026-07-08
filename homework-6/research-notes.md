# Research Notes: FastMCP and MCP Integration

**Date:** 2026-07-08  
**Purpose:** Document FastMCP API, decorator patterns, and MCP tool/resource concepts for implementing `src/mcp/server.py`.

---

## Query 1: FastMCP Decorator API and Tool Implementation

**Question:** How does FastMCP's `@mcp.tool()` decorator work, what parameters does it accept, and what return types are supported?

**Research Findings:**

FastMCP (v0.4+) uses a lightweight decorator-based pattern to expose Python functions as MCP tools:

- **Decorator syntax:** `@mcp.tool()` or `@mcp.tool(name="custom_name")`
- **Function signature:** Tool functions accept typed parameters (int, str, dict, list) and return JSON-serializable data (dict, list, str, int, bool)
- **Parameter validation:** FastMCP automatically validates parameter types against the function signature; parameters must be annotated with type hints (e.g., `transaction_id: str`)
- **Return type:** Functions must return dict, list, or primitive types. FastMCP serializes the return value to JSON automatically
- **Error handling:** Exceptions raised in tool functions are caught by FastMCP and returned as tool errors to the MCP client

**Applied Insights:**

For the pipeline MCP server, we will use:

```python
@mcp.tool()
def get_transaction_status(transaction_id: str) -> dict:
    """Fetch transaction status and details from pipeline results."""
    # Returns dict with transaction data: validation_result, fraud_score, compliance_status

@mcp.tool()
def list_pipeline_results() -> list:
    """List all completed transaction results in the pipeline."""
    # Returns list of dicts or transaction_ids
```

Both functions will have explicit type hints and return JSON-serializable dicts/lists.

---

## Query 2: FastMCP Resource URI Scheme and Handler Registration

**Question:** How does FastMCP implement MCP resources (as opposed to tools), what is the URI scheme, and how are resource handlers registered?

**Research Findings:**

FastMCP v0.4+ supports resources as read-only data exposed via a URI scheme:

- **Resource concept:** MCP resources are identified by a URI (e.g., `pipeline://summary`) and accessed via the `read_resource()` MCP method
- **Decorator syntax:** `@mcp.resource(uri)` or `@mcp.resource(uri_pattern)` registers a function to handle resource reads
- **URI format:** Resources use scheme://path format; FastMCP matches incoming URI read requests to the registered handler
- **Handler signature:** Resource handlers accept a URI as input and return plain text or JSON content
- **Example:** `@mcp.resource("pipeline://summary")` registers a handler for the exact URI; patterns like `pipeline://*` can match any resource under the pipeline scheme
- **Return type:** Typically plain text for readability, but JSON is also valid and will be serialized

**Applied Insights:**

For the pipeline MCP server, we will use:

```python
@mcp.resource("pipeline://summary")
def get_pipeline_summary(uri: str) -> str:
    """Return a plain-text or JSON summary of the pipeline state."""
    # Returns a summary string like: "Pipeline has 8 results; 7 approved, 1 held for review"
```

This resource will be queryable by Claude Code via the MCP client, allowing interactive queries like "What's the status of the pipeline?"

---

## Implementation Plan

Based on the above research:

1. **server.py** will instantiate a FastMCP server, decorate two tools (`get_transaction_status`, `list_pipeline_results`) and one resource (`pipeline://summary`)
2. **__init__.py** will export these functions so they can be imported for testing
3. **mcp.json** will register the server command so Claude Code's MCP client can discover and communicate with it

The lightweight approach leverages FastMCP's built-in JSON serialization and type validation, avoiding manual parsing or error handling.
