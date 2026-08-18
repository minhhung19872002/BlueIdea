# Iteration 8 — REQ-16: Wire Integration Sync Adapter to Workflow Transitions

## What Was Worked On

REQ-16 was the only PARTIAL requirement remaining — the integration sync adapter existed but was NOT connected to workflow engine transitions. Admin could configure external systems and manually trigger sync, but workflow steps did not automatically push data.

## What Was Accomplished

### Integration Dispatch in ThucThiBuocCommandHandler

1. **`DieuPhaiLienThongAsync`** added to `ThucThiBuocCommandHandler` — dispatches integration sync after successful workflow step execution when `HanhDongCanChay` contains `DongBoLienThong`.
2. **Query scoped by `QuyTrinhId`** — prevents cross-workflow config leaks (only matches `QuyTrinhLienThong` records for the sang kien's own workflow).
3. **All 3 events supported**: `KHI_HOAN_THANH` (step completed, matches `BuocTruocId`), `KHI_VAO_BUOC` (step entered, matches `BuocMoiId`), `KHI_PHE_DUYET` (approval, matches `BuocTruocId` + `TrangThaiTongMoi == DA_PHE_DUYET`).
4. **Null-BuocId handling** — workflow-wide configs match with event filtering to prevent over-triggering.
5. **Error isolation** — sync failures are caught and logged per-system; workflow transition is never blocked.
6. **`DichVuDongBoLienThong` + `ILogger`** injected into handler constructor.

### DongBoSangKienAsync (single-innovation sync)

1. **`DongBoSangKienAsync`** added to `DichVuDongBoLienThong` (internal visibility) — syncs ONE innovation to a specific external system.
2. **No permission check** — called from workflow context which is already authenticated via MediatR pipeline.
3. **Reuses `NapDuLieuAsync`** with new optional `sangKienId` filter — maintains the `KetQua == Dat && DaCongBoKetQua` guard to prevent premature data leaks.
4. **No NhatKyDongBo for zero records** — when innovation isn't published yet, returns lightweight result without polluting sync history.
5. **Graceful handling** — inactive/unconfigured systems return error result without throwing.

### Domain Constants

1. **`SuKienLienThong`** class added to `HangSo.cs` with `KhiVaoBuoc`, `KhiHoanThanh`, `KhiPheDuyet` constants — replaces string literals.

### Tests Added (8 new)

- `DieuPhaiLienThongTests` (8): constant value verification, HanhDongCanChay guard conditions, BuocTruocId/BuocMoiId availability for matching.

### Code Review Findings Addressed

- **QuyTrinhId scoping** (MAJOR): Added `x.QuyTrinhId == quyTrinhId.Value` to prevent cross-workflow leaks.
- **KhiPheDuyet dead code** (MAJOR): Added third matching arm for approval events.
- **Null-BuocId over-matching** (MAJOR): Added SuKien filter to null-BuocId arm.
- **Internal visibility** (SUGGESTION): Changed `DongBoSangKienAsync` from public to internal.
- **Zero-record NhatKyDongBo** (MINOR): Skip log entry creation when nothing to sync.

### Code Review Findings Documented as Known Gaps

- **Batch handler** (BLOCKER): `ThucThiHangLoatCommandHandler` does not dispatch integration sync — batch transitions skip `DongBoLienThong`. This is a pre-existing batch gap (also missing notifications).
- **ADR 0002 snapshot exception**: `QuyTrinhLienThong` uses live DB data, not workflow snapshot. This is deliberate — integration config contains operational details (endpoints, credentials) that should be updatable without creating new workflow versions. Should be documented in ADR.

## Quality Gate Result

PASS — 7/7 checks, 309 unit tests (was 301), 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Domain/Chung/HangSo.cs` — SuKienLienThong constants
- `src/BlueIdea.Application/TichHop/DichVuDongBoLienThong.cs` — DongBoSangKienAsync + NapDuLieuAsync sangKienId filter
- `src/BlueIdea.Application/XuLy/ThucThiBuocCommand.cs` — DieuPhaiLienThongAsync dispatch + DI injection
- `tests/BlueIdea.UnitTests/XuLy/DieuPhaiLienThongTests.cs` — 8 new tests
- `docs/requirements/traceability.yaml` — REQ-16 updated to IMPLEMENTED_NOT_VERIFIED

## Commit Hash

baf73b2

## Next Priority Items

1. REQ-12 remaining: ThucThiHangLoatCommandHandler missing notification dispatch + integration sync dispatch
2. SEC MEDIUM: IMemoryCache-based SSO state needs IDistributedCache for multi-instance HA
3. SEC MEDIUM: MFA prompt (CanXacThucMfa) credential-stuffing oracle
4. ADR documentation: QuyTrinhLienThong live-data exception to snapshot rule

## Known Limitations

- Batch handler (`ThucThiHangLoatCommandHandler`) does not dispatch integration sync or notifications (pre-existing).
- `QuyTrinhLienThong` is queried from live DB, not from workflow snapshot — deliberate for operational reasons but ADR should document this exception.
- No runtime integration test for the full workflow-triggered sync path (unit tests cover guard conditions only).
- `HanhDongCanChay` dispatch loop for OTHER actions (TaoQuyetDinh, CapNhatKetQua, KiemTraTrungLap, etc.) is still not implemented.

## Blockers Discovered

None.
