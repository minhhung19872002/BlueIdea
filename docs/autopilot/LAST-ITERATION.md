# Iteration 3 — P0 Security: REQ-26 Similarity IDOR Fixes

## What Was Worked On

Two IDOR vulnerabilities in `DichVuKiemTraTrungLap` (REQ-26, similarity checking service):

1. **ChayAsync IDOR (HIGH)**: `POST /api/v1/sang-kien/{id}/trung-lap/chay-lai` allowed any user with `TrungLapChayLai` permission to re-run similarity check on any sangKien across all organizations. Added conditional org-scope check using `DaXacThuc` sentinel (safe for Hangfire background job caller which has no HTTP context).

2. **DanhDauDaXemXetAsync latent IDOR (MEDIUM)**: Could mark any `KiemTraTrungLap` record as reviewed and overwrite council opinion regardless of org scope. Added `BatBuocTrongPhamViSangKienAsync` after resolving parent `SangKienId` from the fetched record. Also fixed incorrect `BatBuocCoQuyenAsync` signature (was passing `kiemTraId` as `doiTuongId`).

## What Was Accomplished

- Both IDOR vulnerabilities fixed following established pattern from `LayKetQuaGanNhatAsync`
- Code review: confirmed both fixes mechanically correct, no regressions
- Security review: confirmed both fixes effective, no bypass possible
- Security reviewer recommended `DaXacThuc` over `_nguoiDung.Id is not null` as sentinel — adopted
- Quality gate: PASS (7/7 checks, 269 unit tests, 0 warnings)
- Traceability updated: REQ-26 SEC gaps removed, pre-existing gaps documented

## Quality Gate Result

PASS — 7/7 checks, 269 unit tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Application/TrungLap/DichVuKiemTraTrungLap.cs` — Both IDOR fixes
- `docs/requirements/traceability.yaml` — REQ-26 gap updates
- `docs/autopilot/STATE.json` — iteration state
- `docs/autopilot/LAST-ITERATION.md` — this file

## Commit Hash

93dfdf7

## Next Priority Items

Remaining HIGH security issues (REQ-21):
- SEC: Account enumeration via distinct login error messages (HIGH)
- SEC: SSO state parameter not validated server-side (HIGH)
- SEC: Open redirect in duongDanTraVe (HIGH)

Pre-existing lower-priority items from reviews:
- REQ-26: DanhDauDaXemXetAsync uses TrungLapXem (view) for a write op — needs dedicated permission when endpoint is wired
- REQ-26: No integration test for POST chay-lai cross-org denial
- REQ-26: ThongBaoLoi raw exception messages exposed in API response (info disclosure)

## Known Limitations

- DanhDauDaXemXetAsync is not wired to any HTTP endpoint — the IDOR fix is latent (correct but unreachable until endpoint is created)
- No integration test for the ChayAsync IDOR fix (env lacks full .NET runtime for integration tests)
- Pre-existing: DanhDauDaXemXetAsync uses view permission (TrungLapXem) for a write operation

## Blockers Discovered

None.
