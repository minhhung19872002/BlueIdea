# Nhật ký thay đổi

Định dạng theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/).

---

## [1.2.0] — 2026-08-18

Hoàn thiện phần giao diện cho các chức năng trước đây mới có API, và chốt phương án cho chức năng
ứng dụng di động. Sau bản này cả 51 chức năng đều có đủ API + nghiệp vụ + giao diện.

### Hội đồng sáng kiến (chức năng 19–20)

- Màn hình `/hoi-dong`: danh sách, thành lập, sửa, xoá hội đồng kèm cấp xét duyệt, đợt, lĩnh vực
  phụ trách, số thành viên tối thiểu và tỷ lệ thông qua.
- Trang chi tiết có ba tab: Thông tin chung, Thành viên, Phiên họp.
- Tab **Thành viên** sửa trực tiếp trên bảng: chọn tài khoản có sẵn hoặc nhập tay người ngoài hệ
  thống, đặt chức danh và 5 nhóm quyền. Nút Lưu bị khoá khi danh sách chưa hợp lệ (không đúng một
  chủ tịch, hoặc ít hơn số thành viên tối thiểu) — chặn ngay trên giao diện thay vì để người dùng
  bấm rồi mới nhận lỗi từ máy chủ.
- Tab **Phiên họp**: tạo phiên kèm hồ sơ đưa ra xét, điểm danh, bỏ phiếu (đồng ý / không đồng ý /
  ý kiến khác, hỗ trợ phiếu kín), kiểm phiếu realtime so với ngưỡng thông qua của hội đồng, nhập
  kết luận và kết thúc phiên. Phiên đã kết thúc khoá toàn bộ thao tác bỏ phiếu và điểm danh.
- Nút **Xuất phiếu chấm** xuất PDF gộp toàn bộ phiếu chấm của hội đồng (chức năng 35) — trước đây
  endpoint đã có nhưng không màn hình nào gọi tới.

### Đăng nhập một lần SSO trên giao diện (chức năng 21, 41)

- Trang đăng nhập hỏi máy chủ `sso/trang-thai` và chỉ hiện nút **Đăng nhập một lần (SSO)** khi đã
  cấu hình nhà cung cấp — không dẫn người dùng vào một luồng chắc chắn lỗi.
- Trang `/dang-nhap/sso` nhận mã trả về, so `state` với giá trị đã lưu (chống CSRF), đổi mã lấy
  token rồi đưa người dùng về đúng trang họ định vào. Effect chạy hai lần trong StrictMode được
  chặn vì authorization code chỉ đổi được một lần.
- Đăng xuất kiểm tra phiên có phải đăng nhập bằng SSO không; nếu đúng thì lấy `end_session_endpoint`
  **trước** khi xoá token (endpoint này cần đăng nhập) rồi chuyển hướng sang nhà cung cấp.
- Bổ sung `SSO_ISSUER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET`, `SSO_SCOPE` vào `.env.example` và cả
  hai tệp compose — trước đây bản triển khai Docker không có chỗ nào để bật SSO.

### Liên thông hệ thống ngoài (chức năng 16, 41)

- Màn hình `/quan-tri/lien-thong`: khai báo hệ thống (mã, endpoint, kiểu xác thực, client id/secret,
  scope, tần suất), ánh xạ tên trường sang tên mà hệ thống ngoài yêu cầu.
- Ô bí mật để trống khi sửa = giữ nguyên giá trị đang lưu; giao diện chỉ hiển thị "Đã đặt bí mật"
  chứ không bao giờ nhận lại giá trị thật.
- **Xem trước dữ liệu** trước khi đẩy đi, chạy **đồng bộ** theo đợt/năm, và tab **Nhật ký đồng bộ**
  hiển thị số bản ghi thành công/thất bại kèm thông báo lỗi của hệ thống ngoài.

### Biểu mẫu xuất (chức năng 6)

- Thêm tab **Biểu mẫu xuất** vào màn hình Danh mục: CRUD biểu mẫu, tải tệp `.docx` mẫu và quét
  placeholder ngay khi tải lên, ánh xạ từng placeholder sang nguồn dữ liệu kèm kiểu và định dạng
  hiển thị.
- Đổi tệp mẫu giữ nguyên ánh xạ đã cấu hình cho các placeholder trùng tên.

### Bộ lọc yêu thích (chức năng 28)

- Thanh bộ lọc yêu thích dùng chung cho màn hình danh sách: chọn bộ lọc đã lưu, lưu bộ lọc hiện
  tại, đặt mặc định, xoá.
- Bộ lọc mặc định chỉ tự áp dụng khi mở màn hình mà URL **chưa** có tiêu chí nào — liên kết chia sẻ
  luôn thắng bộ lọc cá nhân.
- Máy chủ lưu tiêu chí dưới dạng đối tượng JSON còn màn hình làm việc bằng chuỗi query, nên thanh
  lọc chuyển đổi hai chiều; thêm tiêu chí lọc mới cho màn hình không phải sửa thanh lọc.

### Ứng dụng di động (chức năng 42)

- Chốt phương án **web responsive** thay cho ứng dụng đóng gói: giao diện chạy tốt từ 320px, thanh
  điều hướng chuyển thành Drawer, bảng cuộn ngang trong khung riêng. Ghi rõ giới hạn kèm theo
  (không có thông báo đẩy, không dùng ngoại tuyến) trong `TRANG-THAI-TRIEN-KHAI.md`.

### Sửa lỗi

- Bảng danh sách hồ sơ: cột "Tên sáng kiến" không đặt bề rộng nên khi tổng bề rộng các cột cố định
  vượt khung, phần dư còn lại âm và tên sáng kiến bị bóp thành một ký tự mỗi dòng.
- Menu điều hướng có sẵn mục "Hội đồng sáng kiến" trỏ tới `/hoi-dong` nhưng không có route tương
  ứng, bấm vào ra trang 404. Bổ sung mục "Liên thông hệ thống ngoài" vào nhóm Quản trị.

### Kiểm thử

- Thêm `HoiDongTests`: ràng buộc đúng một chủ tịch, số thành viên tối thiểu, luồng phiên họp đầy đủ
  (tạo phiên → điểm danh → bỏ phiếu → kiểm phiếu → kết luận → khoá bỏ phiếu), chặn người ngoài hội
  đồng bỏ phiếu, xuất phiếu chấm PDF.
- Thêm kiểm thử lưu bộ lọc trùng tên (ghi đè, giữ đúng một bộ lọc mặc định) và vòng lưu–đọc biểu
  mẫu xuất kèm tệp mẫu và bảng ánh xạ placeholder.
- Tổng: 267 unit test + 72 integration test, tất cả đều pass.

---

## [1.1.0] — 2026-08-17

Bổ sung bốn nhóm chức năng còn thiếu sau bản 1.0.0.

### Công việc nền (Hangfire)

- Hàng đợi job lưu trong chính PostgreSQL, chạy trong tiến trình API, tắt được hoàn toàn bằng
  `CongViecNen:BatHangfire=false`.
- Bốn công việc định kỳ: nhắc hạn xử lý và hạn chấm điểm (7h hằng ngày), tự đóng đợt đề nghị quá
  hạn nộp (mỗi giờ), rút hàng đợi email/SMS (mỗi 5 phút), quét bù kiểm tra trùng lặp (mỗi 15 phút).
  Biểu thức cron đọc từ cấu hình nên đổi được tần suất mà không phải build lại.
- Hai công việc theo sự kiện: trích xuất văn bản khi tải tệp, kiểm tra trùng lặp khi nộp hồ sơ.
- Dashboard `/hangfire` chỉ vai trò Quản trị hệ thống mở được — dashboard hiển thị tham số job
  (Id hồ sơ, địa chỉ email) nên không được để mở như trang tĩnh.
- Nhắc hạn chống trùng bằng chính bảng thông báo (không nhắc lại trong 20 giờ), không phải thêm
  cột trạng thái riêng.

### Gửi email và SMS thật (chức năng 50)

- Gửi email qua SMTP bằng MailKit, gửi SMS qua API nhà cung cấp; mật khẩu và API key giải mã
  AES-256-GCM khi dùng.
- Nội dung email gửi dạng `text/plain` chứ không phải HTML — mẫu thông báo do quản trị viên nhập
  nên gửi văn bản thuần loại bỏ hoàn toàn nguy cơ HTML injection.
- Bản tin lỗi được thử lại tối đa 5 lượt. Riêng trường hợp **chưa cấu hình** máy chủ gửi tin thì
  giữ nguyên trạng thái `CHO_GUI` và không tăng số lần thử, để khi quản trị viên cấu hình xong
  hàng đợi tự chạy tiếp thay vì đã cháy hết lượt thử.

### OCR nội bộ nối vào luồng nộp hồ sơ (chức năng 26)

- Tải tệp PDF/ảnh lên sẽ xếp lịch trích xuất văn bản qua dịch vụ Tesseract nội bộ, kết quả lưu vào
  `noi_dung_trich_xuat` để kiểm tra trùng lặp đọc được cả nội dung tệp scan.
- Thứ tự được bảo đảm: nộp hồ sơ chỉ chạy kiểm tra trùng lặp khi không còn tệp nào chờ OCR; tệp
  cuối cùng OCR xong sẽ tự đẩy sang kiểm tra trùng lặp; và một vòng quét định kỳ dọn nốt hồ sơ mắc
  kẹt khi OCR thất bại hẳn.
- Dịch vụ OCR chết hoặc quá thời gian chờ thì suy giảm mềm — hồ sơ vẫn nộp được bình thường.

### Ban hành quyết định và công bố kết quả (chức năng 8, 31, 32, 36)

- Màn hình ban hành quyết định: chọn sáng kiến đủ điều kiện theo đợt, sửa, xoá, xuất PDF theo mẫu
  văn bản hành chính.
- Ràng buộc nghiệp vụ: một sáng kiến chỉ nằm trong **đúng một** quyết định công nhận; quyết định
  đã ký số không sửa/xoá được; quyết định đã công bố kết quả không xoá được.
- Công bố kết quả hàng loạt: đánh dấu đã công bố, mở hiển thị công khai và gửi thông báo tới toàn
  bộ tác giả có tài khoản.

### Giao diện quản trị (chức năng 43, 44, 45, 47)

- Người dùng: thêm, sửa, gán vai trò, đặt lại mật khẩu. Mật khẩu tạm sinh bằng nguồn ngẫu nhiên
  mật mã và chỉ hiển thị đúng một lần.
- Đặt lại mật khẩu thu hồi toàn bộ refresh token đang mở của tài khoản đó.
- Chặn tự khoá mình ra khỏi hệ thống: không được bỏ quyền quản trị hoặc khoá tài khoản khi đó là
  quản trị viên đang hoạt động cuối cùng.
- Vai trò: ma trận phân quyền sửa trực tiếp trên bảng, chọn cả cột, gom thay đổi rồi lưu một lần.
  Vai trò hệ thống không đổi được mã vì mã được mã nguồn tham chiếu trực tiếp.
- Đơn vị: cây tổ chức kèm panel chi tiết, thêm đơn vị con, sửa, xoá; cấu hình tiêu đề văn bản và
  người ký mặc định của đơn vị.

### Sửa lỗi

- **Tệp trùng nội dung không được trích xuất văn bản**: điều kiện xếp lịch OCR dựa trên "tệp mới"
  thay vì trạng thái OCR, nên một tệp được dùng lại theo hash mà chưa từng trích xuất sẽ bị bỏ
  qua vĩnh viễn.
- **Hồ sơ mất ngày công nhận khi ban hành quyết định mới**: hàm gắn sáng kiến đọc ngày ban hành
  lại từ cơ sở dữ liệu, trong khi bản ghi quyết định mới chỉ đang nằm trong change tracker.
- **Trạng thái công bố bị mất với hồ sơ không có bản ghi hội đồng**: trạng thái công bố trước đây
  chỉ lưu ở `ket_qua_xet_duyet`, nhưng không phải hồ sơ nào cũng có bản ghi đó. Chuyển sang lưu
  trên chính hồ sơ.
- **Mã HTTP không đúng ngữ nghĩa**: đăng nhập sai và token hết hạn trả 400 thay vì 401; các lỗi
  xung đột trạng thái trả 400 thay vì 409. Đã gom lại theo đúng nhóm 401 / 403 / 404 / 409 / 422.

### Thay đổi khác

- Ngưỡng rate limit đọc từ cấu hình `GioiHanTruyCap:*` thay vì cố định trong mã nguồn.
- Thêm cột `da_cong_bo_ket_qua` và `ngay_cong_bo_ket_qua` cho bảng `sang_kien` (migration
  `ThemTrangThaiCongBoKetQua`).

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
- Thiếu `.dockerignore` khiến thư mục `obj/` của máy host bị copy vào image, ghi đè kết quả
  `dotnet restore` và làm hỏng bước publish.
- API thoát ngay khi lần kết nối cơ sở dữ liệu đầu tiên thất bại, dẫn tới vòng lặp khởi động lại
  trong docker-compose khi DNS nội bộ chưa sẵn sàng → thêm cơ chế thử lại có backoff, vẫn giữ
  fail-fast cho lỗi migration thật.
- Cổng máy chủ trong docker-compose bị cố định → tham số hóa toàn bộ qua biến `*_PORT` để triển
  khai được trên máy đã dùng sẵn các cổng mặc định.
- Container web luôn báo `unhealthy` dù phục vụ HTTP 200: health check dùng `localhost`, tên này
  phân giải ra `::1` trước trong khi Nginx chỉ lắng nghe IPv4, và BusyBox `wget` không tự chuyển
  sang địa chỉ còn lại → đổi sang `127.0.0.1` và thêm `--start-period`.
