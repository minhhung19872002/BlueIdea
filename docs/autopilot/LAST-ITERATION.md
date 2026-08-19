# Autopilot Iteration 21

## Summary

Resolved TD-005: DoiTuongId was passed through the MediatR authorization pipeline but explicitly discarded in `KiemTraQuyenAsync`, creating false security documentation. Four mutation/query handlers lacked IDOR scope checks.

## Changes

### Interface refactoring (HanhViPipeline.cs)

- Removed `DoiTuongId` from `ICoYeuCauQuyen` (authorization interface)
- Added `DoiTuongId` to `ICoGhiNhatKy` (audit logging interface) — preserving audit trail
- `HanhViPhanQuyen` no longer passes `DoiTuongId` to `BatBuocCoQuyenAsync`
- `HanhViGhiNhatKy` now reads `DoiTuongId` from `ICoGhiNhatKy` instead of casting to `ICoYeuCauQuyen`

### Authorization API cleanup (IDichVuPhanQuyen + DichVuPhanQuyen)

- Removed `doiTuongId` parameter from `KiemTraQuyenAsync` and `BatBuocCoQuyenAsync`
- Removed the explicit discard `_ = doiTuongId` line
- Updated ~55 call sites across 17 service files to use new 2-parameter signature

### IDOR scope checks added (SangKienCommands.cs + ThucThiBuocCommand.cs)

- `CapNhatHoSoCommandHandler`: Added `BatBuocTrongPhamViAsync` after loading record
- `NopHoSoCommandHandler`: Added `BatBuocTrongPhamViAsync` after loading record
- `RutHoSoCommandHandler`: Added `BatBuocTrongPhamViAsync` after loading record + added `.Include(DanhSachTacGia)` for co-author check
- `LayHanhDongKhaDungQueryHandler`: Added scope check before engine call, returns empty for out-of-scope requests

### Tests

- Updated 5 existing `DichVuDonViPhamViTests` for new `BatBuocCoQuyenAsync` signature
- Added 4 integration tests in `IdorBaoVeTests.cs` for cross-org IDOR on CapNhat/Nop/Rut/HanhDong

## Quality Gate

- Result: PASS (7/7)
- Unit tests: 493 (unchanged)
- Warnings: 0

## Requirements Affected

- REQ-23 (Quan ly ho so sang kien) — 2 security gaps closed (DoiTuongId discard, missing hanh-dong auth test)
- TD-005 — Resolved (moved to Resolved Items in technical-debt.md)

## Files Changed

- `src/BlueIdea.Application/Chung/HanhViPipeline.cs` (interface + pipeline changes)
- `src/BlueIdea.Application/Chung/GiaoDienHeThong.cs` (IDichVuPhanQuyen signature)
- `src/BlueIdea.Infrastructure/DichVu/DichVuPhanQuyen.cs` (implementation)
- `src/BlueIdea.Application/SangKien/SangKienCommands.cs` (3 IDOR checks + constructor DI)
- `src/BlueIdea.Application/XuLy/ThucThiBuocCommand.cs` (1 IDOR check + remove dead DoiTuongId)
- 17 service files (BatBuocCoQuyenAsync call site updates)
- `tests/BlueIdea.UnitTests/DanhMuc/DichVuDonViPhamViTests.cs` (signature update)
- `tests/BlueIdea.IntegrationTests/IdorBaoVeTests.cs` (4 new tests)
- `docs/requirements/traceability.yaml` (REQ-23 updated)
- `docs/audit/technical-debt.md` (TD-005 resolved)

## Commit

4a5fdd7

## Next Priority

Remaining items by priority:
1. TD-001: Semantic Embedding is Lexical Only (Medium, BLOCKED_EXTERNAL — needs ONNX model)
2. TD-002: No Frontend Automated Tests (Medium)
3. REQ integration tests — many REQs at IMPLEMENTED_NOT_VERIFIED need runtime integration tests (require Docker)
4. TD-004: Database Partitioning (Low)

## Blockers

Integration tests (4 new + many existing) require Docker for Testcontainers — cannot be executed in current environment.
