# Autopilot Iteration 18

## Summary

Closed remaining controller-level `[Authorize(Policy)]` defense-in-depth gaps across 8 controllers, adding 51 action-level policy attributes. This continues the pattern from iteration 17 which covered catalog and org management controllers.

## Changes

### Controllers modified (51 policy additions total)

| Controller | Policies Added | Permission Codes |
|---|---|---|
| BieuMauXuatController | 8 | DanhMucXem, DanhMucThem, DanhMucSua, DanhMucXoa |
| BieuMauThongKeController | 5 | DanhMucXem, DanhMucThem, DanhMucSua, DanhMucXoa |
| BaoCaoController | 7 | BaoCaoXem (7 read endpoints; export already had BaoCaoXuat) |
| SangKienController | 14 | SangKienXem, SangKienThem, SangKienSua, SangKienNop, SangKienRut, TrungLapXem |
| QuyTrinhController | 5 | QuyTrinhXem |
| TieuChiController | 3 | TieuChiXem |
| HoiDongController | 4 | HoiDongXem |
| BienBanHopController | 4 | HoiDongXem |
| DanhGiaController | 1 | DanhGiaChamDiem |

### Dropdown endpoints (by design — bare [Authorize])

All `LayDanhSachChonAsync` ("chon") endpoints kept bare `[Authorize]` without policy — they serve dropdown/selection data for any authenticated user.

### Test coverage

Expanded `ChinhSachPhanQuyenControllerTests.cs` from 45 to 107+ parameterized tests covering all controller authorization policies via reflection. Added 5 new dropdown exemption test entries.

### By-design exclusions

- TepTinController: data-layer scope via `BatBuocTepTrongPhamViAsync`/`BatBuocHoSoTrongPhamViAsync`
- BoLocYeuThichController: personal data, cross-role
- MfaController: personal security, authenticated-only

## Quality Gate

- Result: PASS (7/7)
- Unit tests: 476 (up from 369)
- Warnings: 0

## Requirements Affected

REQ-06, REQ-07, REQ-09 through REQ-13, REQ-19, REQ-20, REQ-22, REQ-23, REQ-26, REQ-28 through REQ-30, REQ-33 through REQ-35, REQ-37 through REQ-40, REQ-44

## Next Priority

Resource-level scope validation in DichVuPhanQuyen (TD-005: DoiTuongId currently discarded in KiemTraQuyenAsync).
