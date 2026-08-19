# Autopilot Iteration 37

## Summary

Added 43 E2E tests for 9 previously missing REQs, achieving full coverage of all 49 testable requirements. Both the file-level grep check and the `npx playwright test --list` check now show only REQ-41 and REQ-49 (BLOCKED_EXTERNAL) as missing.

## What Was Done

Identified 7 REQs missing from E2E spec files via the canonical grep check, plus 2 more (REQ-11, REQ-32) missing from the `--list` check due to lacking dedicated `test.describe` blocks.

| REQ | Name | Spec File | Tests Added |
|---|---|---|---|
| REQ-05 | Đơn vị phê duyệt | `02-danh-muc.spec.ts` | 10 tests (tree UI, CRUD API, cấp phê duyệt, auth) |
| REQ-11 | Xem / Tìm kiếm sáng kiến | `03-sang-kien.spec.ts` | 3 tests (gợi ý, lịch sử, phân trang) |
| REQ-15 | Tác nhân xử lý | `04-quy-trinh-tieu-chi.spec.ts` | 4 tests (sơ đồ tác nhân, auth) |
| REQ-16 | Cấu hình liên thông | `04-quy-trinh-tieu-chi.spec.ts` | 6 tests (hệ thống, bước liên thông, nhật ký, auth) |
| REQ-19 | Danh sách hội đồng | `05-hoi-dong-danh-gia.spec.ts` | 3 tests (CRUD full cycle, detail UI, auth) |
| REQ-20 | Thành viên hội đồng | `05-hoi-dong-danh-gia.spec.ts` | 4 tests (member save, tabs UI, auth) |
| REQ-22 | Đăng ký nộp sáng kiến | `03-sang-kien.spec.ts` | 7 tests (wizard UI, draft API, tiến độ, auth) |
| REQ-26 | Kiểm tra trùng lặp | `03-sang-kien.spec.ts` | 4 tests (result API, chạy lại, auth) |
| REQ-32 | Kết quả sáng kiến | `08-quyet-dinh.spec.ts` | 2 tests (API list, UI) |

## Quality Gate

- 380/380 E2E tests PASS (3.7 minutes)
- No regressions in existing tests
- REQ coverage: 49/49 testable REQs covered (REQ-41 and REQ-49 BLOCKED_EXTERNAL)

## E2E Progress: 14/15 spec files complete (49/49 testable REQs)

- ✅ 01-xac-thuc (18 tests) — REQ-21
- ✅ 02-danh-muc (36 tests) — REQ-01 to REQ-08 + REQ-05
- ✅ 03-sang-kien (34 tests) — REQ-09, REQ-11, REQ-12, REQ-22, REQ-26
- ✅ 04-quy-trinh-tieu-chi (26 tests) — REQ-10, REQ-14, REQ-15, REQ-16
- ✅ 05-hoi-dong-danh-gia (22 tests) — REQ-13, REQ-17, REQ-18, REQ-19, REQ-20
- ✅ 06-bao-cao-cong-khai (18 tests) — REQ-23 to REQ-25, REQ-34 to REQ-40
- ✅ 07-xu-ly (29 tests) — REQ-27 to REQ-30
- ✅ 08-quyet-dinh (25 tests) — REQ-31, REQ-32, REQ-36
- ✅ 09-danh-gia (34 tests) — REQ-33 to REQ-35
- ✅ 10-tra-cuu (32 tests) — REQ-37
- ✅ 11-bao-cao (26 tests) — REQ-38 to REQ-40
- ⬜ 12-tich-hop (BLOCKED_EXTERNAL — SSO/IOC)
- ✅ 13-di-dong (23 tests) — REQ-42
- ✅ 14-quan-tri (31 tests) — REQ-43 to REQ-48, REQ-50, REQ-51
- ✅ 15-luong-nghiep-vu (35 tests)

## Files Changed

- `tests/BlueIdea.E2eTests/specs/02-danh-muc.spec.ts` — +145 lines (REQ-05)
- `tests/BlueIdea.E2eTests/specs/03-sang-kien.spec.ts` — +173 lines (REQ-11, REQ-22, REQ-26)
- `tests/BlueIdea.E2eTests/specs/04-quy-trinh-tieu-chi.spec.ts` — +115 lines (REQ-15, REQ-16)
- `tests/BlueIdea.E2eTests/specs/05-hoi-dong-danh-gia.spec.ts` — +149 lines (REQ-19, REQ-20)
- `tests/BlueIdea.E2eTests/specs/08-quyet-dinh.spec.ts` — +22 lines (REQ-32)
- `docs/autopilot/STATE.json` — iteration 37 state
- `docs/autopilot/LAST-ITERATION.md` — this file

## Next Priority

- All 49 implementable requirements are VERIFIED with runtime evidence
- All 49 testable REQs have E2E coverage (380 tests)
- Remaining work is blocked on external dependencies (REQ-41 SSO/IOC, REQ-49 CA certificate)
- Consider P2 gap-filling or P3 technical debt items
