# Homework Planning Specification

This file defines the **two-level plan-then-execute artifact set** that drives every homework's work. It describes *what* the artifacts are, *how their fields are interpreted*, and *what rules govern their lifecycle*. It does **not** prescribe which agent, skill, or human produces or consumes them — that responsibility lives in a dedicated skill (TBD).

Anything that reads or writes these artifacts MUST follow this spec. Treat it as a contract.

## The two artifacts

| Artifact | Path | Purpose |
|---|---|---|
| **Super-plan** | `homework-N/PLAN.md` | Milestone DAG. Written once at homework start. Stable across the homework's lifetime. Defines *what to build* and the order. |
| **Session plan** | `homework-N/plans/milestone-<N>.md` | Per-milestone implementation brief. Written when a milestone starts. Drives the edit → review → apply loop within that milestone. Defines *how to build this one piece*. |

Both are checked in. Both are graded evidence under AI-Usage Documentation (25%).

The super-plan is the contract between *the student and the AI* about scope. Each session plan is the contract between *the editor and the reviewer* about approach for one milestone.

## Purpose

The super-plan decomposes a homework into 4–8 independently verifiable milestones, each with a runnable PowerShell `Verify` command, written **before any source code**.

Each session plan is opened when its milestone starts and captures the implementation approach in plain prose so the inner edit → review → apply loop has a shared artifact to reason over.

Both artifacts together serve three audiences:

1. **The student** — alignment on scope (super-plan) and approach (session plans) before any code is written.
2. **Any actor processing the homework** (skill, agent, human) — the super-plan determines what to build and in what order; each session plan determines the implementation strategy and what the reviewer should focus on for that milestone.
3. **The course grader** — full evidence trail: how the work was decomposed (super-plan) and how each milestone was tackled (session plans).

## Lifecycle states

A PLAN.md transitions through these states based on the `Done` markers of its milestones:

| State | Meaning |
|---|---|
| **Draft** | File exists; every milestone has `Done: [ ]`. No source code yet. |
| **In progress** | At least one milestone is `[~]` or `[x]`; some are still `[ ]`. |
| **Blocked** | At least one milestone is `[!]` and advancement has stopped. |
| **Complete** | Every milestone is `[x]`. Ready for PR submission. |

A milestone marked `[x]` is **frozen** — its section MUST NOT be edited later. Re-plans rewrite only `[ ]`/`[~]`/`[!]` sections.

## Super-plan schema (PLAN.md)

Every `homework-N/PLAN.md` MUST follow this shape (verbatim headings):

```markdown
# Homework N — Plan

**TASKS.md commit:** <output of `git rev-parse HEAD` at plan time, or "uncommitted">
**Created:** YYYY-MM-DD
**Stack:** <e.g. .NET 8 / ASP.NET Core>

## Overview
<2-4 sentences: what the homework asks for and the chosen approach. Cite specific TASKS.md requirements by section number.>

## Milestones

### Milestone 1: <short title>
- **Goal:** <one sentence — what this milestone produces>
- **Why this milestone:** <1-3 sentences explaining the part: what it covers, why it earns its own step, what would break if skipped>
- **Files:** <comma-separated paths that will be created or edited; "none" if the milestone is config-only>
- **Depends on:** <milestone numbers, or "none">
- **Parallel:** safe | sequential
- **Verify:**
  ```powershell
  <one or more PowerShell commands that prove the milestone works>
  ```
- **Done:** [ ]

### Milestone 2: ...
```

## Schema rules

- **Number sequentially** starting at 1.
- **4–8 milestones per homework.** Fewer means trivial decomposition; more means over-granularity.
- Each milestone is **independently verifiable**. If the only way to verify is "run the whole app and look at it," the milestone is too big — split it.
- **`Verify` must be PowerShell.** No `&&`, no Linux `curl`, no `cat`. HTTP checks use `Invoke-RestMethod` or `Invoke-WebRequest`. File checks use `Test-Path` or `Get-Content`. Build checks use `dotnet build`.
- **`Verify` must exercise behavior, not existence.** `Test-Path src/Controllers/HealthController.cs` is not a verify — `Invoke-RestMethod http://localhost:5000/health` is. The only valid existence-check verifies are for non-code deliverables (e.g. `Test-Path docs/screenshots/ai-prompt.png`).
- **`Parallel: safe`** — set when this milestone's `Files` list is **disjoint** from the `Files` of every other milestone whose dependencies are simultaneously satisfied (i.e., milestones at the same DAG frontier). Default to `sequential` if unsure.
- **`Parallel: sequential`** — this milestone touches files that other concurrently-eligible milestones also touch, OR the author has not certified disjointness. The consumer MUST run it on its own.

## Done states

| State | Meaning |
|---|---|
| `[ ]` | Not started. |
| `[~]` | In progress — work staged but verify not yet passing. Set *before* starting the milestone as a session-interrupt breadcrumb. |
| `[x]` | Verified — every command under `Verify` exited 0. |
| `[!]` | Blocked — verify failed and was not recovered. MUST have an `**Issue:**` line under the milestone explaining what failed. |

## Dependencies

- `Depends on` lists earlier milestone numbers.
- A milestone MUST NOT be started until every dependency is `[x]`.
- This implicitly forms a DAG. The plan author orders milestones so each only depends on earlier ones; cyclic dependencies are a plan bug.

## Sizing heuristics

A well-sized milestone:
- Touches 1–4 files.
- Has a `Verify` block that runs in under 30 seconds.
- Can be reverted by deleting one commit.
- Maps to a single conceptual deliverable from `TASKS.md`.

If a milestone's goal contains "and" twice, split it.

## Example milestone (well-formed)

```markdown
### Milestone 3: Account-id format validation
- **Goal:** Reject any account id not matching `^ACC-[A-Z0-9]+$` with HTTP 400.
- **Why this milestone:** The TASKS.md spec (section 2) requires the format check; isolating it lets us verify regex behavior without depending on the storage layer (milestone 4) being ready.
- **Files:** src/Validators/AccountIdValidator.cs, src/Controllers/AccountsController.cs
- **Depends on:** 2
- **Parallel:** sequential
- **Verify:**
  ```powershell
  dotnet build
  $ok = Invoke-RestMethod -Uri http://localhost:5000/accounts/ACC-12345 -Method Get
  try { Invoke-RestMethod -Uri http://localhost:5000/accounts/bad-id -Method Get; throw "should have 400'd" } catch { if ($_.Exception.Response.StatusCode.value__ -ne 400) { throw } }
  ```
- **Done:** [ ]
```

## Session plan schema (homework-N/plans/milestone-N.md)

When a milestone transitions out of `[ ]`, the consumer MUST create a session plan at `homework-N/plans/milestone-<N>.md` *before* editing any source code. Schema (verbatim headings):

```markdown
# Milestone <N>: <title> — Session Plan

**Started:** YYYY-MM-DD
**Super-plan reference:** ../PLAN.md milestone <N>

## Approach
<3-6 sentences: chosen implementation strategy and why. Mention alternatives considered and why they were rejected.>

## Touch list
<bullet list of specific functions / classes / endpoints to add or modify, mapped to the Files in the super-plan. One bullet per change.>

## Review focus
<what the reviewer should look for that the Verify command will NOT catch — e.g. null handling, error response shape, naming, idempotency, security smells. 2–5 bullets.>

## Notes
<appended during execution: discoveries, deviations, anything noteworthy. Empty at start.>
```

### Session plan rules

- **Created before editing**, not after. If the plan was written after the code, it is a postmortem, not a plan.
- **Stays within super-plan scope.** The session plan's `Touch list` MUST only reference files in the super-plan's `Files` line for that milestone. Widening requires updating the super-plan first.
- **`Review focus` is mandatory.** It exists so the reviewer agent has criteria beyond what `Verify` covers. An empty `Review focus` defeats the inner loop.
- **`Notes` accumulates.** As implementation proceeds, append observations rather than rewriting earlier prose. The grader reads this section to understand how the milestone unfolded.

## Inner loop: edit → review → apply

For each milestone, the consumer MUST run this loop **before** running the `Verify` block:

1. **Plan** — write `homework-N/plans/milestone-<N>.md` per the schema above. Set the milestone's `Done` to `[~]` in PLAN.md.
2. **Edit** — modify only the files listed in the super-plan's `Files` line. Stay within scope.
3. **Review** — invoke a reviewer (default: the `code-review-advisor` agent in `.claude/agents/`) with the diff and the session plan's `Review focus` as criteria. The reviewer returns structured findings without modifying files.
4. **Apply** — address every reviewer finding. If a finding is rejected, append the reasoning to the session plan's `Notes`.
5. **Re-review** — repeat steps 3–4 until the reviewer is satisfied (no blocking findings).
6. **Verify** — only now run the milestone's `Verify` block from the super-plan. On success, follow the commit convention below.

The inner loop catches what `Verify` cannot: style, security smells, hidden bugs, contract issues. The outer `Verify` catches what review cannot: behavioral regressions and broken integrations.

## Failure handling policy

When a `Verify` command exits non-zero, the consumer follows this rule:

**Policy: One auto-retry, then mark blocked.**

1. **First failure** — read the failing command's output, re-read the session plan and the milestone, and attempt **exactly one** corrective edit cycle (re-enter the inner loop: edit → review → apply, then re-run the full `Verify` block).
2. **Second failure** — set `Done` to `[!]`, append an `**Issue:**` line under the milestone in PLAN.md with: (a) which verify command failed, (b) the exit code or error excerpt, (c) what was attempted in the retry. Append the same plus root-cause analysis to the session plan's `Notes`. Stop. Do not advance to later milestones.
3. **The retry MUST NOT change** `Files` or `Verify` — those are part of the super-plan. If the retry needs to touch files outside `Files`, the milestone is mis-scoped: stop without retrying and request a re-plan.
4. **Never silently widen scope** to make verify pass. If the verify command itself is wrong, stop and request a super-plan update rather than rewriting the verify in place.

Why: stop-and-ask burns wall time on transient errors (a missed `using`, a typo) the consumer can fix itself; mark-blocked-and-skip lets later milestones build atop a broken foundation. One bounded retry catches recoverable mistakes while still surfacing real design issues fast.

## Parallel dispatch

When a consumer has multiple milestones whose dependencies are simultaneously `[x]` (i.e., the current DAG frontier has multiple eligible milestones), and **all** of those milestones carry `Parallel: safe`, the consumer MAY dispatch them concurrently. **Parallel dispatch always uses git worktrees** — there is no shared-working-tree variant.

**Worktree dispatch protocol:**

1. For each parallel-safe milestone M at the frontier, the consumer creates a worktree on a dedicated branch:
   ```powershell
   git worktree add ../worktree-hw-<N>-<M> -b hw-<N>-<M>-work
   ```
2. Each milestone's full inner loop (edit → review → apply → verify → commit) runs **inside its own worktree**, writing the per-milestone commit on the per-milestone branch.
3. After all parallel milestones complete (or any of them block), the consumer merges each successful branch back into the homework branch in the main worktree, in milestone-number order:
   ```powershell
   git merge --no-ff hw-<N>-<M>-work
   ```
4. The worktree is removed once merged:
   ```powershell
   git worktree remove ../worktree-hw-<N>-<M>
   ```
5. Failed (`[!]`) parallel branches are kept until the user resolves them — do not auto-delete.

Port and shared runtime state collisions remain the consumer's responsibility — the spec only guarantees file-list disjointness. If two parallel milestones' `Verify` blocks both bind port 5000, the consumer MUST serialize the Verify phase (run inner loops in parallel, run Verify blocks one at a time) or assign unique ports per worker.

A `Parallel: sequential` milestone MUST NOT be dispatched concurrently with anything, even if its dependencies are met. Sequential milestones run in the main worktree, on the main homework branch.

## Commit convention

Every artifact-mutating action produces exactly one commit. Three commit shapes exist:

### Planner commits

When the planner writes or rewrites `PLAN.md`, it commits the change immediately:

| Mode | Subject | Body |
|---|---|---|
| `fresh-plan` | `hw-<N>-init` | One-line summary: "Plan written: <count> milestones for homework-<N>." |
| `re-plan` | `hw-<N>-re-plan` | One-line summary listing which milestones were rewritten and which were preserved. |

The planner stages **only** `homework-<N>/PLAN.md`. Source code, session plans, and other deliverables are never part of a planner commit.

### Milestone-runner commits

When a milestone moves to `[x]`, **three** changes are staged together in a single commit:

1. The milestone's source code (the files in `Files`).
2. The session plan (`homework-<N>/plans/milestone-<M>.md`) — including any `Notes` appended during the loop.
3. The PLAN.md tick (`Done: [ ]` → `Done: [x]`).

Commit shape:

```
hw-<N>-<M>

<milestone title>
```

The subject `hw-<N>-<M>` is automation-friendly (greppable, sortable). The body holds the milestone title so `git log` searches still surface human-readable context.

Examples:
- `hw-3-1` body `Project scaffold and dependency setup`
- `hw-3-4` body `Account-id format validation`

For parallel-dispatched milestones, the commit lives on the per-milestone work branch (`hw-<N>-<M>-work`) until merged back. The merge commit subject is `merge hw-<N>-<M>-work` with the milestone title in the body.

One milestone = one commit. PR-readiness work (README finalization, screenshot adds, demo scripts) goes in separate commits with their own descriptive subjects (not `hw-<N>-<M>` shape — that's reserved for milestone work).

## Things not to do

- Do **not** start coding a homework before `PLAN.md` exists and its milestone list has been approved.
- Do **not** start editing source code for a milestone before its session plan exists.
- Do **not** edit completed (`[x]`) milestones during a re-plan — rewrite only `[ ]`/`[~]`/`[!]` sections.
- Do **not** edit completed session plans either; they are part of the immutable evidence trail.
- Do **not** combine multiple milestones into a single commit.
- Do **not** mark a milestone `[x]` if any verify command failed. `[!]` is the only honest outcome.
- Do **not** mark `[x]` if the inner review loop was skipped — verify alone is not sufficient.
- Do **not** invent verify commands that always pass (`dotnet --version`, `Test-Path .`). The verify must exercise the milestone's actual behavior.
- Do **not** widen a milestone's `Files` list (in either super-plan or session plan) during execution. If implementation reveals a missing file, stop and re-plan.
- Do **not** dispatch a `Parallel: sequential` milestone concurrently with anything.
- Do **not** assume `Parallel: safe` covers port or DB collisions — it only covers file-list disjointness.

## Verification before PR

Before invoking the PR-creation workflow, confirm:

- Every milestone in `PLAN.md` is `[x]`. No `[ ]`, `[~]`, or `[!]` remains.
- A session plan exists at `homework-N/plans/milestone-<N>.md` for every milestone.
- The required deliverables exist: `PLAN.md`, `plans/`, `README.md`, `HOWTORUN.md`, `docs/screenshots/`, `demo/` (see root `CLAUDE.md`).
- `PLAN.md` and every session plan are checked in. They are graded evidence under AI-Usage Documentation (25%).
