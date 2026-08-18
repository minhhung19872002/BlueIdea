# Security Rules — ATTT Level 2 (ND 85/2016, TT 12/2022)

## Authentication

- Password hashing: **Argon2id** (4 iterations, 64 MB memory, 4 threads), timing-safe comparison.
- Account lockout: 5 failed attempts → 15 min lock. CAPTCHA after 3 failures.
- JWT: access token 15 min + refresh token 7 days, rotating. Refresh token stored as SHA-256 hash.
- Reuse of revoked refresh token → revoke ALL sessions for that user.
- Password change → revoke all open sessions.
- Password policy: configurable min length, complexity, no reuse of last N passwords, forced change after N days.
- Login error messages must NOT reveal whether an account exists.
- MFA: TOTP (RFC 6238), anti-replay, 10 recovery codes.
- SSO: OIDC Authorization Code + PKCE, single logout.

## Authorization

- **RBAC**: Roles are data (not enums). Role × permission matrix is admin-configurable.
- **Every Command/Query** goes through `HanhViPhanQuyen` MediatR pipeline → `IDichVuPhanQuyen.BatBuocCoQuyenAsync`.
- **Data scoping** by role: `TOAN_HE_THONG`, `DON_VI`, `DON_VI_VA_CAP_DUOI`, `CA_NHAN`, `TUY_CHINH` — applied at query level to prevent IDOR.
- Workflow actor check: only assigned actors get available actions.
- A frontend-hidden action is NOT authorization.

## Input Validation

- FluentValidation on server is authoritative. Frontend validates for UX only.
- HtmlSanitizer for rich text — never render raw user HTML.

## File Upload

- Check magic number (not just extension).
- Block executable files.
- Compute SHA-256 hash.
- ClamAV malware scan before storage.
- Access via presigned URL with expiry — never expose direct paths.

## Encryption

- AES-256-GCM for sensitive data at rest (CCCD, integration secrets, SMTP passwords).
- HTTPS + TLS 1.2+ mandatory in production.
- Security headers: CSP, X-Frame-Options: DENY, X-Content-Type-Options, Referrer-Policy, Permissions-Policy.

## Rate Limiting

- Global: 100 req/min/IP.
- Login: 5 attempts/min/IP.

## Audit

- All significant operations logged to `nhat_ky_he_thong`: who, when, IP, before/after values.
- Login history in `nhat_ky_dang_nhap`.

## Infrastructure

- Containers run as non-root user.
- PostgreSQL/MinIO/Redis bind to `127.0.0.1` only.
- Strip `Server` and `X-Powered-By` headers.
- Production: no detailed error messages (only generic for 5xx).

## Prohibited

- Do NOT commit secrets, credentials, or connection strings to git.
- Do NOT bypass backend authorization from frontend.
- Do NOT expose sensitive data in API responses (e.g., integration secrets always return masked).
