# Trạng thái triển khai 51 chức năng

Bảng này ghi **đúng thực trạng** từng chức năng để phục vụ nghiệm thu và lập kế hoạch phần còn lại.
Không tô hồng: chức năng nào mới có API mà chưa có giao diện, hoặc mới có khung mà chưa hoàn
thiện, đều được ghi rõ.

**Chú giải mức độ**

| Ký hiệu | Ý nghĩa |
|---|---|
| ✅ | Hoàn chỉnh: API + nghiệp vụ + giao diện + đã kiểm chứng chạy thật |
| 🟡 | API và nghiệp vụ đã xong, giao diện ở mức cơ bản hoặc chỉ đọc |
| 🟠 | Có mô hình dữ liệu và khung API, nghiệp vụ chưa đầy đủ |
| ⬜ | Chưa triển khai trong đợt này |

---

## Nhóm I — Danh mục hệ thống

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 1 | Lĩnh vực | ✅ | CRUD, cây phân cấp, tìm không dấu, chặn xóa khi đang tham chiếu, xuất Excel |
| 2 | Đối tượng | ✅ | CRUD đầy đủ |
| 3 | Đợt đề nghị | ✅ | CRUD + Mở/Đóng/Khóa đợt + Sao chép đợt; chặn nộp khi hết hạn hoặc đã đóng |
| 4 | Loại tác giả | ✅ | Kèm ràng buộc số tác giả tối đa, áp dụng khi nộp hồ sơ |
| 5 | Đơn vị phê duyệt | ✅ | Cây tổ chức, đường dẫn cây phục vụ phạm vi dữ liệu |
| 6 | Biểu mẫu xuất | 🟡 | Mô hình + seed 5 mẫu + API danh mục. Chưa có màn hình quét placeholder từ `.docx` |
| 7 | Biểu mẫu thống kê | 🟠 | Mô hình dữ liệu và API danh mục; báo cáo tùy biến chưa sinh động từ cấu hình |
| 8 | Quyết định | 🟡 | Mô hình + quan hệ N-N với sáng kiến; chưa có màn hình ban hành quyết định |

## Nhóm II — Quy trình động

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 9 | Cấu hình quy trình | ✅ | CRUD, sao chép, tạo phiên bản mới, kích hoạt, ngừng áp dụng |
| 10 | Cấu hình trường hợp | ✅ | Nhánh rẽ có điều kiện jsonb; rule evaluator đầy đủ toán tử; xem trên designer |
| 11 | Cấu hình bước xử lý | ✅ | Panel cấu hình bước trên trình thiết kế ReactFlow |
| 12 | Chức năng bổ sung | 🟡 | Mô hình + seed + hiển thị; bật/tắt trên giao diện chưa có |
| 13 | Thành phần hồ sơ | ✅ | Cấu hình + checklist kiểm tra khi nộp, chặn nộp khi thiếu |
| 14 | Trạng thái bước | ✅ | Trạng thái theo bước và trạng thái toàn cục |
| 15 | Tác nhân xử lý | ✅ | 7 loại tác nhân, quy tắc MỘT_NGƯỜI / TẤT_CẢ / ĐA_SỐ đã kiểm chứng |
| 16 | Cấu hình liên thông | 🟠 | Mô hình dữ liệu và điểm cắm adapter; chưa nối hệ thống ngoài thật |

## Nhóm III — Tiêu chí động

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 17 | Nhóm tiêu chí | ✅ | Cây 2 cấp, kiểm tra tổng trọng số và tổng điểm realtime |
| 18 | Cấu hình tiêu chí | ✅ | 4 kiểu nhập; mức công nhận theo khoảng điểm, kiểm tra chồng lấn |

## Nhóm IV — Hội đồng

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 19 | Danh sách hội đồng | ✅ | CRUD + API phiên họp, điểm danh, bỏ phiếu, kết luận |
| 20 | Thành viên hội đồng | ✅ | Quyền theo chức danh; ràng buộc đúng 1 chủ tịch; giao diện quản lý thành viên ở mức đọc |

## Nhóm V — Đăng ký nộp hồ sơ

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 21 | Đăng nhập | ✅ | Argon2id, JWT + refresh xoay vòng, khóa tài khoản, buộc đổi mật khẩu lần đầu. SSO OIDC: ⬜ |
| 22 | Đăng ký nộp sáng kiến | ✅ | Wizard 6 bước, tự lưu nháp 30 giây, kiểm tra tỷ lệ đóng góp 100% |
| 23 | Quản lý hồ sơ sáng kiến | ✅ | Danh sách, sửa, rút, tab lịch sử chỉnh sửa có diff trước/sau |
| 24 | Thành phần hồ sơ | ✅ | Checklist trực quan ✓/✗/⚠, chặn nộp và nêu rõ mục còn thiếu |
| 25 | Tệp tin đính kèm | ✅ | Kiểm tra magic number, chặn tệp thực thi, SHA-256. Quét ClamAV: ⬜ |
| 26 | Kiểm tra trùng lặp | ✅ | Pipeline đầy đủ, giao diện đối chiếu 2 cột highlight. OCR: dịch vụ đã đóng gói, chưa nối vào luồng nộp |

## Nhóm VI — Tiếp nhận và xử lý

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 27 | Tiếp nhận hồ sơ | ✅ | Nút hành động sinh động theo quy trình |
| 28 | Danh sách hồ sơ | ✅ | Bộ lọc đa tiêu chí, lưu trong URL, chọn nhiều, xuất Excel |
| 29 | Xử lý hồ sơ | ✅ | Thực thi bước, xử lý hàng loạt, thu hồi, Idempotency-Key |
| 30 | Theo dõi hồ sơ | ✅ | Timeline đầy đủ, badge quá hạn. Job nhắc hạn tự động: ⬜ |
| 31/36 | Đính kèm quyết định | 🟠 | Mô hình dữ liệu xong; chưa có luồng tạo và gắn quyết định trên giao diện |
| 32 | Kết quả sáng kiến | 🟡 | Tổng hợp và lưu kết quả xét duyệt xong; công bố công khai hàng loạt chưa có |

## Nhóm VII — Đánh giá

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 33 | Danh sách hồ sơ đánh giá | ✅ | "Việc của tôi", đếm ngược hạn, phân công loại trừ xung đột lợi ích |
| 34 | Đánh giá hồ sơ | ✅ | Giao diện 2 panel, phiếu chấm render động, tính điểm realtime |
| 35 | Phiếu đánh giá | 🟡 | Lưu/gửi/mở lại phiếu, ma trận điểm qua API. Xuất PDF phiếu hàng loạt: ⬜ |

## Nhóm IX–X — Tra cứu, báo cáo

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 37 | Tra cứu, tìm kiếm | ✅ | Tìm không dấu, tìm nâng cao, chia sẻ link truy vấn, trang công khai. Tìm ngữ nghĩa: ⬜ |
| 38 | DS sáng kiến đạt | ✅ | Bảng + xuất Excel + xuất PDF mẫu văn bản hành chính |
| 39 | DS sáng kiến chưa đạt | ✅ | Kèm lý do và điểm đánh giá |
| 40 | DS theo đơn vị | ✅ | Kèm tỷ lệ đạt, dòng tổng cộng |
| — | Dashboard | ✅ | 4 chỉ số + 3 biểu đồ ECharts + top đơn vị + cảnh báo trùng lặp |

## Nhóm XI–XIII — Tích hợp, di động, quản trị

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 41 | Tích hợp SSO/IOC/TĐKT | 🟠 | Mô hình `he_thong_tich_hop`, `nhat_ky_dong_bo` và điểm cắm adapter; chưa nối hệ thống thật |
| 42 | Ứng dụng di động | ⬜ | Chưa triển khai. Web đã responsive từ 320px nên dùng được trên điện thoại |
| 43 | Quản lý người dùng | 🟡 | Danh sách, tìm kiếm, khóa/mở khóa. Thêm/sửa và import Excel: ⬜ |
| 44 | Quản lý đơn vị | 🟡 | API CRUD đầy đủ + cây tổ chức; giao diện hiện ở mức xem cây |
| 45 | Quản lý vai trò | 🟡 | Ma trận phân quyền hiển thị đầy đủ ở chế độ chỉ đọc |
| 46 | Cấu hình hệ thống | ✅ | Đọc/ghi theo nhóm, có kiểu dữ liệu, màu chủ đạo áp dụng ngay lên giao diện |
| 47 | Cấu hình đơn vị | 🟡 | Trường dữ liệu (logo, tiêu đề văn bản, người ký mặc định) đã có; giao diện chưa có |
| 48 | Cấu hình menu | ✅ | Menu render động từ CSDL và lọc theo quyền; sửa menu bằng API |
| 49 | Cấu hình chữ ký số | 🟠 | Mô hình `cau_hinh_chu_ky_so`, `nhat_ky_ky_so`; chưa tích hợp CA thật |
| 50 | Cấu hình email & SMS | 🟡 | Mô hình + mẫu thông báo + hàng đợi gửi; worker gửi thật: ⬜ |
| 51 | Cấu hình thông tin sáng kiến | ✅ | Ngưỡng trùng lặp, hệ số tính điểm, giới hạn tệp — sửa được trên giao diện |

---

## Tổng hợp

| Mức | Số chức năng |
|---|---|
| ✅ Hoàn chỉnh | 31 |
| 🟡 API xong, giao diện cơ bản | 12 |
| 🟠 Có khung, nghiệp vụ chưa đầy đủ | 6 |
| ⬜ Chưa triển khai | 2 |

## Việc còn lại theo thứ tự ưu tiên

1. **Hangfire job nền** — nhắc hạn xử lý, tự động đóng đợt hết hạn, chạy kiểm tra trùng lặp
   sau khi nộp, gửi email/SMS từ hàng đợi. Hạ tầng (bảng hàng đợi, mẫu thông báo) đã sẵn sàng.
2. **Luồng quyết định công nhận** (chức năng 31/36) và công bố kết quả hàng loạt (32).
3. **Nối OCR vào luồng nộp hồ sơ** — dịch vụ đã đóng gói, cần gọi khi có tệp PDF scan.
4. **Hoàn thiện giao diện quản trị**: thêm/sửa người dùng, sửa cây đơn vị, sửa ma trận phân quyền.
5. **Tích hợp thật**: SSO OIDC, đẩy dữ liệu sang Thi đua khen thưởng và IOC.
6. **Chữ ký số** với nhà cung cấp CA cụ thể của đơn vị.
7. **Ứng dụng di động** React Native dùng chung hợp đồng API hiện có.
