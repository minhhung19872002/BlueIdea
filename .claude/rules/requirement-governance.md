# Requirement Governance

## Authoritative Sources (Priority Order)

1. **Customer technical requirement PDF**: `docs/Chuong V - Yeu cau ve ky thuat (1).pdf` — 51 numbered functions
2. **Normalized master spec**: `docs/00-MASTER-SPEC.md` — detailed breakdown with data model
3. **ADRs**: `docs/ADR/` — architectural decisions with rationale
4. **Implementation status**: `docs/TRANG-THAI-TRIEN-KHAI.md` — current deployment state per function
5. **Acceptance scenarios**: `docs/KICH-BAN-NGHIEM-THU.md` — acceptance test scripts
6. **Security doc**: `docs/AN-TOAN-THONG-TIN.md` — Level 2 security requirements

Implementation must NOT override a customer requirement. If documents conflict, record the conflict.

## Requirement Status Values

| Status | Meaning |
|---|---|
| `NOT_ASSESSED` | Not yet audited |
| `MISSING` | No implementation found |
| `PARTIAL` | Some layers implemented, gaps remain |
| `IMPLEMENTED_NOT_VERIFIED` | Code exists but not runtime-verified |
| `VERIFIED` | All applicable layers have evidence |
| `BLOCKED_EXTERNAL` | Depends on external input (e.g., SSO endpoint, real CA certificate) |
| `NOT_APPLICABLE` | Does not apply to this deployment |

**Forbidden** status values: "mostly done", "almost done", "probably done", "looks good".

## Definition of VERIFIED

A requirement cannot be VERIFIED simply because source files exist. Every applicable layer must have evidence:

```
Requirement → UI → API → Application/Domain Logic → Persistence → Authorization → Integration → Tests → Runtime evidence
```

Not every requirement needs every layer, but every applicable layer must be verified.

## Evidence Model

Every verified requirement records:
- Requirement ID (mapping to 51 functions in customer PDF)
- Frontend routes and source paths
- Backend endpoints and source paths
- Database tables
- Authorization policy
- Test evidence (unit, integration, E2E)
- Known gaps or limitations
- Last verified Git commit
- Verification date

## Traceability

- `docs/requirements/traceability.yaml` — machine-readable requirement status
- `docs/TRANG-THAI-TRIEN-KHAI.md` — human-readable deployment status (canonical, do not duplicate)
- `docs/KICH-BAN-NGHIEM-THU.md` — acceptance test scenarios (canonical)

## Continuous Development Workflow

For any requirement task:

1. Select requirement from traceability
2. Read authoritative requirement (PDF → master spec → ADR)
3. Run requirement auditor agent
4. Inspect current implementation
5. Determine gap
6. Design solution (solution architect agent if complex)
7. Implement in focused scope
8. Add/update tests
9. Run relevant checks
10. QA review
11. Code review
12. Security review if applicable
13. Requirement re-audit
14. Update evidence and status
15. Create PR

No requirement automatically becomes VERIFIED just because code was written.
