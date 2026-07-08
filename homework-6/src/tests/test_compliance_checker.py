"""Unit tests for the Compliance Checker agent."""

import pytest
import json
import tempfile
from pathlib import Path
from agents.compliance_checker import check_compliance, contains_pii


class TestComplianceCheckerApproved:
    """Test APPROVED status."""

    def test_approved_valid_no_issues(self):
        """Test that valid transaction with no issues gets APPROVED."""
        message = {
            'message_id': 'msg-201',
            'data': {
                'transaction_id': 'tx-201',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 5},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'APPROVED'
        assert compliance['hold_flag'] is False
        assert compliance['hold_reasons'] == []


class TestComplianceCheckerHoldValidation:
    """Test HOLD_PENDING_REVIEW due to validation failure."""

    def test_hold_validation_failed(self):
        """Test that validation failure triggers HOLD_PENDING_REVIEW."""
        message = {
            'message_id': 'msg-202',
            'data': {
                'transaction_id': 'tx-202',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': 'invalid',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'validation_result': {
                    'is_valid': False,
                    'errors': ['Invalid amount format: invalid']
                },
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'
        assert compliance['hold_flag'] is True
        assert any('Validation failed' in reason for reason in compliance['hold_reasons'])


class TestComplianceCheckerHoldBlockList:
    """Test HOLD_PENDING_REVIEW due to block list."""

    def test_hold_source_account_blocked(self):
        """Test that source account on block list triggers HOLD_PENDING_REVIEW."""
        message = {
            'message_id': 'msg-203',
            'data': {
                'transaction_id': 'tx-203',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-9999',  # Block listed
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'
        assert compliance['hold_flag'] is True
        assert any('block list' in reason.lower() for reason in compliance['hold_reasons'])

    def test_hold_destination_account_blocked(self):
        """Test that destination account on block list triggers HOLD_PENDING_REVIEW."""
        message = {
            'message_id': 'msg-204',
            'data': {
                'transaction_id': 'tx-204',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-9999',  # Block listed
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'
        assert compliance['hold_flag'] is True
        assert any('block list' in reason.lower() for reason in compliance['hold_reasons'])

    def test_block_list_case_insensitivity_lowercase(self):
        """Test that block list matching is case-insensitive (lowercase match)."""
        message = {
            'message_id': 'msg-205',
            'data': {
                'transaction_id': 'tx-205',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'acc-9999',  # Lowercase
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'
        assert compliance['hold_flag'] is True

    def test_block_list_case_insensitivity_mixed(self):
        """Test that block list matching is case-insensitive (mixed case match)."""
        message = {
            'message_id': 'msg-206',
            'data': {
                'transaction_id': 'tx-206',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'Acc-9999',  # Mixed case
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'


class TestComplianceCheckerHoldPII:
    """Test HOLD_PENDING_REVIEW due to PII in description."""

    def test_hold_pii_password(self):
        """Test that PII keyword 'password' triggers HOLD_PENDING_REVIEW."""
        message = {
            'message_id': 'msg-207',
            'data': {
                'transaction_id': 'tx-207',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment with password reset',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'
        assert compliance['hold_flag'] is True
        assert any('PII' in reason for reason in compliance['hold_reasons'])

    def test_hold_pii_ssn(self):
        """Test that PII keyword 'ssn' triggers HOLD_PENDING_REVIEW."""
        message = {
            'message_id': 'msg-208',
            'data': {
                'transaction_id': 'tx-208',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment for SSN verification',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'

    def test_hold_pii_credit_card(self):
        """Test that PII keyword 'credit card' triggers HOLD_PENDING_REVIEW."""
        message = {
            'message_id': 'msg-209',
            'data': {
                'transaction_id': 'tx-209',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Credit Card payment',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'

    def test_pii_case_insensitivity(self):
        """Test that PII keyword detection is case-insensitive."""
        # Test PASSWORD in caps
        message = {
            'message_id': 'msg-210',
            'data': {
                'transaction_id': 'tx-210',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment with PASSWORD reset',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'
        assert compliance['hold_flag'] is True

    def test_pii_case_insensitivity_mixed(self):
        """Test that PII keyword detection is case-insensitive (mixed case)."""
        message = {
            'message_id': 'msg-211',
            'data': {
                'transaction_id': 'tx-211',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment with PaSsWoRd reset',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'LOW', 'score': 0},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'


class TestComplianceCheckerHoldFraud:
    """Test HOLD_PENDING_REVIEW due to high fraud risk."""

    def test_hold_fraud_high_risk(self):
        """Test that HIGH fraud risk triggers HOLD_PENDING_REVIEW."""
        message = {
            'message_id': 'msg-212',
            'data': {
                'transaction_id': 'tx-212',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'HIGH', 'score': 75},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'
        assert compliance['hold_flag'] is True
        assert any('fraud' in reason.lower() for reason in compliance['hold_reasons'])

    def test_hold_fraud_critical_risk(self):
        """Test that CRITICAL fraud risk triggers HOLD_PENDING_REVIEW."""
        message = {
            'message_id': 'msg-213',
            'data': {
                'transaction_id': 'tx-213',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'CRITICAL', 'score': 100},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'HOLD_PENDING_REVIEW'
        assert compliance['hold_flag'] is True

    def test_approved_medium_fraud_risk(self):
        """Test that MEDIUM fraud risk does not trigger hold."""
        message = {
            'message_id': 'msg-214',
            'data': {
                'transaction_id': 'tx-214',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'validation_result': {'is_valid': True, 'errors': []},
                'fraud_score': {'risk_level': 'MEDIUM', 'score': 35},
                'metadata': {'country': 'US'}
            }
        }
        result = check_compliance(message)
        compliance = result['data']['compliance_status']
        assert compliance['status'] == 'APPROVED'
        assert compliance['hold_flag'] is False


class TestComplianceCheckerWriteResults:
    """Test writing results to shared/results/."""

    def test_write_results_to_file(self, tmp_path):
        """Test that results are written to shared/results/."""
        # Change to temp directory
        original_cwd = Path.cwd()
        try:
            # Create temp shared directory
            temp_dir = tmp_path / "shared"
            temp_dir.mkdir()
            (temp_dir / "results").mkdir()

            # Change working directory
            import os
            os.chdir(tmp_path)

            message = {
                'message_id': 'msg-215',
                'data': {
                    'transaction_id': 'tx-215',
                    'timestamp': '2026-01-15T10:30:00Z',
                    'source_account': 'ACC-1001',
                    'destination_account': 'ACC-2001',
                    'amount': '100.00',
                    'currency': 'USD',
                    'transaction_type': 'transfer',
                    'description': 'Payment',
                    'validation_result': {'is_valid': True, 'errors': []},
                    'fraud_score': {'risk_level': 'LOW', 'score': 0},
                    'metadata': {'country': 'US'}
                }
            }
            result = check_compliance(message)

            # Check that file was written
            results_file = temp_dir / "results" / "msg-215.json"
            assert results_file.exists()

            # Verify file contains valid JSON
            with open(results_file, 'r') as f:
                data = json.load(f)
            assert data['message_id'] == 'msg-215'
            assert 'compliance_status' in data['data']
        finally:
            os.chdir(original_cwd)

    def test_result_json_valid_structure(self, tmp_path):
        """Test that result JSON has all four layers."""
        original_cwd = Path.cwd()
        try:
            temp_dir = tmp_path / "shared"
            temp_dir.mkdir()
            (temp_dir / "results").mkdir()

            import os
            os.chdir(tmp_path)

            message = {
                'message_id': 'msg-216',
                'data': {
                    'transaction_id': 'tx-216',
                    'timestamp': '2026-01-15T10:30:00Z',
                    'source_account': 'ACC-1001',
                    'destination_account': 'ACC-2001',
                    'amount': '100.00',
                    'currency': 'USD',
                    'transaction_type': 'transfer',
                    'description': 'Payment',
                    'validation_result': {'is_valid': True, 'errors': []},
                    'fraud_score': {'risk_level': 'LOW', 'score': 0},
                    'metadata': {'country': 'US'}
                }
            }
            result = check_compliance(message)

            results_file = temp_dir / "results" / "msg-216.json"
            with open(results_file, 'r') as f:
                data = json.load(f)

            # Verify all four layers
            assert 'validation_result' in data['data']
            assert 'fraud_score' in data['data']
            assert 'compliance_status' in data['data']
            assert data['data']['validation_result']['is_valid'] is True
        finally:
            os.chdir(original_cwd)


class TestContainsPII:
    """Test the contains_pii helper function."""

    def test_contains_pii_password(self):
        """Test PII detection for 'password' keyword."""
        assert contains_pii("User password is secret") is True
        assert contains_pii("PASSWORD") is True
        assert contains_pii("PaSsWoRd") is True

    def test_contains_pii_ssn(self):
        """Test PII detection for 'ssn' keyword."""
        assert contains_pii("SSN verification required") is True

    def test_contains_pii_credit_card(self):
        """Test PII detection for 'credit card' keyword."""
        assert contains_pii("Credit Card payment") is True
        assert contains_pii("CREDIT CARD") is True

    def test_contains_pii_pin(self):
        """Test PII detection for 'pin' keyword."""
        assert contains_pii("Enter your PIN") is True

    def test_contains_pii_cvv(self):
        """Test PII detection for 'cvv' keyword."""
        assert contains_pii("CVV required") is True

    def test_no_pii(self):
        """Test that normal text returns False."""
        assert contains_pii("Regular payment for services") is False
        assert contains_pii("Invoice 12345") is False

    def test_contains_pii_empty_string(self):
        """Test that empty string returns False."""
        assert contains_pii("") is False

    def test_contains_pii_none(self):
        """Test that None returns False."""
        assert contains_pii(None) is False

    def test_contains_pii_non_string(self):
        """Test that non-string returns False."""
        assert contains_pii(12345) is False
        assert contains_pii([]) is False
