---
name: homework
description: Plan, execute, and verify a course homework using the two-level plan-then-execute artifact set defined in .claude/docs/Processes/homework-planning-process.md. State-aware orchestrator — reads homework-N/PLAN.md and dispatches to the right subagent (homework-planner for planning, homework-milestone-runner for execution). Each phase runs in a fresh subagent context for isolation. Invoke as "/homework N" or "/homework N <subcommand>".
---

# /homework — Homework Workflow Skill

You have been invoked as the `/homework` skill. You are a **thin orchestrator**: parse args, inspect PLAN.md state, dispatch to the right subagent, and integrate the structured result. The heavy work (file reads, edits, review iterations, verify output) happens inside subagents in their own contexts so this conversation stays clean.

## Architecture

```
User: /homework N [subcommand]
  ↓
Skill (this file — main conversation, lightweight)
  ├─ args parsing + PLAN.md state inspection
  ├─ Agent tool call → homework-planner       (for plan / re-plan)
  ├─ Agent tool call → homework-milestone-runner  (for next / run-all)
  └─ status (no subagent — read PLAN.md, report)
```

The skill never reads source code, never runs Verify, never invokes `code-review-advisor` directly — those happen inside the milestone-runner subagent. The skill receives a structured JSON summary back from each subagent and integrates it.

## Authoritative source

**Read `.claude/docs/Processes/homework-planning-process.md` in full at the start of every invocation.** This skill is the runtime; that document is the contract. Reading it on every invocation costs ~5k tokens but ensures behavior tracks the spec without rebuilding the skill.

Reference it but **do not duplicate** its rules in the skill — point subagents at it, they read it themselves in their own contexts.

## Argument parsing

Invocation format: `/homework <args>`.

Parse `args`:
- **First token** — homework number (positive integer). If missing or unparseable, ask the user which homework before doing anything.
- **Second token** — optional subcommand. Valid: `plan`, `next`, `run-all`, `re-plan`, `status`. If absent, infer from PLAN.md state (Default dispatch below). Reject any other subcommand.

Examples:
- `/homework 3` — state-aware default
- `/homework 3 plan` — force planning phase
- `/homework 3 next` — run next eligible milestone
- `/homework 3 run-all` — chain all remaining milestones
- `/homework 3 re-plan` — rewrite pending sections of PLAN.md
- `/homework 3 status` — read-only summary

## Default dispatch (no subcommand given)

After parsing N, inspect `homework-N/PLAN.md` and dispatch:

| PLAN.md state | Default action | Notes |
|---|---|---|
| File missing | `plan` | Bootstrap from `TASKS.md`. |
| All milestones `[ ]` | Show milestone list, ask user to approve before running `next` | The plan exists but execution hasn't started. |
| At least one `[~]` | `next` (resume the `[~]` milestone) | The runner will pick it up via the `[~]` breadcrumb. |
| `[ ]` + `[x]`, no `[~]`/`[!]` | `next` | Standard advance. |
| Any `[!]` (blocked) | Refuse to advance | Report the `Issue:` line; offer `re-plan` or manual fix. |
| All `[x]` | Report complete | Suggest invoking the `homework-pr-creator` agent. Do not invoke it from this skill. |

## Phase: Planning (`plan` and `re-plan`)

Both phases delegate to the **`homework-planner`** subagent.

1. **Pre-checks (in skill context):**
   - For `plan`: if `PLAN.md` exists and any milestone is `[x]`, ask the user before overwriting (suggest `re-plan` instead).
   - For `re-plan`: confirm `PLAN.md` exists. If not, route to `plan`.
2. **Dispatch** the planner via the Agent tool (`subagent_type: "homework-planner"`). Brief it as a fresh agent — it does not see this conversation. Pass:
   - The homework number.
   - The mode: `fresh-plan` or `re-plan`.
   - Any per-homework stack hint if non-default.
3. **Receive the structured summary** (JSON block in the agent's reply) per the planner's Return shape. Parse:
   - `homework`, `mode`, `plan_path`, `tasks_md_commit`, `plan_commit`
   - `milestones[]` with `n`, `title`, `depends_on`, `parallel`, `verify_summary`, `done`
   - `preserved_milestones[]` (re-plan only)
   - `ambiguities[]` — surface these prominently to the user
4. **Report to the user:** the milestone list (one line each), any ambiguities the planner resolved, the `plan_commit` short hash (the `hw-<N>-init` or `hw-<N>-re-plan` commit), and the suggested next step (`/homework N next`).
5. **Stop** — wait for the user to approve before triggering execution.

## Phase: Executing one milestone (`next`)

Delegates to the **`homework-milestone-runner`** subagent.

### Confirmation gates

Two mandatory pause points require explicit user approval before the runner may continue. The runner signals each gate by returning a structured result with the gate's status; the skill presents the artefact to the user, collects approval (or edits), then re-dispatches with the appropriate `resume_*` mode.

| Gate | Trigger | What to show the user | Re-dispatch mode |
|---|---|---|---|
| **Plan gate** | Runner finishes writing `plans/milestone-<N>.md` | Full session-plan text (Approach + Touch list + Review focus) | `resume_after_plan` |
| **Review gate** | Runner finishes the final review iteration (no blocking findings remain) | Diff summary + reviewer's sign-off | `resume_after_review` |

Workflow per gate:
1. Show the artefact to the user.
2. Ask: **"Approve and continue / Request changes"**. Accept free-text changes.
3. If changes requested: pass them back to a fresh runner invocation with the gate's `resume_*` mode and a `user_feedback` field containing the requested changes. The runner applies feedback and loops back to the same gate.
4. Only when the user explicitly approves does the runner proceed past the gate.

**Never skip a gate**, even in `run-all` mode — each gate requires a separate approval before the runner continues.

### Steps

1. **Pre-checks (in skill context):**
   - Read `homework-N/PLAN.md`. Refuse if missing.
   - Find the next eligible milestone: lowest-numbered `[ ]` or `[~]`. If all `[x]`, report complete; if any `[!]` blocks, refuse.
   - Verify dependencies: every `Depends on` of the target must be `[x]`. If not, refuse and tell the user which dependency is unmet.
2. **Dispatch** the runner via the Agent tool (`subagent_type: "homework-milestone-runner"`). Pass:
   - Homework number.
   - Milestone number (specific) — resolve `"next"` here in the skill, do not push that resolution to the subagent.
   - Mode hint: `resume` if the milestone was already `[~]`, else `fresh`.
   - Confirmation gates are **always active**; the runner must stop at each gate.
3. **Receive the structured summary** per the runner's Return shape. Parse:
   - `status`: `"verified"`, `"blocked"`, `"awaiting"`, `"awaiting_plan_approval"`, or `"awaiting_review_approval"`
   - `files_changed[]`, `commit`, `review_iterations`, `verify_tail`, `issue`
   - `session_plan_text` (present when `status == "awaiting_plan_approval"`)
   - `diff_summary` (present when `status == "awaiting_review_approval"`)
4. **Report to the user** based on status:
   - `verified` — milestone title, commit hash, files changed, review iteration count, verify tail. Suggest `/homework N next` or `/homework N status`.
   - `blocked` — milestone title, the `Issue:` excerpt, what was tried in the auto-retry. Suggest `/homework N re-plan` or manual fix.
   - `awaiting` — what's needed (typically: super-plan update or external service start). Do not advance.
   - `awaiting_plan_approval` — display `session_plan_text` in full. Ask the user to approve or provide changes. On approval, re-dispatch with `mode: resume_after_plan`. On changes, re-dispatch with `mode: resume_after_plan` + `user_feedback`.
   - `awaiting_review_approval` — display `diff_summary` and the reviewer sign-off. Ask the user to approve or provide changes. On approval, re-dispatch with `mode: resume_after_review`. On changes, re-dispatch with `mode: resume_after_review` + `user_feedback`.
5. **Stop** — do not chain to the next milestone unless the user originally said `run-all`.

## Phase: Run all remaining (`run-all`)

Loops `next`, with **worktree-based parallel dispatch** where eligible.

1. **Confirm with the user** before starting unattended chained execution. Show the pending milestones and ask "proceed?". Note: even in `run-all` mode the two confirmation gates (plan gate and review gate) still apply for every milestone — the loop pauses at each gate and waits for user approval before continuing.
2. **Loop:**
   - Read PLAN.md. Identify the **current DAG frontier**: every milestone whose `Depends on` is fully `[x]` and whose own `Done` is `[ ]` or `[~]`.
   - **Sequential dispatch** if any frontier milestone is `Parallel: sequential` OR there's only one eligible milestone: invoke `homework-milestone-runner` once for the lowest-numbered eligible milestone in the **main worktree**. Wait for result.
   - **Parallel worktree dispatch** if **all** eligible frontier milestones are `Parallel: safe` AND there are 2+ of them:
     1. **For each parallel-safe milestone M at the frontier**, the skill creates a worktree:
        ```powershell
        git worktree add ../worktree-hw-<N>-<M> -b hw-<N>-<M>-work
        ```
     2. **Invoke multiple `homework-milestone-runner` subagents in one message** (parallel `Agent` tool calls), one per worktree. Pass the worktree path and the branch name as the `Worktree context` input. Each runner writes its commit on the per-milestone branch.
     3. **After all parallel runners complete** (or any block), merge each successful branch back into the main homework branch in milestone-number order:
        ```powershell
        git merge --no-ff hw-<N>-<M>-work
        ```
     4. **Remove merged worktrees:**
        ```powershell
        git worktree remove ../worktree-hw-<N>-<M>
        ```
     5. **Keep failed (`[!]`) worktrees** until the user resolves them. Do not auto-delete.
   - **Port-collision handling**: if two `Parallel: safe` milestones both bind port 5000 in their `Verify`, serialize the Verify phase across runners (run inner loops concurrently, run Verify blocks one at a time) or instruct the runners to bind unique ports.
3. **After each iteration**, integrate results into the running tally. Stop the loop on:
   - All milestones `[x]` (success) → final report + suggest `homework-pr-creator`.
   - Any milestone `blocked` (auto-retry exhausted) → final report + recommend `re-plan`. Leave its worktree intact.
   - Any milestone `awaiting` (mis-scoped or external dep) → final report + recommend specific fix.
4. **Final report:** how many milestones ran, succeeded, blocked; total wall-clock time; any unmerged worktrees with their branch names; the single recommended next action.

## Phase: Status (`status`)

Read-only. **No subagent.** Done in skill context.

1. Check whether `homework-N/PLAN.md` exists. If not, report and suggest `plan`.
2. Read PLAN.md. Count milestones by `Done` state.
3. For each milestone, list: `N | title | Done | Parallel`.
4. Check whether `homework-N/plans/milestone-<N>.md` exists for each milestone.
5. If any `[!]`: excerpt the `Issue:` line.
6. Recommend the single next action.
7. **Make no changes.**

## Hard constraints

Echoed from the spec for fast failure at the orchestrator level:

- **Never** invoke `homework-milestone-runner` if PLAN.md is missing — route to `plan` first.
- **Never** invoke a runner for a milestone whose dependencies are not all `[x]`.
- **Never** dispatch a `Parallel: sequential` milestone concurrently with anything, even when its dependencies are met.
- **Never** dispatch in parallel without worktrees — shared-working-tree parallelism is not supported.
- **Never** chain milestones automatically unless `run-all` was the original subcommand.
- **Never** let the runner proceed past the plan gate or the review gate without explicit user approval — not even in `run-all` mode.
- **Never** invoke `homework-pr-creator` from this skill — recommend it as a next step instead.
- **Never** read source code or run `Verify` in the skill context — those belong to the milestone-runner.
- **Never** create commits directly in the skill context — both subagents commit their own work (`hw-<N>-init` / `hw-<N>-re-plan` from the planner; `hw-<N>-<M>` from the runner). The skill only creates merge commits during parallel-worktree merge-back.
- **Always** re-read `.claude/docs/Processes/homework-planning-process.md` at the start of each invocation. The spec is the contract; this skill is one runtime.
- **Always** parse the structured JSON from each subagent's reply rather than relying on prose.

## After completion

When `status` shows every milestone in `PLAN.md` is `[x]`:
1. Confirm all six required deliverables exist: `PLAN.md`, `plans/milestone-<N>.md` for every N, `README.md`, `HOWTORUN.md`, `docs/screenshots/`, `demo/`.
2. Report: "Homework-N complete. N milestones verified. Next step: invoke `homework-pr-creator` to draft the PR."
3. **Do not invoke `homework-pr-creator` from this skill.** PR creation is a separate workflow with its own contract; it expects to be the entry point.
