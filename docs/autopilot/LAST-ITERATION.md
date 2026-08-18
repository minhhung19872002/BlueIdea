# Iteration 17 — SEC: Close catalog/org authorization gaps (REQ-01 through REQ-05, REQ-44)

## What Was Worked On

Multiple HIGH-severity authorization gaps across catalog management (REQ-01 through REQ-05) and organization management (REQ-44):
1. `DichVuDonVi.ChuyenChaAsync` — restructure org tree with NO permission check
2. `DichVuDonVi.GopAsync` — destructive unit merge with NO permission check
3. `DichVuDotDeNghi.LayTongQuanAsync` — admin aggregate stats with no permission check
4. `DichVuDotDeNghi.LayDanhSachQuanLyAsync` — admin management list with no permission check
5. All catalog controllers (LinhVuc, DoiTuong, LoaiTacGia, DotDeNghi, DonVi) had only bare `[Authorize]` at class level — no policy-level authorization at the controller boundary

Additionally resolved a pre-existing merge conflict in XacThucController.cs (conflicting `IConfiguration` vs `IDichVuCauHinh` fields).

## What Was Accomplished

1. **Service-level permission checks** added to 4 methods:
   - `ChuyenChaAsync` → `BatBuocCoQuyenAsync(DonViCauHinh, id)`
   - `GopAsync` → `BatBuocCoQuyenAsync(DonViCauHinh)`
   - `LayTongQuanAsync` → `BatBuocCoQuyenAsync(DanhMucXem, id)`
   - `LayDanhSachQuanLyAsync` → `BatBuocCoQuyenAsync(DanhMucXem)`

2. **Controller-level authorization policies** (defense-in-depth) across 5 controllers:
   - LinhVucController: 9 endpoints with policies (DanhMucXem/Them/Sua/Xoa/Xuat)
   - DoiTuongController: 5 endpoints with policies
   - LoaiTacGiaController: 5 endpoints with policies
   - DotDeNghiController: 11 endpoints with policies (incl. lifecycle transitions)
   - DonViController: 7 endpoints with policies (DonViXem/DonViCauHinh)
   - Dropdown/selection endpoints (`chon`, `dang-mo`, `cay`, `logo`) intentionally kept bare `[Authorize]`

3. **Merge conflict resolved** in XacThucController.cs — `_cauHinh` (IConfiguration) + `_dichVuCauHinh` (IDichVuCauHinh)

4. **45 unit tests** verifying controller authorization attributes via reflection (includes class-level Authorize check)

## Files Changed

- `src/BlueIdea.Application/DanhMuc/DichVuDanhMuc.cs` (MODIFIED — 4 permission checks added)
- `src/BlueIdea.Api/Controllers/DanhMucController.cs` (MODIFIED — 19 policy attributes)
- `src/BlueIdea.Api/Controllers/DotDeNghiVaDonViController.cs` (MODIFIED — 18 policy attributes)
- `src/BlueIdea.Api/Controllers/XacThucController.cs` (MODIFIED — merge conflict resolved)
- `tests/BlueIdea.UnitTests/BlueIdea.UnitTests.csproj` (MODIFIED — added API project reference)
- `tests/BlueIdea.UnitTests/Shared/ChinhSachPhanQuyenControllerTests.cs` (NEW — 45 tests)
- `docs/requirements/traceability.yaml` (MODIFIED — updated REQ-01 through REQ-05, REQ-44)

## Quality Gate

PASS (7/7, 369 unit tests + 184 integration tests, 0 warnings)

## Remaining Gaps

- `DichVuPhanQuyen.KiemTraQuyenAsync` discards `doiTuongId` — resource-level scope enforcement is not implemented for mutations (CRITICAL pre-existing, tracked for future iteration)
- DonViController uses DonViXem/DonViCauHinh but base service checks DanhMucXem/Sua/Xoa — dual-permission gate (no regression with current seed data but fragile)
- `LayCayAsync` and `LayDanhSachChonAsync` have no service-level permission check (by design — dropdown/tree for all users)
- No integration tests for catalog CRUD authorization denial

## Next Priority Item

Implement resource-level scope validation in `DichVuPhanQuyen.BatBuocCoQuyenAsync` (CRITICAL security gap from review).
