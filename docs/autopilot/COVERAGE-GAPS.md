# E2E Sub-Function Coverage Gaps

Generated 2026-08-20 from 5-agent audit against docs/00-MASTER-SPEC.md.
Autopilot must fill EVERY gap marked [TODO] before READY_FOR_DEPLOY.
Mark [DONE] after writing + passing the test.

## Priority Legend
- **P0**: Zero coverage — entire feature area untested
- **P1**: Happy-path missing — only auth/validation shell tests exist
- **P2**: Depth gap — test exists but doesn't verify actual behavior

---

## P0 — Zero Coverage (write from scratch)

### REQ-09: Quy trình động (Workflow CRUD) — NO SPEC FILE EXISTS
- [x] API GET /quy-trinh list
- [x] API POST create workflow
- [x] API PUT save diagram (so_do_layout for ReactFlow)
- [x] API POST /kich-hoat (activate)
- [x] API POST /ngung-ap-dung (deactivate)
- [x] API POST /sao-chep (clone)
- [x] API POST /phien-ban-moi (new version)
- [x] Block edit when has in-progress applications (409 QUY_TRINH_DANG_SU_DUNG)
- [x] Auth: unauthenticated → 401, tacgia → 403
- [x] ReactFlow designer UI loads

### REQ-10: Nhánh rẽ (Transitions)
- [x] API CRUD for quy_trinh_truong_hop
- [x] Transition case types: DAT, KHONG_DAT, BO_SUNG_HO_SO, CHUYEN_CAP_CAO_HON, TRA_LAI, RUT_HO_SO
- [x] Condition rule evaluator: AND/OR/NOT, operators =, !=, >, >=, <, <=, IN, CONTAINS, BETWEEN
- [x] Actions: GUI_EMAIL, GUI_SMS, TAO_QUYET_DINH, CAP_NHAT_KET_QUA, YEU_CAU_KY_SO
- [x] Auth 401/403

### REQ-11: Bước xử lý (Workflow Steps)
- [x] API CRUD for quy_trinh_buoc
- [x] Step types: TIEP_NHAN, THAM_DINH, PHAN_CONG_CHAM, CHAM_DIEM, HOP_HOI_DONG, BO_PHIEU, PHE_DUYET, BAN_HANH_QUYET_DINH, CONG_BO, KET_THUC
- [x] Deadline config (calendar days vs working days)
- [x] Required attachment, mandatory comment flags
- [x] la_buoc_bat_dau / la_buoc_ket_thuc flags
- [x] Auth 401/403

### REQ-12: Chức năng bổ sung (Feature Toggles)
- [x] API CRUD for quy_trinh_chuc_nang_bo_sung
- [x] Toggle features: KY_SO, GUI_EMAIL, GUI_SMS, XUAT_BIEU_MAU, BO_PHIEU_KIN, TAO_BIEN_BAN, KIEM_TRA_TRUNG_LAP, CHAM_DIEM_DOC_LAP, CONG_KHAI_KET_QUA
- [x] bat_buoc flag per feature
- [x] cau_hinh JSONB per feature
- [x] Auth 401/403

### REQ-13: Thành phần hồ sơ (Document Components)
- [x] API CRUD for quy_trinh_thanh_phan_ho_so
- [x] Data type: VAN_BAN, TEP, CA_HAI
- [x] Required vs optional flag
- [x] Allowed format JSONB, max size MB, max file count
- [x] Min/max character count validation
- [x] dung_de_kiem_tra_trung_lap flag
- [x] Block edit when workflow is in use
- [x] Auth 401/403

### REQ-14: Trạng thái quy trình (Statuses)
- [x] API CRUD for quy_trinh_trang_thai
- [x] la_trang_thai_ket_thuc flag
- [x] hien_thi_cho_tac_gia flag
- [x] Color, icon, display order config
- [x] Auth 401/403

### REQ-19: Phiên họp (Council Session) — ENTIRE SUBSYSTEM UNTESTED
- [x] API CRUD for phien_hop
- [x] Attendance (diem_danh): mark present/absent
- [x] Voting (bo_phieu): secret vs public ballot
- [x] Vote tally and result
- [x] Minutes (bien_ban): auto-generate from session data
- [x] Digital signature on minutes (BLOCKED_EXTERNAL but test API shell)
- [x] Auth 401/403 per role

---

## P1 — Happy-Path Missing (auth/validation exist, no real data flow)

### REQ-03: Đợt đề nghị — Actions untested
- [x] POST /mo (open round)
- [x] POST /dong (close round)
- [x] POST /khoa (lock round)
- [x] POST /sao-chep (clone round)
- [x] Tab: Quy trình áp dụng
- [x] Tab: Bộ tiêu chí áp dụng
- [x] Tab: Đơn vị áp dụng
- [ ] Business rule: block submission past han_nop_ho_so

### REQ-06: Biểu mẫu xuất — Core feature untested
- [ ] Upload .docx template file
- [ ] System scans {{placeholder}} markers
- [x] Admin maps placeholder → data source (cau_hinh_truong)
- [x] Preview: generate sample file with mapped data
- [x] loai field validation (PHIEU_TIEP_NHAN, PHIEU_DANH_GIA, etc.)

### REQ-08: Quyết định — No write tests
- [x] POST create decision with valid payload + verify persistence
- [x] PUT update decision
- [x] DELETE decision (soft)
- [x] Link sang_kien to decision (quyet_dinh_sang_kien join table)
- [x] Multiple sang_kien per decision
- [ ] File upload for decision document

### REQ-15: Tác nhân — No actor type tested
- [x] Test each of 7 actor types: VAI_TRO, DON_VI, CA_NHAN, PHONG_BAN, CHUC_VU, HOI_DONG, TAC_GIA
- [x] Test each of 4 processing rules: MOT_NGUOI, TAT_CA, DA_SO, LUAN_PHIEN
- [x] Actor CRUD per step

### REQ-16: Tiêu chí — Only SLIDER tested
- [x] Input type: NHAP_SO (number input)
- [x] Input type: LUA_CHON (select dropdown)
- [x] Input type: CO_KHONG (yes/no)
- [x] Weight validation: total must = 100%
- [x] Score range overlap prevention (KHOANG_DIEM_CHONG_LAN)
- [x] Criteria versioning (snapshot per đợt)

### REQ-18: Hội đồng — Write operations missing
- [ ] Conflict of interest detection (author = council member)
- [x] 5 permission flags per member
- [x] chuc_danh constraints
- [x] Council update/delete

### REQ-25: Tệp tin — Security features untested
- [ ] Magic number check (not just extension)
- [ ] Executable file blocking
- [ ] SHA-256 hash computation
- [ ] Presigned URL access (no direct path)
- [ ] PDF/image preview in browser

### REQ-26: Trùng lặp — Pipeline untested
- [ ] OCR text extraction from uploaded file
- [ ] SimHash/MinHash coarse filter
- [ ] TF-IDF cosine + semantic embedding
- [ ] kiem_tra_trung_lap records created
- [ ] UI: side-by-side comparison report
- [ ] PDF report export

### REQ-27: Tiếp nhận — No real acceptance action
- [ ] Happy path: tiepnhan accepts a real submitted application → verify status transition
- [ ] Yêu cầu bổ sung: select missing components + write required info + set deadline
- [ ] Từ chối: write lý do rejection
- [ ] Phiếu tiếp nhận PDF generation
- [ ] Organization scope: user from unit A cannot see unit B applications

### REQ-29: Xử lý — No real workflow transition
- [ ] POST thuc-thi with real sangKienId + truongHopId → verify state changes
- [ ] Required ý kiến when step config requires it
- [ ] Required attachment when step config requires it
- [ ] Batch processing (thuc-thi-hang-loat) happy path with real data
- [ ] Rút hồ sơ (thu-hoi) happy path with real data and lyDo

### REQ-31: Quyết định — Create doesn't verify success
- [ ] POST create with valid payload → GET verify all fields match
- [ ] POST cong-bo happy path (congKhai=true, congKhai=false)
- [ ] PUT update decision
- [ ] Link multiple sang_kien to one quyết định

### REQ-33: Phân công — No real assignment
- [ ] POST phan-cong with real hoiDongId + sangKienId + thanhVienId → verify assigned
- [ ] Conflict of interest: council member who is application author → excluded
- [ ] Auto-assign endpoint
- [ ] Deadline setting in phan-cong

### REQ-34: Đánh giá — No real score submission
- [ ] POST luu-nhap with real score data (all criteria filled) → verify saved
- [ ] POST gui with full score data → verify phiếu status changes
- [ ] Lock phiếu after submission (inputs disabled)
- [ ] Secretary reopens submitted phiếu
- [ ] Auto-compute total score by weight

### REQ-35: Tổng hợp — No real aggregation
- [ ] POST tong-hop with real hoiDongId + dotDeNghiId → verify scores aggregated
- [ ] High/low score exclusion (loai_bo_diem_cao_thap=true)
- [ ] Score visibility: members cannot see each other's scores before submission
- [ ] Recognition level from diem_trung_binh
- [ ] Score matrix structure: rows = apps, columns = members, cells = scores

### REQ-38: Báo cáo tùy chỉnh — No execution tested
- [ ] Select template → set parameters → run → verify results returned
- [ ] Excel export of custom report
- [ ] PDF export of custom report

### REQ-43: Người dùng — All writes missing
- [x] POST create user with valid payload → verify persisted
- [x] PUT update user
- [x] DELETE (soft) user — N/A: system uses PATCH /trang-thai?trangThai=KHOA instead
- [x] PATCH lock user (status KHOA)
- [x] PATCH unlock user
- [x] Reset password
- [x] Assign role + data scope

### REQ-44: Đơn vị — All writes missing
- [x] POST create unit
- [x] PUT update unit
- [x] DELETE unit
- [x] Move branch (change donViChaId)

### REQ-45: Vai trò — All writes missing
- [x] POST create role with valid payload → verify persisted
- [x] PUT update role (name, permissions)
- [x] DELETE role (non-system)
- [x] Cannot delete system role (laHeThong=true) → error
- [x] Clone role
- [x] Permission matrix: assign/revoke permissions

### REQ-46: Cấu hình — All writes missing
- [x] PUT config update → GET verify new value persisted
- [x] POST create manual backup — N/A: backup is shell-only, API is read-only status
- [x] POST restore from backup — N/A: backup is shell-only
- [x] Holiday PUT update
- [x] lapLaiHangNam behavior verification

### REQ-48: Menu — All writes missing
- [x] POST create menu item
- [x] PUT update menu item
- [x] DELETE menu item
- [x] Reorder (drag-and-drop API)
- [x] Assign permission (quyen_ma) to menu item
- [x] Toggle hienThi (enable/disable)

### REQ-50: Email/SMS — All writes missing
- [x] POST create notification template
- [x] PUT update template
- [x] DELETE template
- [ ] POST gui-thu (test send)
- [x] Template variable rendering preview

---

## P2 — Depth Gaps (test exists but doesn't verify behavior)

### Cross-cutting: Filter result verification
- [x] REQ-28: Every filter test must verify returned items MATCH the filter criteria
- [ ] REQ-33: trangThai=CHUA_CHAM filter → verify all returned items are CHUA_CHAM
- [x] REQ-36: Search results contain the search keyword
- [ ] REQ-37: Report filters produce correct data

### Cross-cutting: Sort order verification
- [x] REQ-28: sapXep=ngayNop&huong=desc → verify items are actually in descending order
- [ ] REQ-31: Sort assertions must compare adjacent item values, not just check HTTP 200

### Cross-cutting: Export content validation
- [x] REQ-37: Download Excel/PDF and verify content-length > 0 and correct content-type
- [ ] REQ-40: Parse exported file headers or first bytes to confirm format

### Cross-cutting: Catalog operations (all REQ-01 to REQ-08)
- [ ] PATCH /{id}/trang-thai (enable/disable status toggle) for each catalog
- [ ] POST /import Excel bulk import for at least 2 catalogs
- [ ] GET /export Excel export for at least 2 catalogs
- [ ] Delete-when-referenced returns 409 with reference list for at least 2 catalogs

### REQ-21: Auth depth
- [ ] Full MFA TOTP enrollment: POST ghi-danh → QR data → POST xac-nhan-ghi-danh with valid TOTP
- [ ] Old refresh token invalidated after rotation (POST with old token → 401)
- [ ] Password reuse prevention: change password to same as old → error

### Cross-cutting: Organization scope (IDOR)
- [ ] Login as user in org A, try to GET data from org B → verify 403 or empty result
- [ ] At least 2 endpoints tested for cross-org access
