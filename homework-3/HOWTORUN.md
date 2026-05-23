# HOWTORUN — Regenerating the Spec Package with Spec Kit

This homework is **specification-only**: there is no application to run. "Running it" means regenerating
or extending the specification artifacts with **GitHub Spec Kit**. All commands are PowerShell 5.1.

## Prerequisites

- `uv` / `uvx` (Python toolchain), Python 3.11+, Git.
- The Spec Kit CLI:
  ```powershell
  uv tool install specify-cli --from git+https://github.com/github/spec-kit.git
  specify --version
  ```
  > On Windows, if the CLI crashes with a `charmap` Unicode error, force UTF-8 for the call:
  > ```powershell
  > $env:PYTHONUTF8 = "1"; $env:PYTHONIOENCODING = "utf-8"
  > ```

## 1. Initialize / repair the Spec Kit scaffold (already done in this repo)

Run from inside `homework-3/`. `--no-git` avoids nesting a repo inside the course repo; `--force` merges
into the non-empty directory.

```powershell
Push-Location homework-3
specify init --here --integration claude --script ps --no-git --force
Pop-Location
```

This installs the `speckit-*` skills under `homework-3/.claude/skills/` and the templates/scripts under
`homework-3/.specify/`.

## 2. Run the Spec-Driven workflow

Start Claude Code with the working directory at `homework-3/` so the `/speckit-*` skills resolve, then run
the phases in order (the text after each command is the prompt):

```text
/speckit-constitution   FinTech principles: audit-first, never log PAN/CVV, idempotent writes,
                        money as minor units, least privilege, verifiable quality.
/speckit-specify        Virtual card lifecycle: create, freeze/unfreeze, set limits, view transactions;
                        actors end-user + ops/compliance; regulated environment.
/speckit-clarify        (resolve any ambiguities; informed FinTech defaults)
/speckit-plan           Non-functional targets, data-handling guardrails, beginning/ending context.
/speckit-tasks          Decompose into many small traceable tasks with acceptance criteria.
/speckit-checklist      Generate a security/compliance quality checklist.
/speckit-analyze        Cross-artifact consistency & traceability report.
```

> Do **not** run `/speckit-implement` — there is no code in this homework.

## 3. Verify the outputs (PowerShell)

```powershell
Push-Location homework-3
# Required deliverables exist:
'specification.md','agents.md','README.md','.claude/rules.md' | ForEach-Object {
    if (-not (Test-Path $_)) { throw "missing $_" }
}
# Spec Kit artifacts exist:
'specs/001-virtual-card/spec.md','specs/001-virtual-card/plan.md','specs/001-virtual-card/tasks.md',
'.specify/memory/constitution.md' | ForEach-Object {
    if (-not (Test-Path $_)) { throw "missing $_" }
}
# specification.md has the required layers:
$need = 'High-Level Objective','Mid-Level Objectives','Implementation Notes','Context','Low-Level Tasks',
        'Edge Case','Verification','Performance'
foreach ($h in $need) {
    if (-not (Select-String -Path specification.md -Pattern $h -Quiet)) { throw "missing layer: $h" }
}
"OK — all deliverables present and layered."
Pop-Location
```
