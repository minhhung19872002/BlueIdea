# Iteration 13 — REQ-29 SEC: ThucThiHangLoatCommandHandler Cross-Org Leakage + Batch Validation

## What Was Worked On

Three tightly related security and input validation gaps in ThucThiHangLoatCommandHandler (batch workflow processing, function 29):

1. **SEC MEDIUM: MaHoSo cross-org leakage** — The batch handler queried MaHoSo (case numbers) for arbitrary SangKien IDs without org scope, then leaked those MaHoSo in error messages. Any user with XuLyThucThi permission could learn case numbers from other organizations.
2. **SEC LOW: Existence oracle + timing side-channel** — Cross-org IDs were passed to the workflow engine, which produced distinguishable responses (and timing) for existing vs non-existing records.
3. **DoS + input validation: No batch size cap or dedup** — No FluentValidation validator, no size limit, no deduplication. Could cause DB connection pool exhaustion and ToDictionary crash on duplicate IDs.

## What Was Accomplished

### Fix 1: Org-scoped MaHoSo pre-fetch

- Added `IDichVuPhanQuyen _phanQuyen` to handler constructor
- Before the loop, fetches user's scope via `LayPhamViTruyCapAsync` (cached, single call)
- Batch query loads MaHoSo only for in-scope records:
  - `ToanHeThong`: no filter
  - `ChiCaNhan`: NguoiTaoId or DanhSachTacGia membership
  - Org scope: DonViId in DonViIds OR NguoiTaoId OR DanhSachTacGia (matches canonical `ApDungPhamViDuLieuAsync` pattern)
- Cross-org IDs fall back to `id.ToString()` (GUID the attacker already knows)

### Fix 2: Cross-org ID short-circuit

- If an ID is not in the `maHoSoMap`, the handler skips the workflow engine call entirely
- Returns generic "không tìm thấy hoặc không có quyền xử lý" error using the GUID
- Eliminates both the existence oracle (same error for non-existent and cross-org) and timing side-channel (no workflow engine queries for cross-org IDs)

### Fix 3: FluentValidation + deduplication

- Added `ThucThiHangLoatCommandValidator` with `NotEmpty` + `Count <= 200` for SangKienIds and `NotEmpty` for TruongHopId
- Loop iterates over `request.SangKienIds.Distinct().ToList()` to prevent ToDictionary crash and double workflow transitions
- Result counts use `uniqueIds.Count` for accurate reporting

### Code Review Findings Addressed

- **MAJOR (no regression test)**: Documented as gap in traceability. Environment lacks .NET 8 Docker for Testcontainers.
- **MINOR (org-scope missing author OR clauses)**: Fixed — org-scope branch now includes NguoiTaoId and DanhSachTacGia OR clauses, matching canonical pattern.

### Security Review Findings Addressed

- **Original MaHoSo leakage**: CLOSED by org-scoped pre-fetch
- **Existence oracle (LOW)**: CLOSED by cross-org short-circuit before workflow engine
- **Timing side-channel (LOW)**: CLOSED by cross-org short-circuit
- **No max batch size (MEDIUM)**: CLOSED by FluentValidation validator (max 200)
- **No dedup / ToDictionary crash (LOW)**: CLOSED by `.Distinct()` on SangKienIds
- **Audit log bloat (INFO)**: Mitigated by batch size cap

## Quality Gate Result

PASS — 7/7 checks, 309 unit tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Application/XuLy/ThucThiBuocCommand.cs` — handler + validator
- `docs/requirements/traceability.yaml` — REQ-29 notes updated
- `docs/autopilot/STATE.json` — iteration 13
- `docs/autopilot/LAST-ITERATION.md` — this file

## Commit Hash

(pending)

## Next Priority Items

1. SEC LOW: MFA recovery codes — upgrade from SHA-256 to Argon2id (REQ-21)
2. REQ-12: HanhDongCanChay full dispatch loop (beyond DongBoLienThong + GuiThongBao)
3. TD-005: Implement DoiTuongId object-level scope checking in KiemTraQuyenAsync (MEDIUM, systemic)

## Known Limitations

- No regression test for batch cross-org MaHoSo leakage (documented as gap; env lacks Docker for Testcontainers)
- Integration tests compile but require .NET 8 runtime with Docker
- GuiThongBaoAsync still queries SangKien without org scope (acceptable — only runs after successful workflow execution, no data returned to caller)

## Blockers Discovered

None.
