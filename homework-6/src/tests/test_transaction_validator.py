"""Unit tests for the Transaction Validator agent."""

import pytest
from decimal import Decimal
from agents.transaction_validator import validate_transaction


class TestTransactionValidatorBasic:
    """Test basic transaction validation."""

    def test_valid_transaction(self):
        """Test that a valid transaction passes validation."""
        message = {
            'message_id': 'msg-001',
            'data': {
                'transaction_id': 'tx-001',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '150.50',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment for services'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is True
        assert result['data']['validation_result']['errors'] == []

    def test_positive_amount_non_refund(self):
        """Test that non-refund transactions require positive amounts."""
        message = {
            'message_id': 'msg-002',
            'data': {
                'transaction_id': 'tx-002',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '500.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is True

    def test_refund_with_negative_amount(self):
        """Test that refunds can have negative amounts and pass validation."""
        message = {
            'message_id': 'msg-003',
            'data': {
                'transaction_id': 'tx-003',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '-100.50',
                'currency': 'USD',
                'transaction_type': 'refund',
                'description': 'Refund issued'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is True
        assert result['data']['validation_result']['errors'] == []

    def test_non_refund_with_negative_amount(self):
        """Test that non-refund transactions with negative amounts fail."""
        message = {
            'message_id': 'msg-004',
            'data': {
                'transaction_id': 'tx-004',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '-50.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        assert any('positive' in error.lower() for error in result['data']['validation_result']['errors'])

    def test_zero_amount(self):
        """Test that zero amounts fail validation."""
        message = {
            'message_id': 'msg-005',
            'data': {
                'transaction_id': 'tx-005',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '0',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Zero amount'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        assert any('positive' in error.lower() for error in result['data']['validation_result']['errors'])


class TestTransactionValidatorMissingFields:
    """Test validation of required fields."""

    def test_missing_transaction_id(self):
        """Test that missing transaction_id is detected."""
        message = {
            'message_id': 'msg-006',
            'data': {
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        assert any('transaction_id' in error for error in result['data']['validation_result']['errors'])

    def test_missing_multiple_fields(self):
        """Test that multiple missing fields are all reported."""
        message = {
            'message_id': 'msg-007',
            'data': {
                'transaction_id': 'tx-007',
                'amount': '100.00'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        errors = result['data']['validation_result']['errors']
        # Should report multiple missing fields
        assert len(errors) >= 6

    def test_empty_data(self):
        """Test that empty transaction data reports all required fields as missing."""
        message = {
            'message_id': 'msg-008',
            'data': {}
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        errors = result['data']['validation_result']['errors']
        # All 8 required fields missing (8 errors) + amount validation (1) + currency (1) + timestamp (1) = 11
        assert len(errors) == 11


class TestTransactionValidatorCurrency:
    """Test currency validation."""

    def test_valid_currency(self):
        """Test that valid ISO 4217 currency codes pass."""
        for currency in ['USD', 'EUR', 'GBP', 'JPY']:
            message = {
                'message_id': 'msg-009',
                'data': {
                    'transaction_id': 'tx-009',
                    'timestamp': '2026-01-15T10:30:00Z',
                    'source_account': 'ACC-1001',
                    'destination_account': 'ACC-2001',
                    'amount': '100.00',
                    'currency': currency,
                    'transaction_type': 'transfer',
                    'description': 'Payment'
                }
            }
            result = validate_transaction(message)
            assert result['data']['validation_result']['is_valid'] is True

    def test_invalid_currency(self):
        """Test that invalid currency codes fail."""
        message = {
            'message_id': 'msg-010',
            'data': {
                'transaction_id': 'tx-010',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'XYZ',
                'transaction_type': 'transfer',
                'description': 'Payment'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        assert any('currency' in error.lower() for error in result['data']['validation_result']['errors'])

    def test_empty_currency(self):
        """Test that empty currency fails."""
        message = {
            'message_id': 'msg-011',
            'data': {
                'transaction_id': 'tx-011',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': '',
                'transaction_type': 'transfer',
                'description': 'Payment'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        assert any('currency' in error.lower() for error in result['data']['validation_result']['errors'])


class TestTransactionValidatorTimestamp:
    """Test timestamp validation."""

    def test_valid_iso8601_with_z_suffix(self):
        """Test that ISO 8601 timestamps with Z suffix are valid."""
        message = {
            'message_id': 'msg-012',
            'data': {
                'transaction_id': 'tx-012',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is True

    def test_valid_iso8601_without_z_suffix(self):
        """Test that ISO 8601 timestamps without Z suffix are valid."""
        message = {
            'message_id': 'msg-013',
            'data': {
                'transaction_id': 'tx-013',
                'timestamp': '2026-01-15T10:30:00',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is True

    def test_invalid_iso8601_timestamp(self):
        """Test that invalid ISO 8601 timestamps fail."""
        message = {
            'message_id': 'msg-014',
            'data': {
                'transaction_id': 'tx-014',
                'timestamp': '2026/01/15 10:30:00',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        assert any('timestamp' in error.lower() for error in result['data']['validation_result']['errors'])

    def test_empty_timestamp(self):
        """Test that empty timestamp fails."""
        message = {
            'message_id': 'msg-015',
            'data': {
                'transaction_id': 'tx-015',
                'timestamp': '',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        assert any('timestamp' in error.lower() for error in result['data']['validation_result']['errors'])


class TestTransactionValidatorAmount:
    """Test amount validation with Decimal precision."""

    def test_decimal_precision_no_rounding_errors(self):
        """Test that Decimal amounts don't have float rounding errors."""
        message = {
            'message_id': 'msg-016',
            'data': {
                'transaction_id': 'tx-016',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '0.1',  # Problematic with float
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Decimal test'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is True

    def test_large_amount(self):
        """Test that large amounts are handled correctly."""
        message = {
            'message_id': 'msg-017',
            'data': {
                'transaction_id': 'tx-017',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '999999999.99',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Large transfer'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is True

    def test_invalid_amount_format(self):
        """Test that invalid amount formats fail."""
        message = {
            'message_id': 'msg-018',
            'data': {
                'transaction_id': 'tx-018',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': 'not-a-number',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Bad amount'
            }
        }
        result = validate_transaction(message)
        assert result['data']['validation_result']['is_valid'] is False
        assert any('amount' in error.lower() for error in result['data']['validation_result']['errors'])
