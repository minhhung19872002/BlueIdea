# Hồ sơ đề xuất cấp độ an toàn hệ thống thông tin

**Hệ thống:** Nền tảng số dùng chung phục vụ hoạt động sáng kiến (BlueIdea)
**Căn cứ:** Nghị định 85/2016/NĐ-CP; Thông tư 12/2022/TT-BTTTT; Mục 3.3 E-HSMT

---

## 1. Xác định cấp độ

Hệ thống xử lý **thông tin riêng và thông tin cá nhân** của cán bộ, công chức, viên chức và
người dân tham gia hoạt động sáng kiến; **không xử lý thông tin bí mật nhà nước**.

Đối chiếu Khoản 2 Điều 8 Nghị định 85/2016/NĐ-CP và Điều 7 Thông tư 12/2022/TT-BTTTT, hệ thống
được đề xuất **Cấp độ 2**.

### Loại thông tin xử lý

| Nhóm thông tin | Ví dụ | Mức nhạy cảm |
|---|---|---|
| Thông tin cá nhân | Họ tên, ngày sinh, số CCCD, email, điện thoại của tác giả | Cao |
| Thông tin nghiệp vụ | Nội dung sáng kiến, điểm chấm, kết quả xét duyệt | Trung bình |
| Thông tin tổ chức | Cơ cấu đơn vị, danh sách hội đồng | Trung bình |
| Thông tin hệ thống | Nhật ký thao tác, nhật ký đăng nhập | Trung bình |

---

## 2. Phương án bảo đảm an toàn theo từng mức

### 2.1 Mức quản lý — tổ chức

- Tài liệu [`QUY-CHE-SU-DUNG-HE-THONG.md`](QUY-CHE-SU-DUNG-HE-THONG.md) quy định trách nhiệm
  từng nhóm người dùng, quy định mật khẩu và quy định bảo vệ tài khoản.
- Phân tách rõ **tài khoản quản trị hệ thống** (`QUAN_TRI_HE_THONG`) và **tài khoản nghiệp vụ**.
  Tài khoản quản trị không dùng cho công việc hằng ngày.
- Toàn bộ thao tác thay đổi dữ liệu đều ghi nhật ký, phục vụ truy vết và xử lý sự cố.

### 2.2 Mức hệ điều hành và hạ tầng

| Biện pháp | Cài đặt trong hệ thống |
|---|---|
| Container chạy user không phải root | `deploy/Dockerfile.api`, `ai-service/Dockerfile` tạo user `blueidea`/`aiuser` |
| Healthcheck cho mọi dịch vụ | Khai báo trong `deploy/docker-compose.yml` |
| Không expose dịch vụ nội bộ | PostgreSQL, Redis, MinIO chỉ bind `127.0.0.1` |
| Múi giờ và mã hóa ký tự thống nhất | `TZ=Asia/Ho_Chi_Minh`, `UTF-8`, collation ICU `vi-VN` |

### 2.3 Mức mạng

- Bắt buộc **HTTPS/TLS 1.2+** ở lớp Nginx; bật HSTS khi không ở môi trường phát triển
  (`app.UseHsts()` trong `Program.cs`).
- Nginx **chặn method lạ** (`deploy/nginx/web.conf` trả 405 cho method ngoài danh sách cho phép).
- CORS chỉ cho phép các nguồn khai báo trong `Cors:NguonChoPhep`.
- Chỉ mở cổng thật sự cần: 80/443 cho Nginx, 8080 cho API (nội bộ).

### 2.4 Mức máy chủ ứng dụng

| Yêu cầu | Cài đặt |
|---|---|
| Giới hạn tần suất | 100 req/phút/IP toàn cục; 5 lần/phút/IP cho đăng nhập (`AddRateLimiter`) |
| Security headers | `X-Content-Type-Options`, `X-Frame-Options: DENY`, `Referrer-Policy`, `Permissions-Policy`, `CSP` (`MiddlewareHeaderBaoMat`) |
| Ẩn thông tin máy chủ | Gỡ header `Server`, `X-Powered-By` |
| Không lộ chi tiết lỗi | `MiddlewareXuLyLoi` chỉ trả thông báo chung cho lỗi 5xx ở môi trường production |
| Nén phản hồi | `UseResponseCompression` |

### 2.5 Mức cơ sở dữ liệu

- Tài khoản ứng dụng **chỉ có quyền DML** trên production; DDL dành riêng cho tài khoản chạy
  migration. Script `deploy/postgres/khoi-tao/01-extensions.sql` tạo sẵn role `app_ro` chỉ đọc.
- PostgreSQL **không expose ra Internet** (chỉ bind `127.0.0.1` trong compose).
- **Mã hóa cột nhạy cảm** ở tầng ứng dụng bằng AES-256-GCM: số CCCD của tác giả, client secret
  của hệ thống tích hợp, mật khẩu SMTP/SMS.
- **Soft delete toàn hệ thống**: dữ liệu không bị xóa vật lý, phục vụ truy vết và khôi phục.
- Bảng nhật ký lớn (`nhat_ky_he_thong`, `nhat_ky_dang_nhap`, `thong_bao`) được thiết kế để
  phân vùng theo tháng khi khối lượng tăng.

### 2.6 Mức ứng dụng

#### Xác thực

- Mật khẩu băm bằng **Argon2id** — 4 vòng lặp, 64 MB bộ nhớ, 4 luồng (khuyến nghị OWASP).
- So sánh hash bằng `CryptographicOperations.FixedTimeEquals` để chống **timing attack**.
- **Khóa tài khoản** sau 5 lần đăng nhập sai liên tiếp, trong 15 phút (cấu hình được).
- Thông báo lỗi đăng nhập **không tiết lộ** tài khoản có tồn tại hay không.
- **JWT access token 15 phút** + **refresh token 7 ngày**, lưu dưới dạng hash SHA-256 trong CSDL.
- Refresh token **xoay vòng**: mỗi lần làm mới thì thu hồi token cũ. Nếu phát hiện token đã thu
  hồi được dùng lại (dấu hiệu lộ token), hệ thống **thu hồi toàn bộ phiên** của người dùng đó.
- Đổi mật khẩu sẽ thu hồi mọi phiên đang mở.
- Chính sách mật khẩu cấu hình được: độ dài tối thiểu, chữ hoa/thường/số/ký tự đặc biệt,
  không trùng N mật khẩu gần nhất, buộc đổi sau N ngày.
- Hỗ trợ MFA TOTP cho tài khoản quản trị (trường `mfa_enabled`, `mfa_secret`).

#### Phân quyền

- **RBAC**: vai trò là dữ liệu, không phải enum cứng; ma trận vai trò × quyền cấu hình được.
- **Kiểm tra quyền trên từng chức năng**: mọi command đi qua `HanhViPhanQuyen` trong pipeline
  MediatR, gọi `IDichVuPhanQuyen.BatBuocCoQuyenAsync`.
- **Phạm vi dữ liệu** theo vai trò: `TOAN_HE_THONG`, `DON_VI`, `DON_VI_VA_CAP_DUOI`, `CA_NHAN`,
  `TUY_CHINH` — áp dụng ngay ở tầng truy vấn nên **chống IDOR** từ gốc.
- Tác nhân xử lý bước được kiểm tra riêng trong engine quy trình: người không phải tác nhân
  không nhận được bất kỳ hành động nào (kiểm chứng bằng kiểm thử tích hợp).

#### Chống OWASP Top 10

| Nguy cơ | Biện pháp |
|---|---|
| A01 Broken Access Control | Kiểm tra quyền từng chức năng + phạm vi dữ liệu ở tầng truy vấn |
| A02 Cryptographic Failures | Argon2id cho mật khẩu; AES-256-GCM cho dữ liệu nhạy cảm; TLS bắt buộc |
| A03 Injection | EF Core tham số hóa toàn bộ truy vấn; **không dùng template engine** cho mẫu thông báo (xem bên dưới) |
| A04 Insecure Design | Rule evaluator tự viết, không `eval` động; giới hạn độ sâu biểu thức |
| A05 Security Misconfiguration | Security headers; ẩn header máy chủ; Swagger tắt mặc định ở production |
| A06 Vulnerable Components | Loại bỏ Scriban do có lỗ hổng critical chưa vá; bật `NuGetAudit` mức lỗi |
| A07 Auth Failures | Khóa tài khoản, rate limit đăng nhập, refresh token xoay vòng |
| A08 Data Integrity | Optimistic concurrency bằng `phien_ban`; `Idempotency-Key` chống double-submit |
| A09 Logging Failures | Audit log đầy đủ, tự động loại bỏ trường nhạy cảm khỏi log |
| A10 SSRF | Không có chức năng cho người dùng nhập URL để máy chủ gọi |

#### Quyết định đáng chú ý về mẫu thông báo

Mẫu email/SMS do **quản trị viên nhập trên giao diện**. Nếu dùng template engine đầy đủ
(Scriban, Handlebars), nội dung mẫu có thể chứa biểu thức hoặc lời gọi hàm — trở thành bề mặt
tấn công **template injection** cho phép thực thi mã tùy ý trên máy chủ.

Hệ thống dùng `BoKetXuatMau` tự viết: chỉ thay thế placeholder `{{ ten_bien }}` bằng văn bản
thuần, có giới hạn độ dài và timeout regex. **Không bao giờ thực thi mã trong mẫu.**

Đồng thời gói Scriban đã bị loại khỏi dự án do có lỗ hổng critical
([GHSA-5wr9-m6jw-xx44](https://github.com/advisories/GHSA-5wr9-m6jw-xx44)) chưa có bản vá.

#### An toàn tệp tải lên

- Kiểm tra **magic number** của tệp, **không tin phần mở rộng** do client gửi.
- Danh sách đen phần mở rộng thực thi (`.exe`, `.dll`, `.bat`, `.ps1`, `.js`, `.php`…).
- Giới hạn dung lượng theo cấu hình; tính **SHA-256** để chống lưu trùng.
- Chống **path traversal**: đường dẫn tệp luôn được chuẩn hóa và kiểm tra nằm trong thư mục bucket.
- Truy cập tệp qua endpoint có kiểm tra quyền, **không expose đường dẫn hệ thống tệp**.

#### Nhật ký và giám sát

- `nhat_ky_he_thong` ghi: ai, khi nào, từ IP nào, user agent, module, hành động, đối tượng,
  **giá trị trước và sau**, kết quả.
- Trường nhạy cảm (`matKhau`, `token`, `clientSecret`, `soCccd`, `mfaSecret`…) **tự động bị thay
  bằng `***`** trước khi ghi log.
- Ghi audit thất bại **không làm hỏng nghiệp vụ chính** — chỉ ghi cảnh báo vào log ứng dụng.
- Health check `/health` và `/health/ready`; log tập trung qua Seq.

---

## 3. Việc phải làm trước khi vận hành thật

Đây là **điều kiện bắt buộc**, không phải khuyến nghị:

- [ ] Đổi toàn bộ khóa trong `.env`: `JWT_KHOA_KY`, `MA_HOA_KHOA`, mật khẩu PostgreSQL/Redis/MinIO.
      Các giá trị mặc định trong repo chỉ dùng cho môi trường phát triển.
- [ ] Đổi mật khẩu tài khoản `admin` và toàn bộ tài khoản mẫu; xóa tài khoản mẫu không dùng.
- [ ] Đặt `KhoiTao:NapDuLieuMau = false` trên production.
- [ ] Cấu hình chứng thư TLS thật cho Nginx; bật HSTS.
- [ ] Giới hạn `Cors:NguonChoPhep` đúng tên miền của đơn vị.
- [ ] Bật MFA cho toàn bộ tài khoản có vai trò `QUAN_TRI_HE_THONG`.
- [ ] Cấu hình IP allowlist cho khu vực quản trị ở lớp Nginx hoặc tường lửa.
- [ ] Thiết lập sao lưu tự động và **thực hiện diễn tập khôi phục** ít nhất một lần
      (xem `TAI-LIEU-QUAN-TRI-VAN-HANH.md`).
- [ ] Rà soát và phê duyệt ma trận phân quyền theo đúng quy chế của đơn vị.
