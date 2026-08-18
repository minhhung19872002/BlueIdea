# Git & Multi-Developer Safety

This repository is actively developed by multiple people and Claude sessions.

## Rules

- **Never** reset unrelated work or force push unless explicitly authorized.
- **Never** automatically commit unrelated files.
- **Inspect** `git status` and `git diff` before committing or any destructive operation.
- **Stash** uncommitted changes (with `-u` for untracked) before operations that could discard them.
- Keep commits logically scoped — one concern per commit.
- No direct push to main unless explicitly authorized.
- No auto-merge into protected branches.

## Branch Naming Convention for AI Work

```
ai/req-{number}-{short-description}
ai/security-audit
ai/release-audit
ai/fix-{issue-description}
```

Examples:
```
ai/req-009-workflow-versioning
ai/req-026-similarity-pipeline
ai/security-audit-2026-08
```

## Before Modifying Code

1. Run `git status` to check for uncommitted work.
2. If uncommitted changes exist from others: stash or work around them, never discard.
3. Identify directly changed files and affected shared dependencies.
4. After broad `git add`, review with `git status` — check suspicious filenames for secrets.

## PR Convention

- Title: under 70 chars, describes the change.
- Body: Summary bullets + test plan + `Co-Authored-By: Claude Opus 4.6 <noreply@anthropic.com>`.
- Link to requirement ID when applicable.

## Prohibited

- Do NOT commit secrets, credentials, or `.env` files.
- Do NOT `git reset --hard` without checking for uncommitted work first.
- Do NOT use `--no-verify` or skip hooks.
- Do NOT push to main without PR review.
