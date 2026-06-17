# How to Run — Homework 4

PowerShell 5.1 runbook. All commands run from the `homework-4/` directory.

---

## Prerequisites

- .NET SDK 10+ (`dotnet --version`)
- Claude Code CLI (`claude --version`)
- Agent files present in `.claude/agents/` (already checked in)

---

## 1. Build the sample app

```powershell
dotnet build src/SampleApp.slnx
```

Expected: build succeeds with no errors.

---

## 2. Run baseline tests (green on buggy code)

```powershell
dotnet test src/SampleApp.slnx
```

Expected: all 7 tests pass. The baseline tests deliberately avoid the buggy boundaries so the
pipeline starts from a green state.

---

## 3. Demonstrate the seeded bugs

**Bug 1 — off-by-one discount (qty=10 should get 10% off, gets 0%):**
```powershell
dotnet run --project src/SampleApp -- total 10 100
# Observe: Discount: 0%  (BUG — should be 10%)
```

**Bug 2 — tax on pre-discount subtotal (qty=20 overcharges):**
```powershell
dotnet run --project src/SampleApp -- total 20 100
# Observe: Total: 1960.00  (BUG — should be 1944.00)
```

**Security issue — hardcoded admin token:**
```powershell
dotnet run --project src/SampleApp -- auth super-secret-admin-token-123
# Observe: Access GRANTED  (SECURITY: token is hardcoded in source)
```

---

## 4. Run the full 4-agent pipeline

From within Claude Code (the CLI or IDE extension):

```
/pipeline 001
```

This single command runs all six stages in order:

| Stage | Actor | Input | Output |
|---|---|---|---|
| 1 | Bug Researcher (inline) | `context/bugs/001/bug-context.md`, `src/` | `research/codebase-research.md` |
| 2 | Research Verifier agent | `codebase-research.md` | `research/verified-research.md` |
| 3 | Bug Planner (inline) | `verified-research.md` | `implementation-plan.md` |
| 4 | Bug Fixer agent | `implementation-plan.md` | `fix-summary.md` + patched source |
| 5a | Security Verifier agent | `fix-summary.md` + changed files | `security-report.md` |
| 5b | Unit Test Generator agent | `fix-summary.md` + changed files | `test-report.md` + test files |

Stages 5a and 5b run in parallel.

---

## 5. Verify fixes are applied

After the pipeline completes:

```powershell
# Bug 1 fixed — qty=10 now gets 10% discount
dotnet run --project src/SampleApp -- total 10 100
# Expected: Discount: 10%, Total: 972.00  (100 * 10 * 0.9 * 1.08)

# Bug 2 fixed — tax now on discounted subtotal
dotnet run --project src/SampleApp -- total 20 100
# Expected: Total: 1944.00  (2000 * 0.9 * 1.08)

# Re-run all tests (including agent-generated ones)
dotnet test src/SampleApp.slnx
```

---

## 6. Review pipeline artifacts

```powershell
Get-ChildItem context/bugs/001 -Recurse -Filter *.md | Select-Object FullName
```

Key files:
- `context/bugs/001/research/verified-research.md` — research quality rating
- `context/bugs/001/fix-summary.md` — before/after diff per fix
- `context/bugs/001/security-report.md` — severity-rated security findings
- `context/bugs/001/test-report.md` — FIRST-compliant test results
