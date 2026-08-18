# Iteration 11 — REQ-21/REQ-41 SEC: IDistributedCache SSO State + ADR 0003

## What Was Worked On

SEC MEDIUM: SSO state validation used IMemoryCache, which does not work across multiple API instances behind a load balancer. A state token generated on instance A would fail validation on instance B, breaking SSO CSRF protection in HA deployments.

Batched with: ADR documentation for the QuyTrinhLienThong live-data exception to the snapshot rule, which was identified as a gap in iteration 8's code review.

## What Was Accomplished

### IDistributedCache Migration (SEC MEDIUM → Resolved)

1. **PackageReference added** — `Microsoft.Extensions.Caching.StackExchangeRedis` added to `BlueIdea.Infrastructure.csproj` (version pinned in `Directory.Packages.props` at 8.0.10).
2. **DI registration** — `DangKyHaTang.cs` now registers `IDistributedCache`: Redis-backed (`AddStackExchangeRedisCache`) when `ConnectionStrings:Redis` is configured, with `InstanceName = "blueidea:"` for key isolation; falls back to `AddDistributedMemoryCache()` for tests and single-instance deployments.
3. **XacThucController updated** — SSO state operations (`BatDauSsoAsync`, `DoiMaSsoAsync`) now use `IDistributedCache` with `SetAsync`/`GetAsync`/`RemoveAsync`. `BatDauSso` method became async. `IMemoryCache` remains registered and used by `DichVuCauHinh`, `DichVuPhanQuyen`, `NguonNgayNghiLeTuCsdl` — those are intentionally process-local.

### Code Review Findings Addressed

- **MAJOR (stale comment in TrangSsoTraVe.tsx)**: Fixed — JSDoc updated to accurately describe server-side SSO state storage (IDistributedCache).
- **MINOR (rate limiting asymmetry)**: Fixed — `[EnableRateLimiting("DangNhap")]` added to `BatDauSsoAsync` (initiation endpoint now rate-limited like exchange endpoint).
- **MINOR (Redis InstanceName)**: Fixed — `o.InstanceName = "blueidea:"` prevents key collision on shared Redis instances.
- **MINOR (DoiMaSsoDto FluentValidation)**: Deferred — pre-existing issue not introduced by this change. The null State case is correctly rejected by the cache miss path.

### ADR 0003 — QuyTrinhLienThong Live-Data Exception

Documented in `docs/ADR/0003-lien-thong-du-lieu-song.md`. Integration configs (`quy_trinh_lien_thong`) are deliberately read from live DB data, not from snapshot, because they contain operational settings (endpoints, API keys) that must reflect current state. The ADR explains the rationale and distinction from ADR 0002's snapshot rule.

### Traceability Updates

- REQ-21: Removed SEC MEDIUM gap (IMemoryCache → IDistributedCache). Added B11 notes.
- REQ-41: Removed duplicate SEC MEDIUM gap.
- REQ-16: Updated gap text to reference ADR 0003.

## Quality Gate Result

PASS — 7/7 checks, 309 unit tests, 0 warnings, frontend typecheck + build clean.

## Files Changed

- `src/BlueIdea.Api/Controllers/XacThucController.cs` — IDistributedCache for SSO state, rate limiting on BatDauSsoAsync
- `src/BlueIdea.Infrastructure/BlueIdea.Infrastructure.csproj` — StackExchangeRedis package reference
- `src/BlueIdea.Infrastructure/DangKyHaTang.cs` — IDistributedCache DI registration (Redis + fallback)
- `web/src/features/xac-thuc/TrangSsoTraVe.tsx` — stale security comment fixed
- `docs/ADR/0003-lien-thong-du-lieu-song.md` — new ADR
- `docs/requirements/traceability.yaml` — REQ-21, REQ-41, REQ-16 gaps updated

## Commit Hash

7cb5f0c

## Next Priority Items

1. SEC LOW: MFA recovery codes — upgrade from SHA-256 to Argon2id (REQ-21)
2. SEC LOW: LayHanhDongKhaDungQuery existence oracle — add ICoYeuCauQuyen or return 404 (REQ-23)
3. SEC LOW: GoiYAsync permission bypass — add BatBuocCoQuyenAsync (REQ-23)
4. REQ-12: HanhDongCanChay full dispatch loop (beyond DongBoLienThong)

## Known Limitations

- SSO state TOCTOU race (GetAsync + RemoveAsync not atomic) remains a documented LOW risk — IDistributedCache interface does not expose atomic get-and-delete. Exploitability is low due to IdP single-use code + PKCE.
- DoiMaSsoDto lacks FluentValidation — null State is caught by cache miss but returns DuLieuKhongHopLe instead of 422 chiTietLoi (pre-existing, deferred).
- Integration tests compile but require .NET 8 runtime with Docker for Testcontainers.

## Blockers Discovered

None.
