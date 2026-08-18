# Audit Artifacts

This directory contains governance artifacts for tracking project quality and readiness.

## Canonical Locations

| Artifact | Location | Purpose |
|---|---|---|
| Requirement traceability | `docs/requirements/traceability.yaml` | Machine-readable requirement status |
| Deployment status | `docs/TRANG-THAI-TRIEN-KHAI.md` | Human-readable status per function (canonical) |
| Acceptance scenarios | `docs/KICH-BAN-NGHIEM-THU.md` | Acceptance test scripts |
| Production readiness | `docs/audit/production-readiness.md` | Latest readiness assessment |
| External blockers | `docs/audit/external-blockers.md` | Dependencies on external parties |
| Technical debt | `docs/audit/technical-debt.md` | Known limitations and debt items |

## How to Use

### Running a Requirement Audit

Use the `requirement-auditor` agent:
```
Audit requirement #9 (Cau hinh quy trinh) against implementation
```

### Running a Release Audit

Use the `release-auditor` agent:
```
Run production readiness audit for the current state
```

### Updating Traceability

After implementing or verifying a requirement:
1. Update `docs/requirements/traceability.yaml` with new status and evidence
2. Update `docs/TRANG-THAI-TRIEN-KHAI.md` if deployment status changed
3. Commit both changes together

## Status Values

See `.claude/rules/requirement-governance.md` for the full status model and evidence requirements.
