# Hướng dẫn triển khai

Tài liệu này mô tả **cách đưa hệ thống lên môi trường chạy thật**. Phần vận hành hằng ngày (sao lưu,
khôi phục, nâng cấp, xử lý sự cố) nằm ở `TAI-LIEU-QUAN-TRI-VAN-HANH.md`; không lặp lại ở đây.

## 1. Kiến trúc triển khai

```
Internet ──HTTPS──► Nginx (host)
                      ├── /            → container web (Nginx tĩnh, cổng 3000)
                      ├── /api, /hubs  → container api  (cổng 8080)
                      ├── /health      → container api
                      └── /hangfire    → container api (chỉ quản trị hệ thống)

Mạng nội bộ Docker (không mở ra Internet):
  postgres 5432 · redis 6379 · minio 9000 · ai-service 8000 · clamav 3310
```

Nginx ở **host** làm một điểm vào duy nhất nên trình duyệt gọi cùng gốc (`same-origin`) — không cần
CORS và không lộ cổng API ra ngoài. Cấu hình mẫu: `deploy/nginx/blueidea.conf`.

## 2. Yêu cầu tối thiểu

| Thành phần | Tối thiểu | Khuyến nghị |
|---|---|---|
| CPU | 2 vCPU | 4 vCPU |
| RAM | 4 GB | 8 GB (bật MinIO + ClamAV) |
| Ổ đĩa | 40 GB SSD | 100 GB SSD (theo lượng tệp đính kèm) |
| Hệ điều hành | Ubuntu 22.04 LTS | Ubuntu 22.04/24.04 LTS |
| Phần mềm | Docker Engine 24+, Docker Compose v2 | + Nginx, certbot |

> Máy 1 vCPU / 2 GB chạy được bản tối giản (tắt MinIO, ClamAV, Seq) nhưng không nên dùng cho
> môi trường có nhiều người dùng đồng thời.

## 3. Biến môi trường

Tạo tệp `.env` cạnh `deploy/docker-compose.yml`. **Không commit tệp này vào git.**

| Biến | Bắt buộc | Ý nghĩa |
|---|---|---|
| `POSTGRES_PASSWORD` | ✔ | Mật khẩu tài khoản CSDL |
| `REDIS_PASSWORD` | ✔ | Mật khẩu Redis |
| `JWT_KHOA_KY` | ✔ | Khoá ký JWT, tối thiểu 32 ký tự ngẫu nhiên |
| `MA_HOA_KHOA` | ✔ | Khoá AES-256-GCM mã hoá dữ liệu nhạy cảm (CCCD, bí mật tích hợp) |
| `TEN_MIEN` | ✔ | Tên miền dùng cho HTTPS |
| `ASPNETCORE_ENVIRONMENT` | | `Production` khi chạy thật |
| `NAP_DU_LIEU_MAU` | | `false` ở môi trường thật để không nạp dữ liệu demo |
| `POSTGRES_PORT`, `API_PORT`, `WEB_PORT`, `REDIS_PORT`, `AI_PORT`, `MINIO_PORT`, `SEQ_PORT` | | Đổi khi cổng mặc định đã bị chiếm |
| `SSO_ISSUER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET`, `SSO_SCOPE` | | Đăng nhập một lần với hệ thống thành phố (chức năng 21, 41) |
| `KYSO_PFX`, `KYSO_MAT_KHAU_PFX` | | Chứng thư số của máy chủ (chức năng 49) |
| `MINIO_USER`, `MINIO_PASSWORD` | | Khi bật lưu trữ MinIO |
| `QUET_VIRUS` | | `true` để bật quét ClamAV trước khi ghi tệp |
| `GIOI_HAN_REQUEST`, `GIOI_HAN_DANG_NHAP` | | Giới hạn tần suất (mặc định 100 req/phút, 5 lần đăng nhập/phút) |
| `SO_WORKER_NEN` | | Số luồng xử lý công việc nền |
| `THU_MUC_SAO_LUU` | | Thư mục lưu bản sao lưu trên máy chủ |
| `TAG` | | Nhãn Docker image cần chạy (mặc định `latest`) |

Sinh khoá ngẫu nhiên:

```bash
openssl rand -base64 48   # JWT_KHOA_KY
openssl rand -base64 32   # MA_HOA_KHOA
```

## 4. Triển khai lần đầu

```bash
git clone <repo> /opt/blueidea && cd /opt/blueidea
cp deploy/.env.mau .env && vi .env          # điền các biến ở Mục 3

docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml up -d
docker compose -f deploy/docker-compose.yml ps      # tất cả phải "healthy"

curl -s http://127.0.0.1:8080/health                # trả Healthy
```

API tự chạy migration khi khởi động. Nếu `NAP_DU_LIEU_MAU=true`, hệ thống nạp sẵn danh mục, quy
trình mẫu 6 bước, bộ tiêu chí 100 điểm và tài khoản demo (mật khẩu `Sk@2026`, buộc đổi lần đầu).

Sau đó cấu hình Nginx + HTTPS trên host:

```bash
sudo bash deploy/cai-nginx-blueidea.sh          # chép cấu hình mẫu và xin chứng chỉ Let's Encrypt
```

### Việc bắt buộc làm ngay sau khi cài

1. Đổi mật khẩu tài khoản `admin`.
2. Xoá hoặc khoá toàn bộ tài khoản demo nếu đã nạp dữ liệu mẫu.
3. Khai đơn vị, người dùng thật và ma trận phân quyền.
4. Khai quy trình xử lý và bộ tiêu chí của đơn vị, rồi **kích hoạt**.
5. Đặt lịch sao lưu (xem `TAI-LIEU-QUAN-TRI-VAN-HANH.md` mục 4).

## 5. Nâng cấp

```bash
# Cách 1 — qua CI/CD (khuyến nghị): đẩy nhãn phiên bản, GitHub Actions gọi script triển khai
#          .github/workflows/cd.yml → deploy/blueidea-deploy.sh trên máy chủ

# Cách 2 — thủ công
cd /opt/blueidea && git fetch && git checkout <tag>
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml pull
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml up -d
```

**Luôn sao lưu CSDL trước khi nâng cấp** — migration chạy tự động khi API khởi động và không tự
lùi lại được.

> Image phải build lại sau mỗi lần đổi mã nguồn. Container cũ vẫn chạy mã cũ dù kho mã đã cập
> nhật — đây là nguồn nhầm lẫn thường gặp nhất khi kiểm chứng trước nghiệm thu.

## 6. Kiểm chứng sau triển khai

| Kiểm tra | Cách làm | Kết quả mong đợi |
|---|---|---|
| Dịch vụ sống | `curl https://<tên-miền>/health` | `Healthy` |
| Đăng nhập | Mở `https://<tên-miền>/dang-nhap` | Vào được trang chủ |
| Phân quyền máy chủ | Đăng nhập tài khoản tác giả, gọi `GET /api/v1/quy-trinh` | HTTP 403 |
| Tệp tin | Tải lên một PDF trong wizard nộp hồ sơ | Lên được, xem trước được |
| Công việc nền | Mở `/hangfire` bằng tài khoản quản trị | Thấy 4 công việc định kỳ |
| Bảo mật | `curl -I https://<tên-miền>` | Có `Strict-Transport-Security`, `X-Frame-Options`, `Content-Security-Policy` |
| Cổng nội bộ | `ss -ltnp` trên máy chủ | PostgreSQL/Redis/MinIO chỉ nghe `127.0.0.1` |

Chi tiết từng chức năng: chạy theo `KICH-BAN-NGHIEM-THU.md`.

## 7. Môi trường phát triển cục bộ

```bash
docker compose -f deploy/docker-compose.yml up -d postgres redis ai-service
dotnet run --project src/BlueIdea.Api        # http://localhost:8080
cd web && npm install && npm run dev         # http://localhost:5173, proxy /api → :8080
```

Chạy kiểm thử:

```bash
dotnet test tests/BlueIdea.UnitTests           # quy tắc nghiệp vụ
dotnet test tests/BlueIdea.IntegrationTests    # PostgreSQL thật qua Testcontainers
cd tests/BlueIdea.E2eTests && npm install && npx playwright test
```

> Bộ E2E mặc định chờ API ở cổng 8080 và web ở cổng 5173. Máy đang chạy dịch vụ khác trên hai cổng
> này thì đặt `API_PORT`, `WEB_PORT`, `PG_PORT` trước khi chạy — nếu không Playwright sẽ "dùng lại"
> nhầm dịch vụ của dự án khác và báo lỗi không liên quan tới mã nguồn.
