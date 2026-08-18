# Iteration 14 — REQ-21 SEC: MFA Recovery Code Hardening + TatAsync Bypass Fix

## What Was Worked On

Three tightly related MFA security improvements in DichVuMfa (function 21):

1. **SEC LOW: Recovery code hashing upgrade** — Recovery codes were hashed with unsalted SHA-256, making them brute-forceable (~75 seconds on modern GPU) from a database dump. Upgraded to Argon2id via the existing `IDichVuMatKhau` service.
2. **SEC HIGH: TatAsync null-MfaSecret bypass** — When `GiaiMa(MfaSecret)` returned null (e.g., after AES key rotation), `TatAsync` silently skipped the MFA check and disabled MFA with only a password. Now throws `MaXacThucKhongDung`.
3. **SEC MEDIUM: DungMaKhoiPhuc format guard** — Wrong TOTP codes (6 digits, no dash) fell through to `DungMaKhoiPhuc`, triggering up to 10 sequential Argon2id operations (~20s). Added dash-presence check to short-circuit immediately.

## What Was Accomplished

### Fix 1: Argon2id recovery code hashing

- `BamMaKhoiPhuc` changed from `private static` SHA-256 to `private` instance method using `_matKhau.BamMatKhau()` (Argon2id, 4 iter, 64MB, 4 threads)
- New storage format: `"salt:hash"` (Base64 segments) in same JSON array
- `DungMaKhoiPhuc` changed from `private static` to `private` instance method
- New `KhopMaKhoiPhuc` helper: detects format by `:` separator — Argon2id path uses `_matKhau.KiemTra()` (constant-time via `FixedTimeEquals`), legacy SHA-256 path uses `CryptographicOperations.FixedTimeEquals` on raw bytes
- Backward compatible: existing SHA-256 entries still accepted, will age out naturally as users regenerate

### Fix 2: TatAsync null-secret bypass closed

- When `MfaEnabled=true` but `GiaiMa(MfaSecret)` returns null/empty, now throws `MaXacThucKhongDung` instead of falling through to `XoaSachMfa`
- Prevents MFA disable with only password after AES key rotation or data corruption
- Admin `GoMfaChoNguoiKhacAsync` remains available as recovery path (bypasses MFA intentionally for admin reset)

### Fix 3: Format guard prevents Argon2id amplification

- `DungMaKhoiPhuc` now checks `ma.Contains('-')` before any Argon2id work
- TOTP codes (6 digits, no dash) and random strings short-circuit immediately
- Recovery codes always contain `-` (format: `XXXX-XXXX` from `TaoMaKhoiPhuc`)

## Code Review Findings

- **MAJOR (race condition)**: NguoiDung lacks xmin concurrency token — concurrent recovery code use could authenticate twice. Pre-existing systemic issue; noted for backlog (needs UseXminAsConcurrencyToken on NguoiDung entity).
- **MAJOR (performance)**: TOTP-to-recovery fallthrough causing 10× Argon2id — FIXED by format guard.
- **MINOR (timing-safe comparison)**: Legacy SHA-256 used string.Equals — FIXED with FixedTimeEquals.
- **MINOR (format detection)**: `viTriDauHai > 0` intentionally excludes empty salt (`:` at position 0). Base64 salt is always 24+ chars, so `> 0` is correct for all legitimate values.
- **MINOR (no legacy test)**: No integration test for SHA-256 backward compat path — documented as gap.

## Security Review Findings

- **HIGH (TatAsync bypass)**: CLOSED — null MfaSecret now throws.
- **HIGH (legacy SHA-256 crackable)**: Inherent to backward compat; new codes use Argon2id. Users regenerating codes get full protection.
- **MEDIUM (DoS via Argon2id)**: MITIGATED — format guard eliminates non-recovery-code inputs. Rate limiter (5/min/IP) bounds remaining risk.
- **MEDIUM (GoMfaChoNguoiKhacAsync IDOR)**: Pre-existing, not introduced by this change. Noted for next iteration backlog.
- **LOW (legacy timing)**: FIXED — FixedTimeEquals on byte arrays.
- **LOW (loop position timing)**: Accepted — attacker must already hold valid code; information gained is code slot position only.
- **LOW (no FluentValidation for MFA DTOs)**: Pre-existing, noted for backlog.
- **INFO (migration visibility)**: Accepted — no forced migration; legacy codes age out naturally.

## Quality Gate Result

PASS — 7/7 checks, 309 unit tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Application/XacThuc/DichVuMfa.cs` — Argon2id hashing, format guard, TatAsync fix
- `docs/requirements/traceability.yaml` — REQ-21 notes updated
- `docs/autopilot/STATE.json` — iteration 14
- `docs/autopilot/LAST-ITERATION.md` — this file

## Commit Hash

(pending)

## Next Priority Items

1. SEC MEDIUM: GoMfaChoNguoiKhacAsync IDOR — no org-scope check, cross-tenant MFA reset (REQ-21)
2. REQ-12: HanhDongCanChay full dispatch loop (beyond DongBoLienThong + GuiThongBao)
3. TD-005: DoiTuongId object-level scope checking in KiemTraQuyenAsync (MEDIUM, systemic)
4. NguoiDung UseXminAsConcurrencyToken — concurrent recovery code race condition

## Known Limitations

- No regression test for legacy SHA-256 backward compat (documented as gap; env lacks Docker for Testcontainers)
- Legacy SHA-256 codes remain brute-forceable until users regenerate — acceptable since new codes use Argon2id
- GoMfaChoNguoiKhacAsync has no org-scope check (pre-existing, next iteration)
- No FluentValidation on MFA DTOs (pre-existing, low priority)

## Blockers Discovered

None.
