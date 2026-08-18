# ADR 0003 — Cấu hình liên thông đọc dữ liệu sống, không qua snapshot

- **Trạng thái:** Đã quyết định
- **Ngày:** 2026-08-18
- **Liên quan:** ADR 0002 (snapshot quy trình), Mục 3.1 Chức năng 16

## Bối cảnh

ADR 0002 quy định rằng hồ sơ chạy theo snapshot quy trình được đóng băng lúc nộp. Nguyên tắc
này bảo đảm tính giải trình: bước, tác nhân, điều kiện chuyển tiếp của hồ sơ không bao giờ thay
đổi sau khi nộp.

Tuy nhiên, cấu hình liên thông (`quy_trinh_lien_thong`) chứa thông tin vận hành: URL endpoint,
API key, loại xác thực, hệ thống đích. Đây là thông tin có thể thay đổi bất cứ lúc nào khi hệ
thống ngoài đổi endpoint hoặc xoay khóa API.

Nếu liên thông đọc từ snapshot, hồ sơ nộp trước khi đổi endpoint sẽ gửi về URL cũ (đã chết),
không có cách sửa ngoài can thiệp trực tiếp vào dữ liệu hồ sơ.

## Quyết định

**Cấu hình liên thông (`QuyTrinhLienThong`) đọc từ cơ sở dữ liệu sống, không từ snapshot.**

Cụ thể:
1. `ThucThiBuocCommandHandler` và `ThucThiHangLoatCommandHandler` truy vấn bảng `quy_trinh_lien_thong`
   trực tiếp theo `QuyTrinhId` tại thời điểm chuyển bước.
2. Trường `QuyTrinhId` lấy từ snapshot (vì snapshot lưu ID quy trình gốc), nhưng bản ghi
   liên thông được đọc live.
3. Sự kiện liên thông (`KHI_HOAN_THANH`, `KHI_VAO_BUOC`, `KHI_PHE_DUYET`) và bước đích
   (`BuocId`) khớp với snapshot — chỉ endpoint, xác thực, và ánh xạ dữ liệu là live.

## Lý do phân biệt với ADR 0002

| Thuộc tính | Bước/tác nhân/điều kiện (snapshot) | Liên thông (live) |
|---|---|---|
| Bản chất | Logic nghiệp vụ — quyết định hồ sơ đi đâu | Vận hành — gửi dữ liệu ra hệ thống ngoài |
| Ảnh hưởng khi thay đổi | Hồ sơ rơi vào trạng thái không xác định | Liên thông gửi đúng nơi (hoặc không gửi) |
| Tần suất thay đổi | Hiếm (khi sửa quy trình) | Thường xuyên (xoay khóa API, đổi endpoint) |
| Hậu quả đóng băng | Bảo toàn tính giải trình | Hồ sơ cũ gửi về URL chết |

## Hệ quả

**Tích cực**

- Quản trị viên đổi endpoint hoặc xoay khóa API mà không cần tạo phiên bản quy trình mới.
- Hồ sơ đang xử lý tự động dùng cấu hình liên thông mới nhất.
- Liên thông thất bại chỉ ghi log, không chặn workflow — nên rủi ro chuyển đổi thấp.

**Tiêu cực và cách giảm thiểu**

- Không có snapshot liên thông nên không thể biết chính xác hồ sơ X dùng endpoint nào lúc gửi.
  *Giảm thiểu:* nhật ký đồng bộ (`NhatKyDongBo`) ghi lại URL đích, thời gian, kết quả mỗi lần
  gửi — đủ để giải trình.
- Xóa cấu hình liên thông giữa chừng khiến hồ sơ không gửi được.
  *Giảm thiểu:* liên thông thất bại chỉ log warning, không chặn workflow. Sweep định kỳ có thể
  gửi lại khi cấu hình được khôi phục.

## Kiểm chứng

- `DieuPhaiLienThongTests` xác nhận hằng số sự kiện khớp với giá trị mặc định của entity.
- `TichHopTests` xác nhận CRUD và đồng bộ thủ công hoạt động qua API thật.
- `ThucThiBuocCommandHandler` và `ThucThiHangLoatCommandHandler` đều truy vấn `QuyTrinhLienThong`
  từ DB theo `QuyTrinhId`, không từ snapshot.
