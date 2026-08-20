# Technical Debt

Known limitations and improvement opportunities. Items are ordered by priority.

## Current Items

### TD-004: Database Partitioning Not Yet Applied

**Area**: Database
**Description**: Large tables (`nhat_ky_he_thong`, `nhat_ky_dang_nhap`, `thong_bao`) are designed for monthly partitioning but partitioning is not yet enabled.
**Impact**: Performance may degrade with years of audit data.
**Resolution**: Apply PostgreSQL declarative partitioning when data volume warrants it.
**Priority**: Low (not an issue at current scale)

### TD-008: USB Token Client Step Is Manual

**Area**: Digital signature
**Description**: The three-step USB token flow works end to end but the client-side signing step is copy/paste of the hash and signature.
**Impact**: Usable but awkward for daily signing.
**Resolution**: Wire the vendor's browser plugin/local port once the unit picks a CA provider. No server change needed.
**Priority**: Low (depends on external vendor choice)

## Dropped From Scope

### TD-007: No Load Testing Evidence (DROPPED 2026-08-20)

Dropped at the owner's decision. The customer requirement is qualitative ("responds quickly",
"handles many concurrent users"); the P95 < 500ms / 500-concurrent target was self-imposed, and a
load test run anywhere other than the real 1 vCPU / 2GB VM would produce numbers that say nothing
about production. Re-open this if the investor asks for measured figures at acceptance.

## Resolved Items

### TD-003: No Monitoring/Alerting (RESOLVED — proactive alerting)

**Resolved by**: Rà soát 20/08/2026
**Resolution**: Errors used to sit in `nhat_ky_loi` and Seq, visible only to whoever opened those
screens — a 2 a.m. error burst could go unnoticed until the next morning. The `canh-bao-suc-khoe`
job (every 15 min) now watches two signals and notifies every system administrator through the
existing notification bell, at `CAO` priority, linking straight to the error log:

- unhandled `LOI` / `NGHIEM_TRONG` entries in the last window exceeding a threshold;
- the email/SMS queue backing up (`CHO_GUI` + `LOI`), which otherwise looks like silence to users
  rather than like a broken SMTP configuration.

Repeat suppression is per administrator over a configurable window, so a long outage produces one
alert to act on rather than dozens. All four numbers are configuration (`GiamSat:*`), changeable
without a rebuild. Test:
`LuongBoSungTests.Lo_Loi_Vuot_Nguong_Thi_Quan_Tri_Duoc_Canh_Bao_Mot_Lan`.

**Not covered**: this is not APM. There is still no request-level tracing, latency histogram or
external uptime probe; `/health` and `/health/ready` remain the integration points for an external
monitor if the unit deploys one.

### TD-001: Semantic Embedding is Lexical Only (RESOLVED — code side)

**Resolved by**: Rà soát 20/08/2026
**Resolution**: `BoNhungOnnx` runs a Vietnamese sentence-transformer through ONNX Runtime in-process
(no third-party AI API — ADR 0001 holds), with a hand-written WordPiece tokenizer reading the
model's own `vocab.txt`. Selected by configuration (`Ai:Nhung:*`); with no model configured the
system keeps using `BoNhungBamTuVung` exactly as before.

Three failure modes are handled rather than left to bite later:
- **Wrong vector size** → refuses to load at startup with a clear message, instead of writing
  vectors the `vector(768)` column will reject one row at a time.
- **Missing files** → warns and falls back, because losing semantic search beats failing to boot.
- **Model change invalidates stored vectors** → each chunk records which model produced it
  (`sang_kien_doan_van.mo_hinh_nhung`); semantic search only compares vectors from the *current*
  model, and the `nhung-lai-doan-van` job re-embeds the old ones every 10 minutes until the store
  is clean. Without this, switching models would leave search silently wrong rather than briefly
  empty.

**Tests**: `BoNhungOnnxTests` (10) run real ONNX inference against a tiny purpose-built model
committed under `tests/BlueIdea.UnitTests/TaiNguyen/`, covering tokenization, `##` continuation,
`[UNK]`, truncation, mask-aware mean pooling, L2 normalisation and the startup dimension check.
`LuongBoSungTests.Doi_Mo_Hinh_Nhung_Thi_Bo_Qua_Vector_Cu_Va_Nhung_Lai` covers the switch-over.

**Remaining (not code)**: the unit still has to supply the model file itself — pick a BERT-family
Vietnamese model exported to ONNX with a `vocab.txt`, put it on the server, fill in `Ai:Nhung:*`.
SentencePiece/BPE models (PhoBERT) are not supported by the tokenizer yet.

### TD-013: Dead Application Code (RESOLVED)

**Resolved by**: Rà soát 20/08/2026
**Resolution**:
- `DichVuCaptcha.DonDepAsync` existed but no schedule called it, so `ma_xac_thuc_tam` (CAPTCHA
  challenges *and* password-reset OTPs — both types share the table) grew without bound. Now a
  recurring job `don-ma-xac-thuc-tam` runs at 03:00 daily, cron overridable via
  `CongViecNen:Lich:DonMaXacThuc`. Test:
  `LuongBoSungTests.Cong_Viec_Don_Ma_Xac_Thuc_Chi_Xoa_Ban_Ghi_Het_Han` — expired rows go, valid
  rows stay.
- `DichVuWorkflow.KhoiTaoAsync` (and its `IWorkflowEngine` contract member) duplicated the
  submission path in `SangKienCommands`. It was a footgun, not just dead weight: it starts a case
  without the component checklist validation and follow-up scheduling the real path performs.
  Deleted from both the interface and the implementation.
- `DichVuKiemTraTrungLap.DanhDauDaXemXetAsync` (by check id) was never wired; only the
  `...TheoSangKienAsync` variant is reachable from the UI. Deleted.



### TD-011: Remaining Config Flags With No Runtime Effect (RESOLVED)

**Resolved by**: Rà soát 20/08/2026

Five more admin-settable flags were stored but never read. Each was either wired to real behaviour
or removed from the UI/API so nobody can set something inert:

| Flag | Outcome |
|---|---|
| `quy_trinh_trang_thai.la_trang_thai_ket_thuc` | **Wired.** A transition that assigns a status marked "kết thúc" now ends the case: no next step, no deadline, `NgayHoanThanh` set. Previously the engine only looked at the step-level `LaBuocKetThuc`, so ticking the status changed nothing. Tests: `BoMayQuyTrinhTests.Trang_Thai_Ket_Thuc_Dung_Ho_So_Lai_Du_Con_Buoc_Ke_Tiep` + a regression test proving unticked statuses still flow on. |
| `don_vi.la_don_vi_phe_duyet` | **Wired.** The server now rejects an approval level pointing at a unit that is not marked as an approving unit (422), and `GET /don-vi/chon?chiDonViPheDuyet=true` feeds the approval-level screen so the list cannot offer a unit that will be refused. Test: `BienBanVaCauHinhTests.Don_Vi_Chua_Danh_Dau_Phe_Duyet_Khong_Khai_Lam_Cap_Xet_Duoc`. |
| `bo_tieu_chi.tu_dong_tong_hop` | **Wired.** When the last assigned evaluator submits their form, the score aggregation runs automatically (no `NguoiKetLuanId` recorded — nobody concluded, the system did). The aggregation core was split out of `TongHopDiemAsync` so the automatic path does not need the `DANH_GIA.TONG_HOP` permission the submitting member lacks. The flag is now settable on the criteria-set form, along with `loai_bo_diem_cao_thap` — which had real scoring effect but no UI, so creating a criteria set from the screen always reset it to the DTO default. Evidence: `LuongNghiepVuTests` asserts the score reaches the application *before* anyone presses "Tổng hợp điểm". |
| `bo_tieu_chi.cho_phep_cham_doc_lap` | **Removed from the API.** Council members always score independently (individual scores stay hidden until the form is submitted), so the "off" position has no meaning to implement. The column stays with a comment pointing here; no migration. |
| `quy_trinh_lien_thong_buoc.dong_bo_hai_chieu` | **Removed from UI and API.** Two-way sync needs an inbound write path; `/api/public/v1` is deliberately read-only, and opening a write path requires the city's real IOC / TĐKT spec (REQ-41, BLOCKED_EXTERNAL). The form previously promised a behaviour that did not exist. |
| `cau_hinh_menu.mo_tab_moi` | **Wired.** A menu row marked "mở tab mới" now opens in a new tab (`window.open`, `noopener,noreferrer`) and says so in its tooltip. |

### TD-012: Per-Component CRUD API For Dossier Components Had No Caller (RESOLVED)

**Resolved by**: Rà soát 20/08/2026
**Resolution**: `TrangThanhPhanHoSo.tsx` saved through `apiQuyTrinh.luuSoDo(...)` — it re-sent the
whole workflow diagram — so the five per-component endpoints had no caller and the concurrency
benefit claimed in `TRANG-THAI-TRIEN-KHAI.md` was not real. The screen now diffs against the loaded
server state and issues only POST / PUT / DELETE for rows that actually changed, plus
`PUT .../sap-xep` when the order moved; it also shows what is about to be sent ("1 dòng thêm mới,
2 dòng đã sửa") and names the offending row when the server rejects one. Reorder is driven by
up/down buttons — previously there was no way to change component order at all.
**Tests**: `ThanhPhanVaBoNhoDemTests.Sap_Xep_Thanh_Phan_Doi_Thu_Tu_Checklist`,
`ThanhPhanVaBoNhoDemTests.Sap_Xep_Thanh_Phan_Doi_Hoi_Quyen_Cau_Hinh_Quy_Trinh`, and 4 E2E tests in
`04-quy-trinh-tieu-chi.spec.ts` — one of which asserts the save issues `POST .../thanh-phan-ho-so`
and **no** `PUT .../so-do`.

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
