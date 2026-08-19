# Technical Debt

Known limitations and improvement opportunities. Items are ordered by priority.

## Current Items

### TD-001: Semantic Embedding is Lexical Only

**Area**: AI/Similarity
**Description**: `BoNhungBamTuVung` uses hashing trick on word/bigram tokens, not a learned model. It detects word-level overlap but misses semantic paraphrasing.
**Impact**: Lower recall on paraphrased content in similarity detection.
**Resolution**: Load ONNX sentence-transformer model via `IBoNhungVanBan`. Architecture supports this without code changes.
**Priority**: Medium (lexical matching still catches most cases)

### TD-003: No Monitoring/Alerting

**Area**: Operations
**Description**: No application performance monitoring (APM), error tracking, or alerting beyond Serilog + Seq.
**Impact**: Production issues may not be detected proactively.
**Resolution**: Consider adding health check dashboard, error rate alerting, or APM integration.
**Priority**: Low (Seq provides basic log monitoring)

### TD-004: Database Partitioning Not Yet Applied

**Area**: Database
**Description**: Large tables (`nhat_ky_he_thong`, `nhat_ky_dang_nhap`, `thong_bao`) are designed for monthly partitioning but partitioning is not yet enabled.
**Impact**: Performance may degrade with years of audit data.
**Resolution**: Apply PostgreSQL declarative partitioning when data volume warrants it.
**Priority**: Low (not an issue at current scale)

### TD-007: No Load Testing Evidence

**Area**: Performance
**Description**: The internal spec targets P95 < 500ms and 500 concurrent users, but no load test has been run or recorded. (The customer requirement itself is qualitative — "responds quickly", "handles many concurrent users" — so this is a self-imposed target.)
**Impact**: Concurrency limits are unknown before go-live.
**Resolution**: Run a k6/JMeter scenario against a production-like environment and record results in the acceptance dossier.
**Priority**: Medium (before final acceptance)

### TD-008: USB Token Client Step Is Manual

**Area**: Digital signature
**Description**: The three-step USB token flow works end to end but the client-side signing step is copy/paste of the hash and signature.
**Impact**: Usable but awkward for daily signing.
**Resolution**: Wire the vendor's browser plugin/local port once the unit picks a CA provider. No server change needed.
**Priority**: Low (depends on external vendor choice)

## Resolved Items

### TD-006: WAL Archiving Not Enabled (RESOLVED)

**Resolved by**: Rà soát 19/08/2026
**Resolution**: `archive_mode=on` with `archive_timeout=300` enabled in `deploy/docker-compose.prod.yml`, WAL shipped to a host directory outside the Postgres data volume. Point-in-time recovery procedure documented in `TAI-LIEU-QUAN-TRI-VAN-HANH.md` section 4. Previously only a daily `pg_dump` existed, so the effective RPO was ~24h against the ≤1h target.

### TD-009: Configuration Flags With No Runtime Effect (RESOLVED)

**Resolved by**: Rà soát 19/08/2026
**Resolution**: Four admin-configurable flags were stored but never read at runtime — `thanh_phan_ho_so.dung_de_kiem_tra_trung_lap` (REQ-13), `quy_trinh_trang_thai.hien_thi_cho_tac_gia` (REQ-14), `quy_trinh_buoc.canh_bao_truoc_han_gio` (REQ-11), and the whole `cau_hinh_cap_phe_duyet` table (REQ-05). All four now drive behaviour, each with an integration or unit test. This class of defect is the most dangerous at acceptance: the admin toggles something, the system reports success, and nothing changes.

### TD-010: PAdES Verification Always Reported "No Signature" (RESOLVED)

**Resolved by**: Rà soát 19/08/2026
**Resolution**: `XacMinhAsync` fed the signed PDF into `SignedCms` as if it were a detached CMS blob, which throws, so verifying a PAdES-signed decision always answered "no signature found". Verification now detects PDF/XML containers and checks the embedded signature over the `/ByteRange` (PAdES) or the XML-DSig block (XAdES).

### TD-002: No Frontend Automated Tests (RESOLVED)

**Resolved by**: Iterations 31-35
**Resolution**: 337 Playwright E2E tests across 14 spec files covering all non-blocked requirements. Tests run across 4 viewports (320px-1280px), cover authentication, catalogs, workflow, scoring, councils, submissions, processing, decisions, evaluation, search, reports, mobile responsive, and full lifecycle flows. REQ-41 (SSO/IOC) remains BLOCKED_EXTERNAL — not a test gap.
**Commit**: 9ff38fd

### TD-005: DoiTuongId Discarded in KiemTraQuyenAsync (RESOLVED)

**Resolved by**: Iteration 21
**Resolution**: Option B — removed `DoiTuongId` from `ICoYeuCauQuyen` authorization interface, moved it to `ICoGhiNhatKy` for audit logging. Removed dead `doiTuongId` parameter from `IDichVuPhanQuyen.KiemTraQuyenAsync` and `BatBuocCoQuyenAsync` (was explicitly discarded). Added `BatBuocTrongPhamViAsync` IDOR scope checks to 4 handlers: `CapNhatHoSoCommandHandler`, `NopHoSoCommandHandler`, `RutHoSoCommandHandler`, `LayHanhDongKhaDungQueryHandler`. IDOR protection is now per-service responsibility (documented). 4 integration tests added.
**Commit**: 4a5fdd7
