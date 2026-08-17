# BlueIdea — Nền tảng số dùng chung phục vụ hoạt động sáng kiến

Hệ thống quản lý toàn trình hoạt động sáng kiến, cải tiến kỹ thuật cho chính quyền địa phương:
**đăng ký → tiếp nhận → thẩm định → hội đồng chấm điểm → công nhận → thống kê báo cáo**.

Toàn bộ nghiệp vụ, dữ liệu và giao diện dùng **tiếng Việt có dấu (Unicode NFC)**.
Quy trình xử lý, tiêu chí chấm điểm, thành phần hồ sơ, biểu mẫu, menu và vai trò đều
**cấu hình được trên giao diện quản trị** — đổi quy trình không cần sửa code.

> **AI chạy hoàn toàn nội bộ.** Chức năng OCR và kiểm tra trùng lặp/đạo văn không gọi bất kỳ
> API AI bên thứ ba nào. Xem [`docs/ADR/0001-ai-noi-bo.md`](docs/ADR/0001-ai-noi-bo.md).

---

## 1. Chạy nhanh bằng Docker

```bash
cp .env.example .env          # rồi đổi các khoá bảo mật bên trong
docker compose --env-file .env -f deploy/docker-compose.yml up -d
```

> Cần `--env-file .env` vì Docker Compose mặc định tìm tệp `.env` trong thư mục chứa
> tệp compose (`deploy/`), không phải thư mục gốc repo. Nếu cổng mặc định đã bị chiếm trên máy,
> đổi các biến `*_PORT` trong `.env`.

| Thành phần | Địa chỉ | Ghi chú |
|---|---|---|
| Web | http://localhost:3000 | Giao diện người dùng |
| API + Swagger | http://localhost:8080/swagger | Tài liệu API tiếng Việt |
| Health check | http://localhost:8080/health | `/health/ready` kiểm tra CSDL |
| Dịch vụ OCR nội bộ | http://localhost:8088/health | Tesseract 5 với `vie` + `eng` |
| MinIO Console | http://localhost:9001 | Lưu trữ tệp |
| Seq | http://localhost:5341 | Log tập trung |

Lần khởi động đầu tiên hệ thống tự chạy migration và nạp dữ liệu mẫu (Mục 10 đặc tả).
API tự thử lại kết nối cơ sở dữ liệu có backoff, nên không phụ thuộc thứ tự khởi động container.

### Tài khoản demo

Mật khẩu chung: `Sk@2026`

| Tài khoản | Vai trò |
|---|---|
| `admin` | Quản trị hệ thống (toàn quyền) |
| `lanhdao` | Lãnh đạo phê duyệt |
| `tiepnhan` | Cán bộ tiếp nhận |
| `thuky` | Thư ký hội đồng |
| `chutich` | Chủ tịch hội đồng |
| `hoidong01`…`hoidong05` | Thành viên hội đồng |
| `gv.lan`, `bs.tuan`, `cb.khoa`, … | Tác giả |

---

## 2. Chạy khi phát triển

```bash
# CSDL (nếu cổng 5432 bận, đặt POSTGRES_PORT trong .env)
docker compose -f deploy/docker-compose.yml up -d postgres

# API
./scripts/chay-api-cuc-bo.ps1 -Cong 5299 -CongPostgres 5432

# Web
cd web && npm install && npm run dev
```

---

## 3. Kiểm thử

```bash
# Unit test toàn bộ business rule (Mục 11 đặc tả)
dotnet test

# Kiểm thử luồng nghiệp vụ end-to-end qua API thật
./scripts/kiem-thu-luong-nghiep-vu.ps1 -Goc http://localhost:5299
```

Kịch bản end-to-end đi trọn vòng đời hồ sơ: nộp → tiếp nhận → thẩm định → phân công →
3 thành viên chấm → tổng hợp điểm → hội đồng kết luận Đạt → ban hành quyết định → báo cáo.

---

## 4. Kiến trúc

Clean Architecture + CQRS nhẹ. Nghiệp vụ phức tạp được tách thành các **engine thuần logic**
không phụ thuộc CSDL, nhờ vậy unit-test được trọn vẹn:

```
src/
├── BlueIdea.Shared/          Result<T>, PagedResult<T>, tiện ích tiếng Việt
├── BlueIdea.Domain/          Thực thể, hằng số nghiệp vụ, quy tắc miền
├── BlueIdea.Workflow/        Engine quy trình động: rule evaluator, validator 7 rule,
│                             tính hạn theo ngày làm việc, snapshot quy trình
├── BlueIdea.Scoring/         Engine tính điểm: 3 cách tính, loại điểm cao/thấp, mức công nhận
├── BlueIdea.Ai/              SimHash, MinHash/LSH, TF-IDF, embedding nội bộ, pipeline trùng lặp
├── BlueIdea.Reporting/       Xuất Excel (ClosedXML) và PDF (QuestPDF)
├── BlueIdea.Application/     Command/Query, validator, dịch vụ nghiệp vụ, hợp đồng hạ tầng
├── BlueIdea.Infrastructure/  EF Core + PostgreSQL, Argon2id, JWT, AES-GCM, lưu trữ tệp, seed
└── BlueIdea.Api/             Controller, middleware, Swagger, SignalR, health check

web/          React 18 + TypeScript + Vite + Ant Design 5
ai-service/   FastAPI + Tesseract 5 (vie+eng) — OCR nội bộ
deploy/       docker-compose, Dockerfile, cấu hình Nginx
docs/         Đặc tả, ADR, tài liệu bàn giao
```

### Điểm thiết kế đáng chú ý

**Quy trình động có phiên bản.** Khi nộp, hồ sơ lưu `quy_trinh_snapshot` (JSON đóng băng cấu
hình quy trình). Engine luôn chạy theo snapshot, nên quản trị viên sửa quy trình không làm
hỏng hồ sơ đang xử lý. Quy trình đang có hồ sơ chạy dở sẽ bị chặn sửa, buộc tạo phiên bản mới.

**Không hardcode nút bấm.** Giao diện gọi `GET /api/v1/sang-kien/{id}/hanh-dong` để lấy danh
sách hành động khả dụng, kèm cờ `biChan` và lý do khi điều kiện chưa thỏa mãn.

**Rule evaluator tự viết, không eval động.** Điều kiện chuyển tiếp lưu dạng `jsonb`, hỗ trợ
`= != > >= < <= IN CONTAINS BETWEEN` và `AND/OR/NOT` lồng nhau, có giới hạn độ sâu.

**Tìm kiếm tiếng Việt không dấu.** Mọi bảng có cột `*_khong_dau` được đồng bộ tự động qua
interceptor; gõ "sang kien" vẫn ra "sáng kiến".

**Kiểm tra trùng lặp nhiều tầng.** Lọc thô bằng SimHash + MinHash/LSH, so khớp tinh bằng
TF-IDF cosine (từ vựng) và cosine embedding (ngữ nghĩa), điểm tổng hợp theo hệ số cấu hình
được. Kết quả kèm từng cặp đoạn trùng và vị trí ký tự để giao diện highlight đối chiếu 2 cột.

---

## 5. An toàn thông tin

Hệ thống áp dụng **Cấp độ 2** theo NĐ 85/2016/NĐ-CP và TT 12/2022/TT-BTTTT.
Chi tiết trong [`docs/AN-TOAN-THONG-TIN.md`](docs/AN-TOAN-THONG-TIN.md).

- Mật khẩu băm **Argon2id** (4 vòng lặp, 64 MB, 4 luồng); khoá tài khoản sau 5 lần sai.
- JWT access token 15 phút + refresh token 7 ngày **xoay vòng và thu hồi được**; phát hiện
  tái sử dụng token đã thu hồi thì thu hồi cả chuỗi.
- Dữ liệu cá nhân nhạy cảm (số CCCD, secret tích hợp) mã hoá **AES-256-GCM** ở tầng ứng dụng.
- Kiểm tra quyền trên **từng chức năng** qua pipeline MediatR, kèm phạm vi dữ liệu theo đơn vị.
- **Audit log đầy đủ** (ai / khi nào / IP / giá trị trước–sau), tự động loại bỏ trường nhạy cảm.
- Tệp tải lên kiểm tra **magic number** (không tin phần mở rộng), chặn tệp thực thi, tính SHA-256.
- Security headers, CORS allowlist, rate limit 100 req/phút/IP và 5 lần đăng nhập/phút/IP.
- Container chạy user không phải root; PostgreSQL/MinIO/Redis chỉ mở trên `127.0.0.1`.
- Mẫu thông báo dùng bộ thay thế placeholder tự viết thay vì template engine đầy đủ, nhằm
  loại bỏ hoàn toàn nguy cơ template injection từ nội dung do quản trị viên nhập.

---

## 6. Tài liệu

| Tài liệu | Nội dung |
|---|---|
| [`docs/00-MASTER-SPEC.md`](docs/00-MASTER-SPEC.md) | Đặc tả gốc đầy đủ |
| [`docs/TAI-LIEU-MO-TA-GIAI-PHAP.md`](docs/TAI-LIEU-MO-TA-GIAI-PHAP.md) | Kiến trúc, công nghệ, sơ đồ |
| [`docs/TAI-LIEU-HUONG-DAN-SU-DUNG.md`](docs/TAI-LIEU-HUONG-DAN-SU-DUNG.md) | Hướng dẫn theo từng vai trò |
| [`docs/TAI-LIEU-QUAN-TRI-VAN-HANH.md`](docs/TAI-LIEU-QUAN-TRI-VAN-HANH.md) | Cài đặt, sao lưu, xử lý sự cố |
| [`docs/AN-TOAN-THONG-TIN.md`](docs/AN-TOAN-THONG-TIN.md) | Hồ sơ đề xuất cấp độ 2 |
| [`docs/KICH-BAN-NGHIEM-THU.md`](docs/KICH-BAN-NGHIEM-THU.md) | Test case ánh xạ 1-1 với 51 chức năng |
| [`docs/TRANG-THAI-TRIEN-KHAI.md`](docs/TRANG-THAI-TRIEN-KHAI.md) | Trạng thái thực tế từng chức năng |
| [`docs/CHANGELOG.md`](docs/CHANGELOG.md) | Nhật ký thay đổi |
| [`docs/ADR/`](docs/ADR) | Các quyết định kiến trúc |

---

## 7. Giấy phép và ghi chú

Mã nguồn phục vụ hồ sơ dự thầu và triển khai nội bộ cho cơ quan nhà nước.
Trước khi đưa vào vận hành thật, **bắt buộc** đổi toàn bộ khoá trong `.env`
và rà soát checklist trong `docs/TAI-LIEU-QUAN-TRI-VAN-HANH.md`.
