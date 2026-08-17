# Nhật ký thay đổi

Định dạng theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/).

---

## [1.0.0] — 2026-08-17

Bản đầu tiên: xây dựng nền tảng theo đặc tả `docs/00-MASTER-SPEC.md`.

### Nền tảng và kiến trúc

- Khởi tạo solution .NET 8 gồm 9 dự án theo Clean Architecture, bật `TreatWarningsAsErrors`
  và kiểm tra lỗ hổng gói NuGet ở mức lỗi build.
- Mô hình dữ liệu đầy đủ ~55 bảng PostgreSQL 16 với quy ước dùng chung: khóa `uuid`, audit,
  soft delete toàn hệ thống, `timestamptz` lưu UTC, `jsonb` cho dữ liệu bán cấu trúc.
- Cột `*_khong_dau` đồng bộ tự động qua interceptor, phục vụ tìm kiếm tiếng Việt không dấu.
- Sắp xếp theo collation ICU `vi-VN`.

### Engine nghiệp vụ

- **Engine quy trình động**: rule evaluator tự viết (`= != > >= < <= IN CONTAINS BETWEEN`,
  `AND/OR/NOT` lồng nhau, giới hạn độ sâu), validator 7 quy tắc bắt buộc, tính hạn xử lý theo
  ngày làm việc và ngày nghỉ lễ, quy tắc tác nhân MỘT_NGƯỜI / TẤT_CẢ / ĐA_SỐ.
- **Snapshot quy trình**: hồ sơ chạy theo cấu hình đóng băng lúc nộp; quy trình đang có hồ sơ
  chạy dở bị chặn sửa, buộc tạo phiên bản mới.
- **Engine tính điểm**: 3 cách tính, loại điểm cao/thấp khi ≥ 5 phiếu, làm tròn cấu hình được,
  xác định mức công nhận theo khoảng điểm, kiểm tra chồng lấn khoảng điểm.
- **Engine kiểm tra trùng lặp** chạy hoàn toàn nội bộ: SimHash, MinHash/LSH, TF-IDF cosine,
  Jaccard, vector nhúng; điểm tổng hợp theo hệ số cấu hình được; trả về từng cặp đoạn trùng
  kèm vị trí ký tự để giao diện highlight.

### API và hạ tầng

- Xác thực Argon2id + JWT (access 15 phút) + refresh token 7 ngày xoay vòng, thu hồi được;
  phát hiện tái sử dụng token đã thu hồi thì thu hồi cả chuỗi phiên.
- Phân quyền trên từng chức năng qua pipeline MediatR, kèm phạm vi dữ liệu theo đơn vị.
- Mã hóa AES-256-GCM cho số CCCD và secret tích hợp.
- Tải tệp kiểm tra magic number, chặn tệp thực thi, tính SHA-256, chống path traversal.
- Rate limit 100 req/phút/IP và 5 lần đăng nhập/phút/IP; security headers đầy đủ.
- Swagger tiếng Việt, SignalR hub thông báo realtime, health check `/health` và `/health/ready`.
- Xuất Excel (ClosedXML) và PDF (QuestPDF) theo mẫu văn bản hành chính Việt Nam.

### Giao diện web

- React 18 + TypeScript + Vite + Ant Design 5, chia gói theo route.
- Menu và màu chủ đạo đọc động từ cấu hình hệ thống, lọc theo quyền người dùng.
- Wizard nộp hồ sơ 6 bước có tự lưu nháp 30 giây và checklist thành phần trực quan.
- Màn hình chấm điểm 2 panel với phiếu chấm sinh động từ bộ tiêu chí, tính điểm realtime.
- Trình thiết kế quy trình trên ReactFlow.
- Giao diện đối chiếu trùng lặp 2 cột có highlight đoạn trùng.
- Dashboard ECharts; responsive từ 320 px; hỗ trợ in ấn qua `@media print`.

### Dữ liệu mẫu

9 vai trò với ma trận phân quyền đầy đủ, 22 đơn vị 3 cấp, 8 lĩnh vực, quy trình mẫu 6 bước,
bộ tiêu chí 100 điểm, hội đồng 7 thành viên, 30 tài khoản, 40 hồ sơ ở đủ trạng thái — trong đó
có một cặp cố ý trùng lặp để demo chức năng AI.

### Triển khai

`docker compose` gồm PostgreSQL + pgvector, Redis, MinIO, Seq, dịch vụ OCR, API và web.
Container chạy user không phải root; dịch vụ dữ liệu chỉ bind `127.0.0.1`.

### Kiểm thử

166 unit test, 6 integration test trên PostgreSQL thật qua Testcontainers, và kịch bản
end-to-end 29 bước chạy qua API thật với 8 tài khoản thuộc 6 vai trò.

### Quyết định bảo mật đáng chú ý

- **Loại bỏ gói Scriban** khỏi dự án do có lỗ hổng critical
  ([GHSA-5wr9-m6jw-xx44](https://github.com/advisories/GHSA-5wr9-m6jw-xx44)) chưa có bản vá.
  Thay bằng bộ thay thế placeholder tự viết chỉ xử lý văn bản thuần — đồng thời loại bỏ hoàn
  toàn nguy cơ template injection từ mẫu thông báo do quản trị viên nhập.
- **Danh sách stopword tiếng Việt được thu hẹp có chủ đích**: do so khớp chạy trên văn bản đã bỏ
  dấu, các hư từ như `hồ`, `số`, `vị`, `trọng`, `văn`, `quả` trùng với thuật ngữ nghiệp vụ quan
  trọng sau khi bỏ dấu, nên bị loại khỏi danh sách để không phá hủy ngữ nghĩa khi so khớp.

### Lỗi đã phát hiện và sửa trong quá trình xây dựng

- Npgsql chỉ chấp nhận `timestamptz` với offset UTC → thêm value converter chuẩn hóa UTC cho
  toàn bộ cột `DateTimeOffset`.
- EF Core sinh `UPDATE` thay vì `INSERT` cho thực thể con mới của bản ghi cha đã lưu, do khóa
  chính được đánh dấu `ValueGenerated.OnAdd` → chuyển sang `ValueGeneratedNever()` và thêm trực
  tiếp vào `DbSet`.
- Với minimal hosting, cấu hình do `WebApplicationFactory` thêm bị `appsettings.json` ghi đè,
  khiến integration test nối nhầm sang cơ sở dữ liệu cục bộ → chuyển sang biến môi trường.
- Giao diện chỉ kiểm tra `=== null` trong khi API bỏ hẳn trường `null` khỏi JSON → sửa sang
  optional chaining và bổ sung error boundary cho toàn bộ route.
