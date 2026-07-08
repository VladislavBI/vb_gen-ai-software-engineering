<#
.SYNOPSIS
  Demo of the custom FastMCP server's `read` behavior without a full MCP client.

.DESCRIPTION
  Loads custom-mcp-server/server.py and prints the output of the `read` tool
  (via the shared _read_words helper it delegates to) for several word counts,
  so a grader can reproduce the word-limited lorem-ipsum output directly.

.EXAMPLE
  powershell -ExecutionPolicy Bypass -File homework-5\demo\sample-read-requests.ps1
#>

$ErrorActionPreference = 'Stop'

$serverPath = Join-Path $PSScriptRoot '..\custom-mcp-server\server.py'
$serverPath = (Resolve-Path $serverPath).Path

Write-Host "Custom MCP server: $serverPath"
Write-Host "Demonstrating the 'read' tool for word counts 30, 10, 7:`n"

$python = @"
import importlib.util as u
spec = u.spec_from_file_location('server', r'''$serverPath''')
m = u.module_from_spec(spec)
spec.loader.exec_module(m)
for n in (30, 10, 7):
    text = m._read_words(n)
    print(f'--- read(word_count={n}) -> {len(text.split())} words ---')
    print(text)
    print()
"@

$python | python -
if ($LASTEXITCODE -ne 0) { throw "demo failed (exit $LASTEXITCODE)" }
