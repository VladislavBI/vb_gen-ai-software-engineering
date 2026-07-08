# Milestone 6: Finalize documentation (README, HOWTORUN, demo, Resources-vs-Tools) — Session Plan

**Started:** 2026-06-30
**Super-plan reference:** ../PLAN.md milestone 6

## Approach

Produce the four documentation/demo deliverables that make the homework reproducible and submission-ready, matching the house style of `homework-4/README.md` (Author / GitHub / Date header, table-driven sections). No template placeholders may remain (Verify rejects `[Your Name]`, `YOUR_USERNAME`, `[Date]`). Author is **Vlad Bairak**, GitHub **VladislavBI**, date **2026-06-30**, repo `VladislavBI/vb_gen-ai-software-engineering`.

1. **README.md** — overview of the four configured MCP servers + the custom FastMCP server, the deliverables map, an AI-tools-usage section (how the `/homework` workflow + code-review-advisor produced this), and an architecture note. Links to HOWTORUN, the resources-vs-tools explanation, and the screenshots.
2. **HOWTORUN.md** — concrete PowerShell-first runbook: install deps (`pip install -r custom-mcp-server/requirements.txt`, must mention `fastmcp`), set the env vars the config references (`GITHUB_PAT`, `HOMEWORK_5_DIR`, `JIRA_URL`/`JIRA_USERNAME`/`JIRA_API_TOKEN`), run the custom server (`python custom-mcp-server/server.py`), connect the MCP config (`mcp.json`) to the client, and use/test the `read` tool (plus the pytest command). Documents the Python-version assumption (`python` = 3.x on this Windows stack).
3. **docs/resources-vs-tools.md** — the TASKS.md-required explanation: **Resources** are URIs Claude can read from (files/APIs) — here `lorem://words`; **Tools** are actions Claude can call to perform operations — here `read`. Must contain both a "Resource" and a "Tool" section (Verify greps for both).
4. **demo/sample-read-requests.ps1** — a runnable PowerShell demo that invokes the custom server's behavior (via a small inline Python one-liner against `_read_words`, or by driving the FastMCP in-memory client) for word counts 30/10/7 and prints the results, so a grader can reproduce the `read` output without a full MCP client.

## Touch list

- **homework-5/README.md** (new) — header, Overview, Configured MCP servers table, Custom server section, Deliverables table, AI-tools-usage, links.
- **homework-5/HOWTORUN.md** (new) — Prerequisites, Install, Configure env vars, Run custom server, Connect mcp.json, Use/test `read`, Run tests.
- **homework-5/docs/resources-vs-tools.md** (new) — Resource section + Tool section + how this server embodies each.
- **homework-5/demo/sample-read-requests.ps1** (new) — runnable demo invoking `read` behavior for several word counts.

## Review focus

- **No placeholders** — README contains no `[Your Name]` / `YOUR_USERNAME` / `[Date]` (Verify fails on these); author name present.
- **HOWTORUN completeness** — covers install (mentions `fastmcp`), run, connect config, and use/test the tool; every env var referenced by `mcp.json` is documented; PowerShell snippets are PS 5.1-valid (no `&&`, no Linux `curl`).
- **Resources-vs-Tools accuracy** — distinguishes URIs-Claude-reads (Resource) from actions-Claude-calls (Tool); both sections present and correct for this server.
- **Demo runnability** — `sample-read-requests.ps1` actually runs on this stack (correct relative paths, valid PS), and its output matches the server's real word-limited results.
- **Internal consistency** — server name, URIs (`lorem://words`, `lorem://words/{word_count}`), tool name (`read`), and file paths match what M1–M4 actually built.

## Notes

- code-review-advisor verdict: **no blockers**, one MINOR on the `HOMEWORK_5_DIR` example in HOWTORUN — added an "adjust to your machine" comment so the example absolute path isn't mistaken for a fixed value. Demo script confirmed runnable (prints exact 30/10/7-word outputs matching `lorem-ipsum.md`).
- Sequenced per user request to draft docs before the M5 screenshots are captured. M6 depends on M5 in the DAG, so although the M6 work (and its Verify, which does not require the screenshots) is completed first, the `[x]` tick and the `hw-5-6` commit are held until M5 is `[x]` — commits land in M5→M6 order to keep the dependency history honest.
