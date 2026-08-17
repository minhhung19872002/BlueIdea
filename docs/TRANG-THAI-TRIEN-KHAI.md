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
| 3 | Đợt đề nghị | ✅ | CRUD + Mở/Đóng/Khóa đợt + Sao chép đợt; tự động đóng đợt quá hạn nộp mỗi giờ |
| 4 | Loại tác giả | ✅ | Kèm ràng buộc số tác giả tối đa, áp dụng khi nộp hồ sơ |
| 5 | Đơn vị phê duyệt | ✅ | Cây tổ chức, đường dẫn cây phục vụ phạm vi dữ liệu |
| 6 | Biểu mẫu xuất | 🟡 | Mô hình + seed 5 mẫu + API danh mục. Chưa có màn hình quét placeholder từ `.docx` |
| 7 | Biểu mẫu thống kê | 🟠 | Mô hình dữ liệu và API danh mục; báo cáo tùy biến chưa sinh động từ cấu hình |
| 8 | Quyết định | ✅ | CRUD + chọn sáng kiến đủ điều kiện + xuất PDF; chặn gán trùng và chặn sửa khi đã ký số |

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
| 26 | Kiểm tra trùng lặp | ✅ | Pipeline đầy đủ, giao diện đối chiếu 2 cột highlight. OCR đã nối vào luồng nộp: tệp PDF/ảnh tự trích xuất văn bản rồi mới chạy so khớp |

## Nhóm VI — Tiếp nhận và xử lý

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 27 | Tiếp nhận hồ sơ | ✅ | Nút hành động sinh động theo quy trình |
| 28 | Danh sách hồ sơ | ✅ | Bộ lọc đa tiêu chí, lưu trong URL, chọn nhiều, xuất Excel |
| 29 | Xử lý hồ sơ | ✅ | Thực thi bước, xử lý hàng loạt, thu hồi, Idempotency-Key |
| 30 | Theo dõi hồ sơ | ✅ | Timeline đầy đủ, badge quá hạn. Job nhắc hạn tự động chạy 7h hằng ngày, chống nhắc trùng trong 20 giờ |
| 31/36 | Đính kèm quyết định | ✅ | Màn hình ban hành quyết định, chọn sáng kiến đủ điều kiện, xuất PDF theo mẫu hành chính |
| 32 | Kết quả sáng kiến | ✅ | Công bố kết quả hàng loạt theo quyết định, mở hiển thị công khai và gửi thông báo tới tác giả |

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
| 43 | Quản lý người dùng | ✅ | Thêm/sửa/gán vai trò, đặt lại mật khẩu (thu hồi phiên cũ), khóa/mở khóa. Import Excel: ⬜ |
| 44 | Quản lý đơn vị | ✅ | Cây tổ chức + panel chi tiết, thêm đơn vị con, sửa, xoá ngay trên giao diện |
| 45 | Quản lý vai trò | ✅ | Ma trận phân quyền sửa trực tiếp trên bảng, chọn cả cột, thêm/sửa/xoá vai trò |
| 46 | Cấu hình hệ thống | ✅ | Đọc/ghi theo nhóm, có kiểu dữ liệu, màu chủ đạo áp dụng ngay lên giao diện |
| 47 | Cấu hình đơn vị | ✅ | Sửa được tiêu đề văn bản, người ký mặc định và chức vụ ngay trong form đơn vị |
| 48 | Cấu hình menu | ✅ | Menu render động từ CSDL và lọc theo quyền; sửa menu bằng API |
| 49 | Cấu hình chữ ký số | 🟠 | Mô hình `cau_hinh_chu_ky_so`, `nhat_ky_ky_so`; chưa tích hợp CA thật |
| 50 | Cấu hình email & SMS | 🟡 | Worker gửi thật đã có (SMTP qua MailKit, SMS qua API nhà cung cấp), rút hàng đợi mỗi 5 phút. Màn hình cấu hình máy chủ: ⬜ |
| 51 | Cấu hình thông tin sáng kiến | ✅ | Ngưỡng trùng lặp, hệ số tính điểm, giới hạn tệp — sửa được trên giao diện |

---

## Tổng hợp

| Mức | Số chức năng |
|---|---|
| ✅ Hoàn chỉnh | 39 |
| 🟡 API xong, giao diện cơ bản | 5 |
| 🟠 Có khung, nghiệp vụ chưa đầy đủ | 5 |
| ⬜ Chưa triển khai | 2 |

## Công việc nền (Hangfire)

Chạy trong chính tiến trình API, lưu hàng đợi trong PostgreSQL. Dashboard tại `/hangfire`,
chỉ vai trò Quản trị hệ thống mở được. Tắt hoàn toàn bằng `CongViecNen:BatHangfire=false`.

| Công việc | Lịch mặc định | Nhiệm vụ |
|---|---|---|
| `nhac-han-xu-ly` | 7h hằng ngày | Nhắc hạn xử lý bước và hạn chấm điểm, đánh dấu quá hạn |
| `dong-dot-het-han` | mỗi giờ | Tự đóng đợt đề nghị đã qua hạn nộp (chỉ đợt bật `tự động khoá`) |
| `gui-hang-doi` | mỗi 5 phút | Rút hàng đợi email/SMS và gửi thật |
| `quet-trung-lap-con-thieu` | mỗi 15 phút | Quét bù hồ sơ đã nộp nhưng chưa kiểm tra trùng lặp |

Ngoài ra hai công việc chạy theo sự kiện: trích xuất văn bản (OCR) khi tải tệp lên, và kiểm tra
trùng lặp khi nộp hồ sơ.

Biểu thức cron đọc từ cấu hình `CongViecNen:Lich:*` nên vận hành đổi được tần suất mà không
phải build lại.

## Thứ tự chạy OCR → kiểm tra trùng lặp

Kiểm tra trùng lặp phải đọc được nội dung tệp đính kèm, nên không được chạy trước khi OCR xong:

1. Tải tệp lên → xếp lịch trích xuất văn bản (nếu định dạng rút được văn bản).
2. Nộp hồ sơ → chỉ xếp lịch kiểm tra trùng lặp **nếu không còn tệp nào đang chờ OCR**.
3. Tệp OCR xong → nếu đó là tệp cuối cùng của một hồ sơ đã nộp thì tự đẩy sang kiểm tra trùng lặp.
4. Vòng quét định kỳ 15 phút dọn nốt hồ sơ mắc kẹt (OCR thất bại hẳn, dịch vụ AI chết lúc nộp…).

Bước 4 là lưới an toàn có chủ đích: không có nó, một lần OCR hỏng sẽ khiến hồ sơ vĩnh viễn không
được kiểm tra trùng lặp mà không ai biết.

## Việc còn lại theo thứ tự ưu tiên

1. **Màn hình cấu hình máy chủ email/SMS** (chức năng 50) — worker gửi đã chạy, còn thiếu giao
   diện nhập host/port/tài khoản thay vì sửa thẳng trong cơ sở dữ liệu.
2. **Import người dùng từ Excel** và **xuất PDF phiếu chấm hàng loạt** (chức năng 35).
3. **Báo cáo tuỳ biến sinh động từ cấu hình** (chức năng 7) và màn hình quét placeholder `.docx`
   cho biểu mẫu xuất (chức năng 6).
4. **Tích hợp thật**: SSO OIDC, đẩy dữ liệu sang Thi đua khen thưởng và IOC.
5. **Chữ ký số** với nhà cung cấp CA cụ thể của đơn vị.
6. **Ứng dụng di động** React Native dùng chung hợp đồng API hiện có.
