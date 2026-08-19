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

### TD-004: Database Partitioning Not Yet Applied

**Area**: Database
**Description**: Large tables (`nhat_ky_he_thong`, `nhat_ky_dang_nhap`, `thong_bao`) are designed for monthly partitioning but partitioning is not yet enabled.
**Impact**: Performance may degrade with years of audit data.
**Resolution**: Apply PostgreSQL declarative partitioning when data volume warrants it.
**Priority**: Low (not an issue at current scale)

## Resolved Items

### TD-005: DoiTuongId Discarded in KiemTraQuyenAsync (RESOLVED)

**Resolved by**: Iteration 21
**Resolution**: Option B — removed `DoiTuongId` from `ICoYeuCauQuyen` authorization interface, moved it to `ICoGhiNhatKy` for audit logging. Removed dead `doiTuongId` parameter from `IDichVuPhanQuyen.KiemTraQuyenAsync` and `BatBuocCoQuyenAsync` (was explicitly discarded). Added `BatBuocTrongPhamViAsync` IDOR scope checks to 4 handlers: `CapNhatHoSoCommandHandler`, `NopHoSoCommandHandler`, `RutHoSoCommandHandler`, `LayHanhDongKhaDungQueryHandler`. IDOR protection is now per-service responsibility (documented). 4 integration tests added.
**Commit**: 4a5fdd7
