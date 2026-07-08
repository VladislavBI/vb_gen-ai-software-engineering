#!/usr/bin/env python3
"""
Coverage gate script for homework-6 pytest suite.

Invokes pytest with coverage measurement and enforces a minimum coverage threshold.
Exits with code 0 if coverage >= threshold, code 1 otherwise.

Usage:
    python check_coverage.py --min <threshold>

Example:
    python check_coverage.py --min 80

Exit Codes:
    0 - Tests passed and coverage >= threshold
    1 - Tests failed or coverage < threshold
    2 - Script argument error
"""

import sys
import subprocess
import argparse


def parse_args():
    """Parse command-line arguments."""
    parser = argparse.ArgumentParser(
        description="Run pytest with coverage enforcement."
    )
    parser.add_argument(
        "--min",
        type=int,
        required=True,
        help="Minimum coverage threshold (0-100)"
    )

    args = parser.parse_args()

    if not 0 <= args.min <= 100:
        print(f"Error: threshold must be between 0 and 100, got {args.min}", file=sys.stderr)
        return None

    return args


def run_pytest(min_coverage):
    """
    Run pytest with coverage reporting.

    Args:
        min_coverage: Minimum coverage percentage (0-100)

    Returns:
        Exit code from pytest (0 = pass, non-zero = fail)
    """
    cmd = [
        sys.executable,
        "-m",
        "pytest",
        "--cov=.",
        f"--cov-fail-under={min_coverage}",
        "-q"
    ]

    print(f"Running: {' '.join(cmd)}", file=sys.stderr)
    result = subprocess.run(cmd)
    return result.returncode


def main():
    """Main entry point."""
    args = parse_args()

    if args is None:
        return 2

    exit_code = run_pytest(args.min)

    if exit_code == 0:
        print(f"Coverage gate passed: coverage >= {args.min}%", file=sys.stderr)
    else:
        print(f"Coverage gate failed: coverage < {args.min}% or tests failed", file=sys.stderr)

    return exit_code


if __name__ == "__main__":
    sys.exit(main())
