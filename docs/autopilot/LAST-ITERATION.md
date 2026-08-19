# Autopilot Iteration 40

## Summary

Added 56 E2E tests covering 9 previously untested pages: change password, forgot password, MFA security, holiday CRUD, backup status, notification templates, API key management, sync log, and error log.

## What Was Done

Phase 3 — testing untested pages. Added tests navigating to 9 of the 17 untested page routes:

| Page | Route | Spec File | Tests Added |
|---|---|---|---|
| Đổi mật khẩu | `/doi-mat-khau` | `01-xac-thuc.spec.ts` | 8 tests (UI form, validation, API, auth) |
| Quên mật khẩu | `/quen-mat-khau` | `01-xac-thuc.spec.ts` | 6 tests (step wizard, API, responsive) |
| Bảo mật tài khoản | `/bao-mat-tai-khoan` | `01-xac-thuc.spec.ts` | 5 tests (MFA status, API, auth) |
| Ngày nghỉ lễ | `/quan-tri/danh-muc/ngay-nghi-le` | `14-quan-tri.spec.ts` | 8 tests (UI, CRUD, year filter, auth) |
| Sao lưu | `/quan-tri/sao-luu` | `14-quan-tri.spec.ts` | 5 tests (UI, API fields, auth) |
| Mẫu thông báo | `/quan-tri/mau-thong-bao` | `14-quan-tri.spec.ts` | 7 tests (UI table/columns, API, events, auth) |
| Khoá API ngoài | `/quan-tri/khoa-api` | `14-quan-tri.spec.ts` | 7 tests (UI, CRUD, validation, auth) |
| Nhật ký đồng bộ | `/quan-tri/nhat-ky/dong-bo` | `14-quan-tri.spec.ts` | 3 tests (UI, API, auth) |
| Nhật ký lỗi | `/quan-tri/nhat-ky-loi` | `14-quan-tri.spec.ts` | 7 tests (UI, filters, pagination, auth) |

## Quality Gate

- 856/856 E2E tests PASS (6.6 minutes)
- No regressions in existing tests
- REQ coverage: 49/49 testable REQs covered

## E2E Progress: 14/15 spec files (49/49 testable REQs)

- ✅ 01-xac-thuc (58 tests) — REQ-21 + đổi mật khẩu, quên mật khẩu, MFA
- ✅ 02-danh-muc (89 tests) — REQ-01 to REQ-08
- ✅ 03-sang-kien (58 tests) — REQ-09, REQ-11, REQ-12, REQ-22, REQ-26
- ✅ 04-quy-trinh-tieu-chi (51 tests) — REQ-10, REQ-14, REQ-15, REQ-16
- ✅ 05-hoi-dong-danh-gia (59 tests) — REQ-13, REQ-17, REQ-18, REQ-19, REQ-20
- ✅ 06-bao-cao-cong-khai (92 tests) — REQ-23 to REQ-25, REQ-34 to REQ-40
- ✅ 07-xu-ly (50 tests) — REQ-27 to REQ-30
- ✅ 08-quyet-dinh (42 tests) — REQ-31, REQ-32, REQ-36
- ✅ 09-danh-gia (44 tests) — REQ-33 to REQ-35
- ✅ 10-tra-cuu (45 tests) — REQ-37
- ✅ 11-bao-cao (43 tests) — REQ-38 to REQ-40
- ⬜ 12-tich-hop (BLOCKED_EXTERNAL — SSO/IOC)
- ✅ 13-di-dong (40 tests) — REQ-42
- ✅ 14-quan-tri (139 tests) — REQ-43 to REQ-48, REQ-50, REQ-51 + 6 untested pages
- ✅ 15-luong-nghiep-vu (46 tests)

## Pages Covered This Iteration (9 of 17 untested)

1. ✅ `/doi-mat-khau` — TrangDoiMatKhau
2. ✅ `/bao-mat-tai-khoan` — TrangBaoMatTaiKhoan
3. ✅ `/quen-mat-khau` — TrangQuenMatKhau
4. ⬜ `/bao-cao/tuy-bien` — TrangBaoCaoTuyBien
5. ✅ `/quan-tri/danh-muc/ngay-nghi-le` — TrangNgayNghiLe
6. ✅ `/quan-tri/sao-luu` — TrangSaoLuu
7. ✅ `/quan-tri/mau-thong-bao` — TrangMauThongBao
8. ✅ `/quan-tri/khoa-api` — TrangKhoaApiNgoai
9. ✅ `/quan-tri/nhat-ky/dong-bo` — TrangNhatKyDongBo
10. ✅ `/quan-tri/nhat-ky-loi` — TrangNhatKyLoi
11. ⬜ `/danh-gia/:id/cham-diem` — TrangChamDiem
12. ⬜ `/quan-tri/danh-muc/dot-de-nghi/:id` — TrangChiTietDot
13. ⬜ `/quan-tri/quy-trinh/:id/thiet-ke` — TrangThietKeQuyTrinh
14. ⬜ `/quan-tri/quy-trinh/:id/thanh-phan` — TrangThanhPhanHoSo
15. ⬜ `/quan-tri/quy-trinh/:id/lien-thong` — TrangLienThongBuoc
16. ⬜ `/quan-tri/tieu-chi/:id` — TrangCauHinhTieuChi
17. ⬜ `/sang-kien/:id` — TrangChiTietHoSo

## Files Changed

- `tests/BlueIdea.E2eTests/helpers/constants.ts` — +9 API endpoints
- `tests/BlueIdea.E2eTests/specs/01-xac-thuc.spec.ts` — +19 tests (đổi mật khẩu, quên mật khẩu, MFA)
- `tests/BlueIdea.E2eTests/specs/14-quan-tri.spec.ts` — +37 tests (6 admin pages)
- `docs/autopilot/STATE.json` — iteration 40 state
- `docs/autopilot/LAST-ITERATION.md` — this file

## Next Priority

- Continue Phase 3: test remaining 8 untested pages (bao-cao/tuy-bien, cham-diem, dot-de-nghi detail, quy-trinh design tabs, tieu-chi detail, sang-kien detail)
- These require dynamic IDs (need to fetch a record first then navigate to its detail page)
