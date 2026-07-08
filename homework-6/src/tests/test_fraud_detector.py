"""Unit tests for the Fraud Detector agent."""

import pytest
from decimal import Decimal
from agents.fraud_detector import detect_fraud


class TestFraudDetectorLowRisk:
    """Test low-risk transaction detection."""

    def test_low_risk_transaction(self):
        """Test that low-value, normal transaction scores as LOW."""
        message = {
            'message_id': 'msg-101',
            'data': {
                'transaction_id': 'tx-101',
                'timestamp': '2026-01-15T10:30:00Z',  # Normal hours
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        fraud_score = result['data']['fraud_score']
        assert fraud_score['risk_level'] == 'LOW'
        assert fraud_score['score'] < 20
        assert fraud_score['factors']['high_amount'] is False
        assert fraud_score['factors']['off_hours'] is False
        assert fraud_score['factors']['cross_border'] is False
        assert fraud_score['factors']['wire_transfer'] is False


class TestFraudDetectorHighAmount:
    """Test high-value transaction detection."""

    def test_high_value_transaction_usd(self):
        """Test that transactions >= $10K USD are marked high_amount."""
        message = {
            'message_id': 'msg-102',
            'data': {
                'transaction_id': 'tx-102',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '10000.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Large transfer',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        fraud_score = result['data']['fraud_score']
        assert fraud_score['factors']['high_amount'] is True
        assert fraud_score['score'] >= 40

    def test_high_value_above_threshold(self):
        """Test that amounts strictly above $10K are flagged."""
        message = {
            'message_id': 'msg-103',
            'data': {
                'transaction_id': 'tx-103',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '10000.01',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'High transfer',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['high_amount'] is True

    def test_high_value_just_below_threshold(self):
        """Test that amounts below $10K USD are not flagged as high_amount."""
        message = {
            'message_id': 'msg-104',
            'data': {
                'transaction_id': 'tx-104',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '9999.99',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Just under threshold',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['high_amount'] is False

    def test_high_value_with_currency_conversion_eur(self):
        """Test that EUR amounts are correctly converted to USD equivalence."""
        # 10000 EUR at 0.92 rate = 9200 USD (below threshold)
        message = {
            'message_id': 'msg-105',
            'data': {
                'transaction_id': 'tx-105',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '10000.00',
                'currency': 'EUR',
                'transaction_type': 'transfer',
                'description': 'EUR transfer',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        # 10000 EUR * 0.92 = 9200 USD < 10000 USD threshold
        assert result['data']['fraud_score']['factors']['high_amount'] is False

    def test_high_value_with_currency_conversion_gbp(self):
        """Test that GBP amounts are correctly converted to USD equivalence."""
        # 10000 GBP at 1.27 rate = 12700 USD (above threshold)
        message = {
            'message_id': 'msg-106',
            'data': {
                'transaction_id': 'tx-106',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '10000.00',
                'currency': 'GBP',
                'transaction_type': 'transfer',
                'description': 'GBP transfer',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        # 10000 GBP * 1.27 = 12700 USD >= 10000 USD threshold
        assert result['data']['fraud_score']['factors']['high_amount'] is True


class TestFraudDetectorOffHours:
    """Test off-hours boundary detection."""

    def test_off_hours_boundary_22_00_00(self):
        """Test that 22:00:00 exactly is normal hours (not off-hours)."""
        message = {
            'message_id': 'msg-107',
            'data': {
                'transaction_id': 'tx-107',
                'timestamp': '2026-01-15T22:00:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['off_hours'] is False

    def test_off_hours_boundary_06_00_00(self):
        """Test that 06:00:00 exactly is normal hours (not off-hours)."""
        message = {
            'message_id': 'msg-108',
            'data': {
                'transaction_id': 'tx-108',
                'timestamp': '2026-01-15T06:00:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['off_hours'] is False

    def test_off_hours_after_22_00(self):
        """Test that 22:00:01 is off-hours."""
        message = {
            'message_id': 'msg-109',
            'data': {
                'transaction_id': 'tx-109',
                'timestamp': '2026-01-15T22:00:01Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['off_hours'] is True
        assert result['data']['fraud_score']['score'] >= 20

    def test_off_hours_before_06_00(self):
        """Test that 05:59:59 is off-hours."""
        message = {
            'message_id': 'msg-110',
            'data': {
                'transaction_id': 'tx-110',
                'timestamp': '2026-01-15T05:59:59Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['off_hours'] is True
        assert result['data']['fraud_score']['score'] >= 20

    def test_off_hours_midnight(self):
        """Test that 00:00:00 is off-hours."""
        message = {
            'message_id': 'msg-111',
            'data': {
                'transaction_id': 'tx-111',
                'timestamp': '2026-01-15T00:00:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['off_hours'] is True

    def test_off_hours_23_59_59(self):
        """Test that 23:59:59 is off-hours."""
        message = {
            'message_id': 'msg-112',
            'data': {
                'transaction_id': 'tx-112',
                'timestamp': '2026-01-15T23:59:59Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['off_hours'] is True


class TestFraudDetectorCrossBorder:
    """Test cross-border transaction detection."""

    def test_cross_border_non_us_country(self):
        """Test that non-US country is marked as cross_border."""
        message = {
            'message_id': 'msg-113',
            'data': {
                'transaction_id': 'tx-113',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'CA'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['cross_border'] is True
        assert result['data']['fraud_score']['score'] >= 25

    def test_not_cross_border_us_country(self):
        """Test that US country is not marked as cross_border."""
        message = {
            'message_id': 'msg-114',
            'data': {
                'transaction_id': 'tx-114',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['cross_border'] is False


class TestFraudDetectorWireTransfer:
    """Test wire transfer detection."""

    def test_wire_transfer_type(self):
        """Test that wire transfer transactions are flagged."""
        message = {
            'message_id': 'msg-115',
            'data': {
                'transaction_id': 'tx-115',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'wire_transfer',
                'description': 'Wire',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['wire_transfer'] is True
        assert result['data']['fraud_score']['score'] >= 15

    def test_wire_in_transaction_type_case_insensitive(self):
        """Test that 'wire' in transaction type is detected case-insensitively."""
        message = {
            'message_id': 'msg-116',
            'data': {
                'transaction_id': 'tx-116',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'WIRE',
                'description': 'Wire',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['wire_transfer'] is True

    def test_non_wire_transfer(self):
        """Test that regular transfers are not flagged as wire transfers."""
        message = {
            'message_id': 'msg-117',
            'data': {
                'transaction_id': 'tx-117',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['wire_transfer'] is False


class TestFraudDetectorFactorBleeding:
    """Test that factors don't bleed into each other."""

    def test_high_amount_alone_no_off_hours(self):
        """Test that high_amount=True doesn't set off_hours."""
        message = {
            'message_id': 'msg-118',
            'data': {
                'transaction_id': 'tx-118',
                'timestamp': '2026-01-15T10:30:00Z',  # Normal hours
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '10000.00',
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        factors = result['data']['fraud_score']['factors']
        assert factors['high_amount'] is True
        assert factors['off_hours'] is False
        assert factors['cross_border'] is False
        assert factors['wire_transfer'] is False

    def test_off_hours_alone_no_high_amount(self):
        """Test that off_hours=True doesn't set high_amount."""
        message = {
            'message_id': 'msg-119',
            'data': {
                'transaction_id': 'tx-119',
                'timestamp': '2026-01-15T23:00:00Z',  # Off-hours
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '100.00',  # Low amount
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        factors = result['data']['fraud_score']['factors']
        assert factors['off_hours'] is True
        assert factors['high_amount'] is False
        assert factors['cross_border'] is False
        assert factors['wire_transfer'] is False


class TestFraudDetectorInvalidTransaction:
    """Test fraud detection on invalid transactions."""

    def test_scores_invalid_transaction(self):
        """Test that fraud is scored even on invalid transactions."""
        message = {
            'message_id': 'msg-120',
            'data': {
                'transaction_id': 'tx-120',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '10000.00',  # High amount
                'currency': 'USD',
                'transaction_type': 'transfer',
                'description': 'Payment',
                'metadata': {'country': 'US'},
                'validation_result': {'is_valid': False, 'errors': ['Invalid']}
            }
        }
        result = detect_fraud(message)
        # Should still score (score should include high_amount factor)
        fraud_score = result['data']['fraud_score']
        assert fraud_score['factors']['high_amount'] is True
        assert fraud_score['score'] >= 40


class TestFraudDetectorRefundHighValue:
    """Test fraud detection on refunds."""

    def test_refund_high_value(self):
        """Test that high-value refunds use absolute value for high_amount check."""
        message = {
            'message_id': 'msg-121',
            'data': {
                'transaction_id': 'tx-121',
                'timestamp': '2026-01-15T10:30:00Z',
                'source_account': 'ACC-1001',
                'destination_account': 'ACC-2001',
                'amount': '-10000.00',  # Negative (refund), absolute value >= $10K
                'currency': 'USD',
                'transaction_type': 'refund',
                'description': 'Refund',
                'metadata': {'country': 'US'}
            }
        }
        result = detect_fraud(message)
        assert result['data']['fraud_score']['factors']['high_amount'] is True
        assert result['data']['fraud_score']['score'] >= 40
