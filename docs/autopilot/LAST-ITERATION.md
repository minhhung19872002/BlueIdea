# Autopilot Iteration 19

## Summary

Closed cross-tenant IDOR vulnerability in `DichVuDonVi.ChuyenChaAsync` and `GopAsync` by adding org-scope enforcement. Both methods had `DonViCauHinh` permission gates (added in iteration 17) but no check that the affected unit IDs fell within the caller's accessible organizations.

## Changes

### DichVuDonVi (src/BlueIdea.Application/DanhMuc/DichVuDanhMuc.cs)

- Added `INguoiDungHienTai` dependency to constructor
- Added `BatBuocDonViTrongPhamViAsync` private method — checks `donViId` against `PhamViTruyCap.DonViIds` via `LayPhamViTruyCapAsync`. ToanHeThong bypasses; ChiCaNhan or out-of-scope throws `KhongTimThayException`
- `ChuyenChaAsync`: scope-checks both the moved unit and the new parent
- `GopAsync`: scope-checks both source and destination units

### Unit tests (tests/BlueIdea.UnitTests/DanhMuc/DichVuDonViPhamViTests.cs)

6 tests covering:
- ChuyenCha with unit outside scope → throws
- ChuyenCha with new parent outside scope → throws
- Gop with source outside scope → throws
- Gop with destination outside scope → throws
- ChuyenCha with CA_NHAN scope → throws
- Gop with CA_NHAN scope → throws

## Quality Gate

- Result: PASS (7/7)
- Unit tests: 482 (up from 476)
- Warnings: 0

## Requirements Affected

REQ-44 (Quan ly don vi, to chuc) — MEDIUM security gap closed

## Files Changed

- `src/BlueIdea.Application/DanhMuc/DichVuDanhMuc.cs` (modified)
- `tests/BlueIdea.UnitTests/DanhMuc/DichVuDonViPhamViTests.cs` (new)
- `docs/requirements/traceability.yaml` (updated)

## Commit

052e897

## Next Priority

TD-005: DoiTuongId discarded in KiemTraQuyenAsync — architectural decision needed (implement object-level scope in pipeline, or remove DoiTuongId and document per-service IDOR responsibility).

Alternative: REQ-05 gap — DonViController dual-permission gate (DonViXem at controller vs DanhMucXem at service) inconsistency.

## Blockers

None discovered.
