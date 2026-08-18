# Iteration 15 — REQ-21/REQ-43 SEC: Cross-Tenant IDOR in Admin MFA Reset + Password Reset

## What Was Worked On

Two tightly related cross-tenant IDOR vulnerabilities in admin user management:

1. **SEC MEDIUM: GoMfaChoNguoiKhacAsync IDOR** (REQ-21) — Any admin with `NGUOI_DUNG.DAT_LAI_MAT_KHAU` could strip MFA from users in other organizations via `POST /api/v1/xac-thuc/mfa/go/{guid}`. No org-scope check, no audit log, no session revocation.
2. **SEC MEDIUM: DatLaiMatKhauAsync IDOR** (REQ-43) — Same permission allowed cross-org password resets via `POST /api/v1/he-thong/nguoi-dung/{id}/dat-lai-mat-khau`. No org-scope check.

## What Was Accomplished

### Fix 1: GoMfaChoNguoiKhacAsync (DichVuMfa)

- Added `IDichVuPhanQuyen` and `IDichVuNhatKy` as constructor dependencies
- Self-reset guard: `nguoiDungId == _nguoiDungHienTai.Id` throws `DuLieuKhongHopLe` (admin must use TatAsync with password+TOTP to disable own MFA)
- Defense-in-depth: `BatBuocCoQuyenAsync(NguoiDungDatLaiMatKhau, nguoiDungId)` — permission check at service level (controller also checks via policy)
- Org-scope: `BatBuocNguoiDungTrongPhamViAsync` — verifies target user's `DonViId` is in caller's `PhamViTruyCap.DonViIds`. `ToanHeThong` passes, `ChiCaNhan` and null `DonViId` are denied. Throws `KhongTimThay` (404) to avoid leaking existence.
- Session revocation: all active refresh tokens for target user are revoked (matching `DatLaiMatKhauAsync` pattern)
- Audit log: `GO_MFA_NGUOI_KHAC` action logged via `IDichVuNhatKy.GhiAsync` with before-state (MfaEnabled, MfaNgayBat)

### Fix 2: DatLaiMatKhauAsync (DichVuQuanTriNguoiDung)

- Added `INguoiDungHienTai` as constructor dependency
- Added `BatBuocNguoiDungTrongPhamViAsync` call after loading target user — same org-scope enforcement pattern
- Throws `KhongTimThayException` (404) for out-of-scope targets

### Fix 3: Controller comment (MfaController)

- Corrected misleading Swagger comment that falsely claimed audit logging

## Code Review Findings

- **MAJOR (no audit log)**: FIXED — added `IDichVuNhatKy` and `GhiAsync` call
- **MAJOR (no integration test)**: Acknowledged — env lacks Docker for Testcontainers. Documented as gap.
- **MINOR (discarded doiTuongId)**: Known (TD-005). Keeping for audit context.
- **MINOR (null DonViId)**: Correct — org-unbound users managed only by ToanHeThong admins.
- **SUGGESTION (DatLaiMatKhauAsync)**: FIXED in this iteration.

## Security Review Findings

- **HIGH (missing audit log)**: FIXED — GO_MFA_NGUOI_KHAC action with before-state
- **HIGH (self-bypass)**: FIXED — self-reset blocked, must use TatAsync with password+TOTP
- **HIGH (missing session revocation)**: FIXED — refresh tokens revoked matching DatLaiMatKhauAsync pattern
- **MEDIUM (MediatR bypass)**: Pre-existing architecture — DichVuMfa is a DI service, not MediatR handler. Mitigated by service-level checks. Noted for backlog.
- **MEDIUM (code duplication)**: BatBuocNguoiDungTrongPhamViAsync duplicated in two services. Noted for backlog extraction.
- **LOW (timing side-channel)**: Response timing differs for non-existent vs out-of-scope users. Accepted — requires network-level precision.
- **LOW (stale permission cache)**: 2-minute cache TTL is system-wide tradeoff. Accepted.
- **INFO (misleading comment)**: FIXED — updated MfaController Swagger comment
- **INFO (doiTuongId discarded)**: Known (TD-005), pre-existing

## Quality Gate Result

PASS — 7/7 checks, 309 unit tests, 165 integration tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Application/XacThuc/DichVuMfa.cs` — org-scope, self-reset guard, session revocation, audit log
- `src/BlueIdea.Application/QuanTri/DichVuQuanTriNguoiDung.cs` — org-scope for password reset
- `src/BlueIdea.Api/Controllers/MfaController.cs` — corrected Swagger comment
- `docs/requirements/traceability.yaml` — REQ-21 and REQ-43 notes updated
- `docs/autopilot/STATE.json` — iteration 15
- `docs/autopilot/LAST-ITERATION.md` — this file

## Commit Hash

e0b6943 (security fix), state commit pending

## Next Priority Items

1. REQ-12: HanhDongCanChay full dispatch loop (beyond DongBoLienThong + GuiThongBao)
2. TD-005: DoiTuongId object-level scope checking in KiemTraQuyenAsync (MEDIUM, systemic)
3. NguoiDung UseXminAsConcurrencyToken — concurrent recovery code race condition
4. BatBuocNguoiDungTrongPhamViAsync extraction into shared service (code duplication)
5. GoMfaChoNguoiKhacAsync refactor to MediatR Command (pipeline compliance)

## Known Limitations

- No integration test for admin MFA reset IDOR fix (env lacks Docker for Testcontainers)
- No integration test for password reset IDOR fix (same constraint)
- BatBuocNguoiDungTrongPhamViAsync duplicated across DichVuMfa and DichVuQuanTriNguoiDung
- GoMfaChoNguoiKhacAsync bypasses MediatR pipeline (pre-existing, mitigated by service-level checks)
- 2-minute permission cache TTL could allow brief window after role demotion (system-wide tradeoff)

## Blockers Discovered

None.
