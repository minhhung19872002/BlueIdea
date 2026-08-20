# Autopilot Iteration 41

## Summary

Added 71 E2E tests covering the 8 remaining untested pages that require dynamic IDs (detail/config pages). All 46 navigable pages now have at least one E2E test. Total: 927 tests, all passing.

## What Was Done

Phase 3 — complete page coverage. Added tests for the final 8 untested page routes:

| Page | Route | Spec File | Tests Added |
|---|---|---|---|
| TrangChiTietHoSo | `/sang-kien/:id` | `03-sang-kien.spec.ts` | 11 tests |
| TrangChamDiem | `/danh-gia/:id/cham-diem` | `09-danh-gia.spec.ts` | 10 tests |
| TrangThietKeQuyTrinh | `/quan-tri/quy-trinh/:id/thiet-ke` | `04-quy-trinh-tieu-chi.spec.ts` | 8 tests |
| TrangThanhPhanHoSo | `/quan-tri/quy-trinh/:id/thanh-phan` | `04-quy-trinh-tieu-chi.spec.ts` | 8 tests |
| TrangLienThongBuoc | `/quan-tri/quy-trinh/:id/lien-thong` | `04-quy-trinh-tieu-chi.spec.ts` | 8 tests |
| TrangCauHinhTieuChi | `/quan-tri/tieu-chi/:id` | `04-quy-trinh-tieu-chi.spec.ts` | 10 tests |
| TrangBaoCaoTuyBien | `/bao-cao/tuy-bien` | `06-bao-cao-cong-khai.spec.ts` | 8 tests |
| TrangChiTietDot | `/quan-tri/danh-muc/dot-de-nghi/:id` | `02-danh-muc.spec.ts` | 8 tests |

## Fixes Applied

- Fixed `apiRequest` response parsing (`.json()` before accessing `.duLieu`) in 09-danh-gia and 03-sang-kien specs
- Fixed tacgia authorization tests: use API-level 403 checks instead of UI redirect (frontend doesn't enforce route-level auth)
- Fixed invalid CSS+text= mixed selectors (`.ant-empty, text=...` → separate locator checks)
- Fixed `apiRequest` SecurityError before login (navigate to `/` and login first)
- Fixed strict mode violations (`.first()` for non-unique locators)
- Fixed strict mode in 14-quan-tri.spec.ts (`.getByRole('main').getByText()` for ambiguous text)

## Quality Gate

- 927/927 E2E tests PASS (7.6 minutes)
- No regressions in existing tests
- REQ coverage: 49/49 testable REQs covered
- Page coverage: 46/46 navigable pages covered (all)

## E2E Progress: 14/15 spec files (49/49 testable REQs, 46/46 pages)

- 01-xac-thuc (58 tests) — REQ-21 + auth pages
- 02-danh-muc (97 tests) — REQ-01 to REQ-08 + TrangChiTietDot
- 03-sang-kien (69 tests) — REQ-09, REQ-11, REQ-12, REQ-22, REQ-26 + TrangChiTietHoSo
- 04-quy-trinh-tieu-chi (85 tests) — REQ-10, REQ-14, REQ-15, REQ-16 + design/config pages
- 05-hoi-dong-danh-gia (59 tests) — REQ-13, REQ-17, REQ-18, REQ-19, REQ-20
- 06-bao-cao-cong-khai (100 tests) — REQ-23 to REQ-25, REQ-34 to REQ-40 + TrangBaoCaoTuyBien
- 07-xu-ly (50 tests) — REQ-27 to REQ-30
- 08-quyet-dinh (42 tests) — REQ-31, REQ-32, REQ-36
- 09-danh-gia (54 tests) — REQ-33 to REQ-35 + TrangChamDiem
- 10-tra-cuu (45 tests) — REQ-37
- 11-bao-cao (43 tests) — REQ-38 to REQ-40
- 12-tich-hop (BLOCKED_EXTERNAL — SSO/IOC)
- 13-di-dong (40 tests) — REQ-42
- 14-quan-tri (139 tests) — REQ-43 to REQ-48, REQ-50, REQ-51
- 15-luong-nghiep-vu (46 tests)

## Files Changed

- `tests/BlueIdea.E2eTests/specs/02-danh-muc.spec.ts` — +8 tests (TrangChiTietDot)
- `tests/BlueIdea.E2eTests/specs/03-sang-kien.spec.ts` — +11 tests (TrangChiTietHoSo)
- `tests/BlueIdea.E2eTests/specs/04-quy-trinh-tieu-chi.spec.ts` — +34 tests (4 config pages)
- `tests/BlueIdea.E2eTests/specs/06-bao-cao-cong-khai.spec.ts` — +8 tests (TrangBaoCaoTuyBien)
- `tests/BlueIdea.E2eTests/specs/09-danh-gia.spec.ts` — +10 tests (TrangChamDiem)
- `tests/BlueIdea.E2eTests/specs/14-quan-tri.spec.ts` — strict mode fix
- `docs/autopilot/STATE.json` — iteration 41 state
- `docs/autopilot/LAST-ITERATION.md` — this file

## Next Priority

All pages and REQs are covered. Future work:
- Deepen existing tests with more interaction testing
- Add more edge case and boundary tests
- Consider testing concurrent user scenarios
