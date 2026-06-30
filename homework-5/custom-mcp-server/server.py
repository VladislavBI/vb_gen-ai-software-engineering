"""Custom FastMCP server for Homework 5.

Skeleton milestone (Milestone 2): constructs the importable FastMCP server
instance and provides a stdio runner so the `python custom-mcp-server/server.py`
entry in mcp.json launches a real server. The lorem-ipsum Resource and the
`read` Tool are added in Milestone 3.
"""

from fastmcp import FastMCP

mcp = FastMCP("custom-lorem-server")


if __name__ == "__main__":
    # Default transport is stdio, which is what the mcp.json `custom` entry uses.
    mcp.run()
