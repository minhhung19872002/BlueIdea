# Tài liệu quản trị và vận hành

Dành cho quản trị viên hệ thống và cán bộ vận hành.

---

## 1. Yêu cầu hạ tầng

### Tối thiểu (đến 500 người dùng, ~10.000 hồ sơ/năm)

| Thành phần | Cấu hình |
|---|---|
| CPU | 4 nhân |
| RAM | 8 GB |
| Ổ đĩa | 100 GB SSD (dữ liệu + tệp đính kèm) |
| Hệ điều hành | Ubuntu 22.04 LTS hoặc tương đương, có Docker Engine 24+ |

### Khuyến nghị (đủ đáp ứng 500 người dùng đồng thời)

| Thành phần | Cấu hình |
|---|---|
| CPU | 8 nhân |
| RAM | 16 GB |
| Ổ đĩa | 500 GB SSD, tách riêng volume cho PostgreSQL và tệp đính kèm |

Dịch vụ OCR nên chạy trên máy riêng nếu khối lượng tệp scan lớn.

---

## 2. Cài đặt lần đầu

```bash
git clone https://github.com/minhhung19872002/BlueIdea.git
cd BlueIdea

cp .env.example .env
```

**Sinh khóa bảo mật thật** rồi điền vào `.env`:

```bash
openssl rand -base64 48   # -> JWT_KHOA_KY
openssl rand -base64 32   # -> MA_HOA_KHOA (phải đúng 32 byte)
openssl rand -base64 24   # -> POSTGRES_PASSWORD, REDIS_PASSWORD, MINIO_PASSWORD
```

> **Cảnh báo:** `MA_HOA_KHOA` dùng để mã hóa số CCCD và secret tích hợp.
> Nếu mất khóa này, **không giải mã lại được** dữ liệu đã lưu. Hãy sao lưu khóa vào két bảo mật
> của đơn vị trước khi đưa hệ thống vào vận hành.

```bash
docker compose -f deploy/docker-compose.yml up -d
docker compose -f deploy/docker-compose.yml ps    # kiểm tra tất cả healthy
curl http://localhost:8080/health                  # phải trả Healthy
```

Lần khởi động đầu tiên tự chạy migration và nạp dữ liệu mẫu.

### Bật đăng nhập một lần (SSO) — tuỳ chọn

Để trống thì hệ thống chỉ dùng đăng nhập nội bộ và trang đăng nhập tự ẩn nút SSO. Muốn bật, điền
vào `.env`:

```env
SSO_ISSUER=https://sso.thanhpho.gov.vn/realms/canbo
SSO_CLIENT_ID=blueidea
SSO_CLIENT_SECRET=<bi-mat-do-ben-SSO-cap>
SSO_SCOPE=openid profile email
```

rồi khởi động lại API:

```bash
docker compose -f deploy/docker-compose.yml up -d --force-recreate api
curl http://localhost:8080/api/v1/xac-thuc/sso/trang-thai   # phải trả daCauHinh: true
```

Đăng ký với bên cung cấp SSO **redirect URI** đúng dạng `<địa-chỉ-web>/dang-nhap/sso`
(ví dụ `https://blueidea.thanhpho.gov.vn/dang-nhap/sso`). Sai địa chỉ này thì nhà cung cấp từ
chối ngay ở bước chuyển hướng.

Hệ thống đọc `openid-configuration` của nhà cung cấp nên không phải khai báo từng endpoint. Nếu
nhà cung cấp có công bố `end_session_endpoint` thì nút Đăng xuất sẽ kết thúc luôn phiên bên đó
(single logout); không có thì chỉ đăng xuất cục bộ.

### Nạp chứng thư số để ký văn bản — tuỳ chọn

Ký số cần **hai** phần: chứng thư trên máy chủ và một bản ghi cấu hình trong hệ thống.

1. Đặt tệp PFX vào máy chủ rồi gắn vào container `api` (thư mục `/app/du-lieu` đã có volume sẵn),
   sau đó khai báo trong `.env`:

```env
KYSO_PFX=/app/du-lieu/chung-thu.pfx
KYSO_MAT_KHAU_PFX=<mat-khau-tep-pfx>
```

```bash
docker compose -f deploy/docker-compose.yml up -d --force-recreate api
```

2. Đăng nhập bằng tài khoản quản trị, vào **Quản trị → Chữ ký số**, thêm một cấu hình (nhà cung
   cấp, hình thức ký, thuật toán) và đặt làm mặc định.

Thẻ trạng thái đầu màn hình phải chuyển sang **"Hệ thống sẵn sàng ký số"**; nếu vẫn báo thiếu thì
xem lại mục nào chưa đạt — thẻ ghi rõ thiếu cấu hình hay thiếu chứng thư.

> **Không** lưu khoá ký trong cơ sở dữ liệu. Một lần lộ bản dump cơ sở dữ liệu là lộ luôn quyền ký
> văn bản, nên hệ thống cố tình chỉ đọc khoá từ tệp trên máy chủ.

### Chuyển sang chế độ vận hành thật

Sau khi nghiệm thu xong, sửa `.env`:

```env
ASPNETCORE_ENVIRONMENT=Production
```

và đặt `KhoiTao__NapDuLieuMau=false` trong phần `environment` của dịch vụ `api`, rồi:

```bash
docker compose -f deploy/docker-compose.yml up -d --force-recreate api
```

Xóa dữ liệu mẫu và đổi mật khẩu tài khoản `admin` **trước khi** mở cho người dùng thật.
Xem checklist đầy đủ ở cuối [`AN-TOAN-THONG-TIN.md`](AN-TOAN-THONG-TIN.md).

---

## 3. Vận hành hằng ngày

### Xem log

```bash
docker compose -f deploy/docker-compose.yml logs -f api      # log ứng dụng
docker compose -f deploy/docker-compose.yml logs -f postgres # log CSDL
```

Log tập trung xem tại Seq: `http://localhost:5341`.
Log tệp nằm trong volume `api-logs`, xoay vòng theo ngày, giữ 30 ngày.

### Theo dõi sức khỏe

| Endpoint | Ý nghĩa |
|---|---|
| `GET /health` | Ứng dụng còn sống |
| `GET /health/ready` | Ứng dụng kết nối được CSDL, sẵn sàng nhận request |

Nên cấu hình giám sát gọi `/health/ready` mỗi 30 giây và cảnh báo khi lỗi 3 lần liên tiếp.

### Nhật ký nghiệp vụ

Vào **Quản trị hệ thống → Nhật ký**:

- **Nhật ký hệ thống** — mọi thao tác thay đổi dữ liệu, có giá trị trước/sau.
- **Nhật ký đăng nhập** — phục vụ điều tra khi nghi ngờ tài khoản bị lộ.
- **Nhật ký lỗi** — lỗi ứng dụng chưa được xử lý.
- **Nhật ký đồng bộ** — kết quả đẩy dữ liệu sang hệ thống ngoài.

---

## 4. Sao lưu và khôi phục

### Sao lưu hằng ngày

```bash
#!/bin/bash
# /opt/blueidea/sao-luu.sh — đặt vào cron 01:00 hằng ngày
set -euo pipefail

NGAY=$(date +%Y%m%d)
THU_MUC=/backup/blueidea
mkdir -p "$THU_MUC"

# 1. Cơ sở dữ liệu
docker exec blueidea-postgres pg_dump -U blueidea -Fc blueidea \
  > "$THU_MUC/csdl-$NGAY.dump"

# 2. Tệp đính kèm
docker run --rm -v blueidea_api-data:/du-lieu -v "$THU_MUC":/backup alpine \
  tar czf "/backup/tep-tin-$NGAY.tar.gz" -C /du-lieu .

# 3. Giữ 30 ngày gần nhất
find "$THU_MUC" -name 'csdl-*.dump' -mtime +30 -delete
find "$THU_MUC" -name 'tep-tin-*.tar.gz' -mtime +30 -delete

echo "Sao lưu $NGAY hoàn tất"
```

```cron
0 1 * * * /opt/blueidea/sao-luu.sh >> /var/log/blueidea-sao-luu.log 2>&1
```

### WAL archiving — khôi phục về thời điểm bất kỳ (PITR)

**Đã bật sẵn** trong `deploy/docker-compose.prod.yml`:

```yaml
-c wal_level=replica
-c archive_mode=on
-c archive_timeout=300
-c archive_command='test ! -f /wal-archive/%f && cp %p /wal-archive/%f'
```

WAL được đẩy sang thư mục `${THU_MUC_WAL:-/var/backups/blueidea/wal}` trên máy chủ, chậm nhất
5 phút một lần. Chỉ có bản dump hằng ngày thì điểm khôi phục gần nhất là 1h sáng hôm trước —
mọi hồ sơ nộp trong ngày đều mất; có WAL thì mất mát tối đa tính bằng phút.

Ba việc bắt buộc:

1. **Đưa thư mục WAL vào lịch sao chép ra ngoài máy chủ** cùng với bản dump. WAL nằm cùng máy với
   CSDL thì hỏng ổ đĩa là mất cả hai.
2. **Dọn WAL cũ** sau mỗi lần sao lưu đầy đủ, nếu không thư mục phình vô hạn:
   ```bash
   find /var/backups/blueidea/wal -type f -mtime +30 -delete
   ```
3. **Theo dõi archive_command**: lệnh này thất bại liên tục thì Postgres giữ WAL lại trong
   `pg_wal` cho tới khi đầy ổ đĩa và dừng ghi.
   ```bash
   docker exec blueidea-postgres psql -U blueidea -c      "SELECT archived_count, failed_count, last_failed_time FROM pg_stat_archiver;"
   ```

Với cấu hình này: **RPO ≤ 1 giờ, RTO ≤ 4 giờ** như cam kết trong đặc tả.

### Khôi phục về một thời điểm (dùng WAL)

Dùng khi cần quay lại **ngay trước** một sự cố (xoá nhầm dữ liệu, nâng cấp hỏng) chứ không phải
về mốc 1h sáng.

```bash
# 1. Dừng toàn bộ dịch vụ
docker compose -f deploy/docker-compose.yml down

# 2. Phục hồi bản base backup gần nhất vào thư mục dữ liệu rỗng
#    (bản dump logic pg_dump KHÔNG dùng được cho PITR — phải là base backup vật lý)
docker run --rm -v blueidea_postgres-data:/du-lieu -v /var/backups/blueidea:/backup alpine   sh -c "rm -rf /du-lieu/* && tar xzf /backup/base-<ngày>.tar.gz -C /du-lieu"

# 3. Khai điểm dừng mong muốn
docker run --rm -v blueidea_postgres-data:/du-lieu alpine sh -c "cat >> /du-lieu/postgresql.auto.conf <<'EOF'
restore_command = 'cp /wal-archive/%f %p'
recovery_target_time = '2026-08-19 14:30:00+07'
recovery_target_action = 'promote'
EOF
touch /du-lieu/recovery.signal"

# 4. Khởi động lại và theo dõi log tới khi thấy 'database system is ready to accept connections'
docker compose -f deploy/docker-compose.yml -f deploy/docker-compose.prod.yml up -d postgres
docker logs -f blueidea-postgres

# 5. Kiểm tra dữ liệu rồi mới bật API
docker compose -f deploy/docker-compose.yml start api
```

Base backup vật lý tạo bằng:

```bash
docker exec blueidea-postgres pg_basebackup -U blueidea -D /tmp/base -Ft -z -Xs -P
```

### Khôi phục

```bash
# 1. Dừng API để không có ghi mới
docker compose -f deploy/docker-compose.yml stop api

# 2. Tạo lại CSDL rỗng
docker exec blueidea-postgres psql -U blueidea -d postgres \
  -c "DROP DATABASE IF EXISTS blueidea;" -c "CREATE DATABASE blueidea;"

# 3. Phục hồi
docker exec -i blueidea-postgres pg_restore -U blueidea -d blueidea --no-owner \
  < /backup/blueidea/csdl-20260817.dump

# 4. Phục hồi tệp đính kèm
docker run --rm -v blueidea_api-data:/du-lieu -v /backup/blueidea:/backup alpine \
  tar xzf /backup/tep-tin-20260817.tar.gz -C /du-lieu

# 5. Khởi động lại
docker compose -f deploy/docker-compose.yml start api
curl http://localhost:8080/health/ready
```

> **Bắt buộc:** thực hiện diễn tập khôi phục trên môi trường thử ít nhất **6 tháng một lần**
> và ghi biên bản. Bản sao lưu chưa từng được khôi phục thử thì chưa thể coi là bản sao lưu.

---

## 5. Nâng cấp phiên bản

```bash
cd /opt/blueidea
./sao-luu.sh                    # LUÔN sao lưu trước khi nâng cấp

git pull
docker compose -f deploy/docker-compose.yml build api web
docker compose -f deploy/docker-compose.yml up -d api web

docker compose -f deploy/docker-compose.yml logs -f api | head -50
curl http://localhost:8080/health/ready
```

Migration cơ sở dữ liệu chạy tự động khi API khởi động. Nếu migration lỗi, API sẽ **không khởi
động** và ghi rõ lỗi vào log — đây là hành vi có chủ đích để tránh chạy trên schema sai.

---

## 6. Xử lý sự cố thường gặp

| Hiện tượng | Nguyên nhân thường gặp | Cách xử lý |
|---|---|---|
| API không khởi động, log báo lỗi migration | Schema không khớp phiên bản mã nguồn | Khôi phục bản sao lưu gần nhất, kiểm tra lại phiên bản triển khai |
| `Jwt:KhoaKy phải có ít nhất 32 ký tự` | Chưa đặt `JWT_KHOA_KY` trong `.env` | Sinh khóa mới và khởi động lại API |
| `MaHoa:KhoaBase64 phải là khóa AES-256 (32 byte)` | Khóa không đúng 32 byte sau khi giải base64 | Sinh lại bằng `openssl rand -base64 32` |
| Người dùng đăng nhập báo "tài khoản đang bị khóa" | Nhập sai quá 5 lần | Quản trị viên vào Quản lý người dùng bấm **Mở khóa** (xóa luôn bộ đếm sai) |
| Không tải được tệp lên, báo "nội dung tệp không khớp định dạng" | Tệp bị đổi phần mở rộng, hoặc định dạng không nằm trong danh sách cho phép | Kiểm tra tệp gốc; nếu là định dạng hợp lệ cần bổ sung thì thêm vào cấu hình thành phần hồ sơ |
| Kiểm tra trùng lặp báo lỗi | Nội dung hồ sơ rỗng hoặc dịch vụ OCR không phản hồi | Hồ sơ vẫn xử lý bình thường; bấm **Chạy lại kiểm tra** sau khi khắc phục |
| Cổng 5432 không bind được | Máy chủ đã có PostgreSQL cài sẵn | Đặt `POSTGRES_PORT` khác trong `.env` |
| Trang chủ hiển thị số liệu 0 | Người dùng không có quyền xem toàn hệ thống | Kiểm tra phạm vi dữ liệu của vai trò trong Quản trị → Vai trò |

### Kiểm tra nhanh khi hệ thống chậm

```bash
# Truy vấn đang chạy lâu
docker exec blueidea-postgres psql -U blueidea -d blueidea -c \
  "SELECT pid, now()-query_start AS thoi_gian, left(query,100)
   FROM pg_stat_activity WHERE state='active' ORDER BY thoi_gian DESC LIMIT 10;"

# Kích thước các bảng lớn nhất
docker exec blueidea-postgres psql -U blueidea -d blueidea -c \
  "SELECT relname, pg_size_pretty(pg_total_relation_size(relid)) AS kich_thuoc
   FROM pg_catalog.pg_statio_user_tables
   ORDER BY pg_total_relation_size(relid) DESC LIMIT 10;"
```

Log ứng dụng tự động ghi cảnh báo cho mọi yêu cầu xử lý quá 500 ms — tìm từ khóa
`xử lý chậm` trong log để khoanh vùng.

---

## 7. Công việc định kỳ

| Tần suất | Công việc |
|---|---|
| Hằng ngày | Kiểm tra log lỗi; xác nhận sao lưu đêm thành công |
| Hằng tuần | Rà soát nhật ký đăng nhập thất bại bất thường |
| Hằng tháng | Rà soát tài khoản không còn sử dụng; kiểm tra dung lượng ổ đĩa |
| Mỗi 3 tháng | Cập nhật bản vá hệ điều hành và ảnh Docker |
| Mỗi 6 tháng | **Diễn tập khôi phục dữ liệu**; rà soát lại ma trận phân quyền |
| Hằng năm | Rà soát và cập nhật hồ sơ cấp độ an toàn thông tin |
