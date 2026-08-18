# Requirement Traceability

## Overview

This directory contains machine-readable requirement tracking for BlueIdea's 51 customer functions.

## Files

- `traceability.yaml` — Status of each requirement with evidence pointers
- This `README.md` — How to use the traceability model

## Authoritative Sources

1. `docs/Chuong V - Yeu cau ve ky thuat (1).pdf` — Original customer requirement
2. `docs/00-MASTER-SPEC.md` — Normalized specification
3. `docs/TRANG-THAI-TRIEN-KHAI.md` — Deployment status (human-readable canonical)

## Status Values

| Status | Meaning |
|---|---|
| `NOT_ASSESSED` | Not yet independently audited |
| `MISSING` | No implementation found |
| `PARTIAL` | Some layers exist, gaps remain |
| `IMPLEMENTED_NOT_VERIFIED` | Code exists but no independent runtime verification |
| `VERIFIED` | All applicable layers have evidence from runtime testing |
| `BLOCKED_EXTERNAL` | Depends on external input |
| `NOT_APPLICABLE` | Does not apply |

## How to Update

1. Run the `requirement-auditor` agent to assess a requirement
2. Update `traceability.yaml` with findings
3. Keep `TRANG-THAI-TRIEN-KHAI.md` in sync (it remains the human-readable canonical)
4. Commit both together

## Evidence Model

Every assessed requirement should include:
- Frontend routes and source files
- Backend endpoints and source files
- Database evidence (tables, migrations)
- Authorization evidence (policies, pipeline)
- Test evidence (test names, results)
- Known gaps
- Last verified commit

## Important

- `IMPLEMENTED_NOT_VERIFIED` is the honest default when code exists but no independent runtime audit has been performed
- The `requirement-auditor` agent must verify requirements independently — not from previous summaries
- Implementation must NOT override customer requirements. Conflicts are recorded, not silently resolved.
