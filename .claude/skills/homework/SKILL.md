---
name: homework
description: Plan, execute, and verify a course homework using the two-level plan-then-execute artifact set defined in .claude/docs/Processes/homework-planning-process.md. State-aware orchestrator — reads homework-N/PLAN.md and dispatches to the right subagent (homework-planner for planning, homework-milestone-runner for execution). Each phase runs in a fresh subagent context for isolation. Invoke as "/homework N" or "/homework N <subcommand>".
---

# /homework — Homework Workflow Skill

You have been invoked as the `/homework` skill. You are the **orchestrator**: parse args, inspect PLAN.md state, dispatch narrow jobs to subagents, drive the review loop, enforce the gates, and integrate structured results. The heavy *content* (file reads, edits, the review agent's analysis, verify output) stays inside subagents in their own contexts so this conversation stays clean — but **control flow is yours**, not the runner's.

> **Why the skill owns control flow.** The `homework-milestone-runner` runs on a small model (Haiku). Small models are unreliable at "do part of a task, then stop and wait" — they blow through behavioral gates. So this skill is built so that **gates are structural, not behavioral**: every runner dispatch is one narrow job that ends in a natural `return`, and the skill decides what happens next. The runner cannot skip a gate because it does not control the next step — the skill does. The skill runs on a stronger model, so review-loop decisions and gate enforcement live here.

## Architecture

```
User: /homework N [subcommand]
  ↓
Skill (this file — main conversation, strong model, owns control flow)
  ├─ args parsing + PLAN.md state inspection
  ├─ Agent tool call → homework-planner            (for plan / re-plan)
  ├─ Agent tool call → homework-milestone-runner   (one narrow job per dispatch)
  ├─ Agent tool call → code-review-advisor         (the skill runs the review loop)
  └─ status (no subagent — read PLAN.md, report)
```

The skill never reads source code and never runs Verify — those happen inside the milestone-runner subagent. **The skill orchestrates code review itself**: after the runner edits and returns a diff (written to disk), the skill invokes `code-review-advisor`, feeds findings back to the runner, and loops until clean before presenting the user-facing review gate. The skill receives a structured JSON summary back from each subagent — and, because the runner is a small model, **verifies the runner's git state rather than trusting its JSON** (see "Verify the runner, don't trust it").

## Branch model

Every homework uses a **two-branch** layout — granular history on a feature branch, single-commit review surface on a submission branch.

| Branch | Created when | Cut from | Receives | Used for |
|---|---|---|---|---|
| `hw-<N>-feature` | First `plan` invocation for homework `N` | `main` | All planner + runner commits (`hw-<N>-init`, `hw-<N>-<M>`, parallel-worktree merge commits) | Process evidence — full granular history |
| `hw-<N>-submission` | After every milestone is `[x]` (final squash step) | `main` | One squashed commit containing the entire diff of `hw-<N>-feature` | PR head — the branch the PR is opened from |

Rules:
- **Always checkout `hw-<N>-feature` before dispatching any subagent.** Planner and runner commit on whatever branch is HEAD; the skill is responsible for ensuring HEAD is correct.
- If `hw-<N>-feature` does not exist at `plan` time, create it off `main`: `git checkout main; git pull --ff-only; git checkout -b hw-<N>-feature`.
- If it exists, `git checkout hw-<N>-feature` and continue.
- Never commit on `main` and never on `hw-<N>-submission` until the final squash step.
- The squash step is the **only** time the skill creates a commit directly (see "After completion"). All other commits are produced by subagents on `hw-<N>-feature`.

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
   - **Ensure `hw-<N>-feature` is checked out** (see "Branch model"). For `plan`, create it off `main` if missing. For `re-plan`, it must already exist — refuse if not.
2. **Dispatch** the planner via the Agent tool (`subagent_type: "homework-planner"`). Brief it as a fresh agent — it does not see this conversation. Pass:
   - The homework number.
   - The mode: `fresh-plan` or `re-plan`.
   - Any per-homework stack hint if non-default.
   - The requirement that the **very last milestone** must be a "Finalize documentation" milestone covering: filling in `README.md` template variables, completing `HOWTORUN.md`, and populating `demo/` with runnable scripts and sample requests. No other milestone may depend on it; its `Depends on` must list every preceding milestone.
3. **Receive the structured summary** (JSON block in the agent's reply) per the planner's Return shape. Parse:
   - `homework`, `mode`, `plan_path`, `tasks_md_commit`, `plan_commit`
   - `milestones[]` with `n`, `title`, `depends_on`, `parallel`, `verify_summary`, `done`
   - `preserved_milestones[]` (re-plan only)
   - `ambiguities[]` — surface these prominently to the user
4. **Report to the user:** the milestone list (one line each), any ambiguities the planner resolved, the `plan_commit` short hash (the `hw-<N>-init` or `hw-<N>-re-plan` commit), and the suggested next step (`/homework N next`).
5. **Stop** — wait for the user to approve before triggering execution.

## Phase: Executing one milestone (`next`)

Delegates to the **`homework-milestone-runner`** subagent.

### Confirmation gates and the skill-driven review loop

There are **two user-approval gates** (plan gate, review gate). Between them sits the **skill-driven review loop** — the skill, not the runner, invokes `code-review-advisor`. Each runner dispatch is one narrow job ending in a `return`; the skill enforces every boundary.

| Gate | Trigger (a runner `return`) | What to show the user | Re-dispatch mode |
|---|---|---|---|
| **Plan gate** (user) | Runner returns `awaiting_plan_approval` after writing `plans/milestone-<N>.md` | Full session-plan text (Approach + Touch list + Review focus) | `resume_after_plan` |
| **Review gate** (user) | The skill's review loop reaches a clean verdict (`APPROVE` / `APPROVE_WITH_SUGGESTIONS`) | Diff summary + reviewer's final verdict + iteration count | `resume_after_review` |

User-gate workflow:
1. Show the artefact to the user.
2. Ask: **"Approve and continue / Request changes"**. Accept free-text changes.
3. If changes requested: re-dispatch the runner with the gate's `resume_*` mode and a `user_feedback` field. The runner applies feedback and the flow loops back to the same gate.
4. Only on explicit approval does the flow proceed past the gate.

**Skill-driven review loop** — runs after the runner returns `awaiting_review`, *before* the review gate. This is the core of the orchestrator pattern:

1. Runner edited the in-scope files, wrote the working-tree diff to `homework-N/plans/.review-<M>.diff`, and returned `status: "awaiting_review"` with `diff_path` and `changed_files`.
2. **Verify the runner first** (see "Verify the runner, don't trust it"): confirm the diff file exists and that *no commit was created* (`git log` HEAD unchanged). If the runner over-stepped (e.g. committed early), reset/correct before continuing.
3. Read the `## Review focus` section from `homework-N/plans/milestone-<M>.md` and the milestone's `Goal`/`Why` from PLAN.md.
4. Invoke `code-review-advisor` via the Agent tool. Tell it to **`Read` the `diff_path`** and pass the Review focus + milestone intent as criteria. It is read-only and returns a structured report (Verdict + Findings).
5. Read only the reviewer's **Verdict** and **Findings** into context — never paste the raw diff into the skill conversation. It stays on disk; the reviewer reads it.
6. If Verdict is `REQUEST_CHANGES` or `REJECT`: re-dispatch the runner with `mode: resume_with_findings` and `reviewer_findings` set to the reviewer's Findings block (the copy-pasteable suggestions). The runner applies them, overwrites the diff file, returns `awaiting_review` again. Increment the skill's `review_iterations` counter and loop to step 2.
7. If Verdict is `APPROVE` or `APPROVE_WITH_SUGGESTIONS`: the loop is clean. Present the **review gate** to the user (workflow above).

**Never skip a gate**, even in `run-all` mode. **Never let the runner run Verify or commit** until the user has approved at the review gate (which is the only path to `resume_after_review`).

### Verify the runner, don't trust it

The runner is a small model and may misreport what it did. After every runner `return`, the skill (which has Bash) checks reality rather than trusting the returned JSON:

- After `awaiting_review` / `resume_with_findings`: confirm `git diff --stat` is non-empty, the `.review-<M>.diff` file exists and is fresh, and `git rev-parse HEAD` is unchanged (no premature commit).
- After `resume_after_review` → `verified`: confirm a new commit exists with subject `hw-<N>-<M>`, the `.review-<M>.diff` temp file is gone, and PLAN.md actually shows `[x]`.
- If reality and the JSON disagree, trust git: correct the state (or re-dispatch with explicit instructions) before advancing. Do not propagate a false `verified`.

### Steps

1. **Pre-checks (in skill context):**
   - Read `homework-N/PLAN.md`. Refuse if missing.
   - **Ensure `hw-<N>-feature` is checked out.** If missing, refuse and route the user back to `plan`. The runner commits on HEAD; HEAD must be the feature branch.
   - Find the next eligible milestone: lowest-numbered `[ ]` or `[~]`. If all `[x]`, report complete; if any `[!]` blocks, refuse.
   - Verify dependencies: every `Depends on` of the target must be `[x]`. If not, refuse and tell the user which dependency is unmet.
2. **Dispatch** the runner via the Agent tool (`subagent_type: "homework-milestone-runner"`). Give it **one narrow job** per dispatch. Pass:
   - Homework number.
   - Milestone number (specific) — resolve `"next"` here in the skill, do not push that resolution to the subagent.
   - Mode hint: `resume` if the milestone was already `[~]`, else `fresh`.
   - The skill owns the gates and the review loop; the runner's job is just the current dispatch step.
3. **Receive the structured summary** per the runner's Return shape. Parse:
   - `status`: `"verified"`, `"blocked"`, `"awaiting"`, `"awaiting_plan_approval"`, or `"awaiting_review"`
   - `files_changed[]`, `commit`, `verify_tail`, `issue`, `retry`
   - `session_plan_text` (present when `status == "awaiting_plan_approval"`)
   - `diff_path`, `changed_files` (present when `status == "awaiting_review"`)
   - **Then verify against git** per "Verify the runner, don't trust it" — do not act on the JSON alone.
4. **Act on status:**
   - `verified` — confirm the `hw-<N>-<M>` commit and the `[x]` tick exist in git, then report milestone title, commit hash, files changed, the skill's review-iteration count, verify tail. Suggest `/homework N next` or `/homework N status`.
   - `blocked` — report milestone title, the `Issue:` excerpt, what was tried in the auto-retry. Suggest `/homework N re-plan` or manual fix.
   - `awaiting` — report what's needed (typically: super-plan update or external service start). Do not advance.
   - `awaiting_plan_approval` — **plan gate.** Display `session_plan_text` in full. On approval re-dispatch `mode: resume_after_plan`; on changes re-dispatch `mode: resume_after_plan` + `user_feedback`.
   - `awaiting_review` — **enter the skill-driven review loop** (see "Confirmation gates and the skill-driven review loop"). Do NOT show this to the user yet. Run `code-review-advisor` against `diff_path` with the session plan's Review focus; loop via `resume_with_findings` until the verdict is clean; only then present the **review gate** and, on approval, re-dispatch `mode: resume_after_review`. If the `awaiting_review` carried `retry: true` (it followed a failed Verify), the subsequent `resume_after_review` must include `attempt: 2` so the runner knows a second Verify failure means blocked.
5. **Stop** — do not chain to the next milestone unless the user originally said `run-all`.

## Phase: Run all remaining (`run-all`)

Loops `next`, with **worktree-based parallel dispatch** where eligible.

1. **Confirm with the user** before starting unattended chained execution. Show the pending milestones and ask "proceed?". Note: even in `run-all` mode the two user gates (plan gate and review gate) still apply for every milestone, and the skill still runs its own `code-review-advisor` review loop per milestone — the loop pauses at each user gate and waits for approval before continuing. In parallel-worktree dispatch, run the review loop **per runner, serialized** (one `code-review-advisor` invocation at a time); each milestone's diff lives at its own worktree's `homework-N/plans/.review-<M>.diff`, so there is no file collision.
2. **Loop:**
   - Read PLAN.md. Identify the **current DAG frontier**: every milestone whose `Depends on` is fully `[x]` and whose own `Done` is `[ ]` or `[~]`.
   - **Sequential dispatch** if any frontier milestone is `Parallel: sequential` OR there's only one eligible milestone: invoke `homework-milestone-runner` once for the lowest-numbered eligible milestone in the **main worktree**. Wait for result.
   - **Parallel worktree dispatch** if **all** eligible frontier milestones are `Parallel: safe` AND there are 2+ of them:
     1. **For each parallel-safe milestone M at the frontier**, the skill creates a worktree:
        ```powershell
        git worktree add ../worktree-hw-<N>-<M> -b hw-<N>-<M>-work
        ```
     2. **Invoke multiple `homework-milestone-runner` subagents in one message** (parallel `Agent` tool calls), one per worktree. Pass the worktree path and the branch name as the `Worktree context` input. Each runner writes its commit on the per-milestone branch.
     3. **After all parallel runners complete** (or any block), checkout `hw-<N>-feature` and merge each successful per-milestone branch back in milestone-number order:
        ```powershell
        git checkout hw-<N>-feature
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
- **Never** allow a plan where the last milestone is not a "Finalize documentation" milestone (`README.md` template variables, `HOWTORUN.md`, and `demo/` scripts). If the planner's summary shows otherwise, reject and re-dispatch with explicit correction.
- **Never** read source code or run `Verify` in the skill context — those belong to the milestone-runner.
- **Always** invoke `code-review-advisor` from the skill, never from the runner. The skill owns the review loop; the runner only edits and applies findings.
- **Never** paste the raw milestone diff into the skill conversation — the reviewer `Read`s it from `diff_path` on disk; the skill handles only the bounded Verdict + Findings.
- **Never** trust the runner's returned JSON over git. After every runner `return`, verify the real git state (commit presence, diff file, PLAN.md tick) before acting — the runner is a small model and may misreport.
- **Never** create commits directly in the skill context, with two narrow exceptions: (a) merge commits during parallel-worktree merge-back into `hw-<N>-feature`, and (b) the single squashed commit on `hw-<N>-submission` produced by the final squash step in "After completion". All other commits are produced by subagents on `hw-<N>-feature` (`hw-<N>-init` / `hw-<N>-re-plan` from the planner; `hw-<N>-<M>` from the runner).
- **Never** dispatch a subagent unless `hw-<N>-feature` is the current branch. The skill is responsible for checking out the feature branch before every planner/runner dispatch.
- **Never** commit on `main`, and **never** push commits to `hw-<N>-submission` other than the single final squashed commit.
- **Always** re-read `.claude/docs/Processes/homework-planning-process.md` at the start of each invocation. The spec is the contract; this skill is one runtime.
- **Always** parse the structured JSON from each subagent's reply rather than relying on prose.

## After completion

When `status` shows every milestone in `PLAN.md` is `[x]`:

1. **Verify deliverables.** Confirm all six required artefacts exist on `hw-<N>-feature`: `PLAN.md`, `plans/milestone-<N>.md` for every N, `README.md`, `HOWTORUN.md`, `docs/screenshots/`, `demo/`. If any are missing, stop and report; do not squash an incomplete homework.

2. **Confirm with the user before squashing.** Show: source branch (`hw-<N>-feature`), target branch (`hw-<N>-submission`), commit count to be collapsed, and the proposed squashed commit message (default: `Homework <N>: <short title from TASKS.md>`). Wait for explicit approval. Accept message edits.

3. **Create the squashed submission branch.** From the skill context, run:
   ```powershell
   git checkout main
   git pull --ff-only
   git checkout -b hw-<N>-submission       # if it already exists, refuse and ask the user how to proceed (delete? force-update?)
   git merge --squash hw-<N>-feature
   git commit -m "<approved squashed message>"
   ```
   This is the **only** commit the skill itself creates outside parallel-worktree merges. Do not push automatically — let the PR-creator agent handle pushing.

4. **Report.** "Homework-N complete. N milestones verified on `hw-<N>-feature`. Squashed submission ready on `hw-<N>-submission` (`<short hash>`). Next step: invoke `homework-pr-creator` to open the PR from `hw-<N>-submission` against `main`."

5. **Do not invoke `homework-pr-creator` from this skill.** PR creation is a separate workflow with its own contract; it expects to be the entry point. Tell the user the PR head must be `hw-<N>-submission`, not `hw-<N>-feature`.
