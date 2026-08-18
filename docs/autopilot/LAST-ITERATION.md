# Iteration 7 — REQ-34 + REQ-12: Scoring Snapshot & Notification Channel Gating

## What Was Worked On

Two P0 core business gaps, batched:

1. **REQ-34**: `BoTieuChiSnapshot` did not include `DanhSachMucCongNhan` — recognition levels (score ranges mapping to outcomes like "Xuất sắc") were lost when criteria were snapshotted for evaluation ballots.
2. **REQ-12**: `ThucThiBuocCommandHandler.GuiThongBaoAsync` sent email/SMS notifications unconditionally, ignoring the workflow step's feature toggles (`GUI_EMAIL`, `GUI_SMS`).

## What Was Accomplished

### REQ-34: Scoring Snapshot Fix

1. **`MucCongNhanDto`** record added to `DanhGiaDtos.cs` — captures Id, Ma, Ten, DiemTu, DiemDen, MauSac, LaDat, ThuTu.
2. **`BoTieuChiDto.DanhSachMucCongNhan`** parameter added (nullable with default null for backwards compatibility with old serialized snapshots).
3. **`ChuyenDoiBoTieuChi()`** updated to map recognition levels — filters by `DaXoa`/`TrangThai`, orders by `DiemTu`.
4. **`DanhSachMucDiem` DaXoa filter** added (pre-existing consistency gap caught during code review).
5. **`InternalsVisibleTo`** added to Application.csproj for test access to internal static methods.

### REQ-12: Notification Channel Gating

1. **`IDichVuThongBao`** — new overload `GuiTheoSuKienAsync(..., IReadOnlyCollection<string> kenhChoPhep, ...)` added to interface.
2. **`DichVuThongBao`** — both overloads delegate to `GuiTheoSuKienCoreAsync` which checks `kenhChoPhep` before writing to each channel (APP/EMAIL/SMS). When null, all channels fire (backwards compatible).
3. **`ThucThiBuocCommandHandler`** — `GuiThongBaoAsync` now calls `LayKenhChoPhep(ketQua.ChucNangBat)` to derive allowed channels. APP always fires. EMAIL/SMS only when `MaChucNangBoSung.GuiEmail`/`GuiSms` are in `ChucNangBat`.

### Tests Added (12 new)

- `BoTieuChiSnapshotTests` (6): inclusion, soft-delete filter, inactive filter, ordering by DiemTu, JSON round-trip, backwards compat with old snapshots
- `LayKenhChoPhepTests` (6): null, empty, email only, SMS only, both, unrelated features

## Quality Gate Result

PASS — 7/7 checks, 301 unit tests (was 289), 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Application/DanhGia/DanhGiaDtos.cs` — MucCongNhanDto + BoTieuChiDto parameter
- `src/BlueIdea.Application/DanhGia/DichVuDanhGia.cs` — ChuyenDoiBoTieuChi mapper + DaXoa filter on MucDiem
- `src/BlueIdea.Application/BlueIdea.Application.csproj` — InternalsVisibleTo
- `src/BlueIdea.Application/Chung/GiaoDienHeThong.cs` — IDichVuThongBao overload
- `src/BlueIdea.Infrastructure/DichVu/DichVuNghiepVu.cs` — GuiTheoSuKienCoreAsync with channel filter
- `src/BlueIdea.Application/XuLy/ThucThiBuocCommand.cs` — LayKenhChoPhep + filtered notification call
- `tests/BlueIdea.UnitTests/Scoring/BoTieuChiSnapshotTests.cs` — 6 new tests
- `tests/BlueIdea.UnitTests/XuLy/LayKenhChoPhepTests.cs` — 6 new tests
- `docs/requirements/traceability.yaml` — updated REQ-12 and REQ-34 gaps/notes
- `docs/autopilot/STATE.json` — iteration state
- `docs/autopilot/LAST-ITERATION.md` — this file

## Commit Hash

eec0e01

## Next Priority Items

1. REQ-16 (PARTIAL): Integration sync adapter NOT connected to workflow engine transitions
2. REQ-12 remaining: ThucThiHangLoatCommandHandler missing notification dispatch; HanhDongCanChay dispatch loop
3. SEC MEDIUM: IMemoryCache-based SSO state needs IDistributedCache for multi-instance HA
4. SEC MEDIUM: MFA prompt (CanXacThucMfa) credential-stuffing oracle

## Known Limitations

- Old `BoTieuChiSnapshot` values (written before this change) will deserialize with `DanhSachMucCongNhan = null`. No current code path deserializes the snapshot — it's a write-only audit field.
- Batch processing (`ThucThiHangLoatCommandHandler`) still doesn't send any notifications at all (pre-existing).
- `HanhDongCanChay` dispatch loop not yet implemented — only notification channel gating is enforced.
- No integration test for either fix (unit tests cover logic; env lacks PostgreSQL Testcontainers).

## Blockers Discovered

None.
