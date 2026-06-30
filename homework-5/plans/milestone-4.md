# Milestone 4: Tests — `read` tool word-count coverage (pytest) — Session Plan

**Started:** 2026-06-30
**Super-plan reference:** ../PLAN.md milestone 4

## Approach

Satisfy the planning spec's mandatory Tests milestone, adapted to the Python/FastMCP stack. Rather than only re-checking the `_read_words` helper (already covered by Milestone 3's Verify), drive the actual `read` **Tool** and the **Resources** end-to-end through FastMCP's in-memory `Client`, so the test catches decorator/parameter/registration regressions the helper-level check cannot.

1. **In-memory transport** — `fastmcp.Client(mcp)` connects directly to the server object in-process (no subprocess, no port). Confirmed by introspection: `client.call_tool("read", {"word_count": N})` returns a `CallToolResult` whose `.data` is the tool's string return; `client.read_resource(uri)` returns a list whose `[0].text` is the content; `client.list_tools()` exposes the tool named `read`.
2. **Async-without-plugin** — the Client API is async. To avoid depending on the `pytest-asyncio` plugin and its config, each test is a plain sync function that runs a small coroutine via `asyncio.run(...)`. This keeps `requirements.txt` to just `pytest` for the test add.
3. **Server import** — load `server.py` via `importlib` (same pattern as the Verify) so the test file does not depend on package layout / `sys.path` tweaks.

Coverage:
- `read` tool default → exactly 30 words.
- `read` tool with `word_count=7` → exactly 7 words (parametrized with a couple of values).
- `read` tool edge: `word_count=0` → empty string (no raise).
- tool is registered under the exact name `read` (`list_tools`).
- Resource `lorem://words` → exactly 30 words AND does not raise (the M3 reviewer's routing smoke-test: static URI must not match the template with an empty `word_count`).
- Resource template `lorem://words/7` → exactly 7 words.

Also fold in the carried-forward dependency fix: tighten `requirements.txt` from `fastmcp>=2.0` to `fastmcp>=3.4,<4` (the server uses the 3.x `@mcp.tool` / `fastmcp.Client` surface; a 2.x resolution would break import). `requirements.txt` is in this milestone's Files, so this is in-scope here.

Alternative considered: adding `pytest-asyncio`. Rejected to keep the dependency surface and config minimal; `asyncio.run` per test is sufficient at this test volume.

## Touch list

- **homework-5/custom-mcp-server/requirements.txt**
  - Tighten `fastmcp>=2.0` → `fastmcp>=3.4,<4`.
  - Add `pytest>=8`.
- **homework-5/custom-mcp-server/test_server.py** (new)
  - Module-level helper to import `server.py` via importlib and a small `_call`/`_read` coroutine-runner using `asyncio.run`.
  - `test_read_tool_default_returns_30`.
  - `test_read_tool_custom_word_count` (parametrized: 7, 1, 50).
  - `test_read_tool_zero_returns_empty`.
  - `test_read_tool_registered_name` (asserts a tool named exactly `read`).
  - `test_resource_default_returns_30` (also asserts `lorem://words` does not raise — routing smoke-test).
  - `test_resource_template_returns_n` (`lorem://words/7` → 7 words).

## Review focus

- **Exact-count assertions** — every count assertion uses `len(text.split()) == N`, not substring/length checks, so trailing-newline or join artifacts can't produce a false pass.
- **Result extraction** — tool result read from `CallToolResult.data` (or `.content[0].text` fallback) correctly; resource read from `read_resource(...)[0].text`; no brittle indexing that would `IndexError` on an empty result.
- **No zero-test pass** — the file must contain real collected tests; pytest exit 5 (no tests) must fail the milestone (the Verify already treats non-zero exit as failure).
- **Async hygiene** — each `asyncio.run` wraps a complete `async with Client(...)` block; no leaked event loop or client across tests; tests are independent.
- **Routing smoke-test present** — there is an explicit assertion that `lorem://words` (static) returns 30 words and does not raise `ValueError` from matching the template with an empty segment.
- **requirements pin** — `fastmcp>=3.4,<4` and `pytest` both present and correctly spelled.

## Notes

- code-review-advisor verdict: **APPROVE_WITH_SUGGESTIONS**. Applied both minor suggestions: added an `assert result.content` guard before the `.content[0]` fallback in `_call_read`, and an `assert contents` guard before `contents[0].text` in `_read_resource` — so a malformed/empty result surfaces a named assertion failure instead of a bare `IndexError`.
- Reviewer praised the `is None` guard in `_call_read` (a truthiness check would have treated `word_count=0` as "absent" and let the server default to 30, silently passing the zero-edge test).
- Confirmed source is 69 words, so the parametrized `50` case is a true exact-50, not clamped.
- Carried-forward items from M3 resolved here: `requirements.txt` tightened to `fastmcp>=3.4,<4` (+ `pytest>=8`); the static-vs-template resource routing concern is covered by `test_resource_default_returns_30` (asserts `lorem://words` returns 30 and does not raise).
