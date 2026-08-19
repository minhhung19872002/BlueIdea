# Autopilot Iteration 20

## Summary

Fixed dual-permission gate inconsistency in DonVi (organization unit) service. The `DonViController` correctly used `DON_VI.XEM` and `DON_VI.CAU_HINH` policies, but the inherited `DichVuDanhMucCoSo` base class checked `DANH_MUC.XEM/THEM/SUA/XOA` at the service layer. Users needed permissions from both families to perform any DonVi operation.

## Changes

### DichVuDanhMucCoSo (src/BlueIdea.Application/DanhMuc/DichVuDanhMucCoSo.cs)

- Added 4 virtual properties: `QuyenXem`, `QuyenThem`, `QuyenSua`, `QuyenXoa` (default to `DANH_MUC.*`)
- Replaced all 7 hardcoded `MaQuyen.DanhMuc*` references in method bodies with the virtual properties
- Other catalog services (LinhVuc, DoiTuong, LoaiTacGia, QuyetDinh) continue using defaults unchanged

### DichVuDonVi (src/BlueIdea.Application/DanhMuc/DichVuDanhMuc.cs)

- Overrode all 4 properties: `QuyenXem` → `DonViXem`, `QuyenThem/Sua/Xoa` → `DonViCauHinh`
- Now aligned with controller-level `[Authorize(Policy)]` attributes

### Unit tests (tests/BlueIdea.UnitTests/DanhMuc/DichVuDonViPhamViTests.cs)

5 new tests verifying permission alignment:
- LayDanhSach checks DON_VI.XEM (not DANH_MUC.XEM)
- LayTheoId checks DON_VI.XEM (not DANH_MUC.XEM)
- Them checks DON_VI.CAU_HINH (not DANH_MUC.THEM)
- CapNhat checks DON_VI.CAU_HINH (not DANH_MUC.SUA)
- Xoa checks DON_VI.CAU_HINH (not DANH_MUC.XOA)

## Quality Gate

- Result: PASS (7/7)
- Unit tests: 493 (up from 482)
- Warnings: 0

## Requirements Affected

- REQ-05 (Don vi phe duyet) — MEDIUM security gap closed (dual-permission gate removed from gaps)
- REQ-44 (Quan ly don vi, to chuc) — note added documenting the fix

## Files Changed

- `src/BlueIdea.Application/DanhMuc/DichVuDanhMucCoSo.cs` (modified)
- `src/BlueIdea.Application/DanhMuc/DichVuDanhMuc.cs` (modified)
- `tests/BlueIdea.UnitTests/DanhMuc/DichVuDonViPhamViTests.cs` (modified)
- `docs/requirements/traceability.yaml` (updated)

## Commit

33abd4d

## Next Priority

TD-005: DoiTuongId discarded in KiemTraQuyenAsync — architectural decision needed (implement object-level scope in pipeline, or remove DoiTuongId and document per-service IDOR responsibility).

## Blockers

None discovered.
