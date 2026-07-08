"""
Pipeline MCP server module for pipeline status service.

Exports the main server and query functions for use by the MCP client
and for testing/verification.
"""

from pipeline_mcp.server import get_transaction_status, list_pipeline_results, get_pipeline_summary, server

__all__ = [
    "server",
    "get_transaction_status",
    "list_pipeline_results",
    "get_pipeline_summary"
]
