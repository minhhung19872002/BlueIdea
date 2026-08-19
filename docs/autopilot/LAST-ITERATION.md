# Autopilot Iteration 23

## Summary

Implemented PHAN_CONG_CHAM automated workflow action (REQ-12/REQ-33). When a workflow transitions to a scoring step (CHAM_DIEM), the system now automatically assigns eligible council members to score the innovation. Previously this required manual secretary intervention.

## Changes

### Background job (CongViecPhanCongCham in CongViecNen.cs)

- Looks up `HoiDongId` from the workflow step (`QuyTrinhBuoc`)
- Gets eligible council members (`QuyenChamDiem` + active status)
- Conflict-of-interest exclusion: authors cannot score their own innovation
- Idempotent: calling twice for the same (sangKien, thanhVien) pair skips existing assignments
- Deadline sourced from the step's processing deadline (`SangKienXuLy.HanXuLy`)
- Sends `DUOC_PHAN_CONG_CHAM` notification to assigned members
- `NguoiPhanCongId = null` distinguishes system-assigned from manually-assigned
- Hangfire retry: 2 attempts with 60s/300s delays

### Interface (IHangDoiCongViecNen in GiaoDienHeThong.cs)

- Added `XepLichPhanCongCham(Guid sangKienId, Guid buocMoiId)`

### Hangfire adapter (HangDoiCongViecNenHangfire.cs)

- Wired `XepLichPhanCongCham` to enqueue `CongViecPhanCongCham.ChayAsync`
- No-op implementation added for `HangDoiCongViecNenKhongHoatDong` (test environments)

### Action dispatcher (DichVuDieuPhaiHanhDong.cs)

- PHAN_CONG_CHAM now dispatches to `_hangDoi.XepLichPhanCongCham` instead of logging a warning
- Guards against null `BuocMoiId` (workflow already ended)
- Warning group reduced from 5 to 4 unimplemented actions

### Unit tests (DichVuDieuPhaiHanhDongTests.cs)

- Added `HanhDongTuDong_PhanCongCham_Dung_Gia_Tri` — verifies constant value
- Updated unimplemented count test from 5 to 4 (PHAN_CONG_CHAM removed)
- Added `HanhDongTuDong_Da_Trien_Khai_Co_4_Hanh_Dong` — verifies implemented set
- Added `PhanCongCham_Khong_Trong_Nhom_Chua_Trien_Khai` — guards against regression

## Quality Gate

- Result: PASS (7/7)
- Unit tests: 499 (496 + 3 new)
- Warnings: 0

## Requirements Affected

- REQ-12 (Chuc nang bo sung) — PHAN_CONG_CHAM implemented, gap updated from 5 to 4 unimplemented actions
- REQ-33 (Danh sach ho so danh gia) — automated scoring assignment now wired

## Files Changed

- `src/BlueIdea.Application/Chung/GiaoDienHeThong.cs` (XepLichPhanCongCham interface)
- `src/BlueIdea.Application/XuLy/DichVuDieuPhaiHanhDong.cs` (PHAN_CONG_CHAM dispatch)
- `src/BlueIdea.Infrastructure/CongViecNen/CongViecNen.cs` (CongViecPhanCongCham job)
- `src/BlueIdea.Infrastructure/CongViecNen/HangDoiCongViecNenHangfire.cs` (Hangfire + no-op)
- `tests/BlueIdea.UnitTests/XuLy/DichVuDieuPhaiHanhDongTests.cs` (3 new + 1 updated tests)
- `docs/requirements/traceability.yaml` (REQ-12, REQ-33 updated)

## Commit

(pending)

## Next Priority

Remaining items by priority:
1. TD-001: Semantic Embedding is Lexical Only (Medium, BLOCKED_EXTERNAL — needs ONNX model)
2. TD-002: No Frontend Automated Tests (Medium)
3. REQ-12: 4 remaining unimplemented actions — TAO_BIEN_BAN is most feasible next (background job pattern, DichVuBienBanHop.LapAsync is idempotent)
4. REQ integration tests — many REQs at IMPLEMENTED_NOT_VERIFIED need runtime integration tests (require Docker)
5. TD-004: Database Partitioning (Low)

## Blockers

Integration tests require Docker for Testcontainers — cannot be executed in current environment.
