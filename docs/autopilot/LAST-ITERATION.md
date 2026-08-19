# Autopilot Iteration 26

## Summary

Two changes in this iteration:

1. **CONG_BO_KET_QUA** (REQ-12): Implemented automated result publication workflow action. When workflow transitions from a step with CONG_BO_KET_QUA, a background job marks evaluation results as published, sets innovation to public, and notifies authors.

2. **P0 Bug Fix**: Fixed regression in `LayHanhDongKhaDungQueryHandler` where org-scope IDOR check (added in iteration 21, commit 4a5fdd7) blocked cross-department council members from seeing workflow actions. Council members from PHONG_YTE/PHONG_VHTT couldn't act on innovations from TH_LE_LOI (under PHONG_GDDT).

## Changes

### CONG_BO_KET_QUA Feature

- `src/BlueIdea.Application/Chung/GiaoDienHeThong.cs` — Added `XepLichCongBoKetQua(Guid sangKienId)` to `IHangDoiCongViecNen`
- `src/BlueIdea.Application/XuLy/DichVuDieuPhaiHanhDong.cs` — Wired CONG_BO_KET_QUA dispatch, reduced unimplemented warning group from 3 to 2
- `src/BlueIdea.Infrastructure/CongViecNen/CongViecNen.cs` — `CongViecCongBoKetQua`: marks `DaCongBo=true`, `NgayCongBo`, sets innovation `CongKhai=true`, `DaCongBoKetQua=true`, notifies authors. Idempotent. Hangfire retry 2 attempts 60/300s
- `src/BlueIdea.Infrastructure/CongViecNen/HangDoiCongViecNenHangfire.cs` — Wired Hangfire + no-op implementation
- `tests/BlueIdea.UnitTests/XuLy/DichVuDieuPhaiHanhDongTests.cs` — Updated counts (3→2 unimplemented, 5→6 implemented), added value test

### Cross-Org Council Member Scope Fix

- `src/BlueIdea.Application/XuLy/ThucThiBuocCommand.cs` — In `LayHanhDongKhaDungQueryHandler.Handle`, replaced blanket org-scope rejection with bypass for:
  - Users who are active `HoiDongThanhVien` AND have a `SangKienPhanCong` record for the innovation (scoring assignment)
  - Users who are active `HoiDongThanhVien` AND whose council has the innovation in a `PhienHopHoSo` (council session)
  - Falls through to `BoMayQuyTrinh.LayHanhDongKhaDungAsync` which is the authoritative actor check

## Quality Gate

- Result: PASS (8/8)
- Unit tests: 501
- Integration tests: 197
- Warnings: 0

## Requirements Affected

- REQ-12 (Chuc nang bo sung) — CONG_BO_KET_QUA implemented, gap reduced from 3 to 2 unimplemented actions (TAO_QUYET_DINH, YEU_CAU_KY_SO remain)

## Commits

- b24adda — feat: implement CONG_BO_KET_QUA automated workflow action (REQ-12)
- 2213dd5 — fix: allow cross-org council members to see workflow actions (IDOR scope regression)

## Next Priority

1. TD-001: Semantic Embedding is Lexical Only (Medium, BLOCKED_EXTERNAL — needs ONNX model)
2. TD-002: No Frontend Automated Tests (Medium)
3. REQ-12: 2 remaining unimplemented actions — TAO_QUYET_DINH (needs admin input), YEU_CAU_KY_SO (interactive signing)
4. REQ integration tests — many REQs at IMPLEMENTED_NOT_VERIFIED need runtime integration tests
5. TD-004: Database Partitioning (Low)

## Blockers

None. Docker is available for Testcontainers.
