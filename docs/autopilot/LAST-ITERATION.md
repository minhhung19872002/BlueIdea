# Autopilot Iteration 33

## Summary

Added 3 new Playwright E2E spec files (84 tests) covering processing, decisions, and search. Fixed a server crash bug caused by emoji characters in search queries.

## New E2E Spec Files

| Spec | Tests | Requirements |
|---|---|---|
| `07-xu-ly.spec.ts` | 29 | REQ-27 (tiếp nhận), REQ-28 (danh sách), REQ-29 (xử lý), REQ-30 (theo dõi) |
| `08-quyet-dinh.spec.ts` | 23 | REQ-31 (đính kèm quyết định), REQ-32 (kết quả), REQ-36 (merged) |
| `10-tra-cuu.spec.ts` | 32 | REQ-37 (tra cứu, tìm kiếm, cổng công khai) |

## Test Categories Covered

Each spec covers:
- Frontend UI tests (page load, title, search bar, table)
- Backend API tests (GET list, pagination, sorting, filtering, CRUD)
- Authorization tests (401 unauthenticated, 403 wrong role)
- Edge cases (XSS, SQL injection, emoji, special characters, boundary pagination)

## Bug Fix

**VanBanTiengViet.BoDau emoji crash** — `String.Normalize()` throws `ArgumentException` on unpaired UTF-16 surrogates (emoji like 🎓 are stored as surrogate pairs in C#). Fixed by adding `char.IsSurrogate(kyTu) continue;` guard before the `.Normalize()` call. Emoji characters are correctly stripped from the accent-free search text since they can never match Vietnamese search indices.

- File: `src/BlueIdea.Shared/TiengViet/VanBanTiengViet.cs` line 105
- Affects: all search queries that contain emoji or non-BMP Unicode characters

## E2E Progress: 10/15 spec files complete

- ✅ 01-xac-thuc (18 tests)
- ✅ 02-danh-muc (14 tests)
- ✅ 03-sang-kien (18 tests)
- ✅ 04-quy-trinh-tieu-chi (15 tests)
- ✅ 05-hoi-dong-danh-gia (21 tests)
- ✅ 06-bao-cao-cong-khai (18 tests)
- ✅ 07-xu-ly (29 tests) — NEW
- ✅ 08-quyet-dinh (23 tests) — NEW
- ⬜ 09-danh-gia (REQ-33 to REQ-35 — partially covered in 05)
- ✅ 10-tra-cuu (32 tests) — NEW
- ⬜ 11-bao-cao (REQ-38 to REQ-40 — partially covered in 06)
- ⬜ 12-tich-hop (BLOCKED_EXTERNAL)
- ⬜ 13-di-dong (REQ-42 — mobile responsive)
- ✅ 14-quan-tri (31 tests)
- ⬜ 15-luong-nghiep-vu (full lifecycle)

## Quality Gate

- 219/219 E2E tests PASS
- 501/501 unit tests PASS
- No regressions

## Files Changed

- `tests/BlueIdea.E2eTests/specs/07-xu-ly.spec.ts` — 29 E2E tests (NEW)
- `tests/BlueIdea.E2eTests/specs/08-quyet-dinh.spec.ts` — 23 E2E tests (NEW)
- `tests/BlueIdea.E2eTests/specs/10-tra-cuu.spec.ts` — 32 E2E tests (NEW)
- `src/BlueIdea.Shared/TiengViet/VanBanTiengViet.cs` — emoji surrogate guard in BoDau

## Next Priority

- P1 E2E: Write remaining spec files (09-danh-gia, 13-di-dong, 15-luong-nghiep-vu)
- P2: Verify REQ-12 and REQ-42 to move toward READY_FOR_DEPLOY

## Blockers

None.
