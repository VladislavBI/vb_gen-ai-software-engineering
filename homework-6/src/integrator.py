"""Multi-agent transaction pipeline integrator.

Orchestrates the creation of shared directories and loads sample transactions
into the pipeline's input queue.
"""

import argparse
import sys
from pathlib import Path

from pipeline.messaging import load_sample_transactions, create_message_envelope, save_message


def setup_directories() -> None:
    """Create the four shared subdirectories (input, processing, output, results).

    Uses idempotent directory creation (mkdir -p equivalent).
    """
    shared_dirs = ['input', 'processing', 'output', 'results']

    for dir_name in shared_dirs:
        dir_path = Path('shared') / dir_name
        dir_path.mkdir(parents=True, exist_ok=True)
        print(f"Created/verified directory: {dir_path}")


def load_transactions_and_queue(sample_file: str) -> None:
    """Load sample transactions and queue them for processing.

    Loads all transactions from sample-transactions.json, wraps each in a
    message envelope with source_agent='integrator' and
    target_agent='transaction_validator', and saves to shared/input/.

    Args:
        sample_file: Path to sample-transactions.json (relative to src directory)

    Raises:
        FileNotFoundError: If sample_file does not exist
        json.JSONDecodeError: If sample_file is malformed
    """
    try:
        transactions = load_sample_transactions(sample_file)
    except FileNotFoundError as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)
    except ValueError as e:
        print(f"Error: {e}", file=sys.stderr)
        sys.exit(1)

    print(f"Loaded {len(transactions)} transactions from {sample_file}")

    for transaction in transactions:
        try:
            message = create_message_envelope(
                transaction=transaction,
                source_agent="integrator",
                target_agent="transaction_validator",
                message_type="transaction"
            )
            save_message(message, 'input')
            print(f"Queued transaction {transaction.get('transaction_id')} "
                  f"(message_id={message['message_id']})")
        except (ValueError, KeyError) as e:
            print(f"Error processing transaction: {e}", file=sys.stderr)
            sys.exit(1)

    print(f"Successfully queued {len(transactions)} messages to shared/input/")


def main():
    """Main entry point for the integrator.

    Supports --setup mode which creates directories and loads sample transactions.
    """
    parser = argparse.ArgumentParser(
        description="Multi-agent transaction pipeline integrator"
    )
    parser.add_argument(
        '--setup',
        action='store_true',
        help='Initialize directories and load sample transactions'
    )

    args = parser.parse_args()

    if args.setup:
        print("Starting setup mode...")
        setup_directories()
        # sample-transactions.json is in the homework root (parent of src)
        sample_file = Path(__file__).parent.parent / "sample-transactions.json"
        load_transactions_and_queue(str(sample_file))
        print("Setup complete.")
    else:
        print("Usage: python integrator.py --setup", file=sys.stderr)
        sys.exit(1)


if __name__ == "__main__":
    main()
