# ADR 0001 — Toàn bộ AI chạy nội bộ, không gọi API bên thứ ba

- **Trạng thái:** Đã quyết định
- **Ngày:** 2026-08-17
- **Liên quan:** Mục 3.2 E-HSMT; Mục 0 và Mục 5 (chức năng 26) của đặc tả

## Bối cảnh

Hệ thống phải phát hiện trùng lặp/đạo văn giữa hồ sơ sáng kiến mới và kho sáng kiến các năm
trước, đồng thời trích xuất văn bản từ tệp đính kèm (kể cả PDF scan).

E-HSMT quy định rõ: toàn bộ mô hình, dữ liệu huấn luyện và quá trình suy luận phải chạy trên
hạ tầng nội bộ của đơn vị; dữ liệu phải được phân quyền nhiều lớp, ghi log truy cập, mã hóa
khi lưu trữ và truyền tải.

## Quyết định

**Không sử dụng bất kỳ dịch vụ AI bên thứ ba nào** (OpenAI, Gemini, Claude API, Azure AI,
AWS Bedrock…). Cụ thể:

### 1. OCR — dịch vụ container riêng

`ai-service/` là dịch vụ FastAPI đóng gói **Tesseract 5** với traineddata `vie` + `eng`.
Dịch vụ chỉ lắng nghe trong mạng nội bộ của `docker-compose` (`127.0.0.1:8088`), không expose
ra Internet. Với PDF, dịch vụ ưu tiên đọc text-layer và chỉ OCR những trang thật sự là ảnh scan.

### 2. So khớp trùng lặp — thuần .NET, chạy trong tiến trình API

Không gọi mạng ra ngoài ở bất kỳ bước nào:

| Tầng | Thuật toán | Mục đích |
|---|---|---|
| Lọc thô | SimHash 64-bit + khoảng cách Hamming | Loại nhanh phần lớn ứng viên |
| Lọc thô | MinHash + LSH banding trên shingle 5-gram | Bắt cặp gần giống mà SimHash bỏ sót |
| So khớp tinh (từ vựng) | TF-IDF cosine + Jaccard | Phát hiện sao chép nguyên văn |
| So khớp tinh (ngữ nghĩa) | Cosine trên vector nhúng | Phát hiện diễn đạt lại |

Điểm tổng hợp: `tỷ_lệ = hệ_số_từ_vựng × từ_vựng + hệ_số_ngữ_nghĩa × ngữ_nghĩa`,
mặc định `0.4 / 0.6`, sửa được trong `cau_hinh_he_thong` không cần build lại.

### 3. Vector nhúng — mô hình cục bộ, có phương án dự phòng

`IBoNhungVanBan` là điểm cắm cho mô hình embedding tiếng Việt chạy bằng **ONNX Runtime**
trên máy chủ của đơn vị (khuyến nghị `dangvantuan/vietnamese-embedding` hoặc
`intfloat/multilingual-e5-base` export sang ONNX).

Bản cài đặt mặc định đi kèm là `BoNhungBamTuVung` — bộ nhúng 768 chiều dùng kỹ thuật
*hashing trick* trên từ đơn và bi-gram, chuẩn hóa L2. Đây **không phải mô hình học sâu**, mà là
phương án bảo đảm hệ thống vẫn phát hiện được trùng lặp khi đơn vị chưa nạp mô hình ONNX
(yêu cầu *graceful degradation*, Mục 7 đặc tả). Bộ nhúng này:

- Tất định tuyệt đối giữa các lần chạy và giữa các máy (dùng FNV-1a, không dùng
  `string.GetHashCode()` vốn có randomized hashing trong .NET).
- Không cần tải bất cứ tài nguyên nào từ Internet.

Tên mô hình đang dùng được ghi vào kết quả mỗi lần kiểm tra (`tenMoHinhNhung`) để truy vết
về sau.

**Cách nạp mô hình ONNX (bổ sung 20/08/2026).** Bản cài đặt `BoNhungOnnx` đã có sẵn trong
`BlueIdea.Ai/Nhung/`, chạy bằng ONNX Runtime ngay trong tiến trình API. Vận hành chỉ cần đặt
hai tệp lên máy chủ rồi khai trong `appsettings` / biến môi trường:

```jsonc
"Ai": {
  "Nhung": {
    "DuongDanMoHinh": "/var/lib/blueidea/mo-hinh/vietnamese-sbert.onnx",
    "DuongDanTuVung": "/var/lib/blueidea/mo-hinh/vocab.txt",
    "TenMoHinh": "vietnamese-sbert",   // ghi vào kết quả kiểm tra để truy vết
    "SoTokenToiDa": 256,
    "HaThapChu": true
  }
}
```

Ba điều hệ thống tự lo, không phải sửa mã:

1. **Sai số chiều thì từ chối nạp.** Cột `embedding` là `vector(768)`; mô hình sinh số chiều khác
   sẽ làm API dừng ngay lúc khởi động kèm thông báo rõ, thay vì ghi hỏng dữ liệu dần dần.
2. **Thiếu tệp thì lùi về bộ băm từ vựng** kèm cảnh báo trong log — mất tìm ngữ nghĩa còn hơn cả
   hệ thống không vào được.
3. **Vector cũ được nhúng lại tự động.** Mỗi đoạn văn ghi kèm tên mô hình đã sinh ra nó
   (`sang_kien_doan_van.mo_hinh_nhung`). Tìm ngữ nghĩa chỉ so với vector của **đúng** mô hình đang
   chạy — cosine giữa hai không gian vector khác nhau là một con số vô nghĩa chứ không phải một
   con số thấp — còn công việc nền `nhung-lai-doan-van` gặm dần kho cũ mỗi 10 phút cho tới khi
   sạch. Vì vậy đổi mô hình chỉ làm tìm ngữ nghĩa *tạm* rỗng, không làm nó trả kết quả sai.

Bộ tách từ là **WordPiece** đọc từ `vocab.txt` của chính mô hình (tự viết, không kéo thư viện —
phần phải khớp tuyệt đối là dữ liệu từ vựng chứ không phải thư viện). Mô hình dùng SentencePiece
hoặc BPE (PhoBERT chẳng hạn) chưa dùng được; chọn mô hình họ BERT có `vocab.txt`.

## Hệ quả

**Tích cực**

- Đáp ứng đúng ràng buộc pháp lý và ràng buộc thầu; dữ liệu sáng kiến không rời khỏi hạ tầng đơn vị.
- Không phát sinh chi phí theo lượt gọi, không phụ thuộc đường truyền Internet.
- Thời gian xử lý ổn định: đo thực tế ~400 ms cho một hồ sơ đối chiếu với 35 hồ sơ khác.
- Kết quả tái lập được — quan trọng khi hội đồng cần giải trình vì sao một hồ sơ bị cảnh báo.

**Tiêu cực và cách giảm thiểu**

- Bộ nhúng dự phòng yếu hơn mô hình học sâu ở khả năng bắt diễn đạt lại hoàn toàn.
  *Giảm thiểu:* thành phần từ vựng (TF-IDF + Jaccard) vẫn bắt tốt sao chép nguyên văn — dạng
  gian lận phổ biến nhất; đơn vị nên nạp mô hình ONNX khi có điều kiện hạ tầng.
- Cần tài nguyên máy chủ cho OCR. *Giảm thiểu:* OCR tách container riêng, có thể scale độc lập
  hoặc tắt khi không dùng.
- Kết quả AI **chỉ mang tính cảnh báo**, không tự động loại hồ sơ. Quyết định cuối cùng luôn
  thuộc về hội đồng, có ghi nhận ý kiến vào `kiem_tra_trung_lap.y_kien_hoi_dong`.

## Kiểm chứng

Dữ liệu mẫu chứa một cặp hồ sơ cố ý trùng nhau (`SK-2025-0001` và `SK-2026-0002`, dùng chung
3/5 đoạn nội dung). Kết quả đo thực tế:

| Cặp đối chiếu | Tổng hợp | Từ vựng | Ngữ nghĩa | Số đoạn trùng |
|---|---|---|---|---|
| Cặp cố ý trùng | **85,7 %** → NGHIÊM TRỌNG | 72,5 % | 94,6 % | 7 |
| Hồ sơ không liên quan (cao nhất) | 31,2 % → AN TOÀN | 16,3 % | 41,2 % | 0 |

Khoảng cách phân tách rõ ràng: cặp trùng thật vượt xa ngưỡng cảnh báo đỏ (40 %) trong khi
hồ sơ không liên quan nằm dưới ngưỡng vàng (20 %).
