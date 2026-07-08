"""Multi-agent transaction pipeline integrator.

Orchestrates the creation of shared directories and loads sample transactions
into the pipeline's input queue, or runs the pipeline agents when no --setup flag.
"""

import argparse
import json
import logging
import sys
from pathlib import Path

from pipeline.messaging import load_sample_transactions, create_message_envelope, save_message
from agents.transaction_validator import validate_transaction
from agents.fraud_detector import detect_fraud
from agents.compliance_checker import check_compliance

# Configure logging
logging.basicConfig(level=logging.INFO, format='%(asctime)s - %(name)s - %(levelname)s - %(message)s')


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


def run_pipeline() -> None:
    """Run the transaction processing pipeline.

    Reads all .json files from shared/input/, processes each through:
      1. Transaction Validator -> shared/processing/
      2. Fraud Detector -> shared/output/
      3. Compliance Checker -> shared/results/

    Verifies all results are written to shared/results/.
    """
    input_dir = Path('shared') / 'input'
    processing_dir = Path('shared') / 'processing'
    output_dir = Path('shared') / 'output'
    results_dir = Path('shared') / 'results'

    # Verify input directory exists
    if not input_dir.exists():
        print(f"Error: Input directory not found: {input_dir}", file=sys.stderr)
        sys.exit(1)

    # Ensure processing, output, and results directories exist
    processing_dir.mkdir(parents=True, exist_ok=True)
    output_dir.mkdir(parents=True, exist_ok=True)
    results_dir.mkdir(parents=True, exist_ok=True)

    # Load all input messages
    input_files = sorted(input_dir.glob('*.json'))
    if not input_files:
        print("Warning: No input files found in shared/input/", file=sys.stderr)
        return

    print(f"Found {len(input_files)} input messages to process")

    processed_count = 0
    failed_count = 0

    for input_file in input_files:
        try:
            # Load message
            with open(input_file, 'r', encoding='utf-8') as f:
                message = json.load(f)

            message_id = message.get('message_id', 'UNKNOWN')
            print(f"Processing message {message_id}...")

            # Stage 1: Transaction Validator
            message = validate_transaction(message)
            processing_file = processing_dir / f"{message_id}.json"
            with open(processing_file, 'w', encoding='utf-8') as f:
                json.dump(message, f, indent=2, ensure_ascii=False)

            # Stage 2: Fraud Detector
            message = detect_fraud(message)
            output_file = output_dir / f"{message_id}.json"
            with open(output_file, 'w', encoding='utf-8') as f:
                json.dump(message, f, indent=2, ensure_ascii=False)

            # Stage 3: Compliance Checker (writes to results/)
            message = check_compliance(message)

            processed_count += 1
            print(f"  [OK] Message {message_id} processed successfully")

        except json.JSONDecodeError as e:
            print(f"Error decoding JSON from {input_file.name}: {e}", file=sys.stderr)
            failed_count += 1
        except (IOError, OSError) as e:
            print(f"Error processing {input_file.name} (file I/O): {e}", file=sys.stderr)
            failed_count += 1
        except (KeyError, ValueError) as e:
            print(f"Error processing {input_file.name} (data validation): {e}", file=sys.stderr)
            failed_count += 1

    # Verify results
    result_files = list(results_dir.glob('*.json'))
    print(f"\nPipeline complete: {processed_count} processed, {failed_count} failed")
    print(f"Results written to shared/results/: {len(result_files)} files")

    if processed_count > 0 and len(result_files) < processed_count:
        print(f"Warning: Expected {processed_count} results, but found {len(result_files)}", file=sys.stderr)
        sys.exit(1)


def main():
    """Main entry point for the integrator.

    Supports --setup mode which creates directories and loads sample transactions.
    Without --setup, runs the transaction processing pipeline.
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
        print("Starting pipeline mode...")
        run_pipeline()
        print("Pipeline complete.")


if __name__ == "__main__":
    main()
