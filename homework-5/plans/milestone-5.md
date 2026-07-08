# Milestone 5: MCP interaction screenshots (four servers) — Session Plan

**Started:** 2026-06-30
**Super-plan reference:** ../PLAN.md milestone 5

## Approach

This milestone's deliverables are four PNG screenshots — binary evidence that each configured MCP server performed a real interaction. Unlike the code milestones, the "edit" step here is **manual capture by the student**: the screenshots must show the student's own MCP client (Claude Code / Claude Desktop / Copilot) invoking each server with the student's live credentials. The agent cannot fabricate or synthesize these images. The agent's role is to (a) specify precisely what each screenshot must demonstrate so it satisfies the TASKS.md success criteria, and (b) run the Verify (existence + custom-server-registered) once the files are placed.

The four files (exact names pinned by TASKS.md Expected Structure), all under `homework-5/docs/screenshots/`:

1. **github-mcp-result.png** — GitHub MCP performing one interaction against the student's repo. Suggested prompt: *"List the 5 most recent pull requests in VladislavBI/<repo>"* or *"Summarize the last 5 commits on main."* Screenshot must show the MCP tool call AND its result.
2. **filesystem-mcp-result.png** — Filesystem MCP listing/reading within the configured directory (`${HOMEWORK_5_DIR}`). Suggested prompt: *"List the files in the homework-5 directory"* or *"Read custom-mcp-server/server.py and summarize it."* Screenshot must show the tool call and the returned listing/content.
3. **jira-or-notion-mcp-result.png** — Jira MCP answering the TASKS.md-mandated request: *"Give me the tickets of the last 5 bugs on a project."* Screenshot must show request + response, with only ticket numbers/keys visible (redact summaries/PII per TASKS.md).
4. **custom-mcp-read-tool-result.png** — the custom FastMCP server's `read` tool invoked from the client. Suggested prompt: *"Use the read tool to return 10 words"* (or default 30). Screenshot must show the `read` tool call and the word-limited lorem-ipsum output.

## Touch list

- **homework-5/docs/screenshots/github-mcp-result.png** — student captures; place file.
- **homework-5/docs/screenshots/filesystem-mcp-result.png** — student captures; place file.
- **homework-5/docs/screenshots/jira-or-notion-mcp-result.png** — student captures (Jira), redacted to keys only; place file.
- **homework-5/docs/screenshots/custom-mcp-read-tool-result.png** — student captures the `read` tool result; place file.

## Review focus

- **Coverage** — all four named files exist with the EXACT filenames the Verify and TASKS.md expect (including `jira-or-notion-mcp-result.png` even though Jira was chosen).
- **Evidence quality** — each image shows both the request/tool-call and the result, not just a config screen (a screenshot of settings is not an "interaction result").
- **PII hygiene** — the Jira screenshot exposes only ticket keys/numbers, not customer data or sensitive summaries (explicit TASKS.md Task 3 requirement).
- **Custom-tool authenticity** — the custom screenshot shows the `read` tool returning word-limited content, demonstrating the M3 behavior end-to-end through a real client.

## Notes

- **Status: awaiting human-in-the-loop capture.** The agent paused here because the four PNGs require the student's live MCP client + credentials and cannot be generated programmatically. Once the student places the four files under `homework-5/docs/screenshots/`, the agent runs the Verify (existence of all four + `custom` registered in mcp.json) and commits the milestone.
