# Autopilot Iteration 34

## Summary

Added 3 new Playwright E2E spec files (83 tests) covering evaluation/scoring, reports/statistics, and mobile responsive features.

## New E2E Spec Files

| Spec | Tests | Requirements |
|---|---|---|
| `09-danh-gia.spec.ts` | 34 | REQ-33 (phân công chấm), REQ-34 (phiếu chấm điểm), REQ-35 (tổng hợp, ma trận điểm) |
| `11-bao-cao.spec.ts` | 26 | REQ-38 (sáng kiến đạt), REQ-39 (chưa đạt), REQ-40 (theo đơn vị/tác giả) |
| `13-di-dong.spec.ts` | 23 | REQ-42 (responsive web: mobile 375px, min 320px, tablet 768px, desktop 1280px) |

## Test Categories Covered

**09-danh-gia (REQ-33–35):**
- API: viec-cua-toi (list, pagination, status filter), phan-cong (assignment), phieu (scoring form fetch/save/submit), mo-lai (reopen), tong-hop (aggregate), ma-tran-diem (score matrix), ky-so (digital sign), lich-su-ky-so
- Authorization: 401 unauthenticated, 403 for tacgia role, assignment-level checks
- UI: page loads, title, filter dropdown, table/empty state
- Edge cases: boundary pagination, non-existent IDs

**11-bao-cao (REQ-38–40):**
- API: tong-quan dashboard, sang-kien-dat, sang-kien-chua-dat, theo-don-vi, theo-tac-gia, thoi-gian-xu-ly, tong-hop-nam
- Export: Excel (xuat-excel with content-type check), PDF (xuat-pdf with content-type check)
- Authorization: 401 unauthenticated, 403 for tacgia
- Edge cases: non-existent year returns empty/zero

**13-di-dong (REQ-42):**
- Mobile (375px): hamburger visible, sidebar hidden, drawer opens/closes, search hidden, 6 pages load
- Minimum (320px): login page, dashboard, public portal all load
- Tablet (768px): still mobile layout (hamburger, no sidebar)
- Desktop (1280px): sidebar visible, hamburger hidden, avatar+name visible, nav in sider

## E2E Progress: 13/15 spec files complete

- ✅ 01-xac-thuc (18 tests)
- ✅ 02-danh-muc (14 tests)
- ✅ 03-sang-kien (18 tests)
- ✅ 04-quy-trinh-tieu-chi (15 tests)
- ✅ 05-hoi-dong-danh-gia (21 tests)
- ✅ 06-bao-cao-cong-khai (18 tests)
- ✅ 07-xu-ly (29 tests)
- ✅ 08-quyet-dinh (23 tests)
- ✅ 09-danh-gia (34 tests) — NEW
- ✅ 10-tra-cuu (32 tests)
- ✅ 11-bao-cao (26 tests) — NEW
- ⬜ 12-tich-hop (BLOCKED_EXTERNAL — SSO/IOC)
- ✅ 13-di-dong (23 tests) — NEW
- ✅ 14-quan-tri (31 tests)
- ⬜ 15-luong-nghiep-vu (full lifecycle — remaining)

## Quality Gate

- 302/302 E2E tests PASS
- 501/501 unit tests PASS
- No regressions

## Files Changed

- `tests/BlueIdea.E2eTests/specs/09-danh-gia.spec.ts` — 34 E2E tests (NEW)
- `tests/BlueIdea.E2eTests/specs/11-bao-cao.spec.ts` — 26 E2E tests (NEW)
- `tests/BlueIdea.E2eTests/specs/13-di-dong.spec.ts` — 23 E2E tests (NEW)

## Commit

`ac4d51d` — test: add 83 E2E tests (danh-gia, bao-cao, di-dong) covering REQ-33–35, REQ-38–40, REQ-42

## Next Priority

- P1 E2E: Write `15-luong-nghiep-vu.spec.ts` (full lifecycle cross-cutting test)
- `12-tich-hop` remains BLOCKED_EXTERNAL (SSO/IOC integration)
- Push to origin when ready

## Blockers

- REQ-41 (tích hợp SSO/IOC) blocked on external SSO endpoint availability
