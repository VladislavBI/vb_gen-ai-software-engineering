"""Fraud Detector agent for the multi-agent pipeline.

Scores fraud risk on all transactions regardless of validation status.
"""

import logging
from datetime import datetime
from decimal import Decimal, InvalidOperation

from pipeline.messaging import redact_account

logger = logging.getLogger(__name__)

# Currency conversion rates to USD
CURRENCY_RATES = {
    'USD': Decimal('1.00'),
    'EUR': Decimal('0.92'),
    'GBP': Decimal('1.27'),
}

# High-risk amount threshold in USD
HIGH_RISK_AMOUNT_USD = Decimal('10000.00')

# Off-hours boundaries (< 06:00 or > 22:00)
NORMAL_HOURS_START = 6  # 06:00
NORMAL_HOURS_END = 22   # 22:00


def detect_fraud(message: dict) -> dict:
    """
    Detect fraud risk on a transaction message and enrich with fraud score.

    Scores all transactions regardless of validation status. Combines multiple
    risk factors to produce a final risk_level (LOW, MEDIUM, HIGH, CRITICAL).

    Args:
        message: Message envelope with transaction data

    Returns:
        The enriched message with data.fraud_score added
    """
    transaction = message.get('data', {})
    risk_score = Decimal('0')

    # Track individual factors as booleans during evaluation
    high_amount = False
    off_hours = False
    cross_border = False
    wire_transfer = False

    # Risk factor 1: High-value transactions (>= $10K USD equivalent) - 40%
    try:
        amount = Decimal(str(transaction.get('amount', 0)))
        currency = transaction.get('currency', 'USD').upper()

        # Use absolute value for refunds
        if transaction.get('transaction_type', '').lower() == 'refund':
            amount = abs(amount)

        # Convert to USD equivalent
        rate = CURRENCY_RATES.get(currency, Decimal('1.00'))
        amount_usd = amount * rate

        if amount_usd >= HIGH_RISK_AMOUNT_USD:
            high_amount = True
            risk_score += Decimal('40')
    except (InvalidOperation, ValueError, TypeError, AttributeError):
        pass

    # Risk factor 2: Off-hours transactions (< 06:00 or > 22:00:00) - 20%
    try:
        timestamp_str = transaction.get('timestamp', '')
        if timestamp_str:
            # Remove Z suffix if present
            if timestamp_str.endswith('Z'):
                timestamp_str = timestamp_str[:-1]
            dt = datetime.fromisoformat(timestamp_str)
            hour = dt.hour
            minute = dt.minute
            second = dt.second

            # Off-hours: strictly before 06:00 or after 22:00:00 (22:00:01+)
            if hour < NORMAL_HOURS_START or hour > NORMAL_HOURS_END or (hour == NORMAL_HOURS_END and (minute > 0 or second > 0)):
                off_hours = True
                risk_score += Decimal('20')
    except (ValueError, AttributeError):
        pass

    # Risk factor 3: Cross-border transactions (country != 'US') - 25%
    try:
        country = transaction.get('metadata', {}).get('country', 'US')
        if country and country.upper() != 'US':
            cross_border = True
            risk_score += Decimal('25')
    except (AttributeError, TypeError):
        pass

    # Risk factor 4: Wire transfers - 15%
    try:
        transaction_type = transaction.get('transaction_type', '').lower()
        if 'wire' in transaction_type:
            wire_transfer = True
            risk_score += Decimal('15')
    except (AttributeError, TypeError):
        pass

    # Determine risk level based on cumulative score
    if risk_score < Decimal('20'):
        risk_level = 'LOW'
    elif risk_score < Decimal('50'):
        risk_level = 'MEDIUM'
    elif risk_score < Decimal('80'):
        risk_level = 'HIGH'
    else:
        risk_level = 'CRITICAL'

    # Enrich message with fraud score (score as int per spec)
    fraud_score = {
        "score": int(risk_score),
        "risk_level": risk_level,
        "factors": {
            "high_amount": high_amount,
            "off_hours": off_hours,
            "cross_border": cross_border,
            "wire_transfer": wire_transfer,
        },
        "timestamp": datetime.utcnow().isoformat() + 'Z'
    }

    message['data']['fraud_score'] = fraud_score

    # Log HIGH and CRITICAL detections
    if risk_level in ['HIGH', 'CRITICAL']:
        source_account = redact_account(transaction.get('source_account', 'UNKNOWN'))
        dest_account = redact_account(transaction.get('destination_account', 'UNKNOWN'))
        transaction_id = transaction.get('transaction_id', 'UNKNOWN')

        logger.warning(
            f"Transaction {transaction_id} fraud detection: {risk_level} "
            f"(score={int(risk_score)}, {source_account} -> {dest_account})"
        )

    return message
