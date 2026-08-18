# Iteration 10 — REQ-12/REQ-16: Batch Processing Notification + Integration Sync

## What Was Worked On

ThucThiHangLoatCommandHandler (batch processing) silently dropped all post-transition side effects. Authors processed in batch received no notifications, and integration sync was never dispatched — causing inconsistent behavior between single and batch processing paths.

## What Was Accomplished

### Batch Handler — Notification Dispatch

1. **IDichVuThongBao injected** — batch handler now sends notifications to innovation authors after each successful transition.
2. **Channel gating** — reuses `ThucThiBuocCommandHandler.LayKenhChoPhep()` to gate EMAIL/SMS channels via workflow feature toggles (ChucNangBat).
3. **Error isolation** — notification failures are logged but never block the batch loop. `OperationCanceledException` is re-thrown for clean cancellation.

### Batch Handler — Integration Sync Dispatch

1. **DichVuDongBoLienThong injected** — batch handler now dispatches integration sync when `HanhDongCanChay` contains `DongBoLienThong`.
2. **Same query logic** — queries `QuyTrinhLienThong` for matching configs (3 event types: KhiHoanThanh, KhiVaoBuoc, KhiPheDuyet), supporting wildcard configs.
3. **Error isolation** — outer try/catch wraps the entire sync dispatch; per-config try/catch handles individual system failures. Neither blocks the batch.

### ChoThemTacNhan Guard (Code Review Finding)

1. **Spurious notification prevention** — code review identified that multi-actor steps (TAT_CA/DA_SO processing rules) return `ThanhCong=true` with `ChoThemTacNhan=true` for partial votes. Without a guard, each partial vote triggers a "hồ sơ được tiếp nhận" notification.
2. **Guard added to both handlers** — `if (!ketQua.ChoThemTacNhan)` check before `GuiThongBaoAsync` in both `ThucThiBuocCommandHandler` (line 121) and `ThucThiHangLoatCommandHandler`.
3. **Pre-existing bug fixed** — the single handler had this same issue; the fix corrects both paths simultaneously.

### Code Review Findings Addressed

- **MAJOR (ChoThemTacNhan spurious notifications)**: Fixed — guard added to both handlers.
- **MINOR (asymmetric cancellation)**: Fixed — both `GuiThongBaoAsync` and `DieuPhaiLienThongAsync` use `when (ex is not OperationCanceledException)` for symmetric cancellation behavior.
- **MAJOR (code duplication)**: Accepted — notification/sync logic mirrors single handler. Reuses static methods (`SuKienTheoTrangThai`, `LayKenhChoPhep`). Full extraction deferred as over-engineering per project rules.
- **MAJOR (O(N) DB queries)**: Accepted — matches single handler's pattern (same queries per item). Pre-fetching optimization deferred as it's not a correctness issue and batch sizes are small in practice.

## Quality Gate Result

PASS — 7/7 checks, 309 unit tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Application/XuLy/ThucThiBuocCommand.cs` — batch handler notification + sync dispatch, single handler ChoThemTacNhan guard
- `docs/requirements/traceability.yaml` — REQ-12 and REQ-16 gaps updated

## Commit Hash

60d2e67

## Next Priority Items

1. SEC MEDIUM: IMemoryCache-based SSO state → IDistributedCache for multi-instance HA (REQ-21/REQ-41)
2. ADR documentation: QuyTrinhLienThong live-data exception to snapshot rule
3. SEC LOW: MFA recovery codes — upgrade from SHA-256 to Argon2id
4. REQ-12: HanhDongCanChay full dispatch loop (beyond DongBoLienThong)

## Known Limitations

- Notification and integration sync logic is duplicated between single and batch handlers (code review accepted this as appropriate given project rules against premature abstraction).
- Batch handler does O(N) DB queries per item for side effects (same as calling single handler N times; optimization deferred).
- HanhDongCanChay dispatch loop still only handles DongBoLienThong — other auto-actions (TAO_QUYET_DINH, KY_SO, etc.) are handled by other subsystems.
- Integration tests compile but require .NET 8 runtime with Docker for Testcontainers.

## Blockers Discovered

None.
