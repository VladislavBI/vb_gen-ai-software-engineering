"""Custom FastMCP server for Homework 5.

Exposes the lorem-ipsum source through:
  * a Resource (a URI Claude can read) at `lorem://words` (default 30 words)
    and `lorem://words/{word_count}` (an explicit count from the URI), and
  * a Tool named `read` (an action Claude can call) that returns the first
    `word_count` words of the source.

Both delegate to the shared `_read_words` helper so the word-limiting behavior
is exercised by one code path (directly in Milestone 3's verify and through
FastMCP's in-memory client in Milestone 4's tests).
"""

from pathlib import Path

from fastmcp import FastMCP

mcp = FastMCP("custom-lorem-server")

# Resolve the source relative to THIS file, not the process CWD: the mcp.json
# `custom` entry launches the server as `python custom-mcp-server/server.py`,
# so the working directory at launch is the client's, not this folder.
_LOREM_PATH = Path(__file__).parent / "lorem-ipsum.md"


def _read_words(word_count: int = 30) -> str:
    """Return the first `word_count` whitespace-delimited words of the source.

    Degrades gracefully on out-of-range input so the tool/resource never raise:
    a non-positive count yields an empty string; a count larger than the file
    yields every available word.
    """
    if word_count <= 0:
        return ""
    words = _LOREM_PATH.read_text(encoding="utf-8").split()
    return " ".join(words[:word_count])


@mcp.tool
def read(word_count: int = 30) -> str:
    """Read `word_count` words (default 30) from the lorem-ipsum source."""
    return _read_words(word_count)


@mcp.resource("lorem://words")
def lorem_words() -> str:
    """The lorem-ipsum source, limited to the default 30 words."""
    return _read_words()


@mcp.resource("lorem://words/{word_count}")
def lorem_words_n(word_count: int) -> str:
    """The lorem-ipsum source, limited to `word_count` words from the URI.

    FastMCP coerces the URI path segment to `int` from the annotation above.
    """
    return _read_words(word_count)


if __name__ == "__main__":
    # Default transport is stdio, which is what the mcp.json `custom` entry uses.
    mcp.run()
