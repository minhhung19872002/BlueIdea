# Autopilot Iteration 27

## Summary

Bulk verification iteration: promoted 16 requirements from IMPLEMENTED_NOT_VERIFIED to VERIFIED by running existing integration tests against real PostgreSQL via Testcontainers. No code changes — purely evidence-gathering and traceability update.

## Requirements Verified

| Requirement | Test Suite | Tests |
|---|---|---|
| REQ-01 (Linh vuc) | NhapDanhMucVaOpenApiTests + HopDongApiDanhMucTests + SapXepVaPhanCapTests | Import CRUD, /chon contract, hierarchy |
| REQ-06 (Bieu mau xuat) | BieuMauVaPhieuTests + NhapXuatVaCauHinhTests | Template CRUD, preview, PDF, placeholder mapping |
| REQ-07 (Bieu mau thong ke) | BieuMauThongKeTests | CRUD, column validation, export format enforcement, PDF |
| REQ-10 (Cau hinh truong hop) | NhanhTheoDuLieuTests | Data-condition branching (3 conditions) in full workflow |
| REQ-14 (Trang thai buoc xu ly) | NhanhTheoDuLieuTests | trangThaiTong transitions through workflow lifecycle |
| REQ-17 (Danh sach nhom tieu chi) | NhanhTheoDuLieuTests | Criteria groups read during real scoring |
| REQ-18 (Cau hinh tieu chi dong) | NhanhTheoDuLieuTests | Per-criteria scoring with diemToiDa |
| REQ-28 (Danh sach ho so) | SapXepVaPhanCapTests | Sorting, NULLS LAST, direction-only default |
| REQ-33 (Danh sach ho so danh gia) | NhanhTheoDuLieuTests | Scoring assignment in full workflow |
| REQ-34 (Danh gia ho so) | NhanhTheoDuLieuTests | Council scoring submission + aggregation |
| REQ-35 (Phieu danh gia) | BieuMauVaPhieuTests | Evaluation form ZIP export with PDF entries |
| REQ-44 (Quan ly don vi) | ToChucVaMenuTests | Cycle prevention, self-parent guard, org merge |
| REQ-45 (Quan ly vai tro) | MauThongBaoVaVaiTroTests | Role clone with permission preservation |
| REQ-46 (Cau hinh he thong) | CauHinhCoHieuLucTests + MauThongBaoVaVaiTroTests | Config enforcement, brand images, holidays |
| REQ-48 (Cau hinh menu) | ToChucVaMenuTests | CRUD, WEB/MOBILE tree separation, drag-drop reorder |
| REQ-50 (Cau hinh email va SMS) | MauThongBaoVaVaiTroTests + NhapXuatVaCauHinhTests | Template CRUD, preview, config validation |

## Test Suites Run

| Test Suite | Tests | Result |
|---|---|---|
| NhanhTheoDuLieuTests | 3 | PASS |
| ToChucVaMenuTests | 5 | PASS |
| BieuMauVaPhieuTests | 5 | PASS |
| BieuMauThongKeTests | 6 | PASS |
| MauThongBaoVaVaiTroTests | 5 | PASS |
| CauHinhCoHieuLucTests | 9 | PASS |
| SapXepVaPhanCapTests | 4 | PASS |
| HopDongApiDanhMucTests | 1 | PASS |
| NhapXuatVaCauHinhTests | 16 | PASS |
| NhapDanhMucVaOpenApiTests | 6 | PASS |
| **Total** | **60** | **ALL PASS** |

## Quality Gate

- Result: PASS (8/8)
- Unit tests: 501
- Integration tests: 197
- Warnings: 0

## Requirement Score Update

- Before: 13 VERIFIED, 32 IMPLEMENTED_NOT_VERIFIED, 4 PARTIAL, 2 BLOCKED_EXTERNAL
- After: 29 VERIFIED, 16 IMPLEMENTED_NOT_VERIFIED, 4 PARTIAL, 2 BLOCKED_EXTERNAL

## Commits

- f9ffc06 — chore: verify 16 requirements with runtime integration test evidence (iteration 27)

## Also Pushed

- Pushed 3 unpushed commits from iteration 26 (74dcbc5)

## Files Changed

- `docs/requirements/traceability.yaml` — 16 requirements promoted to VERIFIED with test evidence

## Next Priority

Remaining IMPLEMENTED_NOT_VERIFIED (16):
1. REQ-02 (Doi tuong) — needs catalog CRUD integration test
2. REQ-03 (Dot de nghi) — needs lifecycle (mo/dong/khoa) integration test
3. REQ-04 (Loai tac gia) — needs catalog CRUD integration test
4. REQ-05 (Don vi phe duyet) — needs tree-path auto-calculation test
5. REQ-09 (Cau hinh quy trinh) — needs workflow config CRUD integration test
6. REQ-11 (Cau hinh buoc xu ly) — needs step config CRUD integration test
7. REQ-12 (Chuc nang bo sung) — 2 remaining unimplemented actions
8. REQ-13 (Thanh phan ho so) — per-step components gap
9. REQ-15 (Tac nhan xu ly) — needs all 7 actor types integration test
10. REQ-16 (Cau hinh lien thong) — needs workflow-triggered sync integration test
11. REQ-23 (Quan ly ho so) — needs diff accuracy and withdrawal tests
12. REQ-25 (Tap tin dinh kem) — needs blocked extensions, ClamAV tests
13. REQ-26 (Kiem tra trung lap) — ONNX model BLOCKED_EXTERNAL for semantic
14. REQ-30 (Theo doi ho so) — needs timeline/overdue tests
15. REQ-38-40 (Reports) — needs dedicated endpoint tests
16. REQ-42 (Mobile) — needs responsive breakpoint tests
17. REQ-47 (Cau hinh don vi) — needs document config tests
18. REQ-51 (Cau hinh thong tin sang kien) — needs config behavior tests

## Blockers

None. Docker is available for Testcontainers.
