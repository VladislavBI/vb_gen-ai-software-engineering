# PowerShell Conventions (Windows / PS 5.1)

The development environment is **Windows 11 with PowerShell 5.1 as the primary shell**. Apply these rules whenever this repo, a `HOWTORUN.md`, or a demo script needs commands.

## Authoring rules (user-facing scripts)

- **Use PowerShell syntax exclusively** in scripts intended for the user (`run.ps1`, examples in `HOWTORUN.md`, `demo/sample-requests.ps1`). Do not author `run.sh` / curl-only examples unless `TASKS.md` explicitly requires them — in that case, add a PowerShell counterpart alongside.
- **HTTP testing**: prefer `Invoke-RestMethod` / `Invoke-WebRequest` over `curl`. `curl` in PS 5.1 is an alias for `Invoke-WebRequest` and behaves differently than Linux `curl` — do not assume `-X`, `-d` flags work.
- **File and process inspection** in user-facing docs: `Get-ChildItem`, `Select-String`, `Get-Content`, `Get-Process` rather than `ls`, `grep`, `cat`, `ps`.
- **No `&&` / `||` chain operators** in PS 5.1. Use `; if ($?) { ... }` or split into separate lines.
- **File encoding**: default `Out-File` / `Set-Content` is UTF-16 LE with BOM. When writing config files (`.env`, `appsettings.json`) pass `-Encoding utf8`.

## Verifying your own work (Claude)

When verifying via the Bash/PowerShell tools, prefer the `PowerShell` tool over `Bash` for any command the user might re-run. The `Bash` tool here runs against an MSYS-style bash and produces different paths/behaviour than the user sees in their shell.

## Project-wide verification snippets

```powershell
# Repo state
git status
git log --oneline -10

# Find a file
Get-ChildItem -Recurse -Filter "TASKS.md"

# Search content
Select-String -Path "homework-*\src\**\*.cs" -Pattern "TODO"

# HTTP smoke test against a running homework API
Invoke-RestMethod -Uri http://localhost:5080/health -Method Get
$body = @{ amount = 12.5; currency = "USD" } | ConvertTo-Json
Invoke-RestMethod -Uri http://localhost:5080/transactions -Method Post `
    -ContentType 'application/json' -Body $body
```

## Canonical Verify pattern for endpoint milestones

Every milestone whose `Verify` block exercises a live endpoint follows this shape — build, start the API in the background, poll, assert with `Invoke-RestMethod`, and **always stop the process in `finally`** so a failed assertion does not leave a port-bound dotnet process behind:

```powershell
Push-Location homework-N\src
dotnet build HomeworkN.sln
if ($LASTEXITCODE -ne 0) { Pop-Location; throw "build failed" }
$proc = Start-Process -FilePath dotnet `
    -ArgumentList 'run --project HomeworkN.Api --no-build --urls http://localhost:5080' `
    -PassThru -WindowStyle Hidden
try {
    Start-Sleep -Seconds 4   # give Kestrel time to bind; raise if cold-start is slow
    # ... Invoke-RestMethod / Invoke-WebRequest assertions go here ...
} finally {
    Stop-Process -Id $proc.Id -Force -ErrorAction SilentlyContinue
    Pop-Location
}
```

### Port discipline

- **`5080`** is the default API port for every homework's Verify block. Do not use `5000` (the SDK default) — it collides with other ASP.NET Core dev runs and with macOS AirPlay on student dual-boot setups.
- **`5081`** is reserved for the *parallel* API run (e.g. an NBomber load-test scenario that boots its own API while a Verify-time API is still on 5080). Pick a higher unique port for any third concurrent process.
- Catching a non-2xx response: `Invoke-RestMethod` throws on non-2xx — wrap the call in `try { ... } catch { if ($_.Exception.Response.StatusCode.value__ -ne <expected>) { throw } }`. `Invoke-WebRequest -UseBasicParsing` behaves the same way; use it when you want headers/raw body without throwing on JSON parse failure.

## Stack-specific commands

PowerShell is just the shell — for .NET build / test / scaffold commands see:
- `../Architecture/dotnet-stack.md` — versions, NuGet packages
- `../Architecture/project-architecture.md` — scaffold for the API/BLL/DAL layout
- `../Architecture/testing-strategy.md` — `dotnet test` filters and coverage flags
