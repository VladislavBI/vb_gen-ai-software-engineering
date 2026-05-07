# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Repository Purpose

This is a **course homework repository** for "GenAI and Agentic AI for Software Engineering." It is a fork-and-submit template — each homework lives in its own `homework-N/` directory and is delivered through a pull request to the student's own fork (not upstream). See `README.md` for the canonical course rules and grading criteria.

## How This Repository Is Structured

- `homework-N/TASKS.md` — **assignment specification**. Treat as read-only authoritative requirements written by the instructor.
- `homework-N/README.md` — **the student's** write-up of what they built. This is what the user edits.
- `homework-N/HOWTORUN.md` — runbook the user produces.
- `homework-N/src/`, `homework-N/docs/screenshots/`, `homework-N/demo/` — code, evidence, demo scripts.
- `homework-N/CLAUDE.md` — per-homework guidance (added by the user as each HW starts). When present, it **overrides** this root file for that homework's scope.

This root `CLAUDE.md` therefore contains only *cross-cutting* rules. Per-homework requirements live in each homework's `TASKS.md` and `CLAUDE.md`.

### Reference docs (`.claude/docs/`)

Cross-cutting reference documents live under `.claude/docs/` in three categories: `Architecture/`, `Infrastructure/`, `Processes/`. Use the **decision table** below to pick which doc to load — do not load all of them speculatively. Each linked file is authoritative for its area; this `CLAUDE.md` keeps only the dispatcher and cross-cutting rules.

| If you are about to... | Read first | Notes |
|---|---|---|
| Scaffold a new homework backend | `Architecture/dotnet-stack.md` + `Architecture/project-architecture.md` | Versions, packages, API/BLL/DAL layout |
| Write or modify code in API/BLL/DAL | `Architecture/common-rules.md` | Type choices, endpoint patterns, validation, logging, `.editorconfig` + `Directory.Build.props` enforcement |
| Write or modify tests | `Architecture/testing-strategy.md` | xUnit + FluentAssertions + Moq, per-layer test scope, coverage |
| Write any PowerShell command (script, `HOWTORUN.md`, demo) | `Infrastructure/powershell-conventions.md` | PS 5.1 quirks, encoding, `Invoke-RestMethod` over `curl` |
| Add or modify a homework's Dockerfile or `docker-compose.yml` | `Infrastructure/docker-conventions.md` | Multi-stage build, base-image versions, Compose for Postgres/Redis/etc., adapt `.claude/static/Dockerfile` |
| Open a PR or draft a PR body | `Infrastructure/pull-request-process.md` | Fork-only, branch naming, screenshots, reviewer assignment |
| Finalize a homework's `README.md` | `Infrastructure/template-variables.md` | Placeholder substitutions |
| Start or advance a homework plan | `Processes/homework-planning-process.md` | Super-plan + session-plan artifact spec |

## Default Stack

The student plans to use **.NET / ASP.NET Core** for assignments where a backend is required (HW1, HW2, etc.), even though `TASKS.md` files often suggest Node.js or Python as examples. Stack-specific conventions live in `.claude/docs/Architecture/dotnet-stack.md` — read it before scaffolding any homework code.

## Shell Conventions

PowerShell 5.1 is the primary shell. Full conventions — encoding, no `&&`/`||`, `Invoke-RestMethod` over `curl`, when to prefer the `PowerShell` tool over `Bash` — live in `.claude/docs/Infrastructure/powershell-conventions.md`. Read it before authoring any user-facing command, script, or `HOWTORUN.md` snippet.

## Cross-Cutting Workflows

### Homework planning artifacts (start here for every homework)
Every homework follows a **two-level plan-then-execute** discipline:

- **Super-plan** (`homework-N/PLAN.md`) — milestone DAG with 4–8 independently verifiable milestones, each carrying a runnable PowerShell `Verify` command and a `Parallel: safe | sequential` label. Written once at homework start.
- **Session plans** (`homework-N/plans/milestone-<N>.md`) — per-milestone implementation briefs, written when each milestone starts. Drive the inner **edit → review → apply** loop (default reviewer: `code-review-advisor`) before `Verify` is run.

**No source code is written before the super-plan exists and its milestone list is approved.** **No source code is edited within a milestone before its session plan exists.**

The full artifact specification — both schemas, lifecycle states (`[ ]`/`[~]`/`[x]`/`[!]`), verify-command rules, dependency rules, sizing heuristics, inner-loop steps, parallel-dispatch rules, failure handling policy, and commit convention — lives in `.claude/docs/Processes/homework-planning-process.md`. Read it in full before reading or modifying any PLAN.md or session plan; treat it as the contract.

The workflow that produces and consumes these artifacts (decompose `TASKS.md` → super-plan → session plan → edit → review → apply → verify → commit) is implemented as the **`/homework` skill** at `.claude/skills/homework/SKILL.md`. Invoke it as `/homework <N> [subcommand]` where `<N>` is the homework number. Subcommands: `plan`, `next`, `run-all`, `re-plan`, `status`. Without a subcommand the skill is state-aware and dispatches based on PLAN.md state. The artifact spec is intentionally actor-neutral so multiple consumers (the skill, agents, manual edits) can interoperate through the same files.

Both `PLAN.md` and the `plans/` directory are graded evidence under AI-Usage Documentation (25%) — keep them checked in.

### Pull request workflow
Every homework is submitted via a PR on the student's **own fork** (never upstream). The full process — branch naming, PR body sections, screenshot requirements, reviewer assignment — lives in `.claude/docs/Infrastructure/pull-request-process.md`. Read it before opening a PR or drafting a PR body.

### Template variables
Each `homework-N/README.md` ships with placeholder variables (`[Your Name]`, `[Date]`, `YOUR_USERNAME`, etc.) that must be filled in before submission. The full list, the values to substitute, and where each one appears are documented in `.claude/docs/Infrastructure/template-variables.md`. Apply substitutions automatically when finalizing a homework's `README.md`.

### Documentation set per homework
A homework submission is incomplete without:
- `PLAN.md` — super-plan, every milestone marked `[x]` (graded as AI-usage evidence — see `.claude/docs/Processes/homework-planning-process.md`)
- `plans/milestone-<N>.md` — one session plan per milestone, with completed `Notes` sections (graded as AI-usage evidence)
- `README.md` — overview, AI-tools usage, architecture notes (graded for AI-usage documentation, 25%)
- `HOWTORUN.md` — step-by-step run instructions (PowerShell-first per above)
- `docs/screenshots/` — AI-interaction screenshots and a running-app screenshot (graded, 10%)
- `demo/` — runnable scripts and sample requests

Do not consider a homework "done" until all six exist, every milestone in `PLAN.md` is `[x]`, every milestone has a session plan, and the PR body matches the standards in `.claude/docs/Infrastructure/pull-request-process.md`.

## Verification Commands

Project-wide PowerShell snippets (`git status`, `Get-ChildItem -Recurse`, `Select-String`) live in `.claude/docs/Infrastructure/powershell-conventions.md`. .NET-specific commands are split by concern: `dotnet new`/scaffold in `.claude/docs/Architecture/project-architecture.md`, `dotnet test`/coverage in `.claude/docs/Architecture/testing-strategy.md`. Per-homework `CLAUDE.md` files may specialize these.

## Things Not To Do

- Do **not** edit any `TASKS.md` — those are the instructor's spec.
- Do **not** open PRs against the upstream repo (`README.md` step 64 is explicit). PRs go to the student's fork only.
- Do **not** delete or rename `homework-N/` directories that already have submitted work, even if they look empty — `.gitkeep` files preserve required structure.
- Do **not** commit binaries, build output, or `.env` files. The `.gitignore` and `.claudeignore` already exclude these — keep them out of context.

## Memory and Context

The user's name is Vlad Bairak (Git author). Today's date conventions for `[Date]` placeholders should use **ISO format `YYYY-MM-DD`** unless the user specifies otherwise.
