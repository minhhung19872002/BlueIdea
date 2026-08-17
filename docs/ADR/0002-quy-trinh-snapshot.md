# ADR 0002 — Hồ sơ chạy theo snapshot quy trình, không theo quy trình hiện hành

- **Trạng thái:** Đã quyết định
- **Ngày:** 2026-08-17
- **Liên quan:** Mục 4.2 và Mục 5 (Nhóm II) của đặc tả

## Bối cảnh

Quy trình xử lý hồ sơ là dữ liệu cấu hình được: quản trị viên có thể thêm bước, đổi tác nhân,
sửa điều kiện chuyển tiếp bất cứ lúc nào. Nhưng một đợt xét sáng kiến kéo dài nhiều tháng,
trong đó luôn có hồ sơ đang xử lý dở dang.

Nếu engine đọc quy trình hiện hành, việc sửa cấu hình sẽ khiến hồ sơ đang chạy rơi vào trạng
thái không xác định: bước hiện tại có thể bị xóa, nhánh chuyển tiếp có thể trỏ tới bước không
còn tồn tại, tác nhân có thể đổi khiến người đang xử lý mất quyền giữa chừng.

Với hệ thống hành chính, hậu quả không chỉ là lỗi kỹ thuật — hồ sơ của công dân bị kẹt và
không giải trình được vì sao.

## Quyết định

**Đóng băng cấu hình quy trình vào hồ sơ tại thời điểm nộp.**

1. Khi hồ sơ được nộp, toàn bộ cấu hình quy trình (bước, tác nhân, trường hợp chuyển tiếp,
   trạng thái, thành phần hồ sơ, chức năng bổ sung) được serialize thành JSON và lưu vào cột
   `sang_kien.quy_trinh_snapshot`.

2. `IBoMayQuyTrinh` **luôn** chạy theo snapshot này, không bao giờ đọc bảng `quy_trinh`.

3. Quy trình đang có hồ sơ xử lý dở dang thì **bị chặn sửa sơ đồ** (HTTP 409, mã lỗi
   `QUY_TRINH_DANG_SU_DUNG`). Quản trị viên phải dùng chức năng **Tạo phiên bản mới**, sinh ra
   một bản ghi `quy_trinh` mới với `phien_ban` tăng dần và `quy_trinh_goc_id` trỏ về bản gốc.

4. Hồ sơ nộp sau đó dùng phiên bản mới; hồ sơ cũ tiếp tục chạy trọn vẹn theo phiên bản cũ.

## Phương án đã cân nhắc và loại bỏ

| Phương án | Lý do loại bỏ |
|---|---|
| Cấm sửa quy trình khi đợt đang mở | Quá cứng nhắc; thực tế luôn cần sửa lỗi cấu hình giữa chừng |
| Cho sửa tự do, migrate hồ sơ sang cấu hình mới | Không có cách ánh xạ đúng khi bước bị xóa; phá vỡ tính giải trình của hồ sơ |
| Chỉ lưu số phiên bản, đọc lại bảng quy trình theo phiên bản | Vẫn phải giữ mọi phiên bản trong bảng; truy vấn phức tạp hơn snapshot mà không lợi gì |

## Hệ quả

**Tích cực**

- Hồ sơ giải trình được tuyệt đối: mở hồ sơ ra là biết chính xác nó chạy theo cấu hình nào.
- Quản trị viên sửa quy trình mà không sợ làm hỏng hồ sơ đang chạy.
- Engine đơn giản hơn: không cần xử lý trường hợp bước biến mất giữa chừng.
- Kiểm thử dễ hơn: snapshot là dữ liệu thuần, nạp thẳng vào engine để test.

**Tiêu cực và cách giảm thiểu**

- Snapshot làm tăng dung lượng bảng `sang_kien` (khoảng 20–40 KB mỗi hồ sơ).
  *Giảm thiểu:* cột dùng `jsonb` được TOAST nén tự động; với 10.000 hồ sơ/năm thì phần tăng
  thêm không đáng kể so với nội dung sáng kiến và tệp đính kèm.
- Sửa lỗi cấu hình không tự động áp dụng cho hồ sơ đang chạy.
  *Giảm thiểu:* đây là hành vi có chủ đích. Trường hợp thật sự cần can thiệp, quản trị viên
  dùng chức năng thu hồi bước hoặc xử lý hành chính, và mọi thao tác đều vào nhật ký hệ thống.

## Kiểm chứng

`BoMayQuyTrinhTests` có hai bài kiểm thử trực tiếp cho quyết định này:

- `Snapshot_Giu_Nguyen_Cau_Hinh_Quy_Trinh` — serialize rồi khôi phục giữ nguyên toàn bộ bước,
  trường hợp và tác nhân.
- `Snapshot_Van_Chay_Duoc_Engine_Sau_Khi_Khoi_Phuc` — engine chạy trên snapshot đã khôi phục
  cho ra đúng danh sách hành động khả dụng như trên quy trình gốc.
