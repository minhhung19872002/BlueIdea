# Iteration 2 — P0 Scoring Engine Fixes

## What Was Worked On

Three P0 scoring engine bugs (REQ-17, REQ-34):

1. **TrungBinhCong computes sum instead of average** (BoTinhDiem.cs): Changed ternary to switch expression with proper `diemTheoNhom.Values.Sum() / nhomTheoId.Count` branch.
2. **BoTieuChiSnapshot never written** (DichVuDanhGia.cs): Added `JsonSerializer.Serialize(ChuyenDoiBoTieuChi(boTieuChi))` to persist criteria snapshot in evaluation form save.
3. **KiemTraBoTieuChi not called during save** (DichVuTieuChi.cs): Added validation call before SaveChangesAsync, changed to navigation property Add for correct in-memory state, included MucCongNhan for overlap validation.

## What Was Accomplished

- All three fixes verified correct by direct file inspection and code review
- 2 new unit tests added for TrungBinhCong (with HopLe assertions)
- All 269 unit tests pass
- Quality gate: PASS (7/7 checks)
- Traceability updated: REQ-17 PARTIAL -> IMPLEMENTED_NOT_VERIFIED, REQ-34 PARTIAL -> IMPLEMENTED_NOT_VERIFIED

## Quality Gate Result

PASS — 7/7 checks, 269 unit tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Scoring/BoTinhDiem.cs` — TrungBinhCong switch branch
- `src/BlueIdea.Application/DanhGia/DichVuDanhGia.cs` — BoTieuChiSnapshot serialization
- `src/BlueIdea.Application/TieuChi/DichVuTieuChi.cs` — KiemTraBoTieuChi validation + Include fix
- `tests/BlueIdea.UnitTests/Scoring/BoTinhDiemTests.cs` — 2 new TrungBinhCong tests
- `docs/requirements/traceability.yaml` — REQ-17, REQ-34 status updates
- `docs/autopilot/STATE.json` — iteration state
- `.gitignore` — minor cleanup

## Commit Hash

(pending commit)

## Next Priority Item

- REQ-26 SEC HIGH: ChayAsync IDOR — no org scoping on similarity re-run (queued for Run-002-B2)
- REQ-12: Feature toggle flags not enforced at runtime by BoMayQuyTrinh

## Known Limitations

- BoTieuChiSnapshot does not include DanhSachMucCongNhan (recognition level labels). Core scoring data IS captured; only recognition level label auditing from snapshot is incomplete.
- Tests are unit-only; no integration test with real DB for criteria save validation.

## Blockers Discovered

None.
