# MASTER PROMPT — XÂY DỰNG "NỀN TẢNG SỐ DÙNG CHUNG PHỤC VỤ HOẠT ĐỘNG SÁNG KIẾN" (PHẦN MỀM SÁNG KIẾN)

> **Cách dùng:** đặt file này vào thư mục gốc repo với tên `docs/00-MASTER-SPEC.md`, rồi mở Claude Code và chạy:
> `Đọc docs/00-MASTER-SPEC.md. Đây là đặc tả đầy đủ. Bắt đầu từ PHASE 0, làm tuần tự từng phase, sau mỗi phase chạy build + test + báo cáo lại cho tôi trước khi sang phase tiếp theo. Không bỏ qua chức năng nào trong bảng truy vết ở Mục 14.`

---

## 0. VAI TRÒ & NGUYÊN TẮC LÀM VIỆC

Bạn là **Senior Full-stack Architect + Tech Lead**, xây dựng một hệ thống phần mềm cấp chính quyền địa phương tại Việt Nam, phục vụ nghiệp vụ **quản lý sáng kiến, cải tiến kỹ thuật** (đăng ký → tiếp nhận → thẩm định → hội đồng chấm điểm → công nhận → thống kê báo cáo).

Nguyên tắc bắt buộc:

1. **Toàn bộ nghiệp vụ, dữ liệu, UI dùng tiếng Việt có dấu (Unicode chuẩn NFC).** Code, tên biến, tên bảng, tên API dùng tiếng Anh hoặc tiếng Việt không dấu snake_case — **nhất quán trong toàn dự án**.
2. **Không hardcode nghiệp vụ.** Quy trình xử lý, tiêu chí chấm điểm, thành phần hồ sơ, biểu mẫu, menu, vai trò — tất cả phải **cấu hình được trên giao diện quản trị**, không sửa code khi đơn vị đổi quy trình.
3. **Không gọi API AI bên thứ ba** (OpenAI, Gemini, Claude API, Azure AI…). Toàn bộ OCR + phân tích trùng lặp phải chạy **on-premise/nội bộ**. Đây là yêu cầu bắt buộc của hồ sơ mời thầu.
4. **Mọi thao tác thay đổi dữ liệu đều phải ghi audit log** (ai, khi nào, từ IP nào, giá trị trước/sau).
5. Ưu tiên **Clean Architecture + CQRS nhẹ**, code dễ đọc, có comment tiếng Việt tại các đoạn nghiệp vụ phức tạp.
6. Mỗi phase kết thúc phải: build thành công, migration chạy được, seed data chạy được, có unit test cho business rule, cập nhật `docs/CHANGELOG.md` và `README.md`.
7. Khi có điểm mơ hồ trong đặc tả: **chọn phương án phổ biến nhất trong hệ thống hành chính Việt Nam**, ghi lại quyết định vào `docs/ADR/` (Architecture Decision Record) và tiếp tục — không dừng lại hỏi.

---

## 1. BỐI CẢNH NGHIỆP VỤ

Hệ thống phục vụ UBND phường/thành phố quản lý hoạt động sáng kiến:

- **Tác giả** (cán bộ, công chức, viên chức, giáo viên, y bác sĩ, người dân) đăng ký hồ sơ sáng kiến theo **đợt đề nghị** (theo năm/quý).
- **Cán bộ tiếp nhận** kiểm tra tính hợp lệ, yêu cầu bổ sung hoặc chuyển vào quy trình xét duyệt.
- **Hội đồng sáng kiến** (cấp cơ sở → cấp thành phố) chấm điểm theo **bộ tiêu chí động**, họp hội đồng, lập biên bản, bỏ phiếu.
- **Người có thẩm quyền** ban hành **quyết định công nhận sáng kiến**.
- Hệ thống thống kê, báo cáo, liên thông sang **hệ thống Thi đua khen thưởng** và **IOC** của thành phố.
- Có **AI kiểm tra trùng lặp/đạo văn** so với kho sáng kiến đã nộp các năm trước.

Vai trò người dùng chuẩn:

| Mã vai trò | Tên | Mô tả |
|---|---|---|
| `TAC_GIA` | Tác giả | Nộp, sửa, rút hồ sơ, theo dõi tiến độ |
| `CAN_BO_TIEP_NHAN` | Cán bộ tiếp nhận | Kiểm tra hợp lệ, tiếp nhận/trả hồ sơ |
| `THU_KY_HOI_DONG` | Thư ký hội đồng | Phân công chấm, tổng hợp điểm, lập biên bản |
| `THANH_VIEN_HOI_DONG` | Thành viên hội đồng | Chấm điểm, nhận xét, bỏ phiếu |
| `CHU_TICH_HOI_DONG` | Chủ tịch hội đồng | Kết luận, ký biên bản |
| `LANH_DAO_PHE_DUYET` | Lãnh đạo phê duyệt | Phê duyệt, ban hành quyết định |
| `QUAN_TRI_DON_VI` | Quản trị đơn vị | Quản lý người dùng trong đơn vị, xem thống kê đơn vị |
| `QUAN_TRI_HE_THONG` | Quản trị hệ thống | Toàn quyền cấu hình |
| `LANH_DAO_XEM` | Lãnh đạo/xem báo cáo | Chỉ đọc dashboard, báo cáo |

> Vai trò là **dữ liệu**, không phải enum cứng: 9 vai trò trên là **seed data**, admin có thể tạo thêm vai trò và gán quyền chi tiết.

---

## 2. TECH STACK BẮT BUỘC

### Backend
- **.NET 8 LTS** (ASP.NET Core Web API), C# 12, nullable enable, `TreatWarningsAsErrors=true`
- **Entity Framework Core 8** + **Npgsql** provider, Code-First Migrations
- **PostgreSQL 16** + extensions: `uuid-ossp`, `pg_trgm` (fuzzy text), `unaccent` (bỏ dấu tiếng Việt), `pgvector` (vector similarity), `pgcrypto`
- **MediatR** (CQRS), **FluentValidation**, **Mapster**
- **Serilog** (sink: Console + File rolling + PostgreSQL table `nhat_ky_he_thong`)
- **Hangfire** (job nền: gửi email/SMS, quét trùng lặp, đồng bộ IOC, nhắc hạn xử lý, backup logic)
- **SignalR** (thông báo realtime, cập nhật trạng thái hồ sơ)
- **MinIO** (S3-compatible, self-hosted) cho lưu trữ tệp; abstraction `IFileStorage` để có thể đổi sang local disk
- **DocumentFormat.OpenXml** + **ClosedXML** (xuất Word/Excel từ biểu mẫu)
- **QuestPDF** (xuất PDF phiếu đánh giá, biên bản)
- **Swashbuckle** (OpenAPI) — bắt buộc tài liệu hoá 100% endpoint
- **xUnit + FluentAssertions + Testcontainers** (integration test với PostgreSQL thật)

### AI / OCR (chạy nội bộ, KHÔNG gọi API ngoài)
- **Tesseract OCR 5** (traineddata `vie` + `eng`) hoặc **PaddleOCR** chạy trong container riêng, expose HTTP nội bộ
- **ONNX Runtime** trong .NET để chạy mô hình embedding tiếng Việt offline (khuyến nghị: `dangvantuan/vietnamese-embedding` hoặc `intfloat/multilingual-e5-base` export sang ONNX)
- **pgvector** lưu embedding, tìm kiếm ANN bằng `HNSW index`
- Thuật toán bổ trợ chạy thuần .NET: **SimHash + MinHash/LSH shingling** cho near-duplicate, **TF-IDF cosine** cho so khớp từ vựng

### Frontend Web
- **React 18 + TypeScript 5 + Vite**
- **Ant Design 5** (`antd`) + `@ant-design/icons` — phù hợp hệ thống hành chính, có sẵn Table/Form/Steps/Tree
- **TanStack Query v5** (server state) + **Zustand** (client state: auth, theme, cấu hình hệ thống)
- **React Router v6** (data router, lazy route)
- **react-hook-form + zod** (form + validation), **dayjs** (locale `vi`)
- **@dnd-kit** (kéo thả thiết kế quy trình, sắp xếp tiêu chí)
- **ReactFlow** (sơ đồ trực quan quy trình động)
- **ECharts for React** (dashboard, biểu đồ thống kê)
- **i18next** (vi mặc định, chuẩn bị sẵn khung en)
- **axios** với interceptor refresh token

### Mobile (Android + iOS)
- **React Native (Expo SDK mới nhất) + TypeScript**, dùng chung `packages/shared-types` và `packages/api-client` với web (monorepo pnpm workspace).
- Nếu team quen Flutter hơn thì có thể thay bằng Flutter 3.x — nhưng **phải giữ nguyên hợp đồng API**.

### Hạ tầng
- **Docker + docker-compose** (api, web, postgres, minio, redis, ocr-service, seq)
- **Redis** (cache cấu hình, distributed lock cho Hangfire, rate limit)
- **Nginx** reverse proxy + HTTPS/TLS 1.2+
- CI: GitHub Actions (build, test, docker image)

---

## 3. CẤU TRÚC REPO

```
sangkien-platform/
├── docs/
│   ├── 00-MASTER-SPEC.md          # file này
│   ├── ADR/                        # quyết định kiến trúc
│   ├── API.md                      # sinh từ OpenAPI
│   ├── DEPLOYMENT.md
│   └── CHANGELOG.md
├── src/
│   ├── SangKien.Domain/            # Entity, Enum, Value Object, Domain Event, Domain Exception
│   ├── SangKien.Application/       # Command/Query, Validator, DTO, Interface, business rules
│   ├── SangKien.Infrastructure/    # EF Core, Repository, Storage, Email/SMS, SSO, Integration
│   ├── SangKien.Workflow/          # Engine quy trình động (runtime + designer contract)
│   ├── SangKien.Scoring/           # Engine tiêu chí động + tính điểm
│   ├── SangKien.Ai/                # OCR client, chunking, embedding, similarity, plagiarism report
│   ├── SangKien.Reporting/         # Sinh biểu mẫu Word/Excel/PDF, báo cáo thống kê
│   ├── SangKien.Api/               # Controllers, Middleware, Auth, SignalR Hub, DI
│   └── SangKien.Shared/            # Constants, Result<T>, PagedResult<T>, helper tiếng Việt
├── tests/
│   ├── SangKien.UnitTests/
│   └── SangKien.IntegrationTests/
├── web/                            # React app
│   ├── src/
│   │   ├── api/                    # api client theo module
│   │   ├── app/                    # router, providers, layout
│   │   ├── components/             # dùng chung
│   │   ├── features/               # theo module nghiệp vụ
│   │   ├── hooks/ lib/ types/ locales/
│   └── vite.config.ts
├── mobile/                         # React Native app
├── ai-service/                     # container OCR (Python FastAPI + Tesseract/PaddleOCR)
├── deploy/
│   ├── docker-compose.yml
│   ├── docker-compose.prod.yml
│   └── nginx/
└── README.md
```

---

## 4. MÔ HÌNH DỮ LIỆU (PostgreSQL)

Quy ước chung cho **mọi bảng**:
- Khóa chính `id uuid PRIMARY KEY DEFAULT gen_random_uuid()`
- Audit: `nguoi_tao_id uuid`, `ngay_tao timestamptz`, `nguoi_sua_id uuid`, `ngay_sua timestamptz`, `da_xoa boolean DEFAULT false` (**soft delete toàn hệ thống**, global query filter trong EF Core)
- Bảng danh mục có thêm: `ma varchar(50) UNIQUE`, `ten varchar(500)`, `mo_ta text`, `thu_tu int`, `trang_thai smallint` (1 hoạt động / 0 ngừng)
- Trường JSON dùng `jsonb`, có GIN index khi cần truy vấn
- Toàn bộ `timestamptz`, lưu UTC, hiển thị theo `Asia/Ho_Chi_Minh`
- Cột text tiếng Việt cần tìm kiếm: tạo thêm cột computed `*_khong_dau` = `unaccent(lower(cot))` + GIN `pg_trgm` index

### 4.1 Nhóm DANH MỤC (chức năng 1–8)

**`linh_vuc`** — Lĩnh vực áp dụng sáng kiến (Giáo dục, CNTT, Quản lý hành chính, Y tế…)
`id, ma, ten, ten_khong_dau, mo_ta, linh_vuc_cha_id (uuid, null → hỗ trợ phân cấp), thu_tu, trang_thai`

**`doi_tuong`** — Đối tượng áp dụng/hưởng lợi (cá nhân, tập thể, học sinh, cán bộ…)
`id, ma, ten, mo_ta, thu_tu, trang_thai`

**`dot_de_nghi`** — Đợt tiếp nhận & xét duyệt
`id, ma, ten, nam int, ky varchar(20) (QUY_1..QUY_4/NAM/DOT_1...), tu_ngay date, den_ngay date, han_nop_ho_so timestamptz, han_cham_diem timestamptz, cap_xet_duyet varchar(30) (CO_SO|THANH_PHO|TINH), quy_trinh_id uuid, bo_tieu_chi_id uuid, don_vi_ap_dung_ids jsonb, trang_thai varchar(20) (NHAP|DANG_MO|DA_DONG|DA_KHOA), tu_dong_khoa boolean, ghi_chu text`
> Business rule: khi `now() > han_nop_ho_so` hoặc `trang_thai = DA_KHOA` → **không cho nộp/sửa hồ sơ mới**. Job Hangfire chạy mỗi 15 phút tự động chuyển `DANG_MO → DA_DONG` khi hết hạn nếu `tu_dong_khoa = true`.

**`loai_tac_gia`** — cá nhân / nhóm tác giả / đơn vị
`id, ma, ten, cho_phep_nhieu_tac_gia boolean, so_tac_gia_toi_da int, thu_tu, trang_thai`

**`don_vi`** — Cơ cấu tổ chức (dạng cây, dùng cho cả đơn vị chủ quản lẫn đơn vị/phòng ban/hội đồng có thẩm quyền phê duyệt)
`id, ma, ten, ten_viet_tat, don_vi_cha_id, cap int, loai varchar(30) (DON_VI|PHONG_BAN|HOI_DONG|UBND), path ltree/varchar (đường dẫn cây, ví dụ /root/ubnd/pgd), dia_chi, dien_thoai, email, nguoi_dai_dien, chuc_vu_nguoi_dai_dien, la_don_vi_phe_duyet boolean, cap_phe_duyet varchar(30), thu_tu, trang_thai`

**`cau_hinh_cap_phe_duyet`** — cấp phê duyệt theo từng đợt/lĩnh vực (chức năng 5)
`id, dot_de_nghi_id (nullable), linh_vuc_id (nullable), don_vi_phe_duyet_id, thu_tu_cap, ghi_chu`

**`bieu_mau_xuat`** — Mẫu biểu xuất dữ liệu (Phiếu tiếp nhận, Biên bản họp hội đồng, Quyết định công nhận…)
`id, ma, ten, loai varchar(50) (PHIEU_TIEP_NHAN|PHIEU_DANH_GIA|BIEN_BAN_HOP|QUYET_DINH|TONG_HOP|KHAC), dinh_dang varchar(10) (DOCX|XLSX|PDF), file_template_id uuid → tep_tin, cau_hinh_truong jsonb, pham_vi_ap_dung jsonb, trang_thai`
> `cau_hinh_truong` mô tả mapping placeholder → nguồn dữ liệu:
> ```json
> [{"placeholder":"{{TEN_SANG_KIEN}}","nguon":"sang_kien.ten_sang_kien","kieu":"text"},
>  {"placeholder":"{{DS_TAC_GIA}}","nguon":"sang_kien_tac_gia[]","kieu":"table","cot":["ho_ten","chuc_vu","don_vi","ty_le_dong_gop"]}]
> ```

**`bieu_mau_thong_ke`** — Mẫu báo cáo thống kê (chức năng 7)
`id, ma, ten, loai_bao_cao, cau_hinh_tieu_chi jsonb (nhóm theo lĩnh vực/đơn vị/tác giả/đợt/kết quả), cau_hinh_cot jsonb, cau_hinh_bo_loc jsonb, dinh_dang_xuat jsonb, trang_thai`

**`quyet_dinh`** — Danh mục loại quyết định công nhận (chức năng 8) + quyết định thực tế
`id, so_quyet_dinh, ngay_ban_hanh date, loai varchar(30) (CAP_CO_SO|CAP_THANH_PHO|CAP_TINH), trich_yeu text, nguoi_ky, chuc_vu_nguoi_ky, don_vi_ban_hanh_id, dot_de_nghi_id, tep_tin_id, da_ky_so boolean, trang_thai`

**`quyet_dinh_sang_kien`** — bảng nối N-N giữa quyết định và sáng kiến được công nhận
`id, quyet_dinh_id, sang_kien_id, muc_cong_nhan varchar(50), ghi_chu`

### 4.2 Nhóm QUY TRÌNH ĐỘNG (chức năng 9–16)

**`quy_trinh`**
`id, ma, ten, mo_ta, cap varchar(30) (CO_SO|THANH_PHO|TINH), phien_ban int, quy_trinh_goc_id uuid (khi sao chép/nâng phiên bản), pham_vi_ap_dung jsonb ({dot_de_nghi_ids, linh_vuc_ids, loai_sang_kien_ids}), la_mac_dinh boolean, trang_thai varchar(20) (NHAP|DANG_AP_DUNG|NGUNG_AP_DUNG), so_do_layout jsonb (toạ độ node cho ReactFlow)`
> Hỗ trợ: **tạo mới, chỉnh sửa, sao chép (clone toàn bộ bước/trường hợp/tác nhân), kích hoạt/ngừng áp dụng**.
> Rule quan trọng: quy trình **đã có hồ sơ đang chạy thì không được sửa** → bắt buộc "Tạo phiên bản mới". Hồ sơ giữ `quy_trinh_snapshot_id` để chạy đúng phiên bản tại thời điểm nộp.

**`quy_trinh_buoc`** (chức năng 11)
`id, quy_trinh_id, ma, ten, thu_tu int, loai_buoc varchar(40) (TIEP_NHAN|THAM_DINH|PHAN_CONG_CHAM|CHAM_DIEM|HOP_HOI_DONG|BO_PHIEU|PHE_DUYET|BAN_HANH_QUYET_DINH|CONG_BO|KET_THUC), so_ngay_xu_ly int, tinh_theo_ngay_lam_viec boolean, bat_buoc_dinh_kem boolean, danh_sach_tep_bat_buoc jsonb, bat_buoc_nhap_y_kien boolean, cho_phep_uy_quyen boolean, cho_phep_thu_hoi boolean, la_buoc_bat_dau boolean, la_buoc_ket_thuc boolean, canh_bao_truoc_han_gio int, mo_ta_huong_dan text`

**`quy_trinh_buoc_tac_nhan`** (chức năng 15)
`id, buoc_id, loai_tac_nhan varchar(30) (NGUOI_DUNG|VAI_TRO|DON_VI|HOI_DONG|CHUC_DANH_HOI_DONG|NGUOI_TAO_HO_SO|LANH_DAO_DON_VI_TAC_GIA), tham_chieu_id uuid, quy_tac_xu_ly varchar(20) (MOT_NGUOI|TAT_CA|DA_SO|CHU_TICH_QUYET_DINH), ty_le_dong_thuan numeric(5,2), thu_tu`

**`quy_trinh_trang_thai`** (chức năng 14)
`id, buoc_id (nullable → trạng thái toàn cục), ma, ten (Chờ xử lý, Đang xử lý, Yêu cầu bổ sung, Đã phê duyệt, Không đạt…), mau_sac varchar(20), icon, la_trang_thai_ket_thuc boolean, hien_thi_cho_tac_gia boolean, thu_tu`

**`quy_trinh_truong_hop`** (chức năng 10 — nhánh rẽ)
`id, buoc_id, ma (DAT|KHONG_DAT|BO_SUNG_HO_SO|CHUYEN_CAP_CAO_HON|TRA_LAI|RUT_HO_SO), ten, buoc_tiep_theo_id uuid (null = kết thúc), trang_thai_gan_id uuid, dieu_kien jsonb (biểu thức: {"truong":"tong_diem","toan_tu":">=","gia_tri":80} hoặc AND/OR lồng nhau), hanh_dong jsonb (["GUI_EMAIL","GUI_SMS","TAO_QUYET_DINH","CAP_NHAT_KET_QUA","YEU_CAU_KY_SO"]), mau_thong_bao_id, mau_nut varchar(20), thu_tu`

**`quy_trinh_thanh_phan_ho_so`** (chức năng 13)
`id, quy_trinh_id, ma, ten (Mô tả giải pháp, Minh chứng, Báo cáo hiệu quả, Phụ lục…), bat_buoc boolean, loai_du_lieu varchar(20) (VAN_BAN|TEP|CA_HAI), dinh_dang_cho_phep jsonb ([".pdf",".docx",".xlsx",".jpg",".png"]), dung_luong_toi_da_mb int, so_luong_toi_da int, so_ky_tu_toi_thieu int, so_ky_tu_toi_da int, dung_de_kiem_tra_trung_lap boolean, thu_tu, mo_ta_huong_dan`

**`quy_trinh_chuc_nang_bo_sung`** (chức năng 12)
`id, quy_trinh_id, buoc_id (nullable), ma_chuc_nang varchar(50) (KY_SO|GUI_EMAIL|GUI_SMS|XUAT_BIEU_MAU|BO_PHIEU_KIN|TAO_BIEN_BAN|KIEM_TRA_TRUNG_LAP|CHAM_DIEM_DOC_LAP|CONG_KHAI_KET_QUA), bat_buoc boolean, cau_hinh jsonb`

**`quy_trinh_lien_thong`** (chức năng 16)
`id, quy_trinh_id, buoc_id, he_thong_tich_hop_id, su_kien varchar(50) (KHI_VAO_BUOC|KHI_HOAN_THANH|KHI_PHE_DUYET), loai_du_lieu, cau_hinh_mapping jsonb, dong_bo_hai_chieu boolean, trang_thai`

### 4.3 Nhóm TIÊU CHÍ ĐỘNG (chức năng 17–18)

**`bo_tieu_chi`**
`id, ma, ten, nam int, cap varchar(30), thang_diem_toi_da numeric(6,2) (mặc định 100), diem_dat_toi_thieu numeric(6,2), cach_tinh varchar(30) (TONG_DIEM|TRUNG_BINH_CONG|TRUNG_BINH_TRONG_SO), lam_tron int, cho_phep_cham_doc_lap boolean, tu_dong_tong_hop boolean, loai_bo_diem_cao_thap boolean, pham_vi_ap_dung jsonb, trang_thai`

**`nhom_tieu_chi`** (chức năng 17)
`id, bo_tieu_chi_id, ma, ten (Tính mới, Tính hiệu quả, Khả năng áp dụng, Phạm vi ảnh hưởng…), mo_ta, trong_so numeric(5,2), diem_toi_da numeric(6,2), thu_tu, trang_thai`

**`tieu_chi`** (chức năng 18)
`id, nhom_tieu_chi_id, ma, ten, mo_ta, diem_toi_da numeric(6,2), diem_toi_thieu numeric(6,2), trong_so numeric(5,2), kieu_nhap varchar(20) (NHAP_SO|THANG_DIEM|LUA_CHON|CO_KHONG), buoc_nhay numeric(4,2), bat_buoc_nhan_xet boolean, huong_dan_cham text, thu_tu, trang_thai`

**`tieu_chi_muc_diem`** — các mức chọn sẵn khi `kieu_nhap = LUA_CHON`
`id, tieu_chi_id, ten (Xuất sắc/Tốt/Khá/Trung bình), diem, mo_ta, thu_tu`

**`muc_cong_nhan`** — thang xếp loại theo điểm
`id, ma, ten (Sáng kiến cấp cơ sở, Cấp thành phố, Không công nhận), diem_tu, diem_den, bo_tieu_chi_id, mau_sac, thu_tu`

### 4.4 Nhóm HỘI ĐỒNG (chức năng 19–20)

**`hoi_dong`**
`id, ma, ten, cap varchar(30), dot_de_nghi_id, don_vi_id, so_quyet_dinh_thanh_lap, ngay_quyet_dinh date, tep_quyet_dinh_id, thoi_gian_hoat_dong_tu date, thoi_gian_hoat_dong_den date, linh_vuc_phu_trach jsonb, so_thanh_vien_toi_thieu int, ty_le_thong_qua numeric(5,2), trang_thai varchar(20) (DANG_HOAT_DONG|DA_KET_THUC)`

**`hoi_dong_thanh_vien`**
`id, hoi_dong_id, nguoi_dung_id, ho_ten_hien_thi, chuc_vu_cong_tac, don_vi_cong_tac, chuc_danh varchar(30) (CHU_TICH|PHO_CHU_TICH|UY_VIEN|UY_VIEN_THU_KY|THU_KY), quyen_cham_diem boolean, quyen_nhan_xet boolean, quyen_bo_phieu boolean, quyen_ky_bien_ban boolean, quyen_ket_luan boolean, thu_tu, trang_thai`
> Ràng buộc: mỗi hội đồng **chỉ có 1 Chủ tịch**; thành viên có `quyen_cham_diem` mới nhận được phân công chấm.

**`phien_hop_hoi_dong`**
`id, hoi_dong_id, ma_phien, ten_phien, thoi_gian_bat_dau timestamptz, thoi_gian_ket_thuc, dia_diem, hinh_thuc varchar(20) (TRUC_TIEP|TRUC_TUYEN|KET_HOP), chu_tri_id, thu_ky_id, noi_dung text, ket_luan text, trang_thai varchar(20) (DU_KIEN|DANG_DIEN_RA|DA_KET_THUC|DA_HUY), tep_bien_ban_id`

**`phien_hop_ho_so`** — hồ sơ đưa ra họp
`id, phien_hop_id, sang_kien_id, thu_tu, ket_luan_rieng text, ket_qua varchar(20)`

**`phien_hop_diem_danh`**
`id, phien_hop_id, thanh_vien_id, co_mat boolean, ly_do_vang, thoi_gian_diem_danh`

**`bien_ban_hop`**
`id, phien_hop_id, so_bien_ban, noi_dung_json jsonb, tep_tin_id, trang_thai, ngay_lap`

**`bien_ban_chu_ky`**
`id, bien_ban_id, thanh_vien_id, da_ky boolean, thoi_gian_ky, chu_ky_so_id, anh_chu_ky_id`

**`phieu_bo_phieu`** — bỏ phiếu (kín/công khai)
`id, phien_hop_id, sang_kien_id, thanh_vien_id, y_kien varchar(20) (DONG_Y|KHONG_DONG_Y|Y_KIEN_KHAC), muc_de_xuat, ghi_chu, la_phieu_kin boolean, thoi_gian`

### 4.5 Nhóm HỒ SƠ SÁNG KIẾN (chức năng 21–32)

**`sang_kien`** — bảng trung tâm
```
id, ma_ho_so varchar(50) UNIQUE,       -- SK-2026-0001, sinh theo mẫu cấu hình được
ten_sang_kien varchar(1000), ten_khong_dau,
dot_de_nghi_id, linh_vuc_id, doi_tuong_id, loai_tac_gia_id,
don_vi_id,                              -- đơn vị của tác giả
quy_trinh_id, quy_trinh_snapshot jsonb, -- snapshot quy trình lúc nộp
buoc_hien_tai_id, trang_thai_hien_tai_id,
trang_thai_tong varchar(30),            -- NHAP|DA_NOP|DANG_XU_LY|YEU_CAU_BO_SUNG|DA_PHE_DUYET|KHONG_DAT|DA_RUT|DA_HUY
-- nội dung nghiệp vụ (các trường sinh theo thanh_phan_ho_so, phần cố định:)
mo_ta_giai_phap text, tinh_trang_truoc_khi_ap_dung text, noi_dung_giai_phap text,
tinh_moi text, kha_nang_ap_dung text, pham_vi_ap_dung text,
hieu_qua_kinh_te text, gia_tri_lam_loi_uoc_tinh numeric(18,2),
hieu_qua_xa_hoi text, thoi_gian_ap_dung_tu date, thoi_gian_ap_dung_den date,
noi_dung_dong jsonb,                    -- dữ liệu các thành phần hồ sơ cấu hình động
-- kết quả
ty_le_trung_lap numeric(5,2), trang_thai_kiem_tra_trung_lap varchar(20),
tong_diem numeric(6,2), diem_trung_binh numeric(6,2),
muc_cong_nhan_id, ket_qua varchar(20),  -- DAT|KHONG_DAT
quyet_dinh_id, ngay_cong_nhan date,
-- thời gian & khoá
ngay_nop timestamptz, han_xu_ly_hien_tai timestamptz, ngay_hoan_thanh timestamptz,
dang_khoa boolean, ly_do_khoa, cong_khai boolean,
so_luot_xem int, phien_ban int
```

**`sang_kien_tac_gia`**
`id, sang_kien_id, nguoi_dung_id (nullable — cho phép nhập ngoài hệ thống), ho_ten, ngay_sinh date, gioi_tinh, so_cccd, chuc_vu, don_vi_cong_tac, trinh_do_chuyen_mon, email, dien_thoai, ty_le_dong_gop numeric(5,2), la_tac_gia_chinh boolean, thu_tu`
> Validate: tổng `ty_le_dong_gop` của 1 hồ sơ = 100%; số tác giả ≤ `loai_tac_gia.so_tac_gia_toi_da`.

**`tep_tin`** — bảng tệp dùng chung toàn hệ thống
`id, ten_goc, ten_luu_tru, duong_dan, bucket, kich_thuoc bigint, mime_type, phan_mo_rong, hash_sha256, nguoi_tai_len_id, ngay_tai_len, da_quet_virus boolean, noi_dung_trich_xuat text (từ OCR/parse), trang_thai_ocr varchar(20)`

**`sang_kien_tep_dinh_kem`**
`id, sang_kien_id, tep_tin_id, thanh_phan_ho_so_ma, mo_ta, thu_tu, phien_ban int`

**`sang_kien_lich_su`** (chức năng 23 — lưu lịch sử chỉnh sửa)
`id, sang_kien_id, hanh_dong varchar(50) (TAO|SUA|NOP|RUT|BO_SUNG|XOA_TEP|THEM_TEP), truong_thay_doi jsonb, gia_tri_truoc jsonb, gia_tri_sau jsonb, nguoi_thuc_hien_id, thoi_gian, dia_chi_ip, user_agent, ghi_chu`

**`sang_kien_xu_ly`** — instance của workflow (chức năng 29, 30)
`id, sang_kien_id, buoc_id, ten_buoc_snapshot, trang_thai_id, truong_hop_id, nguoi_xu_ly_id, nguoi_uy_quyen_id, y_kien text, tep_dinh_kem_ids jsonb, thoi_gian_nhan timestamptz, han_xu_ly timestamptz, thoi_gian_xu_ly timestamptz, so_ngay_xu_ly numeric(6,2), qua_han boolean, thu_tu int`

**`sang_kien_phan_cong`** (chức năng 33)
`id, sang_kien_id, hoi_dong_id, thanh_vien_id, nguoi_phan_cong_id, ngay_phan_cong, han_hoan_thanh timestamptz, trang_thai varchar(20) (CHUA_CHAM|DANG_CHAM|DA_CHAM|QUA_HAN), ghi_chu`

**`phieu_danh_gia`** (chức năng 34, 35)
`id, sang_kien_id, hoi_dong_id, thanh_vien_id, bo_tieu_chi_id, bo_tieu_chi_snapshot jsonb, tong_diem numeric(6,2), diem_theo_nhom jsonb, nhan_xet_chung text, uu_diem text, han_che text, de_xuat_muc_cong_nhan_id, ket_luan varchar(20), trang_thai varchar(20) (NHAP|DA_GUI|DA_KY), ngay_cham, ngay_gui, chu_ky_so_id, so_phieu varchar(50)`

**`phieu_danh_gia_chi_tiet`**
`id, phieu_danh_gia_id, tieu_chi_id, ten_tieu_chi_snapshot, diem_toi_da_snapshot, diem numeric(6,2), muc_diem_id, nhan_xet text`

**`ket_qua_xet_duyet`** (chức năng 32)
`id, sang_kien_id, hoi_dong_id, phien_hop_id, so_phieu_cham int, diem_cao_nhat, diem_thap_nhat, diem_trung_binh numeric(6,2), tong_diem_trong_so numeric(6,2), so_phieu_dong_y int, so_phieu_khong_dong_y int, ket_qua varchar(20), muc_cong_nhan_id, ly_do text, nguoi_ket_luan_id, ngay_ket_luan, da_cong_bo boolean, ngay_cong_bo`

### 4.6 Nhóm AI KIỂM TRA TRÙNG LẶP (chức năng 26 + Mục 3.2 E-HSMT)

**`sang_kien_doan_van`** — chunk văn bản để so khớp
`id, sang_kien_id, nguon varchar(50) (NOI_DUNG|TEP_DINH_KEM), tep_tin_id, chi_muc int, noi_dung text, noi_dung_chuan_hoa text, so_tu int, simhash bigint, embedding vector(768)`
> Index: `CREATE INDEX ON sang_kien_doan_van USING hnsw (embedding vector_cosine_ops);`

**`kiem_tra_trung_lap`**
`id, sang_kien_id, ngay_chay, phien_ban_thuat_toan varchar(20), pham_vi jsonb (nam, lĩnh vực, đơn vị), tong_so_doi_chieu int, ty_le_cao_nhat numeric(5,2), muc_canh_bao varchar(20) (AN_TOAN|CANH_BAO|NGHIEM_TRONG), trang_thai varchar(20) (DANG_CHAY|HOAN_THANH|LOI), thoi_gian_xu_ly_ms int, thong_bao_loi`

**`kiem_tra_trung_lap_chi_tiet`**
`id, kiem_tra_id, sang_kien_doi_chieu_id, ty_le_tuong_dong numeric(5,2), ty_le_tu_vung numeric(5,2), ty_le_ngu_nghia numeric(5,2), so_doan_trung int, cac_doan_trung jsonb ([{doan_nguon, doan_dich, ty_le, vi_tri_bat_dau, vi_tri_ket_thuc}])`

### 4.7 Nhóm QUẢN TRỊ HỆ THỐNG (chức năng 43–51)

**`nguoi_dung`**
`id, ten_dang_nhap varchar(100) UNIQUE, mat_khau_hash, mat_khau_salt, ho_ten, ngay_sinh, gioi_tinh, so_cccd, email, dien_thoai, don_vi_id, chuc_vu, anh_dai_dien_id, loai_tai_khoan varchar(20) (NOI_BO|SSO), sso_subject_id, sso_provider, trang_thai varchar(20) (HOAT_DONG|KHOA|CHO_KICH_HOAT), buoc_doi_mat_khau boolean, so_lan_dang_nhap_sai int, khoa_den timestamptz, lan_dang_nhap_cuoi, mfa_enabled boolean, mfa_secret`

**`vai_tro`** `id, ma, ten, mo_ta, la_he_thong boolean, trang_thai`
**`quyen`** `id, ma (SANG_KIEN.XEM, SANG_KIEN.SUA, QUY_TRINH.CAU_HINH…), ten, nhom_chuc_nang, mo_ta`
**`vai_tro_quyen`** `id, vai_tro_id, quyen_id`
**`nguoi_dung_vai_tro`** `id, nguoi_dung_id, vai_tro_id, don_vi_id (phạm vi áp dụng), tu_ngay, den_ngay`
**`pham_vi_du_lieu`** — kiểm soát phạm vi dữ liệu theo vai trò
`id, vai_tro_id, loai_pham_vi varchar(30) (TOAN_HE_THONG|DON_VI|DON_VI_VA_CAP_DUOI|CA_NHAN|TUY_CHINH), don_vi_ids jsonb`

**`cau_hinh_he_thong`** `id, nhom varchar(50), khoa varchar(100) UNIQUE, gia_tri text, gia_tri_json jsonb, kieu_du_lieu, ten_hien_thi, mo_ta, cho_phep_sua boolean`
> Seed tối thiểu: `TEN_HE_THONG`, `LOGO_ID`, `FAVICON_ID`, `MAU_CHU_DAO`, `TEN_DON_VI`, `DIA_CHI`, `EMAIL_HO_TRO`, `DIEN_THOAI_HO_TRO`, `MAU_MA_HO_SO`, `MUC_CANH_BAO_TRUNG_LAP_VANG`, `MUC_CANH_BAO_TRUNG_LAP_DO`, `SO_NGAY_NHAC_TRUOC_HAN`, `DUNG_LUONG_TEP_TOI_DA_MB`, `SO_TEP_TOI_DA`.

**`cau_hinh_menu`** (chức năng 48)
`id, ma, ten, icon, duong_dan, menu_cha_id, thu_tu, quyen_ma, loai varchar(20) (WEB|MOBILE), hien_thi boolean, mo_tab_moi boolean`

**`cau_hinh_email_sms`** (chức năng 50)
`id, loai varchar(10) (EMAIL|SMS), nha_cung_cap, host, port, ten_dang_nhap, mat_khau_ma_hoa, su_dung_ssl boolean, email_gui_di, ten_hien_thi, api_endpoint, api_key_ma_hoa, brandname, trang_thai, la_mac_dinh`

**`mau_thong_bao`**
`id, ma, ten, kenh varchar(20) (EMAIL|SMS|APP|TAT_CA), su_kien varchar(50) (HO_SO_DUOC_TIEP_NHAN|YEU_CAU_BO_SUNG|DUOC_PHAN_CONG_CHAM|SAP_HET_HAN|CO_KET_QUA|DA_PHE_DUYET…), tieu_de, noi_dung text (Handlebars/Scriban placeholders), danh_sach_bien jsonb, trang_thai`

**`thong_bao`** `id, nguoi_nhan_id, tieu_de, noi_dung, loai_su_kien, doi_tuong_lien_quan, doi_tuong_id, duong_dan, muc_do varchar(20), da_doc boolean, ngay_doc, thoi_gian`
**`hang_doi_gui_tin`** `id, kenh, nguoi_nhan, tieu_de, noi_dung, so_lan_thu int, trang_thai, thong_bao_loi, thoi_gian_gui`

**`cau_hinh_chu_ky_so`** (chức năng 49)
`id, nha_cung_cap varchar(50) (BAN_CO_YEU_CHINH_PHU|VNPT_CA|VIETTEL_CA|FPT_CA|MISA|KHAC), loai_ky varchar(30) (USB_TOKEN|HSM|REMOTE_SIGNING|SMART_CA), endpoint, client_id, client_secret_ma_hoa, chung_thu_so, thuat_toan varchar(20), tich_hop_plugin_url, trang_thai, la_mac_dinh`

**`nhat_ky_ky_so`** `id, doi_tuong varchar(50), doi_tuong_id, nguoi_ky_id, thoi_gian_ky, serial_chung_thu, nguoi_cap_chung_thu, hieu_luc_tu, hieu_luc_den, tep_goc_id, tep_da_ky_id, trang_thai, thong_tin_xac_thuc jsonb`

**`he_thong_tich_hop`** (chức năng 41)
`id, ma (SSO_THANH_PHO|IOC|THI_DUA_KHEN_THUONG), ten, endpoint_base, loai_xac_thuc varchar(30) (OAUTH2|OIDC|API_KEY|CHUNG_THU_SO), client_id, client_secret_ma_hoa, scope, cau_hinh_mapping jsonb, tan_suat_dong_bo varchar(30), lan_dong_bo_cuoi, trang_thai`

**`nhat_ky_dong_bo`** `id, he_thong_tich_hop_id, chieu varchar(10) (GUI|NHAN), loai_du_lieu, tong_ban_ghi, thanh_cong, that_bai, du_lieu_gui jsonb, phan_hoi jsonb, trang_thai, thong_bao_loi, thoi_gian_bat_dau, thoi_gian_ket_thuc`

**`nhat_ky_he_thong`** (audit) `id, nguoi_dung_id, ten_dang_nhap, hanh_dong, module, doi_tuong, doi_tuong_id, mo_ta, du_lieu_truoc jsonb, du_lieu_sau jsonb, dia_chi_ip, user_agent, ket_qua varchar(20), thoi_gian`
**`nhat_ky_loi`** `id, muc_do, nguon, thong_bao, stack_trace, du_lieu_ngu_canh jsonb, nguoi_dung_id, dia_chi_ip, thoi_gian, da_xu_ly boolean`
**`nhat_ky_dang_nhap`** `id, ten_dang_nhap, nguoi_dung_id, thanh_cong boolean, ly_do_that_bai, dia_chi_ip, user_agent, thiet_bi, thoi_gian`

**Phân vùng dữ liệu (yêu cầu Mục 3.4 E-HSMT):** `nhat_ky_he_thong`, `nhat_ky_dang_nhap`, `thong_bao` dùng **partition theo tháng** (`PARTITION BY RANGE (thoi_gian)`); `sang_kien` partition theo `nam` nếu dự kiến > 500k bản ghi.

---

## 5. ĐẶC TẢ CHI TIẾT TỪNG NHÓM CHỨC NĂNG

### NHÓM I — DANH MỤC HỆ THỐNG (chức năng 1–8)

Với **mỗi** danh mục, xây dựng đầy đủ:
- API: `GET /api/v1/{danh-muc}` (phân trang, tìm kiếm không dấu, lọc theo trạng thái, sort), `GET /{id}`, `POST`, `PUT /{id}`, `DELETE /{id}` (soft delete + chặn xóa khi đang được tham chiếu), `PATCH /{id}/trang-thai`, `POST /import` (Excel), `GET /export` (Excel), `PUT /sap-xep` (đổi thứ tự).
- UI: trang danh sách (Ant Table + bộ lọc + tìm kiếm gõ tiếng Việt không dấu vẫn ra kết quả), modal thêm/sửa, xác nhận xóa, nút Import/Export Excel, hiển thị dạng cây cho `linh_vuc` và `don_vi`.
- Rule: `ma` unique, không cho xóa bản ghi đang được sử dụng (trả về HTTP 409 kèm danh sách nơi đang tham chiếu).

**Riêng `dot_de_nghi`:** thêm màn hình chi tiết đợt gồm tab: Thông tin chung / Quy trình áp dụng / Bộ tiêu chí áp dụng / Đơn vị áp dụng / Danh sách hồ sơ / Thống kê đợt. Có nút **Mở đợt**, **Đóng đợt**, **Khóa đợt** (khóa = không thao tác gì được nữa, chỉ đọc), **Sao chép đợt từ năm trước** (clone cấu hình).

**Riêng `bieu_mau_xuat`:** cho phép upload file `.docx` mẫu, hệ thống tự **quét các placeholder `{{...}}`** trong file và hiển thị bảng mapping để admin gán nguồn dữ liệu. Có nút **Xem trước** sinh file với dữ liệu mẫu.

### NHÓM II — QUY TRÌNH ĐỘNG (chức năng 9–16)

Đây là **module khó nhất**, làm kỹ:

**Trình thiết kế quy trình (Workflow Designer)** — trang `/quan-tri/quy-trinh/{id}/thiet-ke`:
- Canvas ReactFlow: node = bước xử lý, edge = trường hợp chuyển tiếp (hiển thị nhãn "Đạt", "Không đạt", "Bổ sung hồ sơ"…).
- Panel trái: kéo thả các loại bước vào canvas.
- Panel phải: form cấu hình bước đang chọn — thông tin chung, tác nhân xử lý, thời hạn, trạng thái, trường hợp chuyển tiếp, tệp bắt buộc, chức năng bổ sung.
- Nút: **Lưu nháp**, **Kiểm tra tính hợp lệ**, **Kích hoạt**, **Sao chép quy trình**, **Tạo phiên bản mới**, **Ngừng áp dụng**, **Xem sơ đồ toàn màn hình**, **Xuất PNG/PDF sơ đồ**.

**Validator quy trình (chạy khi bấm Kích hoạt) — bắt buộc kiểm tra:**
1. Có đúng 1 bước bắt đầu và ≥ 1 bước kết thúc.
2. Không có bước "mồ côi" (không có đường vào, trừ bước bắt đầu).
3. Không có bước "cụt" (không có trường hợp đi ra, trừ bước kết thúc).
4. Phát hiện **vòng lặp vô hạn không có điều kiện thoát** (DFS phát hiện chu trình và cảnh báo).
5. Mọi bước đều có ít nhất 1 tác nhân xử lý.
6. Mọi bước loại `CHAM_DIEM` phải gắn với hội đồng và bộ tiêu chí.
7. Điều kiện chuyển tiếp không mâu thuẫn/không phủ hết trường hợp → cảnh báo.

**Workflow Engine (`SangKien.Workflow`)** — interface tối thiểu:
```csharp
public interface IWorkflowEngine {
    Task<WorkflowInstance> KhoiTaoAsync(Guid sangKienId, Guid quyTrinhId, CancellationToken ct);
    Task<IReadOnlyList<HanhDongKhaDung>> LayHanhDongKhaDungAsync(Guid sangKienId, Guid nguoiDungId, CancellationToken ct);
    Task<KetQuaXuLy> ThucThiAsync(XuLyBuocRequest request, CancellationToken ct);
    Task<bool> KiemTraQuyenXuLyAsync(Guid sangKienId, Guid buocId, Guid nguoiDungId, CancellationToken ct);
    Task ThuHoiAsync(Guid sangKienId, Guid nguoiDungId, string lyDo, CancellationToken ct);
}
```
Yêu cầu engine:
- Đọc `quy_trinh_snapshot` của hồ sơ (không đọc quy trình hiện hành) → đảm bảo hồ sơ cũ vẫn chạy đúng quy trình cũ.
- Đánh giá `dieu_kien` (jsonb) bằng **rule evaluator tự viết** (không dùng eval động): hỗ trợ toán tử `=, !=, >, >=, <, <=, IN, CONTAINS, BETWEEN`, phép `AND/OR/NOT` lồng nhau, biến lấy từ context (`tong_diem`, `diem_trung_binh`, `ty_le_trung_lap`, `linh_vuc_id`, `so_phieu_dong_y`, `cap_xet_duyet`…).
- Sau khi thực thi: ghi `sang_kien_xu_ly`, cập nhật `buoc_hien_tai_id`, `trang_thai_hien_tai_id`, `han_xu_ly_hien_tai`, chạy `hanh_dong` (gửi thông báo, tạo quyết định, đẩy liên thông), phát domain event, đẩy SignalR.
- Xử lý **quy tắc TAT_CA / DA_SO**: chỉ chuyển bước khi đủ số tác nhân hoàn thành.
- Tính **hạn xử lý theo ngày làm việc** (loại trừ T7/CN + bảng `ngay_nghi_le`).
- **Idempotent**: chống double-submit bằng `Idempotency-Key` header + optimistic concurrency (`xmin`/`phien_ban`).

API:
```
GET    /api/v1/quy-trinh                       # danh sách
POST   /api/v1/quy-trinh                       # tạo
POST   /api/v1/quy-trinh/{id}/sao-chep
POST   /api/v1/quy-trinh/{id}/phien-ban-moi
POST   /api/v1/quy-trinh/{id}/kich-hoat
POST   /api/v1/quy-trinh/{id}/ngung-ap-dung
GET    /api/v1/quy-trinh/{id}/so-do
PUT    /api/v1/quy-trinh/{id}/so-do            # lưu toàn bộ node+edge+config trong 1 transaction
POST   /api/v1/quy-trinh/{id}/kiem-tra         # validator
GET/POST/PUT/DELETE /api/v1/quy-trinh/{id}/buoc[/{buocId}]
GET/POST/PUT/DELETE /api/v1/quy-trinh/{id}/thanh-phan-ho-so[/{tpId}]
GET/POST/PUT/DELETE /api/v1/quy-trinh/{id}/lien-thong[/{ltId}]
```

### NHÓM III — TIÊU CHÍ ĐỘNG (chức năng 17–18)

- Màn hình cấu hình bộ tiêu chí dạng **cây 2 cấp** (Nhóm tiêu chí → Tiêu chí), kéo thả sắp xếp bằng `@dnd-kit`.
- Thanh trạng thái hiển thị realtime: tổng điểm tối đa các nhóm, tổng trọng số (phải = 100% nếu `cach_tinh = TRUNG_BINH_TRONG_SO`) — cảnh báo đỏ nếu lệch.
- Hỗ trợ **sao chép bộ tiêu chí** sang năm/đợt khác.
- Cấu hình `muc_cong_nhan` theo khoảng điểm, kiểm tra khoảng không chồng lấn/không hở.
- **Engine tính điểm (`SangKien.Scoring`)**:
```csharp
public interface IScoringEngine {
    KetQuaChamDiem TinhDiemPhieu(PhieuDanhGia phieu, BoTieuChi boTieuChi);
    KetQuaTongHop TongHopDiemHoiDong(IEnumerable<PhieuDanhGia> cacPhieu, BoTieuChi boTieuChi);
    MucCongNhan? XacDinhMucCongNhan(decimal diem, Guid boTieuChiId);
}
```
Quy tắc tổng hợp: `TONG_DIEM` = tổng điểm tiêu chí; `TRUNG_BINH_CONG` = trung bình các phiếu; `TRUNG_BINH_TRONG_SO` = `Σ(điểm_nhóm × trọng_số_nhóm)/100`; nếu `loai_bo_diem_cao_thap = true` và số phiếu ≥ 5 → loại 1 điểm cao nhất + 1 thấp nhất. Làm tròn theo `lam_tron`.
- **Chấm điểm độc lập**: thành viên không thấy điểm của người khác cho đến khi Thư ký bấm "Tổng hợp" hoặc đủ 100% phiếu.

### NHÓM IV — HỘI ĐỒNG SÁNG KIẾN (chức năng 19–20)

- CRUD hội đồng, upload quyết định thành lập, quản lý thành viên (chọn từ danh bạ người dùng hoặc nhập ngoài).
- Phân quyền theo chức danh (checkbox: chấm điểm / nhận xét / bỏ phiếu / ký biên bản / kết luận).
- Quản lý **phiên họp**: tạo phiên, chọn hồ sơ đưa ra họp, điểm danh, ghi nhận ý kiến từng hồ sơ, bỏ phiếu (kín/công khai), kết luận, **sinh biên bản họp tự động từ `bieu_mau_xuat`**, ký số biên bản.
- Màn hình "Phòng họp hội đồng": trình chiếu từng hồ sơ, bảng điểm realtime (SignalR), nút bỏ phiếu cho từng thành viên.
- Cảnh báo xung đột lợi ích: nếu thành viên hội đồng đồng thời là tác giả của hồ sơ → **tự động loại khỏi phân công chấm** và hiển thị cảnh báo.

### NHÓM V — ĐĂNG KÝ NỘP HỒ SƠ (chức năng 21–26)

**Chức năng 21 — Đăng nhập:**
- Đăng nhập nội bộ: username/password (Argon2id hoặc PBKDF2 ≥ 210k iterations), JWT access token (15 phút) + refresh token (7 ngày, xoay vòng, lưu DB, thu hồi được).
- **SSO OIDC**: Authorization Code + PKCE, cấu hình được endpoint/client trong `he_thong_tich_hop`. Tự động tạo/đồng bộ tài khoản khi đăng nhập lần đầu (auto-provisioning theo mapping đơn vị/vai trò).
- Khóa tài khoản sau 5 lần sai (15 phút), CAPTCHA sau 3 lần sai, ghi `nhat_ky_dang_nhap`.
- Tùy chọn MFA TOTP cho tài khoản quản trị.
- Đổi mật khẩu, quên mật khẩu qua email OTP, chính sách mật khẩu cấu hình được (độ dài ≥ 8, chữ hoa + số + ký tự đặc biệt, không trùng 3 mật khẩu gần nhất, buộc đổi sau 90 ngày).

**Chức năng 22 — Đăng ký nộp sáng kiến:**
- **Form wizard nhiều bước** (Ant Steps), lưu nháp tự động mỗi 30 giây và khi rời bước:
  1. Chọn đợt đề nghị (chỉ hiện đợt đang mở) → hệ thống nạp quy trình + bộ tiêu chí + thành phần hồ sơ tương ứng.
  2. Thông tin chung: tên sáng kiến, lĩnh vực, đối tượng, loại tác giả, đơn vị.
  3. Tác giả/đồng tác giả: bảng nhập, tự động điền từ hồ sơ người dùng, kiểm tra tổng tỷ lệ đóng góp = 100%.
  4. Nội dung sáng kiến: các trường **render động** theo `quy_trinh_thanh_phan_ho_so` (rich text editor, đếm ký tự, cảnh báo dưới ngưỡng tối thiểu).
  5. Tệp đính kèm: upload theo từng thành phần, kiểm tra định dạng/dung lượng/số lượng.
  6. Xem lại & Nộp: hiển thị checklist đầy đủ/thiếu, chỉ enable nút **Nộp hồ sơ** khi đủ thành phần bắt buộc.
- Sau khi nộp: sinh `ma_ho_so`, chạy `IWorkflowEngine.KhoiTaoAsync`, gửi thông báo, **enqueue job kiểm tra trùng lặp**, sinh Phiếu tiếp nhận (PDF) cho tác giả tải về.

**Chức năng 23 — Quản lý hồ sơ sáng kiến (phía tác giả):**
- Danh sách hồ sơ của tôi với bộ lọc trạng thái/đợt/năm.
- Cho phép **sửa** khi `trang_thai_tong ∈ {NHAP, YEU_CAU_BO_SUNG}` và đợt còn hạn; **rút hồ sơ** khi chưa vào bước chấm điểm (ghi lý do).
- Tab **Lịch sử chỉnh sửa**: hiển thị diff giá trị trước/sau theo từng lần sửa (từ `sang_kien_lich_su`).
- Tab **Tiến độ xử lý**: timeline các bước đã qua + bước hiện tại + người đang xử lý + hạn xử lý.

**Chức năng 24 — Thành phần hồ sơ:** checklist trực quan (✓ đủ / ✗ thiếu / ⚠ chưa đạt số ký tự tối thiểu), chặn nộp chính thức khi thiếu bắt buộc, cảnh báo popup liệt kê chi tiết mục còn thiếu.

**Chức năng 25 — Tệp tin đính kèm:**
- Hỗ trợ PDF, DOC/DOCX, XLS/XLSX, PPT/PPTX, JPG/PNG, ZIP.
- Upload theo chunk (file lớn), progress bar, kéo thả, xem trước PDF/ảnh ngay trên trình duyệt.
- Kiểm tra **magic number** (không tin phần mở rộng), chặn file thực thi, tính SHA-256 chống trùng lặp file, tùy chọn quét ClamAV.
- Lưu trên MinIO, truy cập qua **presigned URL có thời hạn** (không expose đường dẫn trực tiếp).

**Chức năng 26 — Kiểm tra trùng lặp/đạo văn (AI nội bộ):**

Pipeline (chạy trong Hangfire job, có thể chạy lại thủ công):
1. **Trích xuất văn bản**: PDF text-layer → PdfPig; PDF scan/ảnh → gửi sang `ai-service` chạy Tesseract `vie`; DOCX/XLSX → OpenXml. Lưu vào `tep_tin.noi_dung_trich_xuat`.
2. **Chuẩn hóa tiếng Việt**: NFC, lowercase, bỏ dấu câu thừa, chuẩn hóa khoảng trắng, loại stopwords tiếng Việt, giữ bản gốc để highlight.
3. **Chunking**: cắt theo câu/đoạn, cửa sổ trượt ~200 từ, overlap 50 từ → `sang_kien_doan_van`.
4. **Lọc thô (recall cao, rẻ)**: SimHash 64-bit + Hamming distance ≤ 8; MinHash/LSH trên shingle 5-gram → lấy top ứng viên.
5. **So khớp tinh**:
   - Từ vựng: TF-IDF cosine + Jaccard trên shingle.
   - Ngữ nghĩa: embedding (ONNX, chạy local) + cosine similarity qua pgvector HNSW.
   - Điểm tổng hợp: `ty_le = 0.4 × ty_le_tu_vung + 0.6 × ty_le_ngu_nghia` (hệ số cấu hình được trong `cau_hinh_he_thong`).
6. **Kết quả**: lưu `kiem_tra_trung_lap` + chi tiết từng cặp đoạn trùng, mức cảnh báo theo ngưỡng cấu hình (mặc định < 20% An toàn, 20–40% Cảnh báo, > 40% Nghiêm trọng).
7. **UI báo cáo trùng lặp**: giao diện 2 cột đối chiếu, **highlight đoạn trùng bằng màu**, thanh tỷ lệ tổng, danh sách sáng kiến tương đồng kèm link mở, nút "Đánh dấu đã xem xét" + ghi nhận ý kiến hội đồng. Xuất báo cáo PDF.

**Ràng buộc bắt buộc (Mục 3.2 E-HSMT):** toàn bộ mô hình, dữ liệu huấn luyện, quá trình suy luận chạy trong hạ tầng nội bộ; **không gọi API AI bên thứ ba**; dữ liệu huấn luyện lưu trên hạ tầng riêng biệt, phân quyền nhiều lớp, ghi log truy cập, mã hóa khi lưu trữ và truyền tải. Ghi rõ điều này trong `docs/ADR/0001-ai-noi-bo.md` và trong README.

### NHÓM VI — TIẾP NHẬN VÀ XỬ LÝ HỒ SƠ (chức năng 27–32)

- **27 Tiếp nhận hồ sơ**: màn hình cán bộ tiếp nhận — checklist hợp lệ (có cấu hình được), nút **Tiếp nhận** / **Yêu cầu bổ sung** (chọn thành phần thiếu + ghi rõ nội dung cần bổ sung, gửi email/SMS cho tác giả, đặt hạn bổ sung) / **Từ chối** (ghi lý do). Sinh Phiếu tiếp nhận từ biểu mẫu.
- **28 Danh sách hồ sơ**: bảng đa bộ lọc (đợt, trạng thái, lĩnh vực, đơn vị, tác giả, hội đồng, khoảng ngày nộp, mức trùng lặp, khoảng điểm), tìm kiếm không dấu, **lưu bộ lọc yêu thích**, chọn nhiều để xử lý hàng loạt, xuất Excel theo bộ lọc hiện tại, cột tùy biến hiển thị.
- **29 Xử lý hồ sơ**: màn hình chi tiết với các nút hành động **sinh động theo `LayHanhDongKhaDungAsync`** (không hardcode). Bắt buộc nhập ý kiến/đính kèm nếu bước cấu hình yêu cầu. Hỗ trợ **xử lý hàng loạt** cho các hồ sơ cùng bước.
- **30 Theo dõi hồ sơ**: timeline trực quan (Ant Steps dọc) — bước, người xử lý, thời gian nhận, hạn, thời gian xử lý, ý kiến, tệp kèm; badge **quá hạn màu đỏ**; job nhắc hạn gửi thông báo trước N giờ; thông báo realtime khi đổi trạng thái.
- **31 & 36 Đính kèm quyết định**: sau khi phê duyệt, cho phép chọn quyết định đã có hoặc tạo mới (số, ngày, người ký, file), gắn nhiều sáng kiến vào 1 quyết định, ký số file quyết định, cập nhật `ngay_cong_nhan` cho các sáng kiến liên quan.
- **32 Kết quả sáng kiến**: công bố kết quả (đạt/không đạt, mức công nhận, số điểm), cấu hình công bố công khai hay chỉ nội bộ, gửi thông báo hàng loạt cho tác giả, xuất danh sách kết quả (Excel/PDF).

### NHÓM VII — ĐÁNH GIÁ HỒ SƠ (chức năng 33–35)

- **33 Danh sách hồ sơ đánh giá**: màn hình "Việc của tôi" cho thành viên hội đồng — lọc theo đợt, lĩnh vực, trạng thái chấm, hạn hoàn thành; đếm ngược hạn; badge số hồ sơ chưa chấm.
- **Phân công chấm** (thư ký): phân công thủ công hoặc **tự động chia đều** theo lĩnh vực/số lượng, loại trừ xung đột lợi ích, đặt hạn hoàn thành, gửi thông báo.
- **34 Đánh giá hồ sơ**: giao diện 2 panel — trái xem nội dung sáng kiến + tệp đính kèm + báo cáo trùng lặp; phải là **phiếu chấm render động từ bộ tiêu chí**, tự động tính tổng điểm theo trọng số realtime, nhập nhận xét chi tiết, đề xuất mức công nhận. Lưu nháp / Gửi phiếu (sau khi gửi thì khóa, chỉ thư ký mở lại được). Có ký số phiếu nếu cấu hình yêu cầu.
- **35 Phiếu đánh giá**: lưu trữ phiếu điện tử, xuất PDF/Word theo `bieu_mau_xuat`, xuất hàng loạt (ZIP), bảng tổng hợp điểm ma trận (hàng = hồ sơ, cột = thành viên) cho thư ký.

### NHÓM IX — TRA CỨU, TÌM KIẾM (chức năng 37)

- Tìm kiếm cơ bản: 1 ô, full-text tiếng Việt (`to_tsvector('simple', unaccent(...))` + `pg_trgm`), gợi ý tự động (autocomplete), highlight từ khóa.
- **Tìm kiếm nâng cao**: tên sáng kiến, tác giả, đơn vị, lĩnh vực, đợt, năm công nhận, mức công nhận, trạng thái, khoảng điểm, khoảng ngày, mức trùng lặp — kết hợp AND/OR.
- **Tìm kiếm ngữ nghĩa** (tận dụng pgvector): "tìm sáng kiến tương tự" từ 1 hồ sơ bất kỳ.
- Kết quả: bảng/thẻ, sắp xếp theo liên quan/điểm/ngày, xuất Excel/PDF, lưu truy vấn, chia sẻ link truy vấn.
- Trang **tra cứu công khai** (không cần đăng nhập) chỉ hiển thị sáng kiến đã công nhận và có `cong_khai = true`.

### NHÓM X — THỐNG KÊ BÁO CÁO (chức năng 38–40)

**Dashboard** (theo vai trò): số hồ sơ theo trạng thái, tiến độ đợt hiện tại, hồ sơ quá hạn, tỷ lệ đạt/không đạt, top đơn vị, top lĩnh vực, biểu đồ xu hướng theo năm, cảnh báo trùng lặp cao. Dùng ECharts, có bộ lọc thời gian/đơn vị, click biểu đồ → drill-down sang danh sách.

Các báo cáo bắt buộc:
- **38** Danh sách sáng kiến đạt — theo đợt / năm / cấp xét duyệt / trạng thái.
- **39** Danh sách sáng kiến chưa đạt — kèm **lý do và điểm đánh giá**.
- **40** Danh sách sáng kiến theo đơn vị — số lượng, tỷ lệ đạt, phục vụ đánh giá thi đua.
- Bổ sung: thống kê theo lĩnh vực, theo tác giả (cá nhân có nhiều sáng kiến nhất), theo thời gian xử lý trung bình mỗi bước, theo tỷ lệ trùng lặp, báo cáo tổng hợp năm.

Yêu cầu kỹ thuật: mọi báo cáo đều **xuất được Excel (ClosedXML) và PDF (QuestPDF)**, hỗ trợ báo cáo tùy biến từ `bieu_mau_thong_ke`, báo cáo lớn chạy nền qua Hangfire rồi gửi link tải về, cache kết quả 5 phút.

### NHÓM XI — KẾT NỐI HỆ THỐNG KHÁC (chức năng 41)

- **SSO thành phố**: OIDC/SAML2 (làm OIDC trước, để interface mở cho SAML2), đồng bộ tài khoản, single logout.
- **Hệ thống Thi đua khen thưởng**: đẩy danh sách sáng kiến đã công nhận (mã hồ sơ, tên, tác giả, đơn vị, mức công nhận, số/ngày quyết định) — REST + JSON, ký HMAC hoặc mTLS, retry với exponential backoff, ghi `nhat_ky_dong_bo`.
- **Hệ thống IOC thành phố**: đẩy chỉ số tổng hợp (số sáng kiến theo đợt/lĩnh vực/đơn vị, tỷ lệ đạt) theo lịch cấu hình.
- Thiết kế **adapter pattern**: `IIntegrationAdapter` với các implementation `ThiDuaKhenThuongAdapter`, `IocAdapter`, `GenericRestAdapter` — thêm hệ thống mới **chỉ cần cấu hình**, không sửa code.
- Cung cấp **API công khai cho hệ thống ngoài gọi vào**: `/api/public/v1/sang-kien` (API key + IP allowlist + rate limit), có OpenAPI riêng.
- Màn hình quản trị tích hợp: cấu hình endpoint, test kết nối, xem log đồng bộ, đồng bộ thủ công, xem bản ghi lỗi và gửi lại.

### NHÓM XII — ỨNG DỤNG DI ĐỘNG (chức năng 42)

App Android + iOS với các chức năng:
- Đăng nhập (nội bộ + SSO), sinh trắc học (Face ID/vân tay) để mở nhanh.
- Tác giả: xem danh sách hồ sơ của tôi, nộp hồ sơ mới (rút gọn), **chụp ảnh tài liệu đính kèm trực tiếp**, theo dõi tiến độ, nhận push notification.
- Cán bộ/hội đồng: danh sách việc cần xử lý, xem chi tiết hồ sơ, phê duyệt/yêu cầu bổ sung, **chấm điểm trên mobile**, xem thông báo.
- Lãnh đạo: dashboard thống kê rút gọn.
- Yêu cầu: responsive cho cả điện thoại và máy tính bảng, hỗ trợ chế độ tối, offline-first cho danh sách đã tải (cache), push notification qua Firebase (self-host alternative: gửi qua SignalR khi app đang mở).
- Web cũng phải **responsive hoàn toàn** (breakpoint ≥ 320px) — đây là yêu cầu tối thiểu song song.

### NHÓM XIII — QUẢN TRỊ HỆ THỐNG (chức năng 43–51)

- **43 Quản lý người dùng**: CRUD, khóa/mở khóa, đặt lại mật khẩu, buộc đổi mật khẩu, gán vai trò + phạm vi dữ liệu, import Excel hàng loạt, xuất danh sách, xem lịch sử đăng nhập, xem nhật ký thao tác của user.
- **44 Quản lý đơn vị/tổ chức**: cây tổ chức kéo thả, thêm/sửa/xóa/di chuyển nhánh, gộp đơn vị, xuất sơ đồ tổ chức.
- **45 Quản lý vai trò**: CRUD vai trò, ma trận phân quyền (hàng = chức năng, cột = quyền Xem/Thêm/Sửa/Xóa/Duyệt/Xuất), phạm vi dữ liệu, sao chép vai trò.
- **46 Cấu hình hệ thống**: logo, favicon, tên hệ thống, tên đơn vị, màu chủ đạo, thông tin liên hệ, mẫu mã hồ sơ, giới hạn tệp, ngưỡng cảnh báo trùng lặp, ngày nghỉ lễ.
- **47 Cấu hình đơn vị**: mỗi đơn vị có thể có logo/tiêu đề văn bản/người ký mặc định riêng.
- **48 Cấu hình menu**: cây menu kéo thả, gán icon, đường dẫn, quyền hiển thị, bật/tắt, cấu hình riêng cho Web và Mobile.
- **49 Cấu hình tích hợp chữ ký số**: chọn nhà cung cấp (Ban Cơ yếu Chính phủ, VNPT-CA, Viettel-CA, FPT-CA…), cấu hình endpoint/chứng thư, hỗ trợ USB Token (qua plugin trình duyệt), HSM, Remote Signing/SmartCA. Ký PDF chuẩn **PAdES**, ký XML chuẩn **XAdES**; xác thực chữ ký và hiển thị thông tin chứng thư.
- **50 Cấu hình email & SMS**: SMTP (host/port/SSL/tài khoản), nhà cung cấp SMS + brandname, quản lý mẫu email/SMS theo sự kiện với biến động, **nút gửi thử**, xem hàng đợi gửi và log lỗi, cấu hình bật/tắt từng loại thông báo.
- **51 Cấu hình thông tin sáng kiến**: mức cảnh báo trùng lặp, hệ số tính điểm mặc định, thời hạn nộp hồ sơ mặc định, quy định áp dụng theo từng năm, số ký tự tối thiểu cho từng trường nội dung, bật/tắt kiểm tra trùng lặp tự động.
- Bổ sung: trang **Nhật ký hệ thống** (lọc theo user/module/hành động/thời gian, xuất Excel), **Nhật ký lỗi**, **Nhật ký đồng bộ**, **Sao lưu/phục hồi** (danh sách bản backup, tải về, khôi phục — kèm hướng dẫn `pg_dump`/`pg_restore` trong `docs/DEPLOYMENT.md`).

---

## 6. YÊU CẦU AN TOÀN THÔNG TIN (Mục 3.3 E-HSMT)

Hệ thống là **hệ thống nội bộ có xử lý thông tin riêng, thông tin cá nhân, không xử lý thông tin bí mật nhà nước** → áp dụng **Cấp độ 2** theo Khoản 2 Điều 8 Nghị định 85/2016/NĐ-CP và Điều 7 Thông tư 12/2022/TT-BTTTT. Ghi rõ trong `docs/AN-TOAN-THONG-TIN.md` và triển khai:

**Mức quản lý – tổ chức:** sinh sẵn tài liệu `docs/QUY-CHE-SU-DUNG-HE-THONG.md` (trách nhiệm từng nhóm người dùng, quy định mật khẩu, quy định bảo vệ tài khoản).

**Mức hệ điều hành / hạ tầng:** container chạy user non-root, read-only filesystem nơi có thể, tách tài khoản quản trị hệ thống khỏi tài khoản nghiệp vụ, cấu hình healthcheck.

**Mức mạng:** bắt buộc HTTPS/TLS 1.2+ (HSTS), Nginx chặn method lạ, cấu hình firewall mẫu, chỉ mở port cần thiết, PostgreSQL/MinIO **không expose ra Internet**.

**Mức máy chủ ứng dụng:** IP allowlist cho khu vực quản trị (cấu hình được), rate limiting (`AspNetCoreRateLimit`) — mặc định 100 req/phút/IP, 5 lần đăng nhập/phút, giới hạn quyền truy cập thư mục, xác thực tăng cường (MFA) cho vai trò quản trị.

**Mức CSDL:** tài khoản ứng dụng chỉ có quyền DML (không DDL trên production), tách role `app_rw` / `app_ro` / `migration`, không cho phép kết nối trực tiếp từ ngoài, mã hóa cột nhạy cảm (`so_cccd`, secret tích hợp) bằng `pgcrypto` hoặc AES-256-GCM ở tầng ứng dụng.

**Mức ứng dụng:**
- RBAC + phạm vi dữ liệu (global query filter theo `don_vi` cho vai trò cấp đơn vị).
- **Kiểm tra quyền cập nhật trên từng chức năng** (đúng như E-HSMT nêu): mọi command đều đi qua `IPermissionService.KiemTraQuyenAsync(nguoiDung, maQuyen, doiTuongId)`.
- Chống OWASP Top 10: parameterized query (EF Core), chống XSS (sanitize HTML từ rich text bằng HtmlSanitizer), CSRF token cho form, chống IDOR (luôn kiểm tra quyền trên resource id), security headers (CSP, X-Frame-Options, X-Content-Type-Options, Referrer-Policy).
- Mã hóa mật khẩu (Argon2id), mã hóa dữ liệu nhạy cảm khi lưu và truyền.
- Audit log đầy đủ, không ghi mật khẩu/token vào log (Serilog destructuring policy loại bỏ trường nhạy cảm).

---

## 7. YÊU CẦU PHI CHỨC NĂNG (Mục 3.4 E-HSMT)

| Tiêu chí | Mục tiêu bắt buộc |
|---|---|
| Thời gian phản hồi API | P95 < 500ms (truy vấn thường), < 2s (báo cáo tổng hợp) |
| Đồng thời | ≥ 500 người dùng đồng thời không suy giảm rõ rệt |
| Tải trang lần đầu | < 3s (code splitting, lazy route, nén Brotli) |
| Uptime | 24/7, mục tiêu ≥ 99,5% |
| Sao lưu | Full hằng ngày + WAL archiving, giữ 30 ngày, **có kịch bản restore đã kiểm thử** (point-in-time recovery) |
| Khôi phục | RPO ≤ 1 giờ, RTO ≤ 4 giờ |
| Dữ liệu | Hỗ trợ dữ liệu có cấu trúc + phi cấu trúc + bán cấu trúc (jsonb); phân vùng theo thời gian |
| Trình duyệt | Chrome, Edge, Firefox (2 phiên bản gần nhất), Safari |
| Tiếng Việt | Unicode NFC chuẩn, sắp xếp theo collation `vi-VN` |
| Kiến trúc | Modular, tách service độc lập được (AI service đã tách riêng), dễ nâng cấp |
| Chịu lỗi | Circuit breaker (Polly) cho tích hợp ngoài, cô lập lỗi cục bộ, retry có giới hạn, graceful degradation (AI lỗi → hồ sơ vẫn nộp được, chỉ đánh dấu chưa kiểm tra trùng lặp) |
| Giám sát | Health check `/health`, `/health/ready`, metrics Prometheus, log tập trung Seq |
| Triển khai | Web-based hoàn toàn, người dùng chỉ cần trình duyệt |

---

## 8. QUY ƯỚC API

- Base: `/api/v1`, JSON camelCase, thời gian ISO-8601 kèm offset.
- Response chuẩn:
```json
{ "thanhCong": true, "duLieu": {}, "thongBao": "", "maLoi": null, "chiTietLoi": [] }
```
- Phân trang: `?trang=1&soDong=20&sapXep=ngayTao&huong=desc` → `{ "duLieu": [], "tongSo": 0, "trang": 1, "soDong": 20, "tongTrang": 0 }`
- Lỗi validation → HTTP 422 kèm `chiTietLoi: [{ "truong": "tenSangKien", "thongBao": "..." }]`
- Mã lỗi nghiệp vụ dạng chuỗi: `DOT_DE_NGHI_DA_DONG`, `THIEU_THANH_PHAN_BAT_BUOC`, `KHONG_CO_QUYEN_XU_LY_BUOC`, `QUY_TRINH_DANG_SU_DUNG`, `TY_LE_DONG_GOP_KHONG_HOP_LE`.
- Mọi endpoint ghi rõ `[Authorize(Policy = "...")]`, mô tả trong Swagger bằng tiếng Việt.

---

## 9. YÊU CẦU FRONTEND

**Layout:** Sider menu (render từ `cau_hinh_menu` theo quyền) + Header (logo, tên hệ thống, ô tìm kiếm nhanh, chuông thông báo realtime, menu người dùng) + Breadcrumb + Content.

**Danh sách màn hình tối thiểu (route):**
```
/dang-nhap, /quen-mat-khau, /doi-mat-khau
/                                   Dashboard theo vai trò
/sang-kien/cua-toi                  Hồ sơ của tôi (tác giả)
/sang-kien/nop-moi                  Wizard nộp hồ sơ
/sang-kien/:id                      Chi tiết hồ sơ (tab: Nội dung/Tệp/Tiến độ/Lịch sử/Trùng lặp/Điểm)
/sang-kien/:id/sua
/tiep-nhan                          Danh sách chờ tiếp nhận
/xu-ly                              Việc cần xử lý của tôi
/danh-gia                           Hồ sơ được phân công chấm
/danh-gia/:id/cham-diem             Màn hình chấm điểm
/hoi-dong, /hoi-dong/:id, /hoi-dong/:id/phien-hop/:phienId
/tra-cuu                            Tra cứu nâng cao
/bao-cao/*                          Các báo cáo 38/39/40 + tùy biến
/quan-tri/danh-muc/*                8 danh mục
/quan-tri/quy-trinh, /quan-tri/quy-trinh/:id/thiet-ke
/quan-tri/tieu-chi, /quan-tri/tieu-chi/:id
/quan-tri/nguoi-dung, /quan-tri/don-vi, /quan-tri/vai-tro
/quan-tri/cau-hinh/{he-thong|menu|email-sms|chu-ky-so|sang-kien|tich-hop}
/quan-tri/nhat-ky/{he-thong|dang-nhap|loi|dong-bo}
/cong-khai/tra-cuu                  Trang công khai không cần đăng nhập
```

**Chuẩn UI bắt buộc:**
- Hỗ trợ gõ tìm kiếm **không dấu ra kết quả có dấu**.
- Mọi bảng có: phân trang server-side, sort, filter, chọn cột hiển thị, xuất Excel, ghi nhớ trạng thái bộ lọc trong URL query.
- Mọi form dùng `react-hook-form + zod`, hiển thị lỗi tiếng Việt rõ ràng, chặn double-submit.
- Skeleton loading, empty state có hình minh họa + hướng dẫn, error boundary.
- Accessibility cơ bản: label đầy đủ, focus ring, điều hướng bàn phím, tương phản ≥ 4.5:1.
- Theme màu chủ đạo đọc từ API `cau_hinh_he_thong` (không hardcode màu).
- In ấn: CSS `@media print` cho phiếu đánh giá, biên bản.

---

## 10. DỮ LIỆU MẪU (SEED) BẮT BUỘC

Viết `SangKien.Infrastructure/Seed/` sinh dữ liệu chạy được ngay khi demo:
- 9 vai trò + đầy đủ danh sách quyền + ma trận phân quyền mặc định.
- Cây đơn vị 3 cấp (UBND thành phố → phường/phòng ban → tổ/trường học), ~20 đơn vị.
- 8 lĩnh vực (Giáo dục, Y tế, CNTT, Quản lý hành chính, Nông nghiệp, Môi trường, Văn hóa, Khác).
- 6 đối tượng áp dụng, 3 loại tác giả.
- 2 đợt đề nghị (2025 đã đóng, 2026 đang mở).
- **1 quy trình đầy đủ 6 bước mẫu**: Tiếp nhận → Thẩm định sơ bộ → Phân công chấm → Chấm điểm hội đồng → Họp hội đồng & Kết luận → Ban hành quyết định — kèm đủ trường hợp Đạt/Không đạt/Bổ sung/Chuyển cấp.
- **1 bộ tiêu chí mẫu 100 điểm**: Tính mới (30), Tính hiệu quả (30), Khả năng áp dụng (25), Phạm vi ảnh hưởng (15) — mỗi nhóm 2–3 tiêu chí con.
- 3 mức công nhận theo khoảng điểm.
- 1 hội đồng cấp cơ sở với 7 thành viên.
- 40 hồ sơ sáng kiến ở đủ mọi trạng thái (có cả cặp hồ sơ **cố ý trùng lặp ~60%** để demo chức năng AI).
- 30 tài khoản người dùng mẫu, mật khẩu thống nhất `Sk@2026` (buộc đổi lần đầu).
- 5 biểu mẫu xuất `.docx` mẫu, 8 mẫu email/SMS theo sự kiện.

---

## 11. KIỂM THỬ

Bắt buộc có unit test cho:
- Rule evaluator của workflow (điều kiện lồng nhau, AND/OR/NOT).
- Validator quy trình (7 rule ở Mục 5 nhóm II).
- Scoring engine (3 cách tính, loại điểm cao/thấp, làm tròn, xác định mức công nhận).
- Tính hạn xử lý theo ngày làm việc + ngày lễ.
- Validate tỷ lệ đóng góp tác giả, thành phần hồ sơ bắt buộc.
- Thuật toán SimHash/Jaccard/cosine trên bộ dữ liệu cố định.

Integration test (Testcontainers PostgreSQL) cho luồng end-to-end:
`Nộp hồ sơ → Tiếp nhận → Yêu cầu bổ sung → Bổ sung → Tiếp nhận → Phân công → 3 thành viên chấm → Tổng hợp → Họp & kết luận Đạt → Ban hành quyết định → Xuất báo cáo`.

E2E web: Playwright cho 5 luồng chính (đăng nhập, nộp hồ sơ, xử lý, chấm điểm, xuất báo cáo).

---

## 12. LỘ TRÌNH TRIỂN KHAI (làm tuần tự, mỗi phase 1 commit lớn + báo cáo)

| Phase | Nội dung | Đầu ra kiểm chứng |
|---|---|---|
| **0** | Khởi tạo repo, solution, docker-compose, EF Core + migration đầu tiên, Serilog, Swagger, health check, skeleton React + layout + router | `docker compose up` chạy được, mở Swagger + trang login |
| **1** | Auth (JWT + refresh), người dùng, đơn vị, vai trò, quyền, phạm vi dữ liệu, audit log, cấu hình hệ thống, cấu hình menu | Đăng nhập, phân quyền menu hoạt động |
| **2** | 8 danh mục (chức năng 1–8) đầy đủ CRUD + import/export | Quản trị nhập được toàn bộ danh mục |
| **3** | Quy trình động: schema, engine, designer ReactFlow, validator (chức năng 9–16) | Thiết kế + kích hoạt được quy trình mẫu 6 bước |
| **4** | Tiêu chí động + mức công nhận + scoring engine (chức năng 17–18) | Cấu hình bộ tiêu chí 100 điểm, tính điểm đúng |
| **5** | Hội đồng, thành viên, phiên họp, biên bản, bỏ phiếu (chức năng 19–20) | Tạo hội đồng + phiên họp, sinh biên bản |
| **6** | Nộp hồ sơ: wizard, tệp đính kèm, MinIO, thành phần hồ sơ, lịch sử (chức năng 21–25) | Tác giả nộp được hồ sơ hoàn chỉnh |
| **7** | Tiếp nhận & xử lý hồ sơ, theo dõi tiến độ, thông báo, SignalR (chức năng 27–32) | Chạy trọn workflow từ nộp đến phê duyệt |
| **8** | Đánh giá: phân công, phiếu chấm, tổng hợp điểm, xuất phiếu (chức năng 33–36) | Hội đồng chấm và ra kết quả |
| **9** | AI trùng lặp: ai-service OCR, chunking, embedding, pgvector, UI đối chiếu (chức năng 26 + Mục 3.2) | Cặp hồ sơ seed trùng ~60% được phát hiện đúng |
| **10** | Tra cứu, thống kê báo cáo, dashboard, xuất Excel/PDF, biểu mẫu động (chức năng 7, 37–40) | Đủ 3 báo cáo bắt buộc + dashboard |
| **11** | Tích hợp SSO/IOC/Thi đua khen thưởng, chữ ký số, email/SMS (chức năng 41, 49, 50) | Test kết nối + ký số PDF thành công |
| **12** | Mobile app Android/iOS (chức năng 42) | Build được APK + IPA (hoặc chạy Expo Go) |
| **13** | Hardening: bảo mật, hiệu năng, phân vùng dữ liệu, backup, tài liệu, hướng dẫn sử dụng, video demo script | Đủ tài liệu bàn giao + kịch bản nghiệm thu |

---

## 13. TÀI LIỆU BÀN GIAO PHẢI SINH RA

1. `docs/TAI-LIEU-MO-TA-GIAI-PHAP.md` — kiến trúc, công nghệ, sơ đồ (Mermaid).
2. `docs/TAI-LIEU-HUONG-DAN-SU-DUNG.md` — theo từng vai trò, kèm mô tả từng màn hình.
3. `docs/TAI-LIEU-QUAN-TRI-VAN-HANH.md` — cài đặt, cấu hình, sao lưu, phục hồi, xử lý sự cố.
4. `docs/AN-TOAN-THONG-TIN.md` — hồ sơ đề xuất cấp độ 2 theo NĐ 85/2016 + TT 12/2022.
5. `docs/API.md` — sinh từ OpenAPI.
6. `docs/KICH-BAN-NGHIEM-THU.md` — bảng test case ánh xạ 1-1 với 51 chức năng, có cột "Kết quả mong đợi" (dùng để nghiệm thu với Chủ đầu tư).
7. `docs/KE-HOACH-CONG-TAC.md` — tiến độ, nhân sự, mốc bàn giao (phục vụ Mục 4 E-HSMT).

---

## 14. BẢNG TRUY VẾT 51 CHỨC NĂNG → MODULE (KHÔNG ĐƯỢC BỎ SÓT)

| # | Chức năng E-HSMT | Module | Route web chính | Phase |
|---|---|---|---|---|
| 1 | Lĩnh vực | DanhMuc | `/quan-tri/danh-muc/linh-vuc` | 2 |
| 2 | Đối tượng | DanhMuc | `/quan-tri/danh-muc/doi-tuong` | 2 |
| 3 | Đợt đề nghị | DanhMuc | `/quan-tri/danh-muc/dot-de-nghi` | 2 |
| 4 | Loại tác giả | DanhMuc | `/quan-tri/danh-muc/loai-tac-gia` | 2 |
| 5 | Đơn vị phê duyệt | ToChuc | `/quan-tri/don-vi` | 2 |
| 6 | Biểu mẫu xuất | BieuMau | `/quan-tri/danh-muc/bieu-mau-xuat` | 2/10 |
| 7 | Biểu mẫu thống kê | BaoCao | `/quan-tri/danh-muc/bieu-mau-thong-ke` | 10 |
| 8 | Quyết định | QuyetDinh | `/quan-tri/danh-muc/quyet-dinh` | 2 |
| 9 | Cấu hình quy trình | Workflow | `/quan-tri/quy-trinh` | 3 |
| 10 | Cấu hình trường hợp | Workflow | designer → tab Trường hợp | 3 |
| 11 | Cấu hình bước xử lý | Workflow | designer → node bước | 3 |
| 12 | Cấu hình chức năng bổ sung | Workflow | designer → tab Chức năng bổ sung | 3 |
| 13 | Cấu hình thành phần hồ sơ | Workflow | `/quan-tri/quy-trinh/:id/thanh-phan` | 3 |
| 14 | Cấu hình trạng thái bước | Workflow | designer → tab Trạng thái | 3 |
| 15 | Cấu hình tác nhân xử lý | Workflow | designer → tab Tác nhân | 3 |
| 16 | Cấu hình liên thông | TichHop | `/quan-tri/quy-trinh/:id/lien-thong` | 3/11 |
| 17 | Danh sách nhóm tiêu chí | TieuChi | `/quan-tri/tieu-chi/:id` | 4 |
| 18 | Cấu hình tiêu chí động | TieuChi | `/quan-tri/tieu-chi/:id` | 4 |
| 19 | Danh sách hội đồng | HoiDong | `/hoi-dong` | 5 |
| 20 | Cấu hình thành viên hội đồng | HoiDong | `/hoi-dong/:id` | 5 |
| 21 | Đăng nhập (+SSO) | Auth | `/dang-nhap` | 1/11 |
| 22 | Đăng ký nộp sáng kiến | SangKien | `/sang-kien/nop-moi` | 6 |
| 23 | Quản lý hồ sơ sáng kiến | SangKien | `/sang-kien/cua-toi` | 6 |
| 24 | Thành phần hồ sơ | SangKien | wizard bước 4–6 | 6 |
| 25 | Tập tin đính kèm | TepTin | wizard bước 5 | 6 |
| 26 | Kiểm tra trùng lặp/đạo văn | Ai | `/sang-kien/:id` tab Trùng lặp | 9 |
| 27 | Tiếp nhận hồ sơ | XuLy | `/tiep-nhan` | 7 |
| 28 | Danh sách hồ sơ | XuLy | `/xu-ly` | 7 |
| 29 | Xử lý hồ sơ | Workflow | `/sang-kien/:id` | 7 |
| 30 | Theo dõi hồ sơ | XuLy | `/sang-kien/:id` tab Tiến độ | 7 |
| 31 | Đính kèm quyết định | QuyetDinh | `/sang-kien/:id` tab Quyết định | 7 |
| 32 | Kết quả sáng kiến | KetQua | `/bao-cao/ket-qua` | 7 |
| 33 | Danh sách hồ sơ đánh giá | DanhGia | `/danh-gia` | 8 |
| 34 | Đánh giá hồ sơ | DanhGia | `/danh-gia/:id/cham-diem` | 8 |
| 35 | Phiếu đánh giá | DanhGia | `/danh-gia/:id/phieu` | 8 |
| 36 | Đính kèm quyết định | QuyetDinh | `/quan-tri/danh-muc/quyet-dinh` | 7 |
| 37 | Tra cứu, tìm kiếm | TraCuu | `/tra-cuu` | 10 |
| 38 | DS sáng kiến đạt | BaoCao | `/bao-cao/sang-kien-dat` | 10 |
| 39 | DS sáng kiến chưa đạt | BaoCao | `/bao-cao/sang-kien-chua-dat` | 10 |
| 40 | DS sáng kiến theo đơn vị | BaoCao | `/bao-cao/theo-don-vi` | 10 |
| 41 | Tích hợp SSO/IOC/TĐKT | TichHop | `/quan-tri/cau-hinh/tich-hop` | 11 |
| 42 | Ứng dụng di động | Mobile | app | 12 |
| 43 | Quản lý người dùng | QuanTri | `/quan-tri/nguoi-dung` | 1 |
| 44 | Quản lý đơn vị, tổ chức | QuanTri | `/quan-tri/don-vi` | 1 |
| 45 | Quản lý vai trò | QuanTri | `/quan-tri/vai-tro` | 1 |
| 46 | Cấu hình hệ thống | QuanTri | `/quan-tri/cau-hinh/he-thong` | 1 |
| 47 | Cấu hình đơn vị | QuanTri | `/quan-tri/don-vi/:id/cau-hinh` | 2 |
| 48 | Cấu hình menu | QuanTri | `/quan-tri/cau-hinh/menu` | 1 |
| 49 | Cấu hình chữ ký số | KySo | `/quan-tri/cau-hinh/chu-ky-so` | 11 |
| 50 | Cấu hình email & SMS | ThongBao | `/quan-tri/cau-hinh/email-sms` | 11 |
| 51 | Cấu hình thông tin sáng kiến | QuanTri | `/quan-tri/cau-hinh/sang-kien` | 2 |

---

## 15. TIÊU CHÍ HOÀN THÀNH (DEFINITION OF DONE)

Một chức năng chỉ được coi là xong khi:
- [ ] Có API + validator + phân quyền + audit log
- [ ] Có UI web đầy đủ (danh sách/chi tiết/thêm/sửa/xóa/xuất) và responsive
- [ ] Có unit test cho business rule chính
- [ ] Có mục trong Swagger với mô tả tiếng Việt
- [ ] Có seed data để demo được ngay
- [ ] Có dòng tương ứng trong `docs/KICH-BAN-NGHIEM-THU.md`
- [ ] Không có warning build, không có `TODO` còn sót trong code đường chính
- [ ] Đã cập nhật `docs/CHANGELOG.md`

---

## 16. BẮT ĐẦU NGAY

Bắt đầu **PHASE 0**:
1. Tạo cấu trúc thư mục như Mục 3.
2. Tạo solution `.NET 8` với 8 project, cấu hình DI, Serilog, Swagger, health check, global exception middleware, response wrapper.
3. Cấu hình EF Core + Npgsql, tạo `AppDbContext` với base entity + soft delete filter + audit interceptor.
4. Viết migration đầu tiên cho nhóm bảng quản trị (`nguoi_dung`, `vai_tro`, `quyen`, `vai_tro_quyen`, `nguoi_dung_vai_tro`, `don_vi`, `cau_hinh_he_thong`, `cau_hinh_menu`, `nhat_ky_he_thong`).
5. Tạo `web/` với Vite + React + TS + AntD + router + layout + trang đăng nhập (mock API).
6. Viết `deploy/docker-compose.yml` (postgres 16 + pgvector, minio, redis, seq, api, web).
7. Viết `README.md` (cách chạy) và `docs/CHANGELOG.md`.

Xong Phase 0, chạy `docker compose up -d`, xác nhận build sạch, rồi báo cáo tóm tắt và xin xác nhận trước khi sang Phase 1.
