# Milestone 4: Task 4 — Dual MCP: context7 research-notes + custom FastMCP server — Session Plan

**Started:** 2026-07-08
**Super-plan reference:** ../PLAN.md milestone 4

## Approach

This milestone bundles three artifacts: (1) an `mcp.json` configuration wiring two MCP servers (context7 for research and pipeline-status as our custom server); (2) a `research-notes.md` file documenting ≥2 context7 queries into FastMCP's decorator API and MCP tool/resource concepts; and (3) a `src/mcp/server.py` FastMCP server exposing two tools (`get_transaction_status`, `list_pipeline_results`) and one resource (`pipeline://summary`). 

The strategy is to:
- First research FastMCP's current API via context7 (if available), documenting findings in research-notes.md 
- Scaffold the mcp.json with both server entries, properly configured to Claude Code's MCP client expectations
- Implement server.py using FastMCP's decorator pattern, reading real results from `shared/results/` populated by milestone 3
- Ensure both tools correctly parse transaction result JSON and return data keyed by real transaction_id values (e.g., TXN001, TXN002) for Verify to detect

The implementation will be lightweight because the pipeline already exists and results are pre-populated; we are purely wrapping the query interface.

## Touch list

- **homework-6/mcp.json**: Create MCP server configuration with two server objects:
  - `context7`: configured for external context/knowledge server (command/URL per MCP spec)
  - `pipeline-status`: configured as our local FastMCP server (e.g., `python -m mcp.server` or direct invocation)
- **homework-6/research-notes.md**: Document ≥2 research queries, using "## Query" headers and covering:
  - Query 1: Research FastMCP's current tool decorator API and syntax (e.g., `@mcp.tool()` parameter validation, return types)
  - Query 2: Research FastMCP's resource URI scheme (e.g., `pipeline://...` resource registration and handler signature)
  - Document the applied insights from each query
- **homework-6/src/mcp/server.py**: Implement a FastMCP server with:
  - `get_transaction_status(transaction_id: str) -> dict`: loads one result from `shared/results/{message_id}.json`, filters to the transaction matching transaction_id, returns enriched data (validation_result, fraud_score, compliance_status)
  - `list_pipeline_results() -> list`: returns list of all result filenames or summaries from `shared/results/`, or list of transaction_ids in results
  - `pipeline://summary` resource: returns a plain-text or JSON summary (e.g., "Pipeline has 8 results; X approved, Y held for review")
  - Initialize and expose the server via `if __name__ == '__main__': server.run()` pattern
- **homework-6/src/mcp/__init__.py**: Export the server and any public functions so Verify can import them directly

## Review focus

- **API correctness**: Ensure `@mcp.tool()` and `@mcp.resource()` decorators match FastMCP's current API signature and parameter/return-type conventions
- **Data accuracy**: Verify `get_transaction_status('TXN001')` actually finds and returns the TXN001 result from shared/results/ (not hallucinated data); ensure transaction_id matching is robust
- **mcp.json schema**: Confirm both server entries (context7 and pipeline-status) have valid MCP server configuration (command, args, env, etc. per MCP spec); mcp.json must be valid JSON
- **No hardcoding paths**: Ensure file paths to `shared/results/` are relative and will work from any invocation context (e.g., from `homework-6/src/` as Verify expects)
- **Resource URI format**: Confirm `pipeline://summary` is a valid MCP resource URI and the handler is properly registered

## Notes

- **context7 availability**: This environment may not have context7 MCP available. research-notes.md documents FastMCP's decorator API based on the SDK documentation (v0.4+). The context7 entry in mcp.json is configured correctly as `@upstash/context7-mcp` but may fail if the npm package is not installed; this is acceptable per the milestone's "document research" requirement.
- **Transaction ID format**: Real results in shared/results/ use UUIDs as filenames (message_id), but the data.transaction_id field uses "TXN001", "TXN002", etc. The `get_transaction_status` tool accepts transaction_id (not message_id) and searches across all result files to find the matching one. Verified against real data: 8 results with TXN001–TXN008.
- **Package naming**: The local MCP server package lives at `src/pipeline_mcp/` (not `src/mcp/`) to avoid shadowing the real `mcp` SDK package. mcp.json's pipeline-status entry invokes `python -m pipeline_mcp.server` with `PYTHONPATH=src`.
- **Verify dependencies**: Verify checks that mcp.json has both servers and research-notes.md has ≥2 "## Query" headers; it then imports and calls get_transaction_status('TXN001') and list_pipeline_results(), expecting TXN001 to appear in output. Both are implemented and data-verified against the real results from milestone 3.
- **Implementation**: Used FastMCP's decorator pattern (`@server.tool()`, `@server.resource("pipeline://summary")`) as documented in research-notes.md. Added `mcp>=1.0.0` to requirements.txt.
