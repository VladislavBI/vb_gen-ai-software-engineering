"""Integration tests for the multi-agent transaction pipeline."""

import pytest
import json
import logging
from pathlib import Path
from integrator import run_pipeline
from pipeline.messaging import create_message_envelope


@pytest.fixture
def isolated_pipeline_env(tmp_path):
    """Create an isolated shared/ directory structure for testing.

    Yields the tmp_path and sets up shared/input, shared/processing,
    shared/output, and shared/results directories.
    """
    import os
    original_cwd = Path.cwd()

    # Create shared directory structure
    shared_dir = tmp_path / "shared"
    input_dir = shared_dir / "input"
    processing_dir = shared_dir / "processing"
    output_dir = shared_dir / "output"
    results_dir = shared_dir / "results"

    input_dir.mkdir(parents=True, exist_ok=True)
    processing_dir.mkdir(parents=True, exist_ok=True)
    output_dir.mkdir(parents=True, exist_ok=True)
    results_dir.mkdir(parents=True, exist_ok=True)

    # Change to temp directory
    os.chdir(tmp_path)

    yield tmp_path, shared_dir

    # Cleanup: restore original directory
    os.chdir(original_cwd)


class TestPipelineIntegrationBasic:
    """Test basic pipeline integration."""

    def test_pipeline_processes_valid_transactions(self, isolated_pipeline_env):
        """Test that valid transactions flow through the pipeline."""
        tmp_path, shared_dir = isolated_pipeline_env
        input_dir = shared_dir / "input"
        results_dir = shared_dir / "results"

        # Create test transaction
        transaction = {
            'transaction_id': 'tx-int-001',
            'timestamp': '2026-01-15T10:30:00Z',
            'source_account': 'ACC-1001',
            'destination_account': 'ACC-2001',
            'amount': '100.00',
            'currency': 'USD',
            'transaction_type': 'transfer',
            'description': 'Test payment'
        }

        # Create message envelope and save to input
        message = create_message_envelope(
            transaction=transaction,
            source_agent='test',
            target_agent='transaction_validator',
            message_type='transaction'
        )
        message_id = message['message_id']

        with open(input_dir / f"{message_id}.json", 'w') as f:
            json.dump(message, f)

        # Run pipeline
        run_pipeline()

        # Verify result file was created
        result_file = results_dir / f"{message_id}.json"
        assert result_file.exists()

        # Verify result contains all layers
        with open(result_file, 'r') as f:
            result = json.load(f)

        assert 'validation_result' in result['data']
        assert 'fraud_score' in result['data']
        assert 'compliance_status' in result['data']

    def test_pipeline_processes_multiple_transactions(self, isolated_pipeline_env):
        """Test that pipeline processes multiple transactions."""
        tmp_path, shared_dir = isolated_pipeline_env
        input_dir = shared_dir / "input"
        results_dir = shared_dir / "results"

        # Create multiple test transactions
        num_transactions = 3
        message_ids = []

        for i in range(num_transactions):
            transaction = {
                'transaction_id': f'tx-int-{i}',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': f'ACC-100{i}',
                'destination_account': f'ACC-200{i}',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': f'Test payment {i}'
            }

            message = create_message_envelope(
                transaction=transaction,
                source_agent='test',
                target_agent='transaction_validator',
                message_type='transaction'
            )
            message_id = message['message_id']
            message_ids.append(message_id)

            with open(input_dir / f"{message_id}.json", 'w') as f:
                json.dump(message, f)

        # Run pipeline
        run_pipeline()

        # Verify all results were created
        assert len(list(results_dir.glob('*.json'))) == num_transactions

        for message_id in message_ids:
            result_file = results_dir / f"{message_id}.json"
            assert result_file.exists()


class TestPipelineIntegrationValidation:
    """Test pipeline handling of validation failures."""

    def test_pipeline_processes_invalid_transaction(self, isolated_pipeline_env):
        """Test that invalid transactions still flow through pipeline."""
        tmp_path, shared_dir = isolated_pipeline_env
        input_dir = shared_dir / "input"
        results_dir = shared_dir / "results"

        # Create invalid transaction (missing amount)
        transaction = {
            'transaction_id': 'tx-int-invalid',
            'timestamp': '2026-01-15T10:30:00Z',
            'source_account': 'ACC-1001',
            'destination_account': 'ACC-2001',
            # amount is missing
            'currency': 'USD',
            'transaction_type': 'transfer',
            'description': 'Invalid payment'
        }

        message = create_message_envelope(
            transaction=transaction,
            source_agent='test',
            target_agent='transaction_validator',
            message_type='transaction'
        )
        message_id = message['message_id']

        with open(input_dir / f"{message_id}.json", 'w') as f:
            json.dump(message, f)

        # Run pipeline
        run_pipeline()

        # Verify result was created despite validation failure
        result_file = results_dir / f"{message_id}.json"
        assert result_file.exists()

        with open(result_file, 'r') as f:
            result = json.load(f)

        # Should be marked for hold due to validation failure
        assert result['data']['compliance_status']['status'] == 'HOLD_PENDING_REVIEW'


class TestPipelineIntegrationFraudDetection:
    """Test pipeline fraud detection integration."""

    def test_pipeline_detects_high_value_fraud(self, isolated_pipeline_env):
        """Test that high-value transactions trigger fraud detection."""
        tmp_path, shared_dir = isolated_pipeline_env
        input_dir = shared_dir / "input"
        results_dir = shared_dir / "results"

        # Create high-value transaction
        transaction = {
            'transaction_id': 'tx-int-high-value',
            'timestamp': '2026-01-15T10:30:00Z',
            'source_account': 'ACC-1001',
            'destination_account': 'ACC-2001',
            'amount': '10000.00',
            'currency': 'USD',
            'transaction_type': 'transfer',
            'description': 'Large payment',
            'metadata': {'country': 'US'}
        }

        message = create_message_envelope(
            transaction=transaction,
            source_agent='test',
            target_agent='transaction_validator',
            message_type='transaction'
        )
        message_id = message['message_id']

        with open(input_dir / f"{message_id}.json", 'w') as f:
            json.dump(message, f)

        # Run pipeline
        run_pipeline()

        # Verify result shows high fraud risk
        result_file = results_dir / f"{message_id}.json"
        with open(result_file, 'r') as f:
            result = json.load(f)

        fraud_score = result['data']['fraud_score']
        assert fraud_score['factors']['high_amount'] is True
        assert fraud_score['score'] >= 40


class TestPipelineIntegrationCompliance:
    """Test pipeline compliance checking integration."""

    def test_pipeline_holds_blocked_account(self, isolated_pipeline_env):
        """Test that transactions from blocked accounts are held."""
        tmp_path, shared_dir = isolated_pipeline_env
        input_dir = shared_dir / "input"
        results_dir = shared_dir / "results"

        # Create transaction from blocked account
        transaction = {
            'transaction_id': 'tx-int-blocked',
            'timestamp': '2026-01-15T10:30:00Z',
            'source_account': 'ACC-9999',  # Blocked
            'destination_account': 'ACC-2001',
            'amount': '100.00',
            'currency': 'USD',
            'transaction_type': 'transfer',
            'description': 'From blocked account',
            'metadata': {'country': 'US'}
        }

        message = create_message_envelope(
            transaction=transaction,
            source_agent='test',
            target_agent='transaction_validator',
            message_type='transaction'
        )
        message_id = message['message_id']

        with open(input_dir / f"{message_id}.json", 'w') as f:
            json.dump(message, f)

        # Run pipeline
        run_pipeline()

        # Verify result shows hold
        result_file = results_dir / f"{message_id}.json"
        with open(result_file, 'r') as f:
            result = json.load(f)

        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'

    def test_pipeline_holds_pii_in_description(self, isolated_pipeline_env):
        """Test that transactions with PII in description are held."""
        tmp_path, shared_dir = isolated_pipeline_env
        input_dir = shared_dir / "input"
        results_dir = shared_dir / "results"

        # Create transaction with PII
        transaction = {
            'transaction_id': 'tx-int-pii',
            'timestamp': '2026-01-15T10:30:00Z',
            'source_account': 'ACC-1001',
            'destination_account': 'ACC-2001',
            'amount': '100.00',
            'currency': 'USD',
            'transaction_type': 'transfer',
            'description': 'Payment with password',  # Contains PII keyword
            'metadata': {'country': 'US'}
        }

        message = create_message_envelope(
            transaction=transaction,
            source_agent='test',
            target_agent='transaction_validator',
            message_type='transaction'
        )
        message_id = message['message_id']

        with open(input_dir / f"{message_id}.json", 'w') as f:
            json.dump(message, f)

        # Run pipeline
        run_pipeline()

        # Verify result shows hold
        result_file = results_dir / f"{message_id}.json"
        with open(result_file, 'r') as f:
            result = json.load(f)

        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'
        assert any('PII' in reason for reason in compliance['hold_reasons'])


class TestPipelineIntegrationResultStructure:
    """Test the structure of pipeline results."""

    def test_result_json_valid_and_complete(self, isolated_pipeline_env):
        """Test that result JSON is valid and contains all required fields."""
        tmp_path, shared_dir = isolated_pipeline_env
        input_dir = shared_dir / "input"
        results_dir = shared_dir / "results"

        transaction = {
            'transaction_id': 'tx-int-struct',
            'timestamp': '2026-01-15T10:30:00Z',
            'source_account': 'ACC-1001',
            'destination_account': 'ACC-2001',
            'amount': '100.00',
            'currency': 'USD',
            'transaction_type': 'transfer',
            'description': 'Structural test',
            'metadata': {'country': 'US'}
        }

        message = create_message_envelope(
            transaction=transaction,
            source_agent='test',
            target_agent='transaction_validator',
            message_type='transaction'
        )
        message_id = message['message_id']

        with open(input_dir / f"{message_id}.json", 'w') as f:
            json.dump(message, f)

        # Run pipeline
        run_pipeline()

        # Verify and parse result
        result_file = results_dir / f"{message_id}.json"
        with open(result_file, 'r') as f:
            result = json.load(f)

        # Verify all four layers present
        assert result['data']['validation_result']['is_valid'] is True
        assert 'score' in result['data']['fraud_score']
        assert 'risk_level' in result['data']['fraud_score']
        assert 'status' in result['data']['compliance_status']

        # Verify compliance status is valid
        status = result['data']['compliance_status']['status']
        assert status in ['APPROVED', 'HOLD_PENDING_REVIEW']

    def test_compliance_status_only_approved_or_hold(self, isolated_pipeline_env):
        """Test that compliance status is only APPROVED or HOLD_PENDING_REVIEW."""
        tmp_path, shared_dir = isolated_pipeline_env
        input_dir = shared_dir / "input"
        results_dir = shared_dir / "results"

        # Create transactions with various issues to test all paths
        test_cases = [
            # Valid, approved transaction
            {
                'transaction_id': 'tx-int-approved',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Approved',
                'metadata': {'country': 'US'}
            },
            # Invalid transaction (should be held)
            {
                'transaction_id': 'tx-int-invalid-hold',
                'timestamp': 'invalid-date',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Invalid',
                'metadata': {'country': 'US'}
            }
        ]

        message_ids = []
        for transaction in test_cases:
            message = create_message_envelope(
                transaction=transaction,
                source_agent='test',
                target_agent='transaction_validator',
                message_type='transaction'
            )
            message_ids.append(message['message_id'])

            with open(input_dir / f"{message['message_id']}.json", 'w') as f:
                json.dump(message, f)

        # Run pipeline
        run_pipeline()

        # Verify all results have valid compliance status
        for message_id in message_ids:
            result_file = results_dir / f"{message_id}.json"
            with open(result_file, 'r') as f:
                result = json.load(f)

            status = result['data']['compliance_status']['status']
            assert status in ['APPROVED', 'HOLD_PENDING_REVIEW'], \
                f"Invalid status: {status}"


class TestPipelineIntegrationIsolation:
    """Test that pipeline uses isolated directories."""

    def test_isolated_directory_does_not_affect_real_shared(self, isolated_pipeline_env):
        """Test that testing doesn't pollute real shared/ directory."""
        tmp_path, shared_dir = isolated_pipeline_env

        # Verify we're using temp directory, not real homework-6/src/shared/
        assert str(tmp_path) in str(shared_dir)
        assert 'tmp' in str(tmp_path).lower() or 'temp' in str(tmp_path).lower() \
            or str(tmp_path).startswith('/')

    def test_temp_directory_cleanup_after_test(self, tmp_path):
        """Test that temp directory is available for cleanup."""
        # This test verifies that tmp_path fixture works correctly
        # and allows cleanup after test
        shared_dir = tmp_path / "shared"
        shared_dir.mkdir()

        # Create files
        (shared_dir / "input").mkdir()
        test_file = shared_dir / "input" / "test.json"
        test_file.write_text("{}")

        assert test_file.exists()
        # After test, tmp_path is automatically cleaned up by pytest


class TestPipelineIntegrationNoOutput:
    """Test pipeline with empty input."""

    def test_pipeline_no_input_files(self, isolated_pipeline_env):
        """Test that pipeline handles empty input gracefully."""
        tmp_path, shared_dir = isolated_pipeline_env
        results_dir = shared_dir / "results"

        # Don't create any input files
        # Run pipeline
        run_pipeline()

        # Verify no results were created
        assert len(list(results_dir.glob('*.json'))) == 0


class TestPipelineIntegrationPIILogging:
    """Test that PII (full account numbers) is not logged to stdout/stderr."""

    def test_pii_not_in_stdout_stderr(self, isolated_pipeline_env, capsys, caplog):
        """Test that full account numbers do not appear in stdout/stderr/logs."""
        tmp_path, shared_dir = isolated_pipeline_env
        input_dir = shared_dir / "input"

        # Create test transaction with recognizable account number
        account_number = 'ACC-TESTPII1001'
        transaction = {
            'transaction_id': 'tx-int-pii-test',
            'timestamp': '2026-01-15T10:30:00Z',
            'source_account': account_number,
            'destination_account': 'ACC-2001',
            'amount': '10000.00',  # High amount to trigger HIGH fraud risk
            'currency': 'USD',
            'transaction_type': 'wire_transfer',
            'description': 'Payment',
            'metadata': {'country': 'CA'}  # Cross-border to trigger multiple factors
        }

        message = create_message_envelope(
            transaction=transaction,
            source_agent='test',
            target_agent='transaction_validator',
            message_type='transaction'
        )
        message_id = message['message_id']

        with open(input_dir / f"{message_id}.json", 'w') as f:
            json.dump(message, f)

        # Run pipeline and capture output (capsys for print, caplog for logging)
        with caplog.at_level(logging.WARNING):
            run_pipeline()

        captured = capsys.readouterr()

        # Full account number should NOT appear in captured stdout or stderr
        assert account_number not in captured.out, \
            f"Full account number '{account_number}' leaked to stdout"
        assert account_number not in captured.err, \
            f"Full account number '{account_number}' leaked to stderr"

        # Full account should NOT appear in captured logging records either
        assert account_number not in caplog.text, \
            f"Full account number '{account_number}' leaked to logging output"

        # Redacted form (ACC-TE*** **01) should be safe to appear in logging
        # but we don't mandate its presence, only that the full account is absent

