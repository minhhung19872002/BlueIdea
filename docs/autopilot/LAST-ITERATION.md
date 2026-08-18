# Iteration 16 — REQ-12: HanhDongCanChay Full Dispatch Loop

## What Was Worked On

REQ-12 gap: 7 of 10 configured workflow action types (HanhDongCanChay) were silently dropped after workflow transitions. Both `ThucThiBuocCommandHandler` and `ThucThiHangLoatCommandHandler` had duplicated `DieuPhaiLienThongAsync` methods that only handled `DONG_BO_LIEN_THONG` and ignored the other 7 non-notification actions.

## What Was Accomplished

1. **Created `DichVuDieuPhaiHanhDong`** — centralized dispatch service that routes all 10 action types:
   - `DONG_BO_LIEN_THONG`: Implemented (absorbed from handlers)
   - `KIEM_TRA_TRUNG_LAP`: Implemented (schedules background similarity check)
   - `CAP_NHAT_KET_QUA`: Recognized but deferred — `BoMayQuyTrinh.ChuyenBuoc` already writes `hoSo.KetQua` at engine level (lines 328/332), making a dispatch handler redundant
   - `GUI_EMAIL`/`GUI_SMS`: Skipped (handled via `GuiThongBaoAsync` + `ChucNangBat` channel gating)
   - 5 remaining actions log warnings for manual handling

2. **Removed duplicated code** — deleted `DieuPhaiLienThongAsync` from both handlers (~106 lines of duplication), replaced with single `_dieuPhai.DieuPhaiAsync()` call

3. **Error isolation** — per-action try/catch ensures one failure cannot block others

4. **Removed redundant CAP_NHAT_KET_QUA handler** — security review discovered that `BoMayQuyTrinh.ChuyenBuoc` already writes `hoSo.KetQua` before the dispatch runs. The handler would have produced a misleading audit trail (before=DAT, after=DAT). Converted to a debug log.

5. **10 unit tests** verify constant values, action routing, and dispatch filtering logic

## Files Changed

- `src/BlueIdea.Application/XuLy/DichVuDieuPhaiHanhDong.cs` (NEW)
- `src/BlueIdea.Application/XuLy/ThucThiBuocCommand.cs` (MODIFIED — removed duplication, wired dispatch)
- `src/BlueIdea.Application/Chung/DangKyDichVuUngDung.cs` (MODIFIED — DI registration)
- `tests/BlueIdea.UnitTests/XuLy/DichVuDieuPhaiHanhDongTests.cs` (NEW)
- `docs/requirements/traceability.yaml` (MODIFIED — updated REQ-12 evidence and gaps)

## Quality Gate

PASS (7/7, 319 unit tests + 165 integration tests, 0 warnings)

## Commit

`1c1537b` — `feat: centralize HanhDongCanChay dispatch loop (REQ-12)`

## Remaining Gaps

- 5 action types (TAO_QUYET_DINH, YEU_CAU_KY_SO, TAO_BIEN_BAN, PHAN_CONG_CHAM, CONG_BO_KET_QUA) log warnings — require future implementation when business logic is defined
- No runtime integration test for feature toggles affecting live workflow execution
- Change tracker pollution between dispatch actions sharing scoped DbContext (pre-existing architectural pattern)
