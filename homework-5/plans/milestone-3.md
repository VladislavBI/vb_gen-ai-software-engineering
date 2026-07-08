# Milestone 3: Resource + `read` tool with word-count limiting — Session Plan

**Started:** 2026-06-30
**Super-plan reference:** ../PLAN.md milestone 3

## Approach

Implement the functional core of TASKS.md Task 4 in `server.py`, building on the M2 skeleton. All word-limiting logic lives in one shared, directly-testable helper so the Milestone-3 Verify (`m._read_words()`) and the Milestone-4 pytest (driving the `read` Tool over FastMCP's in-memory client) both exercise the same code path.

1. **`_read_words(word_count: int = 30) -> str`** — module-level helper. Reads `lorem-ipsum.md` via `Path(__file__).parent / "lorem-ipsum.md"` (per the M2 review advisory — CWD-independent, since `mcp.json` launches the server from the client's directory), splits on whitespace, returns the first `word_count` words rejoined by single spaces. Guard against `word_count` larger than the file (return all available) and non-positive values (return empty string) so the tool never throws on odd input.

2. **`read` Tool** — `@mcp.tool` decorated function literally named `read` (TASKS.md requires the tool be named `read`), signature `read(word_count: int = 30) -> str`, delegating to `_read_words`. This is the action Claude calls.

3. **Resource** — `@mcp.resource("lorem://words")` returning the default 30 words, plus a templated `@mcp.resource("lorem://words/{word_count}")` so a caller can request an explicit count via URI. Both delegate to `_read_words`. The static URI satisfies the "default 30" requirement; the template satisfies "accepts a word_count parameter."

Alternative considered: a single resource template `lorem://{word_count}` with no static default. Rejected because URI templates require the variable to be present, leaving no clean way to express the default-30 case the task calls for; the static + template pair is the idiomatic FastMCP way to expose both.

FastMCP 3.4.2 API confirmed by introspection: `@mcp.tool` (bare or called) and `@mcp.resource(uri)` are correct; `fastmcp.Client` is available for the M4 in-memory tests.

## Touch list

- **homework-5/custom-mcp-server/server.py**
  - Add `from pathlib import Path`; module-level `_LOREM_PATH = Path(__file__).parent / "lorem-ipsum.md"`.
  - Add `_read_words(word_count: int = 30) -> str` helper with bounds guards.
  - Add `@mcp.tool`-decorated `read(word_count: int = 30) -> str` delegating to `_read_words`.
  - Add `@mcp.resource("lorem://words")` → 30 words, and `@mcp.resource("lorem://words/{word_count}")` → `int(word_count)` words.
  - Keep the existing `mcp = FastMCP(...)` instance and the `__main__` / `mcp.run()` runner unchanged.

## Review focus

- **Exact word count** — `_read_words(30)` returns exactly 30 whitespace-split tokens, `_read_words(7)` exactly 7; rejoining must not introduce empty tokens or double spaces that change `.split()` count.
- **CWD independence** — file read uses `Path(__file__).parent`, never a bare relative path (would `FileNotFoundError` in live MCP use).
- **Edge handling** — `word_count` ≤ 0 and `word_count` > file length must not raise; tool/resource should degrade gracefully (empty string / all words).
- **Tool name fidelity** — the tool is registered under the exact name `read` (the decorator must not rename it); the resource template variable name matches the function parameter.
- **No import-time I/O** — the file must be read inside the helper at call time, not at module import (preserves M2's import-safety so the importlib-based verifies still pass).
- **Type coercion** — the resource template `{word_count}` (string in the URI) is converted to `int` before slicing.

## Notes

- code-review-advisor verdict: **APPROVE_WITH_SUGGESTIONS**. Applied both in-scope nits to `server.py`: clarified the module docstring's template-URI wording, and removed the redundant `int(word_count)` cast in `lorem_words_n` (FastMCP coerces from the `int` annotation) — replaced with an explanatory comment.
- **Deferred MAJOR (out of M3 scope):** reviewer flagged `requirements.txt` pinning `fastmcp>=2.0`, which could resolve to an API-incompatible 2.x in a clean environment (the server uses the 3.x `@mcp.tool`/`fastmcp.Client` surface). `requirements.txt` is NOT in M3's Files (M3 owns only `server.py`); it IS in **Milestone 4's** Files. **Carried to M4:** tighten the pin to `fastmcp>=3.4,<4` when M4 edits `requirements.txt` to add pytest.
- **Carried to M4 (reviewer smoke-test questions):** (a) verify the static `lorem://words` and templated `lorem://words/{word_count}` resources route distinctly in a live client — confirm `lorem://words` does NOT match the template with an empty `word_count` (which would `ValueError` on `int("")`); (b) `_read_words` reads the file live on every call (no caching) — M4 tests must not assume cached results.
