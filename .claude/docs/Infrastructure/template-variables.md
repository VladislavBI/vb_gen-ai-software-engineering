# Template Variables Reference

Each homework's `README.md` ships as a template with placeholder strings that must be replaced before submission. The course `README.md` does the same for the clone URL. This file is the canonical substitution table — apply these whenever finalizing a homework write-up.

## Substitution Table

| Placeholder | Replacement | Where it appears |
|---|---|---|
| `[Your Name]` | `Vlad Bairak` | `homework-N/README.md` (header block) |
| `[Date]` | Today's date in **ISO format** `YYYY-MM-DD` (e.g. `2026-05-07`) | `homework-N/README.md` (header block) |
| `[List tools, e.g., Claude Code, GitHub Copilot]` | Actual tools used for that homework — e.g. `Claude Code (Opus 4.7), GitHub Copilot` | `homework-N/README.md` (header block) |
| `[Briefly describe your implementation - what you built and the key features]` | A 2-4 sentence project overview written by the user | `homework-N/README.md` (Project Overview) |
| `YOUR_USERNAME` | The student's GitHub username — `wbayrakvlad` (verify by reading `git remote -v` before substituting) | Course `README.md` clone URL example only — not auto-substituted, this is documentation |
| `Alexey-Popov` | The instructor's GitHub username — **already correct, do not change** | Course `README.md` reviewer instructions |

## Conventions

- **Date format**: Always ISO `YYYY-MM-DD`. Do not use locale-specific formats (`5/7/2026`, `7 травня 2026`) — they confuse graders and break sorting.
- **Tool list specificity**: Name the model where it matters (`Claude Code (Opus 4.7)` not just `Claude`). The grading criterion "AI Usage Documentation (25%)" rewards specificity.
- **Don't mass-replace blindly**: `[Your Name]` may legitimately appear in template *examples* (e.g. inside a code block showing the template itself). Replace only in the student-authored README header.

## Domain Variables (in `TASKS.md`, do not substitute — these are *examples*)

These look like variables but are domain examples in `TASKS.md` that may flow into your code. Listed for reference so they don't get accidentally treated as placeholders:

| Token | Meaning | Source |
|---|---|---|
| `ACC-XXXXX` | Account number format pattern (HW1) — X is alphanumeric. Use as the regex `^ACC-[A-Z0-9]+$` (case decision yours, document it). | `homework-1/TASKS.md` Task 2 |
| `ACC-12345`, `ACC-67890` | Sample account IDs in HW1 curl examples | `homework-1/TASKS.md` |
| `USD`, `EUR`, `GBP`, `JPY` | Sample ISO-4217 currency codes | `homework-1/TASKS.md` |
| `localhost:3000` | Sample port in HW1's curl examples — when implementing in ASP.NET Core, use whatever port `dotnet run` selects and update `HOWTORUN.md` accordingly. | `homework-1/TASKS.md` |

## Branch / PR Variables

| Variable | Pattern | Example |
|---|---|---|
| Branch name | `homework-N-submission` | `homework-1-submission` |
| PR labels | `homework-N`, `ready-for-review` | `homework-1`, `ready-for-review` |
| PR title | `Homework N: <feature name from TASKS.md>` | `Homework 1: Banking Transactions API` |

When a future Claude instance needs to fill template values, this file is the first place to check.
