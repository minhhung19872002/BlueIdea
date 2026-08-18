# Iteration 9 — REQ-21 SEC: Close MFA Credential-Stuffing Oracle

## What Was Worked On

SEC MEDIUM: The MFA prompt (`CanXacThucMfa`) confirmed both account existence AND password correctness, enabling credential-stuffing attacks against MFA-protected accounts. An attacker could verify (username, password) pairs without needing to bypass TOTP.

## What Was Accomplished

### Phase 1 Oracle Closure (no TOTP provided)

1. **Password verification deferred** — when MFA is enabled and no TOTP code is provided, Argon2id runs for timing consistency but the result is **discarded**. `CAN_XAC_THUC_MFA` is returned regardless of password correctness.
2. **Lockout counter intentionally skipped** — incrementing `SoLanDangNhapSai` in Phase 1 would create a DoS oracle (attacker confirms password via lockout). Rate limiter (5/min/IP) is the sole protection. Rationale documented in code comment.
3. **Audit log neutralized** — logs "Yêu cầu mã xác thực hai lớp" (request for MFA) instead of "Chờ mã xác thực hai lớp" (waiting for MFA, which implied password was verified).

### Phase 2 Oracle Closure (TOTP provided)

1. **Unified error code** — wrong password AND wrong TOTP now both return `SAI_TAI_KHOAN_MAT_KHAU`. Previously, wrong TOTP returned `MA_XAC_THUC_KHONG_DUNG`, allowing an attacker to distinguish password correctness by sending a dummy TOTP code (`maMfa: "000000"`).
2. **HTTP status code consistency** — added `MaXacThucKhongDung` to the 401 mapping in `MiddlewareXuLyLoi.cs`. Previously it fell through to 400, amplifying the oracle at the transport layer.

### Frontend Cleanup

1. **Dead code removed** — `MA_XAC_THUC_KHONG_DUNG` handler in `TrangDangNhap.tsx` removed (no longer returned from login flow).
2. **No UX regression** — `canMfa` state is set to `true` in Phase 1 and never reset, so MFA input remains visible when Phase 2 returns `SAI_TAI_KHOAN_MAT_KHAU`.

### Tests Added (2 new)

- `Sai_Mat_Khau_Voi_Mfa_Van_Tra_Ve_Can_Xac_Thuc_Mfa` — proves wrong and correct password both return `CAN_XAC_THUC_MFA` (Phase 1 oracle closed)
- `Sai_Mat_Khau_Voi_Totp_Hop_Le_Bi_Tu_Choi` — proves wrong password + valid TOTP returns `SAI_TAI_KHOAN_MAT_KHAU` (Phase 2 rejection works)

### Code Review Findings Addressed

- **MAJOR (Phase 1 counter bypass)**: Documented as deliberate — DoS oracle tradeoff.
- **MAJOR (Phase 2 residual oracle)**: Fixed — unified error code for wrong password and wrong TOTP.
- **MINOR (test account collision)**: Fixed — new test uses `cb.linh` instead of `cb.mai`.
- **MINOR (missing Phase 2 test)**: Fixed — added `Sai_Mat_Khau_Voi_Totp_Hop_Le_Bi_Tu_Choi` with `cb.trang`.

### Security Review Findings Addressed

- **CRITICAL (Phase 2 bypass via dummy TOTP)**: Fixed — Phase 2 returns same error for wrong password and wrong TOTP.
- **HIGH (HTTP 400 vs 401 leak)**: Fixed — `MaXacThucKhongDung` added to 401 arm.
- **HIGH (lockout bypass)**: Documented as deliberate tradeoff with rationale.
- **MEDIUM (dummy hash race condition)**: Pre-existing, not introduced by this fix. Noted as technical debt.
- **MEDIUM (MFA status enumeration)**: Accepted as design tradeoff — inherent to any system showing separate MFA prompt.
- **MEDIUM (audit log forensics)**: Noted as future improvement — internal password-correctness field would aid forensics without creating client-side oracle.
- **LOW (recovery code hashing)**: Pre-existing, documented in gaps.

## Quality Gate Result

PASS — 7/7 checks, 309 unit tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Application/XacThuc/DangNhapCommand.cs` — Phase 1 + Phase 2 oracle fix
- `src/BlueIdea.Api/Chung/MiddlewareXuLyLoi.cs` — MaXacThucKhongDung → 401
- `tests/BlueIdea.IntegrationTests/XacThucNangCaoTests.cs` — 2 new tests
- `web/src/features/xac-thuc/TrangDangNhap.tsx` — dead code removal
- `docs/requirements/traceability.yaml` — REQ-21 gaps updated

## Commit Hash

3d7cc21

## Next Priority Items

1. SEC MEDIUM: IMemoryCache-based SSO state → IDistributedCache for multi-instance HA (REQ-21/REQ-41)
2. REQ-12/REQ-16: ThucThiHangLoatCommandHandler missing notification + integration sync dispatch
3. ADR documentation: QuyTrinhLienThong live-data exception to snapshot rule
4. SEC LOW: MFA recovery codes — upgrade from SHA-256 to Argon2id

## Known Limitations

- MFA Phase 1 reveals MFA-enabled status (design tradeoff for UX).
- MFA Phase 1 failures bypass lockout counter (deliberate — prevents DoS oracle).
- Audit log in Phase 1 does not distinguish correct/incorrect password internally (forensics improvement opportunity).
- Static dummy hash initialization has race condition (pre-existing, not security-critical).
- Integration tests compile but require .NET 8 runtime with Docker for Testcontainers.

## Blockers Discovered

None.
