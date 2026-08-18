# Technical Debt

Known limitations and improvement opportunities. Items are ordered by priority.

## Current Items

### TD-001: Semantic Embedding is Lexical Only

**Area**: AI/Similarity
**Description**: `BoNhungBamTuVung` uses hashing trick on word/bigram tokens, not a learned model. It detects word-level overlap but misses semantic paraphrasing.
**Impact**: Lower recall on paraphrased content in similarity detection.
**Resolution**: Load ONNX sentence-transformer model via `IBoNhungVanBan`. Architecture supports this without code changes.
**Priority**: Medium (lexical matching still catches most cases)

### TD-002: No Frontend Automated Tests

**Area**: Testing
**Description**: No Vitest unit tests or Playwright E2E tests exist for the frontend. Verification is currently manual + backend integration tests.
**Impact**: Frontend regressions may go undetected until manual testing.
**Resolution**: Add Vitest for component logic and Playwright for critical user flows.
**Priority**: Medium

### TD-003: No Monitoring/Alerting

**Area**: Operations
**Description**: No application performance monitoring (APM), error tracking, or alerting beyond Serilog + Seq.
**Impact**: Production issues may not be detected proactively.
**Resolution**: Consider adding health check dashboard, error rate alerting, or APM integration.
**Priority**: Low (Seq provides basic log monitoring)

### TD-005: DoiTuongId Discarded in KiemTraQuyenAsync

**Area**: Security/Authorization
**Description**: `DichVuPhanQuyen.KiemTraQuyenAsync` receives a `doiTuongId` parameter but explicitly discards it (`_ = doiTuongId`). The `ICoYeuCauQuyen.DoiTuongId` mechanism documented in AN-TOAN-THONG-TIN.md as "chống IDOR từ gốc" provides no actual object-level scope enforcement. All commands/queries that pass `DoiTuongId` through the pipeline rely on it only for audit logging context (`HanhViGhiNhatKy`), not authorization.
**Impact**: IDOR protection depends entirely on per-service `BatBuocTrongPhamViAsync` / `ApDungPhamViDuLieuAsync` calls. Coverage is uneven — some handlers (e.g., `LayHanhDongKhaDungQuery`) lack explicit scope checks.
**Resolution**: Implement object-level scope checking in `KiemTraQuyenAsync` by verifying the requested object belongs to the caller's accessible organizations, OR remove `DoiTuongId` from the authorization interface and document that IDOR protection is per-service responsibility.
**Priority**: Medium (mitigated by per-service scope checks and workflow engine actor filtering, but creates false security documentation)
**Discovered**: Run-002-B12 security review

### TD-004: Database Partitioning Not Yet Applied

**Area**: Database
**Description**: Large tables (`nhat_ky_he_thong`, `nhat_ky_dang_nhap`, `thong_bao`) are designed for monthly partitioning but partitioning is not yet enabled.
**Impact**: Performance may degrade with years of audit data.
**Resolution**: Apply PostgreSQL declarative partitioning when data volume warrants it.
**Priority**: Low (not an issue at current scale)

## Resolved Items

(None yet — move items here when resolved, with resolution commit)
