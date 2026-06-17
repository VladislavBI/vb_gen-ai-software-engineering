#requires -Version 5.1
# Shared prerequisite gate for the homework-4 bug pipeline. Bound to TWO hooks:
#   - PreToolUse (matcher: Skill)  -> guards `Skill` tool calls to the pipeline
#   - UserPromptSubmit             -> guards user-typed `/pipeline` slash commands
# Enforces that the 4 agent files and a non-empty src/ exist before the pipeline
# runs. Allows every other skill / prompt through untouched.
#
# Contract: Claude Code pipes the hook event JSON on stdin (shape differs per
# hook: PreToolUse has tool_input.skill; UserPromptSubmit has prompt).
# Exit 2 => block and surface stderr back to Claude; exit 0 => allow.

$ErrorActionPreference = 'Stop'

# --- read the hook event from stdin -----------------------------------------
$raw = [Console]::In.ReadToEnd()
if ([string]::IsNullOrWhiteSpace($raw)) { exit 0 }

try { $event = $raw | ConvertFrom-Json } catch { exit 0 }

# --- only enforce for the pipeline skill ------------------------------------
# Two invocation paths must be gated:
#   1. Skill tool call         -> PreToolUse event,    tool_input.skill == 'pipeline'
#   2. User-typed /pipeline     -> UserPromptSubmit event, prompt text carries it
# Slash commands are prompt expansions and never call the Skill tool, so the
# PreToolUse:Skill matcher alone leaves path 2 unguarded. Detect both here.
$invokesPipeline = $false

if ($event.tool_input -and $event.tool_input.skill -eq 'pipeline') {
    $invokesPipeline = $true
}

if (-not $invokesPipeline -and $event.prompt) {
    # Match the raw slash command (`/pipeline`, optional args) and, as a
    # fallback, a distinctive marker from the expanded SKILL.md body.
    if ($event.prompt -match '(?im)^\s*/pipeline\b' -or
        $event.prompt -match 'Bug Pipeline Orchestrator') {
        $invokesPipeline = $true
    }
}

if (-not $invokesPipeline) { exit 0 }

# --- resolve the project root -----------------------------------------------
$root = $event.cwd
if ([string]::IsNullOrWhiteSpace($root)) { $root = $env:CLAUDE_PROJECT_DIR }
if ([string]::IsNullOrWhiteSpace($root)) { $root = (Get-Location).Path }

$missing = New-Object System.Collections.Generic.List[string]

# --- check the 4 required agent files ---------------------------------------
$agents = 'research-verifier','bug-fixer','security-verifier','unit-test-generator'
foreach ($a in $agents) {
    $p = Join-Path $root (".claude/agents/$a.md")
    if (-not (Test-Path -LiteralPath $p -PathType Leaf)) {
        $missing.Add("agent: .claude/agents/$a.md")
    }
}

# --- check the sample app (Task 5): src/ exists and is non-empty ------------
$src = Join-Path $root 'src'
$srcOk = $false
if (Test-Path -LiteralPath $src -PathType Container) {
    if (Get-ChildItem -LiteralPath $src -Recurse -File -ErrorAction SilentlyContinue | Select-Object -First 1) {
        $srcOk = $true
    }
}
if (-not $srcOk) { $missing.Add("sample app: src/ is missing or empty (complete Task 5 first)") }

# --- verdict ----------------------------------------------------------------
if ($missing.Count -gt 0) {
    $msg  = "Cannot run /pipeline - prerequisites are missing:`n"
    $msg += ($missing | ForEach-Object { "  - $_" }) -join "`n"
    $msg += "`nCreate the missing items, then re-invoke /pipeline."
    [Console]::Error.WriteLine($msg)
    exit 2
}

exit 0
