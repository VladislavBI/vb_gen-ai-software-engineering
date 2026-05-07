# Pull Request Process

Distilled from the course `README.md` (sections "How to Submit Your Homework" and "Submission Requirements"). PRs are the primary submission artifact — they are graded, and an insufficient PR body is grounds for rejection **even if the code is correct**.

## Where the PR Goes

PRs are opened **against the student's own fork**, not the upstream course repo. Both the **base** and the **compare** sides live inside the fork:

- **base repository**: the student's fork (e.g. `wbayrakvlad/...`)
- **base branch**: `main`
- **compare branch**: `homework-N-submission`

Never click "Compare across forks" or change the base owner. The course repo is a template — submissions don't merge there.

## Branch Naming

One branch per homework, named `homework-N-submission` (e.g. `homework-1-submission`, `homework-2-submission`). One PR per homework. Don't pile multiple homework into one PR.

## Workflow (PowerShell)

```powershell
# Start a new homework
git checkout main
git pull
git checkout -b homework-1-submission

# ... work, commit, push ...

git add homework-1
git commit -m "Complete homework 1"
git push -u origin homework-1-submission

# Create the PR (gh CLI)
gh pr create `
  --base main `
  --head homework-1-submission `
  --title "Homework 1: Banking Transactions API" `
  --body-file .pr-body.md `
  --reviewer Alexey-Popov `
  --label homework-1,ready-for-review
```

If `gh` is not installed/auth'd, open the "Compare & pull request" link printed by `git push` and fill the form manually.

## Required PR Body Sections

The PR description is graded — it must stand alone (a reviewer should be able to evaluate the submission without cloning). Include all of:

1. **Summary of what was implemented** — enough detail for someone unfamiliar with the branch. Bullet the endpoints / features delivered against `TASKS.md`.
2. **AI tools used** — which tools (Claude Code, Copilot, etc.), the prompting workflow, what was generated vs. what was hand-written, and what the student verified themselves. This section maps directly to the **AI Usage Documentation (25%)** grading criterion.
3. **How reviewers can run and verify** — link to `HOWTORUN.md` *and* paste the minimal "clone → run → curl" sequence inline so the PR body is self-sufficient. PowerShell commands only (see root `CLAUDE.md`).
4. **Challenges encountered and how they were addressed** — short, specific, concrete. Include failed AI attempts or corrections, not just successes.
5. **Screenshots / demos** — embed key images directly in the PR body using `![alt](docs/screenshots/...)` paths *or* link clearly to `docs/screenshots/`. The course `README.md` calls out screenshots as a hard expectation. At minimum:
   - At least one **AI tool interaction** (prompt + response)
   - One showing the **application running** (e.g. Swagger UI, terminal output of a successful request)
   - For HW2 onward: **test coverage report** screenshot

6. **Checklist** confirming all required deliverables are present (per `TASKS.md` Deliverables section).

## Reviewer Assignment

Always add **`Alexey-Popov`** as a reviewer (he is the instructor named in `README.md`). Optionally add labels `homework-N` and `ready-for-review`.

## Quality Bar (from README, paraphrased)

> Do not submit a bare or one-line PR. Homework submitted without a proper PR description and the expected visual documentation **will be rejected**.

Treat the PR body as the primary submission narrative — link to `README.md` / `HOWTORUN.md` in the repo, but the PR itself must still stand on its own.

## Drafting the PR Body

When generating a PR body, source content from:

- `homework-N/TASKS.md` → list of features delivered (cross off each task)
- `homework-N/README.md` → architecture decisions, AI tool list
- `homework-N/HOWTORUN.md` → run/verify steps
- `git log homework-N-submission --not main` → commits to summarize
- `git diff main...homework-N-submission --stat` → scope of changes

Write the body to a file (`.pr-body.md` is gitignored) and pass to `gh pr create --body-file` to preserve formatting.
