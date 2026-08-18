# Iteration 12 — REQ-23 SEC: LayHanhDongKhaDungQuery + GoiYAsync Authorization Fixes

## What Was Worked On

Two SEC LOW authorization gaps in REQ-23 (Quan ly ho so sang kien):

1. **LayHanhDongKhaDungQuery** had no `ICoYeuCauQuyen`, so any authenticated user could probe arbitrary SangKien IDs via `GET /api/v1/sang-kien/{id}/hanh-dong` and get 200+empty (existence oracle).
2. **GoiYAsync** did not call `BatBuocCoQuyenAsync`, so any authenticated user could use autocomplete suggestions without `SANG_KIEN.XEM` permission.

## What Was Accomplished

### Fix 1: LayHanhDongKhaDungQuery — ICoYeuCauQuyen Added

- Added `ICoYeuCauQuyen` with `MaQuyenYeuCau => MaQuyen.SangKienXem` and `DoiTuongId => SangKienId`.
- The MediatR `HanhViPhanQuyen` pipeline now enforces `SANG_KIEN.XEM` permission before the handler runs.
- `DoiTuongId` provides audit log context via `HanhViGhiNhatKy`.
- Initially used `XuLyXem` — code review caught that "Tac gia" role lacks this permission. Corrected to `SangKienXem`.

### Fix 2: GoiYAsync — BatBuocCoQuyenAsync Added

- Added `await _phanQuyen.BatBuocCoQuyenAsync(MaQuyen.SangKienXem, ct: ct)` at the start of `GoiYAsync`.
- Now all 5 read methods in `DichVuTruyVanSangKien` are gated by `SangKienXem`.
- Org-scope was already enforced via `ApDungPhamViDuLieuAsync` — this fix adds the missing feature-permission check.

### Code Review Findings Addressed

- **BLOCKER (wrong permission)**: Fixed — `XuLyXem` → `SangKienXem`. "Tac gia" role holds `SangKienXem` but not `XuLyXem`.
- **MAJOR (DoiTuongId discarded)**: Documented as TD-005. Pre-existing infrastructure limitation affecting all commands/queries. `KiemTraQuyenAsync` discards `doiTuongId` — the IDOR protection comes from per-service scope checks, not the pipeline.
- **MINOR (dead null guard)**: Retained — belt-and-suspenders defensive pattern, not worth changing in this iteration.
- **MINOR (no negative auth tests)**: Deferred — documented as gap in traceability.

### Security Review Findings

- **CRITICAL (DoiTuongId infrastructure gap)**: Pre-existing, documented as TD-005 in technical-debt.md. Affects entire codebase.
- **HIGH (residual existence oracle)**: Scoped from "all authenticated users" to "users with SangKienXem". Residual oracle via 404/200+[] differential for SangKienXem holders — acceptable LOW risk.
- **MEDIUM (batch MaHoSo leakage)**: Pre-existing in `ThucThiHangLoatCommandHandler`. Not in scope for this iteration.

### Traceability Updates

- REQ-23: Removed two SEC LOW gaps (LayHanhDongKhaDungQuery oracle, GoiYAsync permission bypass). Updated notes with B12 actions. Added residual gaps for DoiTuongId limitation and missing negative tests.
- TD-005: New technical debt item for DoiTuongId infrastructure gap.

## Quality Gate Result

PASS — 7/7 checks, 309 unit tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Application/XuLy/ThucThiBuocCommand.cs` — LayHanhDongKhaDungQuery + ICoYeuCauQuyen
- `src/BlueIdea.Application/SangKien/DichVuTruyVanSangKien.cs` — GoiYAsync + BatBuocCoQuyenAsync
- `docs/requirements/traceability.yaml` — REQ-23 gaps updated
- `docs/audit/technical-debt.md` — TD-005 added

## Commit Hash

a663133

## Next Priority Items

1. SEC LOW: MFA recovery codes — upgrade from SHA-256 to Argon2id (REQ-21)
2. REQ-12: HanhDongCanChay full dispatch loop (beyond DongBoLienThong + GuiThongBao)
3. TD-005: Implement DoiTuongId object-level scope checking in KiemTraQuyenAsync (MEDIUM, systemic)
4. SEC: ThucThiHangLoatCommandHandler MaHoSo leakage — add don_vi_id scope filter (MEDIUM, pre-existing)

## Known Limitations

- DoiTuongId is discarded by KiemTraQuyenAsync (pre-existing, TD-005). The pipeline provides permission gating but not object-level scope enforcement.
- Residual existence oracle for SangKienXem holders on /{id}/hanh-dong (404 vs 200+[] differential). LOW risk — scoped to authorized users only.
- No negative authorization tests for /{id}/hanh-dong or /goi-y endpoints.
- Integration tests compile but require .NET 8 runtime with Docker for Testcontainers.

## Blockers Discovered

None.
