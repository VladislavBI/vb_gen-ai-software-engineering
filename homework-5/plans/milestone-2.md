# Milestone 2: Custom FastMCP server skeleton + dependencies — Session Plan

**Started:** 2026-06-30
**Super-plan reference:** ../PLAN.md milestone 2

## Approach

Stand up the minimal, importable foundation for the custom MCP server so the environment is proven reproducible before any resource/tool behavior is layered on (that is Milestone 3). Three files:

1. **`requirements.txt`** — declare `fastmcp` as the single explicit dependency (TASKS.md Task 4 requires `fastmcp` be present in project dependencies). Pin loosely (`fastmcp>=2.0`) so the in-memory client API used by the Milestone 4 tests is available. `pytest` is intentionally NOT added here — Milestone 4 owns that addition (its Files list includes `requirements.txt`).
2. **`lorem-ipsum.md`** — the source text the resource will read in Milestone 3. Must contain comfortably more than 40 whitespace-delimited words so the default 30-word and custom-count tests have headroom. A single classic lorem-ipsum paragraph (~60+ words) satisfies this.
3. **`server.py`** — the FastMCP server module. For this milestone it only needs to (a) import `FastMCP`, (b) construct a module-level `mcp = FastMCP(...)` instance, and (c) expose a `__main__` runner so `python server.py` starts the stdio server (matching the `custom` entry in `mcp.json`). The word-limiting helper, the Resource, and the `read` Tool are deliberately deferred to Milestone 3 to keep this milestone to a verifiable skeleton.

Alternative considered: implementing the full resource/tool here and collapsing Milestones 2+3. Rejected because the super-plan separates "environment reproducible / module imports" from "behavior correct," and each has its own independently-runnable Verify. Keeping them split honors the plan's DAG and sizing heuristics.

## Touch list

- **homework-5/custom-mcp-server/requirements.txt** — add `fastmcp>=2.0`.
- **homework-5/custom-mcp-server/lorem-ipsum.md** — add one lorem-ipsum paragraph of ≥40 words (target ~60) of plain prose, no markdown headings that would skew word counting unexpectedly.
- **homework-5/custom-mcp-server/server.py** — `from fastmcp import FastMCP`; module-level `mcp = FastMCP("custom-lorem-server")`; `if __name__ == "__main__": mcp.run()`. No resource/tool yet.

## Review focus

- **Import safety**: `server.py` must import with zero side effects beyond constructing `mcp` — the Verify execs the module via importlib, so any top-level error (bad import, file I/O at import time) fails the milestone. No reading of `lorem-ipsum.md` at import time.
- **`mcp` object presence**: the module-level attribute must be exactly `mcp` (the Verify asserts `hasattr(m,'mcp')`).
- **Dependency hygiene**: `requirements.txt` lists `fastmcp` (spelled correctly, lowercase) and does not prematurely pull in `pytest` (Milestone 4's responsibility) or unrelated packages.
- **Word-count headroom**: `lorem-ipsum.md` word count is comfortably ≥40 (Verify floor) and ideally ≥30 with margin so Milestone 3's exact-30 slice is a true subset.
- **Runner correctness**: the `__main__` block uses `mcp.run()` (stdio default) so the `python custom-mcp-server/server.py` command in `mcp.json` actually launches a server, not just imports.

## Notes

- code-review-advisor verdict: **APPROVE**, no blockers. All six review-focus criteria passed.
- `lorem-ipsum.md` is intentionally punctuation-free (69 words) so Python `.split()` and PowerShell `.Split()` count identically — avoids "amet," vs "amet" ambiguity for the exact-count assertions in M3/M4.
- **Forward advisory carried to Milestone 3:** the Resource/`_read_words` must read the lorem file via `Path(__file__).parent / "lorem-ipsum.md"`, NOT a bare relative path. `mcp.json` launches the server as `python custom-mcp-server/server.py`, so the process CWD is the client's directory (likely repo root), and a relative open would raise `FileNotFoundError` at runtime (passing import-time Verify but failing live use).
- Kept `mcp.run()` with no explicit transport — FastMCP 2.x defaults to stdio, matching the `mcp.json` custom entry.
