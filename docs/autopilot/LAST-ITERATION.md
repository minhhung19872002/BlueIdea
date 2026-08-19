# Autopilot Iteration 35

## Summary

Added `15-luong-nghiep-vu.spec.ts` — the full lifecycle cross-cutting E2E test with 35 tests. This completes the non-blocked E2E spec files (14/15 done, with 12-tich-hop BLOCKED_EXTERNAL for SSO/IOC).

## New E2E Spec File

| Spec | Tests | Requirements |
|---|---|---|
| `15-luong-nghiep-vu.spec.ts` | 35 | REQ-09 (tạo hồ sơ), REQ-27 (tiếp nhận), REQ-29 (xử lý), REQ-33 (phân công chấm), REQ-34 (chấm điểm), REQ-35 (tổng hợp), REQ-37 (ban hành quyết định) |

## Test Categories

**Luồng đạt — full lifecycle (14 tests):**
- Lookup catalogs (đợt, lĩnh vực, hội đồng)
- Author creates, uploads MINH_CHUNG, submits → DA_NOP
- B1: Reception officer accepts
- B2: Secretary reviews
- B3: Secretary assigns scoring + transitions
- B4: All 7 council members score + execute TAT_CA workflow step
- B4→B5: Verify step advanced past scoring
- B5: Chair aggregates scores + concludes
- B6: Leader issues decision → DA_PHE_DUYET
- Progress timeline shows all milestones
- Processing history records correct actors
- Public search returns approved applications

**Alternative flows (11 tests):**
- B1 rejection: TRA_LAI → KHONG_DAT
- B1 supplement request: BO_SUNG_HO_SO → YEU_CAU_BO_SUNG
- Author withdrawal after reception

**Cross-cutting authorization (6 tests):**
- Author cannot execute workflow steps
- Unauthenticated access → 401 (thuc-thi, nộp, phân công)
- Author cannot assign scoring → 403
- Author cannot aggregate scores → 403

**Edge cases (4 tests):**
- Invalid truongHopId → 400/404
- Empty payload → 400/422
- Non-existent sang-kien submission → 400/404
- Actions for non-existent sang-kien → empty/404

## Key Technical Findings

- B4 (CHAM_DIEM) uses `QuyTacXuLy = TAT_CA`: each assigned council member must call both `POST /danh-gia/phieu/gui` (submit score) AND `POST /xu-ly/thuc-thi` (execute workflow step). Scoring alone doesn't advance the workflow.
- The `SoTacNhanDuKien` is computed from `SangKienPhanCong` count, not total council size. With `tuDongChiaDeu: true`, the actual assignment count may differ from total members.
- Token caching with retry logic needed for rate-limit resilience in full-suite runs.
- Public search API is at `/api/v1/cong-khai/sang-kien`, not `/api/v1/tra-cuu`.

## E2E Progress: 14/15 spec files complete

- ✅ 01-xac-thuc (18 tests)
- ✅ 02-danh-muc (14 tests)
- ✅ 03-sang-kien (18 tests)
- ✅ 04-quy-trinh-tieu-chi (15 tests)
- ✅ 05-hoi-dong-danh-gia (21 tests)
- ✅ 06-bao-cao-cong-khai (18 tests)
- ✅ 07-xu-ly (29 tests)
- ✅ 08-quyet-dinh (23 tests)
- ✅ 09-danh-gia (34 tests)
- ✅ 10-tra-cuu (32 tests)
- ✅ 11-bao-cao (26 tests)
- ⬜ 12-tich-hop (BLOCKED_EXTERNAL — SSO/IOC)
- ✅ 13-di-dong (23 tests)
- ✅ 14-quan-tri (31 tests)
- ✅ 15-luong-nghiep-vu (35 tests) — NEW

## Quality Gate

- 337/337 E2E tests PASS
- 501/501 unit tests PASS
- No regressions

## Files Changed

- `tests/BlueIdea.E2eTests/specs/15-luong-nghiep-vu.spec.ts` — 35 E2E tests (NEW)
- `docs/autopilot/STATE.json` — iteration 35 state
- `docs/autopilot/LAST-ITERATION.md` — this file

## Blockers

- REQ-41 (tích hợp SSO/IOC) blocked on external SSO endpoint availability

## Next Priority

- All non-blocked E2E spec files complete
- Consider integration tests or P2 items
- Push to origin when ready
