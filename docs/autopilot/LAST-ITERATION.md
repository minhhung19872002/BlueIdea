# Iteration 5 — REQ-12: Feature Toggle Enforcement in Workflow Engine

## What Was Worked On

REQ-12 (PARTIAL → IMPLEMENTED_NOT_VERIFIED): Feature toggle flags (`ChucNangBoSung`) were stored in config and preserved in workflow snapshots but never consulted by the workflow engine at runtime — admin could configure all 9 feature toggles per step/workflow, but the engine completely ignored them during step execution.

## What Was Accomplished

### Core Fix: Feature Toggle Enforcement in BoMayQuyTrinh

1. **Action-to-feature mapping**: Created `BanDoHanhDongChucNang` dictionary mapping 6 transition actions to their corresponding feature toggle codes:
   - `GUI_EMAIL` → `GUI_EMAIL`
   - `GUI_SMS` → `GUI_SMS`
   - `YEU_CAU_KY_SO` → `KY_SO`
   - `KIEM_TRA_TRUNG_LAP` → `KIEM_TRA_TRUNG_LAP`
   - `TAO_BIEN_BAN` → `TAO_BIEN_BAN`
   - `CONG_BO_KET_QUA` → `CONG_KHAI_KET_QUA`

2. **`LayChucNangBuoc()`**: Returns all enabled feature codes for a step (checking both step-level `BuocId == stepId` and workflow-level `BuocId == null` toggles).

3. **`LocHanhDongTheoChucNang()`**: Filters `HanhDongCanChay` — keeps actions that either don't have a feature-gate mapping (core actions like `TAO_QUYET_DINH`, `CAP_NHAT_KET_QUA`) or whose corresponding feature IS enabled.

4. **`ChuyenBuoc()`**: Now filters `HanhDongCanChay` through `LocHanhDongTheoChucNang` and populates `ChucNangBat` on `KetQuaXuLy`.

5. **`LayHanhDongKhaDung()`**: Now populates `ChucNangBuoc` on each `HanhDongKhaDung` so the frontend knows which features are available for the current step.

### Model Changes

- `KetQuaXuLy.ChucNangBat`: List of enabled feature codes for the step just processed
- `HanhDongKhaDung.ChucNangBuoc`: List of enabled feature codes for the step

### Tests Added (8 new, 1 updated)

- `Hanh_Dong_Can_Chay_Loc_Theo_Chuc_Nang_Bo_Sung_Dang_Bat` — Features ON → actions kept
- `Hanh_Dong_Can_Chay_Bi_Loc_Khi_Chuc_Nang_Khong_Bat` — Features OFF → actions filtered out
- `Hanh_Dong_Khong_Chi_Phoi_Boi_Chuc_Nang_Van_Giu_Nguyen` — Core actions (TAO_QUYET_DINH) unaffected
- `Chuc_Nang_Toan_Quy_Trinh_Ap_Dung_Cho_Moi_Buoc` — Workflow-level toggles apply to all steps
- `Chuc_Nang_Bat_Tra_Ve_Danh_Sach_Chuc_Nang_Dang_Bat_Cho_Buoc` — ChucNangBat populated correctly
- `Hanh_Dong_Kha_Dung_Bao_Gom_Chuc_Nang_Buoc` — HanhDongKhaDung.ChucNangBuoc correct
- `Snapshot_Giu_Nguyen_Chuc_Nang_Bo_Sung_Qua_Vong_Doi` — Snapshot round-trip preserves enforcement
- Updated `Thuc_Thi_Chuyen_Sang_Buoc_Tiep_Theo` — now includes feature toggles to match new behavior

## Quality Gate Result

PASS — 7/7 checks, 288 unit tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Workflow/BoMayQuyTrinh.cs` — Feature toggle enforcement logic
- `src/BlueIdea.Workflow/MoHinh/MoHinhWorkflow.cs` — ChucNangBat/ChucNangBuoc properties
- `tests/BlueIdea.UnitTests/TienIch/XuongDuLieuTest.cs` — ThemChucNangBoSung helper
- `tests/BlueIdea.UnitTests/Workflow/BoMayQuyTrinhTests.cs` — 8 new tests + 1 updated
- `docs/requirements/traceability.yaml` — REQ-12 status PARTIAL → IMPLEMENTED_NOT_VERIFIED
- `docs/autopilot/STATE.json` — iteration state
- `docs/autopilot/LAST-ITERATION.md` — this file

## Commit Hash

(pending)

## Next Priority Items

1. REQ-16 (PARTIAL): Integration sync adapter NOT connected to workflow engine transitions
2. SEC MEDIUM: IMemoryCache-based SSO state needs IDistributedCache for multi-instance HA
3. SEC MEDIUM: MFA prompt (CanXacThucMfa) credential-stuffing oracle
4. Various LOW security gaps (REQ-23, REQ-26)

## Known Limitations

- Feature toggles affect action filtering (what automated actions fire) and are exposed in the API response. Frontend already has toggle UI; backend now enforces them.
- Toggles like `BO_PHIEU_KIN`, `CHAM_DIEM_DOC_LAP`, and `XUAT_BIEU_MAU` are now exposed via `ChucNangBuoc` in the available actions response, but their enforcement at the query level (e.g., hiding individual scores when `CHAM_DIEM_DOC_LAP` is on) requires separate work in the evaluation service (`DichVuDanhGia`).
- No integration test for feature toggles affecting a live workflow (unit tests cover the engine logic).

## Blockers Discovered

None.
