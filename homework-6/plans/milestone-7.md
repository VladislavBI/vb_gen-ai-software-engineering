# Milestone 7: Agent 4 — Finalize documentation, demo, and screenshots — Session Plan

**Started:** 2026-07-08
**Super-plan reference:** ../PLAN.md milestone 7

## Approach

This milestone consolidates the completed pipeline system (milestones 1–6) into user-facing documentation and runnable demo materials. The approach is sequential and documentation-heavy, covering four deliverables:

1. **README.md**: Author Vlad Bairak; 1–2 paragraph overview of the pipeline's purpose and capabilities; per-agent responsibility bullets; a text-based ASCII diagram showing the real pipeline architecture (integrator → shared/input → validator → shared/processing → fraud_detector → shared/output → compliance_checker → shared/results); and a tech-stack table itemizing Python 3.12, FastMCP, pytest/pytest-cov, context7 MCP, and PowerShell slash commands.

2. **HOWTORUN.md**: Numbered, step-by-step instructions for reproducing the system end-to-end, assuming a fresh environment with Python 3.12 and pip installed. Steps cover: dependency installation, pipeline setup, running the pipeline, and verifying results.

3. **demo/run-demo.ps1**: A PowerShell script (PS 5.1 safe, no `??` or `//` operators) that demonstrates the pipeline in action. It should invoke the real `integrator.py` commands (setup and run) and display a summary of the output using the same output parsing logic as the `/run-pipeline` slash command.

4. **demo/sample-requests.md**: Example transaction JSON payloads (drawn from `sample-transactions.json`) and example MCP tool calls (using `get_transaction_status` and `list_pipeline_results` from the custom FastMCP server) with expected outputs.

5. **docs/screenshots/ placeholder**: Create the directory structure for screenshots (the actual PNG files must be captured manually by the user, so this milestone will end in "awaiting" status rather than "verified").

## Touch list

- **README.md**: Create from scratch with all required sections (author, overview, per-agent bullets, ASCII diagram, tech-stack table).
- **HOWTORUN.md**: Create from scratch with numbered steps covering setup, installation, and verification.
- **demo/run-demo.ps1**: Create from scratch as a runnable PowerShell script that orchestrates the pipeline and displays results.
- **demo/sample-requests.md**: Create from scratch with 2–3 example transactions and 2 example MCP tool calls with expected outputs.
- **docs/screenshots/**: Create the directory; add a `.gitkeep` or a README explaining the 5 required screenshots (pipeline-run.png, test-coverage.png, skill-run-pipeline.png, hook-trigger.png, mcp-interaction.png).

## Review focus

- README completeness: author name, ASCII diagram with required symbols (─, ->, |), tech-stack table is readable and accurate.
- HOWTORUN accuracy: steps are numbered, sequential, and match the actual commands (integrator.py --setup, pip install, pytest, /run-pipeline).
- demo/run-demo.ps1 executability: PowerShell 5.1 compatible (no `??`, `//`, or ternary operators), properly paths the src directory, error handling on failure.
- demo/sample-requests.md clarity: JSON payloads are valid and representative, MCP tool examples show realistic request/response pairs.
- Screenshot placeholder documentation: clear instructions for what each screenshot captures and where to save it (exact path).

## Notes

**Status:** Milestone will end in "awaiting" (not "verified") because the 5 screenshot PNG files cannot be fabricated by the agent — they require manual user interaction (running the pipeline, slash commands, and MCP server, then taking screenshots and saving them to the exact paths).

**Implementation plan:**
1. Write README.md with real pipeline architecture diagram and accurate tech stack.
2. Write HOWTORUN.md with step-by-step instructions mirroring the `/run-pipeline` skill and the pytest coverage workflow.
3. Write demo/run-demo.ps1 as a fully functional, tested PowerShell script.
4. Write demo/sample-requests.md with realistic examples.
5. Create docs/screenshots/ directory and add a detailed README explaining the 5 screenshot requirements.
6. Run the non-screenshot parts of Verify to confirm README content, HOWTORUN existence, and demo/ non-empty.
7. Return status "awaiting" with a detailed, step-by-step list of instructions for the user to capture each of the 5 screenshots.
