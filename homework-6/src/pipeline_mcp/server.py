"""
FastMCP server for the pipeline status service.

Exposes tools for querying transaction status and listing results,
plus a resource for pipeline summary.
"""

import json
from pathlib import Path
from typing import Dict, List, Any

try:
    from mcp.server.fastmcp import FastMCP
except ImportError:
    # Fallback for testing when mcp package may not be fully configured
    FastMCP = None


# Initialize the FastMCP server
server = FastMCP("pipeline-status") if FastMCP else None


def get_transaction_status(transaction_id: str) -> Dict[str, Any]:
    """
    Fetch transaction status and enriched data from pipeline results.

    Searches all result files in shared/results/ for a transaction matching
    the given transaction_id and returns its complete status including
    validation_result, fraud_score, and compliance_status.

    Args:
        transaction_id: The transaction ID to query (e.g., 'TXN001')

    Returns:
        Dictionary containing transaction data with validation, fraud, and compliance info

    Raises:
        FileNotFoundError: If no result file contains the transaction_id
    """
    results_dir = Path(__file__).parent.parent / "shared" / "results"

    # Search all result files for the matching transaction
    for result_file in results_dir.glob("*.json"):
        try:
            with open(result_file, 'r') as f:
                result_data = json.load(f)

            # Check if this result contains our transaction
            if result_data.get("data", {}).get("transaction_id") == transaction_id:
                # Return the enriched transaction data
                return {
                    "transaction_id": transaction_id,
                    "message_id": result_data.get("message_id"),
                    "timestamp": result_data.get("timestamp"),
                    "data": result_data.get("data", {}),
                    "validation_result": result_data.get("data", {}).get("validation_result"),
                    "fraud_score": result_data.get("data", {}).get("fraud_score"),
                    "compliance_status": result_data.get("data", {}).get("compliance_status")
                }
        except (json.JSONDecodeError, IOError):
            # Skip malformed files
            continue

    # Transaction not found
    raise FileNotFoundError(f"Transaction {transaction_id} not found in pipeline results")


def list_pipeline_results() -> List[str]:
    """
    List all completed transaction results in the pipeline.

    Scans the shared/results/ directory and returns a list of transaction IDs
    from all completed transactions.

    Returns:
        List of transaction_ids (e.g., ['TXN001', 'TXN002', ...])
    """
    results_dir = Path(__file__).parent.parent / "shared" / "results"
    transaction_ids = []

    for result_file in sorted(results_dir.glob("*.json")):
        try:
            with open(result_file, 'r') as f:
                result_data = json.load(f)
            transaction_id = result_data.get("data", {}).get("transaction_id")
            if transaction_id:
                transaction_ids.append(transaction_id)
        except (json.JSONDecodeError, IOError):
            # Skip malformed files
            continue

    return transaction_ids


def get_pipeline_summary() -> str:
    """
    Generate a summary of the pipeline state.

    Returns:
        A plain-text summary of completed results and their statuses
    """
    results_dir = Path(__file__).parent.parent / "shared" / "results"

    approved_count = 0
    held_count = 0
    total_count = 0

    for result_file in results_dir.glob("*.json"):
        try:
            with open(result_file, 'r') as f:
                result_data = json.load(f)
            total_count += 1

            compliance = result_data.get("data", {}).get("compliance_status", {})
            if compliance.get("hold_flag", False):
                held_count += 1
            else:
                approved_count += 1
        except (json.JSONDecodeError, IOError):
            # Skip malformed files
            continue

    summary = f"Pipeline has {total_count} results; {approved_count} approved, {held_count} held for review"
    return summary


# Register tools with the FastMCP server if server is available
if server:
    @server.tool()
    def get_transaction_status_tool(transaction_id: str) -> Dict[str, Any]:
        """Get the status of a specific transaction from the pipeline."""
        return get_transaction_status(transaction_id)

    @server.tool()
    def list_pipeline_results_tool() -> List[str]:
        """List all completed transaction results in the pipeline."""
        return list_pipeline_results()

    @server.resource("pipeline://summary")
    def pipeline_summary_resource() -> str:
        """Get a summary of the pipeline state."""
        return get_pipeline_summary()


def main():
    """Run the FastMCP server."""
    if server:
        server.run()
    else:
        print("FastMCP server not available; running in test mode")


if __name__ == "__main__":
    main()
