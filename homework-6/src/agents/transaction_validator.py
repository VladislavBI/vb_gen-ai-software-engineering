"""Transaction Validator agent for the multi-agent pipeline.

Validates transaction fields and enriches the message with validation results.
"""

import logging
from datetime import datetime
from decimal import Decimal, InvalidOperation

from pipeline.messaging import redact_account

# Configure logging with PII redaction
logger = logging.getLogger(__name__)

# ISO 4217 currency codes (common set)
VALID_CURRENCIES = {
    'USD', 'EUR', 'GBP', 'JPY', 'CHF', 'CAD', 'AUD', 'NZD', 'CNY', 'INR',
    'MXN', 'SGD', 'HKD', 'NOK', 'SEK', 'DKK', 'PLN', 'CZK', 'HUF', 'RON',
    'BGN', 'HRK', 'RUB', 'TRY', 'ZAR', 'BRL', 'ARS'
}

# Required transaction fields
REQUIRED_FIELDS = {
    'transaction_id', 'timestamp', 'source_account', 'destination_account',
    'amount', 'currency', 'transaction_type', 'description'
}


def validate_transaction(message: dict) -> dict:
    """
    Validate a transaction message and enrich it with validation results.

    Args:
        message: Message envelope with transaction data in message['data']

    Returns:
        The enriched message with data.validation_result added
    """
    errors = []
    transaction = message.get('data', {})

    # Check required fields
    for field in REQUIRED_FIELDS:
        if field not in transaction:
            errors.append(f"Missing required field: {field}")

    # Validate amount (Decimal for precision)
    try:
        amount = Decimal(str(transaction.get('amount', 0)))
        transaction_type = transaction.get('transaction_type', '').lower()

        # Refunds can be negative, other transactions must be positive
        if transaction_type == 'refund':
            if amount >= 0:
                errors.append("Refund transaction must have negative amount")
        else:
            if amount <= 0:
                errors.append("Transaction amount must be positive (non-refund)")
    except (InvalidOperation, ValueError, TypeError):
        errors.append(f"Invalid amount format: {transaction.get('amount')}")

    # Validate currency (ISO 4217)
    currency = transaction.get('currency', '').upper()
    if not currency or currency not in VALID_CURRENCIES:
        errors.append(f"Invalid currency code: {currency}")

    # Validate timestamp (ISO 8601)
    timestamp_str = transaction.get('timestamp', '')
    try:
        # Parse ISO 8601 format (with or without Z suffix)
        if timestamp_str.endswith('Z'):
            timestamp_str = timestamp_str[:-1] + '+00:00'
        datetime.fromisoformat(timestamp_str.replace('Z', '+00:00'))
    except (ValueError, AttributeError):
        errors.append(f"Invalid ISO 8601 timestamp: {transaction.get('timestamp')}")

    is_valid = len(errors) == 0

    # Enrich message with validation result
    validation_result = {
        "is_valid": is_valid,
        "errors": errors,
        "timestamp": datetime.utcnow().isoformat() + 'Z'
    }

    message['data']['validation_result'] = validation_result

    # Log the outcome with PII redaction
    source_account = redact_account(transaction.get('source_account', 'UNKNOWN'))
    dest_account = redact_account(transaction.get('destination_account', 'UNKNOWN'))
    transaction_id = transaction.get('transaction_id', 'UNKNOWN')

    if is_valid:
        logger.info(
            f"Transaction {transaction_id} validation PASSED "
            f"({source_account} -> {dest_account})"
        )
    else:
        logger.warning(
            f"Transaction {transaction_id} validation FAILED "
            f"({source_account} -> {dest_account}): {', '.join(errors)}"
        )

    return message
