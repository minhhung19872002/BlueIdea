# Kịch bản nghiệm thu

Tài liệu dùng để nghiệm thu với Chủ đầu tư. Mỗi dòng ánh xạ tới một chức năng trong bảng truy vết
51 chức năng của E-HSMT, kèm cách kiểm chứng và kết quả mong đợi.

**Chuẩn bị:** chạy `docker compose -f deploy/docker-compose.yml up -d`, đăng nhập tài khoản
tương ứng với mật khẩu `Sk@2026`. Trạng thái triển khai thực tế của từng chức năng xem
[`TRANG-THAI-TRIEN-KHAI.md`](TRANG-THAI-TRIEN-KHAI.md).

**Cột "Tự động"** cho biết bước đó đã được kiểm thử tự động hay chưa:
- `UT` — có unit test (`dotnet test tests/BlueIdea.UnitTests`)
- `IT` — có integration test trên PostgreSQL thật (`dotnet test tests/BlueIdea.IntegrationTests`)
- `KB` — có trong kịch bản `scripts/kiem-thu-luong-nghiep-vu.ps1`

---

## A. Xác thực và phân quyền

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| A1 | 21 | Đăng nhập `admin` / `Sk@2026` | Vào được trang chủ, menu hiển thị đủ 10 mục | IT, KB |
| A2 | 21 | Đăng nhập sai mật khẩu | Báo "Tên đăng nhập hoặc mật khẩu không đúng", **không** tiết lộ tài khoản có tồn tại | IT |
| A3 | 21 | Nhập sai 5 lần liên tiếp | Tài khoản bị khóa tạm 15 phút; ghi vào Nhật ký đăng nhập | — |
| A4 | 21 | Đăng nhập tài khoản mới tạo | Bị buộc chuyển sang trang Đổi mật khẩu, không vào được chức năng khác | — |
| A5 | 21 | Đổi mật khẩu bằng mật khẩu cũ | Báo "không được trùng 3 mật khẩu gần nhất" | — |
| A6 | 43 | Gọi API bất kỳ khi chưa đăng nhập | HTTP 401 kèm mã lỗi `CHUA_XAC_THUC` | IT |
| A7 | 45 | Đăng nhập `gv.lan` (tác giả), mở `/quan-tri/quy-trinh` | HTTP 403 `KHONG_CO_QUYEN`; menu Quản trị không hiển thị | IT |
| A8 | 48 | So sánh menu của `admin` và `gv.lan` | Menu lọc theo quyền, tác giả chỉ thấy các mục được phép | KB |

## B. Danh mục hệ thống

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| B1 | 1 | Vào Quản trị → Danh mục → Lĩnh vực, gõ "giao duc" | Ra "Giáo dục và Đào tạo" — tìm không dấu ra kết quả có dấu | IT |
| B2 | 1 | Thêm lĩnh vực trùng mã đã có | HTTP 409 `TRUNG_MA` | — |
| B3 | 1 | Xóa lĩnh vực đang có hồ sơ sử dụng | HTTP 409 `DANG_DUOC_THAM_CHIEU`, nêu rõ nơi đang tham chiếu và số lượng | — |
| B4 | 1 | Bấm Xuất Excel | Tải về tệp `.xlsx` có tiêu đề, thời điểm xuất, dòng tổng số | — |
| B5 | 3 | Mở đợt chưa gán quy trình | Bị chặn: "Phải gán quy trình và bộ tiêu chí trước khi mở đợt" | — |
| B6 | 3 | Sao chép đợt từ năm trước | Đợt mới giữ nguyên quy trình, bộ tiêu chí, đơn vị áp dụng; trạng thái NHÁP | — |
| B7 | 4 | Chọn loại tác giả "Cá nhân" rồi thêm đồng tác giả | Nút thêm bị vô hiệu hóa khi đã đạt số tác giả tối đa | — |
| B8 | 5, 44 | Mở Quản trị → Đơn vị | Cây tổ chức 3 cấp, khoảng 22 đơn vị | — |

## C. Quy trình động

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| C1 | 9 | Mở `/quan-tri/quy-trinh/{id}/thiet-ke` | Sơ đồ 6 bước với nhãn nhánh Đạt / Không đạt / Bổ sung | — |
| C2 | 9 | Bấm **Kiểm tra hợp lệ** trên quy trình mẫu | Báo hợp lệ; có cảnh báo về vòng lặp "Bổ sung hồ sơ" (có điều kiện thoát nên chỉ là cảnh báo) | UT |
| C3 | 9 | Xóa tác nhân của một bước rồi kiểm tra lại | Báo lỗi `BUOC_KHONG_CO_TAC_NHAN` kèm tên bước | UT |
| C4 | 9 | Bỏ đánh dấu bước bắt đầu rồi kiểm tra | Báo lỗi `KHONG_CO_BUOC_BAT_DAU` | UT |
| C5 | 9 | Tạo hai bước trỏ vòng vào nhau, không nhánh nào có điều kiện | Báo lỗi `VONG_LAP_VO_HAN` | UT |
| C6 | 9 | Xóa hội đồng khỏi bước Chấm điểm rồi kiểm tra | Báo lỗi `BUOC_CHAM_DIEM_THIEU_CAU_HINH` nêu rõ thiếu hội đồng / bộ tiêu chí | UT |
| C7 | 9 | Sửa sơ đồ quy trình đang có hồ sơ chạy dở | HTTP 409 `QUY_TRINH_DANG_SU_DUNG`, gợi ý tạo phiên bản mới | — |
| C8 | 9 | Bấm **Tạo phiên bản mới** | Sinh quy trình mới với số phiên bản tăng; hồ sơ cũ vẫn chạy theo bản cũ | UT |
| C9 | 10 | Xem hồ sơ có tổng điểm < 50 ở bước Họp hội đồng | Nút "Đạt" bị vô hiệu hóa, tooltip nêu rõ điều kiện chưa thỏa | IT, KB |

## D. Tiêu chí và chấm điểm

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| D1 | 17 | Mở bộ tiêu chí mẫu | 4 nhóm, tổng 100 điểm, thanh trạng thái báo hợp lệ | UT |
| D2 | 17 | Sửa trọng số một nhóm thành 40 (tổng thành 110) | Cảnh báo đỏ "Tổng trọng số phải bằng 100%" | UT |
| D3 | 18 | Đặt hai mức công nhận có khoảng điểm chồng lấn | Báo lỗi `KHOANG_DIEM_CHONG_LAN` | UT |
| D4 | 34 | Chấm điểm, kéo thanh trượt từng tiêu chí | Tổng điểm và nhãn ĐẠT/CHƯA ĐẠT cập nhật tức thì | — |
| D5 | 34 | Chấm điểm vượt điểm tối đa của tiêu chí | Bị chặn ngay trên giao diện và ở API | UT |
| D6 | 34 | Gửi phiếu rồi mở lại | Phiếu bị khóa; chỉ thư ký mở lại được (`PHIEU_DA_GUI_KHONG_SUA_DUOC`) | — |
| D7 | 34 | Thành viên hội đồng đồng thời là tác giả hồ sơ | Bị loại khỏi phân công; nếu cố chấm thì báo `XUNG_DOT_LOI_ICH` | — |
| D8 | 35 | Thư ký mở bảng ma trận điểm | Hàng = hồ sơ, cột = thành viên; điểm chỉ hiện sau khi phiếu đã gửi | — |
| D9 | 18 | Bộ tiêu chí bật "loại điểm cao/thấp", 5 phiếu | Loại 1 điểm cao nhất + 1 thấp nhất, trung bình 3 phiếu còn lại | UT |

## E. Nộp và xử lý hồ sơ

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| E1 | 22 | Đăng nhập `gv.lan`, mở wizard nộp hồ sơ | Chỉ hiện đợt đang mở và còn hạn nộp | IT, KB |
| E2 | 22 | Nhập tổng tỷ lệ đóng góp ≠ 100% rồi sang bước tiếp | Bị chặn, nêu rõ tổng hiện tại | UT |
| E3 | 24 | Nộp khi chưa có tệp minh chứng | Bị chặn `THIEU_THANH_PHAN_BAT_BUOC`, nêu đúng tên mục còn thiếu | IT, KB |
| E4 | 25 | Đổi tên `virus.exe` thành `tailieu.pdf` rồi tải lên | Bị từ chối: nội dung tệp không khớp định dạng (kiểm tra magic number) | — |
| E5 | 22 | Nộp hồ sơ đầy đủ | Sinh mã `SK-2026-xxxx`, chuyển sang bước Tiếp nhận | IT, KB |
| E6 | 23 | Tác giả mở hồ sơ đã nộp | Không thấy nút xử lý nào (không phải tác nhân của bước) | IT, KB |
| E7 | 27 | `tiepnhan` mở hồ sơ | Thấy đúng 3 nút: Tiếp nhận / Yêu cầu bổ sung / Từ chối | IT, KB |
| E8 | 29 | Bấm Tiếp nhận, nhập ý kiến | Chuyển sang bước Thẩm định sơ bộ | IT, KB |
| E9 | 29 | Bước bắt buộc nhập ý kiến mà để trống | Bị chặn kèm thông báo rõ ràng | UT |
| E10 | 15 | 3 thành viên hội đồng lần lượt xác nhận chấm xong | Người 1, 2 chưa chuyển bước (`1/3`, `2/3`); người 3 mới chuyển | IT, KB |
| E11 | 29 | Chọn nhiều hồ sơ cùng bước rồi xử lý hàng loạt | Báo số thành công/thất bại và lý do từng hồ sơ lỗi | — |
| E12 | 30 | Mở tab Tiến độ xử lý | Timeline đủ các bước, người xử lý, hạn, ý kiến; badge đỏ khi quá hạn | IT, KB |
| E13 | 23 | Mở tab Lịch sử chỉnh sửa | Hiển thị diff giá trị trước/sau từng lần sửa | KB |
| E14 | 29 | Gửi lại cùng một yêu cầu xử lý (double-submit) | Lần thứ hai bị chặn bằng `Idempotency-Key` | — |

## F. Kiểm tra trùng lặp

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| F1 | 26 | Mở `SK-2026-0002`, tab Kiểm tra trùng lặp | Tỷ lệ ≈ 86 %, mức **NGHIÊM TRỌNG** | IT |
| F2 | 26 | Xem bảng chi tiết | Cặp trùng thật xếp đầu, tách bạch tỷ lệ từ vựng và ngữ nghĩa | IT |
| F3 | 26 | Bấm vào dòng có đoạn trùng | Cửa sổ đối chiếu 2 cột, đoạn trùng được tô vàng | — |
| F4 | 26 | Xem hồ sơ không liên quan | Tỷ lệ < 20 %, mức AN TOÀN, số đoạn trùng = 0 | IT |
| F5 | 26 | Bấm Chạy lại kiểm tra | Hoàn tất dưới 1 giây với ~35 hồ sơ đối chiếu | IT |
| F6 | 26 | Kiểm tra mã nguồn và log mạng | Không có lời gọi ra Internet trong toàn bộ pipeline | — |

## G. Tra cứu và báo cáo

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| G1 | 37 | Gõ "sang kien" ở ô tìm kiếm | Ra kết quả chứa "sáng kiến" | IT |
| G2 | 37 | Đặt bộ lọc nâng cao rồi sao chép liên kết, mở ở tab mới | Bộ lọc được khôi phục nguyên vẹn từ URL | — |
| G3 | 37 | Mở `/cong-khai/tra-cuu` khi chưa đăng nhập | Chỉ hiển thị sáng kiến đã công nhận và được đánh dấu công khai | — |
| G4 | 38 | Mở Báo cáo → Sáng kiến đạt | Danh sách kèm điểm, mức công nhận, số quyết định | IT, KB |
| G5 | 39 | Mở Báo cáo → Sáng kiến chưa đạt | Có cột Lý do và điểm đánh giá | KB |
| G6 | 40 | Mở Báo cáo → Theo đơn vị | Có tỷ lệ đạt từng đơn vị và dòng tổng cộng | KB |
| G7 | 38 | Bấm Xuất Excel và Xuất PDF | Tệp tải về đúng định dạng, PDF theo mẫu văn bản hành chính | IT |
| G8 | — | Mở Trang chủ | 4 chỉ số + 3 biểu đồ + top đơn vị + cảnh báo trùng lặp | KB |

## H. Phi chức năng

| # | Yêu cầu | Các bước kiểm chứng | Kết quả mong đợi |
|---|---|---|---|
| H1 | Tiếng Việt | Kiểm tra toàn bộ nhãn, thông báo lỗi | 100 % tiếng Việt có dấu, chuẩn NFC |
| H2 | Responsive | Thu cửa sổ xuống 320px | Menu chuyển thành Drawer, bảng cuộn ngang, không vỡ bố cục |
| H3 | Hiệu năng | Gọi API danh sách hồ sơ | P95 < 500 ms |
| H4 | Hiệu năng | Tải trang lần đầu | < 3 s nhờ chia nhỏ gói theo route |
| H5 | Bảo mật | Xem header phản hồi | Có đủ `X-Content-Type-Options`, `X-Frame-Options`, `CSP`, `Referrer-Policy` |
| H6 | Bảo mật | Gọi API đăng nhập 6 lần trong 1 phút | Lần thứ 6 trả HTTP 429 |
| H7 | Bảo mật | Xem bảng `nhat_ky_he_thong` sau khi đổi mật khẩu | Có bản ghi; trường mật khẩu hiển thị `***` |
| H8 | Chịu lỗi | Dừng dịch vụ OCR rồi nộp hồ sơ | Hồ sơ vẫn nộp được, chỉ đánh dấu chưa kiểm tra trùng lặp |
| H9 | Giám sát | Gọi `/health` và `/health/ready` | Trả `Healthy` |
| H10 | Sao lưu | Chạy `pg_dump` rồi `pg_restore` sang CSDL mới | Dữ liệu khôi phục đầy đủ, hệ thống chạy bình thường |

---

## Chạy toàn bộ kiểm thử tự động

```bash
# 166 unit test cho business rule + 6 integration test trên PostgreSQL thật
dotnet test

# Kịch bản end-to-end qua API thật (29 bước kiểm chứng)
./scripts/kiem-thu-luong-nghiep-vu.ps1 -Goc http://localhost:8080
```

Kết quả tại thời điểm bàn giao: **166/166 unit test đạt**, **6/6 integration test đạt**,
**29/29 bước kịch bản end-to-end đạt**.
