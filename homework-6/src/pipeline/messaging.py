"""Message envelope and I/O helpers for the transaction pipeline."""

import json
import uuid
from datetime import datetime, timezone
from pathlib import Path


def load_sample_transactions(path: str) -> list[dict]:
    """
    Load and parse sample transactions from a JSON file.

    Args:
        path: Path to sample-transactions.json

    Returns:
        List of transaction dictionaries

    Raises:
        FileNotFoundError: If the file does not exist
        json.JSONDecodeError: If the file is malformed JSON
    """
    filepath = Path(path)
    if not filepath.exists():
        raise FileNotFoundError(f"Transaction file not found: {path}")

    with open(filepath, 'r', encoding='utf-8') as f:
        transactions = json.load(f)

    if not isinstance(transactions, list):
        raise ValueError(f"Expected a list of transactions, got {type(transactions)}")

    return transactions


def create_message_envelope(
    transaction: dict,
    source_agent: str,
    target_agent: str,
    message_type: str
) -> dict:
    """
    Wrap a transaction in a standard JSON message envelope.

    Args:
        transaction: The transaction data to wrap
        source_agent: Name of the agent sending the message
        target_agent: Name of the agent receiving the message
        message_type: Type of message (e.g., "transaction")

    Returns:
        A message envelope dict with standard fields:
        - message_id: UUID4 string
        - timestamp: ISO 8601 UTC timestamp (YYYY-MM-DDTHH:MM:SSZ)
        - source_agent: Source agent name
        - target_agent: Target agent name
        - message_type: Message type
        - data: The nested transaction object
    """
    # Generate UUID4 for message_id
    message_id = str(uuid.uuid4())

    # Generate ISO 8601 UTC timestamp
    timestamp = datetime.now(timezone.utc).strftime('%Y-%m-%dT%H:%M:%SZ')

    envelope = {
        "message_id": message_id,
        "timestamp": timestamp,
        "source_agent": source_agent,
        "target_agent": target_agent,
        "message_type": message_type,
        "data": transaction
    }

    return envelope


def save_message(message: dict, shared_dir: str) -> None:
    """
    Write a message JSON file to the shared directory.

    Args:
        message: The message envelope to save
        shared_dir: Name of subdirectory within shared/ (e.g., "input", "processing")

    Raises:
        ValueError: If message_id is missing from the message
    """
    if "message_id" not in message:
        raise ValueError("Message must have a message_id field")

    message_id = message["message_id"]
    filepath = Path("shared") / shared_dir / f"{message_id}.json"

    # Ensure directory exists (mkdir -p equivalent)
    filepath.parent.mkdir(parents=True, exist_ok=True)

    with open(filepath, 'w', encoding='utf-8') as f:
        json.dump(message, f, indent=2, ensure_ascii=False)
