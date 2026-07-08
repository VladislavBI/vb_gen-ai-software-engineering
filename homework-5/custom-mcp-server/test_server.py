"""Tests for the custom FastMCP server's `read` tool and lorem resources.

These drive the actual Tool and Resources through FastMCP's in-memory Client
(in-process, no subprocess or port), so they catch decorator/registration/
parameter regressions that a direct `_read_words` helper check cannot.

Each test is a plain sync function that runs a small coroutine via
`asyncio.run`, avoiding a dependency on the pytest-asyncio plugin.
"""

import asyncio
import importlib.util
from pathlib import Path

import pytest
from fastmcp import Client

_SERVER_PATH = Path(__file__).parent / "server.py"


def _load_server():
    spec = importlib.util.spec_from_file_location("server", _SERVER_PATH)
    module = importlib.util.module_from_spec(spec)
    spec.loader.exec_module(module)
    return module


server = _load_server()


def _call_read(word_count=None):
    """Call the `read` tool over the in-memory client; return its string data."""
    async def _run():
        async with Client(server.mcp) as client:
            args = {} if word_count is None else {"word_count": word_count}
            result = await client.call_tool("read", args)
            # FastMCP returns the tool's value in .data; fall back to text content.
            if result.data is not None:
                return result.data
            assert result.content, f"empty tool result for args {args}"
            return result.content[0].text

    return asyncio.run(_run())


def _read_resource(uri):
    async def _run():
        async with Client(server.mcp) as client:
            contents = await client.read_resource(uri)
            assert contents, f"read_resource({uri!r}) returned empty list"
            return contents[0].text

    return asyncio.run(_run())


def _list_tool_names():
    async def _run():
        async with Client(server.mcp) as client:
            return [t.name for t in await client.list_tools()]

    return asyncio.run(_run())


def test_read_tool_default_returns_30():
    text = _call_read()
    assert len(text.split()) == 30


@pytest.mark.parametrize("count", [7, 1, 50])
def test_read_tool_custom_word_count(count):
    text = _call_read(count)
    assert len(text.split()) == count


def test_read_tool_zero_returns_empty():
    text = _call_read(0)
    assert text == ""


def test_read_tool_registered_name():
    assert "read" in _list_tool_names()


def test_resource_default_returns_30():
    # Also a routing smoke-test: the static `lorem://words` must resolve here
    # and not match the `lorem://words/{word_count}` template with an empty
    # segment (which would raise ValueError on int("")).
    text = _read_resource("lorem://words")
    assert len(text.split()) == 30


def test_resource_template_returns_n():
    text = _read_resource("lorem://words/7")
    assert len(text.split()) == 7
