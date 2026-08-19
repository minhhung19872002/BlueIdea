# Autopilot Iteration 22

## Summary

Fixed REQ-15 bug: CHU_TICH_QUYET_DINH processing rule in the workflow engine was functionally identical to MOT_NGUOI, allowing any step actor (including regular council members) to advance a Chairman-decision step. The Chairman's decision should be the only one that advances the workflow.

## Changes

### Workflow engine fix (BoMayQuyTrinh.cs)

- Extracted `KhopTacNhan` helper from `LaTacNhanCuaBuoc` — single-actor matching logic now reusable without duplication
- Refactored `LaTacNhanCuaBuoc` to use `KhopTacNhan`
- **Fixed `DemTacNhan`**: Separated ChuTichQuyetDinh from MotNguoi. When ChuTichQuyetDinh is the effective rule, only actors whose `QuyTacXuLy == ChuTichQuyetDinh` can advance the step. Other actors' actions are recorded (ThanhCong=true, ChoThemTacNhan=true) but the step does not advance until the Chairman acts.

### Test helpers (XuongDuLieuTest.cs)

- Added `ThemTacNhanChucDanh` extension method for creating `ChucDanhHoiDong`-type actors in tests

### Unit tests (BoMayQuyTrinhTests.cs)

- Added `NguoiVoiChucDanh` helper for creating user context with council role
- Added 3 tests:
  1. `Quy_Tac_CHU_TICH_QUYET_DINH_Chu_Tich_Chuyen_Buoc_Ngay` — Chairman advances immediately
  2. `Quy_Tac_CHU_TICH_QUYET_DINH_Uy_Vien_Ghi_Nhan_Nhung_Chua_Chuyen_Buoc` — Member recorded but step stays
  3. `Quy_Tac_CHU_TICH_QUYET_DINH_Sau_Uy_Vien_Chu_Tich_Chuyen_Buoc` — Chairman advances after member already recorded

## Quality Gate

- Result: PASS (7/7)
- Unit tests: 496 (493 + 3 new)
- Warnings: 0

## Requirements Affected

- REQ-15 (Tac nhan xu ly) — ChuTichQuyetDinh bug fixed, gap updated

## Files Changed

- `src/BlueIdea.Workflow/BoMayQuyTrinh.cs` (KhopTacNhan extraction + DemTacNhan fix)
- `tests/BlueIdea.UnitTests/TienIch/XuongDuLieuTest.cs` (ThemTacNhanChucDanh helper)
- `tests/BlueIdea.UnitTests/Workflow/BoMayQuyTrinhTests.cs` (3 new tests + NguoiVoiChucDanh helper)
- `docs/requirements/traceability.yaml` (REQ-15 updated)

## Commit

(pending)

## Next Priority

Remaining items by priority:
1. TD-001: Semantic Embedding is Lexical Only (Medium, BLOCKED_EXTERNAL — needs ONNX model)
2. TD-002: No Frontend Automated Tests (Medium)
3. REQ integration tests — many REQs at IMPLEMENTED_NOT_VERIFIED need runtime integration tests (require Docker)
4. TD-004: Database Partitioning (Low)

## Blockers

Integration tests require Docker for Testcontainers — cannot be executed in current environment.
