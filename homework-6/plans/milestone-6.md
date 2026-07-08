# Milestone 6: Agent 3 — /run-pipeline, /validate-transactions skills + coverage-gate push hook — Session Plan

**Started:** 2026-07-08
**Super-plan reference:** ../PLAN.md milestone 6

## Approach

Milestone 6 delivers three automation artifacts required by Task 3:

1. **Two slash commands** (`run-pipeline.md` and `validate-transactions.md`): Markdown files that serve as Claude Code instructions. Each file describes a multi-step workflow (clearing directories, running agents, displaying results) in a format that Claude Code can parse and execute as a skill. These are similar in structure to the `write-spec.md` pattern.

2. **Coverage gate script** (`check_coverage.py`): A lightweight Python wrapper that accepts a `--min <n>` threshold argument and invokes pytest with `--cov-fail-under=<n>`, propagating its exit code (0 for pass, non-zero for fail). This reuses pytest's built-in coverage enforcement rather than reimplementing percentage parsing from scratch, reducing complexity and bugs.

3. **Settings.json hook**: A JSON configuration file wiring a pre-git-push hook that calls `check_coverage.py --min 80` to gate commits on coverage compliance. The hook pattern follows Claude Code conventions for blocking tools on external command exit codes.

The session plan stays within the Files list (the four deliverables above) and makes no changes outside the scope. Milestone 5's test suite is already in place, so the coverage gate only needs to reuse pytest's own validation mechanism.

## Touch list

- **`homework-6/.claude/commands/run-pipeline.md`**: Create skill with steps to clear `shared/` directories, run `python integrator.py`, parse and display results summary from `shared/results/` (transaction count, status distribution, rejected reasons).
- **`homework-6/.claude/commands/validate-transactions.md`**: Create skill with steps to load `sample-transactions.json`, invoke validator in dry-run mode (if available) or simulate validation, display table of transaction counts and validity breakdown.
- **`homework-6/scripts/check_coverage.py`**: Create Python script that parses command-line `--min <threshold>` argument, invokes `python -m pytest --cov=. --cov-fail-under=<threshold> -q`, captures and propagates exit code (test framework's exit code becomes script exit code).
- **`homework-6/.claude/settings.json`**: Create JSON file with hook entry (PreToolUse or PreCommand hook) that matches git-push operations and runs `python scripts/check_coverage.py --min 80`, blocking the push if exit code is non-zero.

## Review focus

- **Skill command clarity**: Are the steps in run-pipeline.md and validate-transactions.md precise and executable by Claude Code without ambiguity? Do they follow the pattern from write-spec.md?
- **Error handling**: Does check_coverage.py gracefully handle missing pytest, missing tests directory, or other runtime errors, or does it crash with unclear exit codes?
- **Coverage script robustness**: Does the script correctly invoke pytest and propagate its exit code (e.g., exit 0 when coverage ≥ threshold, exit 1 when coverage < threshold)?
- **Hook syntax and schema**: Does the settings.json hook entry use valid Claude Code hook syntax (PreToolUse, matching criteria, command specification)? Note: the actual hook trigger cannot be tested without a real git push, so this is best-effort validation; Verify only checks that settings.json contains a reference to check_coverage.
- **Dry-run mode simulation**: For validate-transactions.md, if the validator doesn't have a --dry-run flag, does the skill gracefully load the validator module and call its validation function directly?

## Notes

**Review pass 1 & 2 sign-off (2026-07-08):** code-review-advisor confirmed all four files present and functional:
- `run-pipeline.md` and `validate-transactions.md` correctly structure multi-step workflows as Claude Code skills.
- `check_coverage.py` script correctly accepts `--min <threshold>`, invokes pytest with coverage, and propagates exit codes (0 for pass, non-zero for fail).
- `settings.json` hook entry is wired to call the coverage gate script on git push, following Claude Code hook conventions.
- Note: Discussion on whether to use PreToolUse vs UserPromptSubmit hook scope — reviewed and PreToolUse determined appropriate for this gate's early-check semantics (check before tool execution rather than after prompt entry).
- All blocking findings resolved. Verify passed: skill files exist, settings.json references check_coverage, gate correctly blocks at min=999 (exit non-zero) and passes at min=80 (exit zero). Coverage: 84.77% (target 80%).
