"""Compliance Checker agent for the multi-agent pipeline.

Checks transaction compliance and writes approved transactions to results.
"""

import json
import logging
from datetime import datetime
from pathlib import Path

from pipeline.messaging import redact_account

logger = logging.getLogger(__name__)

# Block list of accounts
BLOCK_LIST = {"ACC-9999", "ACC-BLOCKED"}

# PII keywords (case-insensitive)
PII_KEYWORDS = {
    'password', 'ssn', 'credit card', 'pin', 'cvv',
    'security code', 'atm code'
}


def contains_pii(text: str) -> bool:
    """Check if text contains PII keywords (case-insensitive)."""
    if not text or not isinstance(text, str):
        return False
    text_lower = text.lower()
    return any(keyword in text_lower for keyword in PII_KEYWORDS)


def check_compliance(message: dict) -> dict:
    """
    Check transaction compliance and write approved transactions to results.

    Checks:
    1. Validation result (must be valid)
    2. Block list (source and destination accounts)
    3. PII in description
    4. Fraud risk level (HIGH or CRITICAL triggers hold)

    Args:
        message: Message envelope with transaction data

    Returns:
        The enriched message with data.compliance_status added
    """
    transaction = message.get('data', {})
    hold_reasons = []
    hold_flag = False

    # Check 1: Validation result
    validation_result = transaction.get('validation_result', {})
    if not validation_result.get('is_valid', False):
        hold_flag = True
        errors = validation_result.get('errors', [])
        hold_reasons.append(f"Validation failed: {', '.join(errors)}")

    # Check 2: Block list
    source_account = transaction.get('source_account', '')
    dest_account = transaction.get('destination_account', '')

    if source_account.upper() in BLOCK_LIST:
        hold_flag = True
        hold_reasons.append(f"Source account on block list: {source_account}")

    if dest_account.upper() in BLOCK_LIST:
        hold_flag = True
        hold_reasons.append(f"Destination account on block list: {dest_account}")

    # Check 3: PII in description
    description = transaction.get('description', '')
    if contains_pii(description):
        hold_flag = True
        hold_reasons.append("Description contains PII keywords")

    # Check 4: High-risk fraud score
    fraud_score = transaction.get('fraud_score', {})
    risk_level = fraud_score.get('risk_level', 'LOW')
    if risk_level in ['HIGH', 'CRITICAL']:
        hold_flag = True
        hold_reasons.append(f"High fraud risk detected: {risk_level}")

    # Determine compliance status
    compliance_status = 'HOLD_PENDING_REVIEW' if hold_flag else 'APPROVED'

    # Enrich message with compliance status
    compliance_result = {
        "status": compliance_status,
        "hold_flag": hold_flag,
        "hold_reasons": hold_reasons,
        "timestamp": datetime.utcnow().isoformat() + 'Z'
    }

    message['data']['compliance_status'] = compliance_result

    # Write enriched message to shared/results/
    try:
        message_id = message.get('message_id')
        if message_id:
            results_dir = Path('shared') / 'results'
            results_dir.mkdir(parents=True, exist_ok=True)

            results_file = results_dir / f"{message_id}.json"
            with open(results_file, 'w', encoding='utf-8') as f:
                json.dump(message, f, indent=2, ensure_ascii=False)
        else:
            logger.error("Cannot write results: message_id is missing")
    except (IOError, OSError) as e:
        logger.error(f"Failed to write results file: {e}")

    # Log HOLD_PENDING_REVIEW outcomes
    if compliance_status == 'HOLD_PENDING_REVIEW':
        source_redacted = redact_account(source_account)
        dest_redacted = redact_account(dest_account)
        transaction_id = transaction.get('transaction_id', 'UNKNOWN')

        logger.warning(
            f"Transaction {transaction_id} flagged for compliance review "
            f"({source_redacted} -> {dest_redacted}): {'; '.join(hold_reasons)}"
        )
    else:
        # Log approval (at info level for debugging)
        source_redacted = redact_account(source_account)
        dest_redacted = redact_account(dest_account)
        transaction_id = transaction.get('transaction_id', 'UNKNOWN')
        logger.info(
            f"Transaction {transaction_id} compliance check APPROVED "
            f"({source_redacted} -> {dest_redacted})"
        )

    return message
