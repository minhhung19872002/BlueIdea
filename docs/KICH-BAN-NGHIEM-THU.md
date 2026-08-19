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
| A9 | 21, 41 | Để trống `SSO_ISSUER` trong `.env` rồi mở `/dang-nhap` | **Không** hiện nút "Đăng nhập một lần (SSO)" — không dẫn người dùng vào luồng chắc chắn lỗi | — |
| A10 | 21, 41 | Khai báo `SSO_ISSUER`, `SSO_CLIENT_ID` rồi mở lại `/dang-nhap` | Hiện nút SSO; bấm vào chuyển sang nhà cung cấp kèm `state` và `code_challenge` (PKCE S256), `redirect_uri` = `<web>/dang-nhap/sso` | UT |
| A11 | 21, 41 | Mở `/dang-nhap/sso?code=abc&state=sai-lech` | Báo "Phiên đăng nhập SSO không khớp" và không cấp token (chống CSRF) | — |
| A12 | 41 | Đăng nhập bằng SSO rồi bấm Đăng xuất | Thu hồi phiên nội bộ, sau đó chuyển sang `end_session_endpoint` của nhà cung cấp (single logout); nhà cung cấp không công bố endpoint này thì chỉ đăng xuất cục bộ | UT |

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
| D10 | 19 | Mở Xử lý & đánh giá → Hội đồng sáng kiến, bấm **Thành lập hội đồng** | Tạo được hội đồng kèm cấp, đợt, lĩnh vực phụ trách, số thành viên tối thiểu, tỷ lệ thông qua | IT |
| D11 | 20 | Tab **Thành viên**, đặt hai người cùng chức danh Chủ tịch rồi Lưu | Nút Lưu bị khoá, cảnh báo "Hội đồng phải có đúng 1 Chủ tịch"; nếu gọi thẳng API thì trả `HOI_DONG_DA_CO_CHU_TICH` | UT |
| D12 | 20 | Xoá bớt thành viên còn ít hơn số tối thiểu rồi Lưu | Bị chặn, nêu rõ số thành viên tối thiểu của hội đồng | UT |
| D13 | 19 | Tab **Phiên họp** → Tạo phiên họp, chọn hồ sơ đưa ra xét | Phiên ở trạng thái Dự kiến, tự sinh mã phiên và tạo sẵn dòng điểm danh cho mọi thành viên | IT |
| D14 | 19 | Trong phiên họp, tick **Có mặt** cho một thành viên | Điểm danh lưu ngay, tiêu đề tab đổi thành `Điểm danh (1/7)` | IT |
| D15 | 19 | Thành viên có quyền bỏ phiếu bấm **Đồng ý** cho một hồ sơ | Ghi nhận phiếu, khối kiểm phiếu cập nhật tổng phiếu / đồng ý / tỷ lệ và nhãn Đạt–Chưa đạt ngưỡng thông qua | IT |
| D16 | 19 | Người không thuộc hội đồng bỏ phiếu | Bị chặn: "Bạn không phải thành viên của hội đồng này" | UT |
| D17 | 19 | Chủ tịch nhập kết luận rồi bấm **Kết thúc phiên** | Phiên chuyển Đã kết thúc, lưu kết luận; bỏ phiếu tiếp bị chặn "Phiên họp đã kết thúc" | IT |
| D18 | 35 | Trong trang hội đồng bấm **Xuất phiếu chấm** | Tải về một tệp PDF gộp toàn bộ phiếu chấm của hội đồng | IT |

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
| G9 | 28 | Ở `/xu-ly` đặt bộ lọc, bấm **Lưu bộ lọc hiện tại**, tick "Đặt làm mặc định" | Bộ lọc xuất hiện trong danh sách kèm dấu ★ | IT |
| G10 | 28 | Rời khỏi màn hình rồi mở lại `/xu-ly` không kèm tham số | Bộ lọc mặc định tự áp dụng, URL được điền lại đúng tiêu chí đã lưu | — |
| G11 | 28 | Mở `/xu-ly?trangThaiTong=DA_NOP` (liên kết chia sẻ) | Giữ nguyên tiêu chí trên liên kết, **không** bị bộ lọc mặc định ghi đè | — |
| G12 | 28 | Lưu bộ lọc trùng tên đã có | Ghi đè bộ lọc cũ thay vì báo lỗi trùng | UT |
| G13 | 6 | Vào Quản trị → Danh mục → tab **Biểu mẫu xuất**, thêm biểu mẫu và tải tệp `.docx` mẫu | Hệ thống liệt kê placeholder `{{ }}` tìm được, kể cả placeholder bị Word cắt thành nhiều đoạn | UT |
| G14 | 6 | Ánh xạ từng placeholder sang nguồn dữ liệu rồi Lưu | Mở lại biểu mẫu thấy đúng tệp mẫu và bảng ánh xạ đã lưu | IT |
| G15 | 16 | Vào Quản trị → Liên thông hệ thống ngoài, thêm hệ thống kèm client secret | Danh sách hiện "Đã đặt bí mật"; giá trị bí mật **không** bao giờ trả về giao diện | IT |
| G16 | 16 | Sửa hệ thống nhưng để trống ô bí mật | Bí mật cũ được giữ nguyên (vẫn "Đã đặt bí mật") | IT |
| G17 | 41 | Bấm **Xem trước dữ liệu** | Bảng liệt kê sáng kiến đã công bố sẽ được đẩy đi, lọc được theo đợt và năm; không gửi gì cho hệ thống ngoài | IT |
| G18 | 41 | Bấm **Đồng bộ** trên một hệ thống đang hoạt động | Báo số bản ghi thành công/thất bại và ghi một dòng vào tab Nhật ký đồng bộ | IT |
| G19 | 37 | Trên trang Tra cứu mở khối *Tìm theo ý nghĩa*, mô tả nội dung cần tìm rồi bấm Tìm | Ra danh sách kèm **độ tương đồng** và **đoạn khớp nhất**, không cần trùng từ khoá | — |
| G20 | 1 | Vào Danh mục → Lĩnh vực, bấm mũi tên lên/xuống trên một dòng | Thứ tự đổi và lưu ngay; mở lại trang vẫn giữ thứ tự mới | — |

## I. Ký số, thông báo và vòng đời đợt

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| I1 | 49 | Vào Quản trị → Chữ ký số khi chưa khai báo gì | Thẻ trạng thái báo "Chưa ký số được", nêu rõ thiếu cấu hình hay thiếu chứng thư trên máy chủ | — |
| I2 | 49 | Thêm cấu hình chữ ký số kèm client secret rồi mở lại danh sách | Chỉ hiện "Đã đặt bí mật"; giá trị bí mật **không** bao giờ trả về giao diện | IT |
| I3 | 49 | Đặt cấu hình thứ hai làm mặc định | Cấu hình trước tự bỏ mặc định — luôn chỉ một cấu hình mặc định | IT |
| I4 | 49 | Sửa quyết định, tải tệp văn bản lên rồi lưu | Mở lại thấy đúng tệp; nút **Ký số** chuyển sang dùng được | IT |
| I5 | 49 | Bấm **Ký số** trên quyết định đã có tệp | Báo đã ký; quyết định hiện nhãn *Đã ký số* và không sửa/xoá được nữa | — |
| I6 | 49 | Mở **Lịch sử ký số** rồi bấm **Xác minh** | Hiện serial chứng thư, người ký, thời gian ký và kết luận **Chữ ký hợp lệ** | — |
| I7 | 49 | Sửa nội dung tệp gốc rồi xác minh lại | Báo chữ ký **không hợp lệ** — phát hiện văn bản đã bị thay đổi | UT |
| I8 | — | Đăng nhập tài khoản có thông báo, xem chuông trên thanh trên | Badge hiện đúng số chưa đọc; bấm vào mở danh sách thông báo | — |
| I9 | — | Bấm vào một thông báo chưa đọc | Đánh dấu đã đọc (badge giảm) và mở thẳng hồ sơ liên quan nếu có | — |
| I10 | — | Bấm **Đọc tất cả** | Badge về 0; chỉ ảnh hưởng thông báo của chính mình | IT |
| I11 | — | Người A gọi API đánh dấu đã đọc thông báo của người B | HTTP 404 — thông báo của người khác coi như không tồn tại | IT |
| I12 | 3 | Vào Danh mục → Đợt đề nghị | Bảng hiện trạng thái vòng đời và cảnh báo *Thiếu quy trình / Thiếu bộ tiêu chí* | — |
| I13 | 3 | Bấm **Mở đợt** khi đợt chưa gán quy trình hoặc bộ tiêu chí | Bị chặn, nêu rõ phải gán trước | — |
| I14 | 3 | Bấm **Sao chép** một đợt, đặt mã và năm mới | Đợt mới giữ nguyên quy trình, bộ tiêu chí, đơn vị áp dụng; trạng thái **Nháp** | — |
| I15 | 9 | Bấm **Sao chép thành quy trình mới** trên một quy trình | Quy trình mới ở trạng thái nháp, giữ nguyên bước và tác nhân | — |
| I16 | 18 | Trong bộ tiêu chí, thêm hai mức công nhận có khoảng điểm chồng lấn rồi lưu | Bị chặn với mã `KHOANG_DIEM_CHONG_LAN` | UT |
| I17 | 33 | Trên hồ sơ, bấm **Phân công chấm điểm**, chọn hội đồng và hạn | Các thành viên được phân công nhận thông báo; thành viên là tác giả bị loại | IT |
| I18 | 32 | Bấm **Tổng hợp điểm**, chọn hội đồng | Hiện bảng: số phiếu dùng, cao/thấp nhất, trung bình, điểm cuối, kết quả đạt và mức công nhận | IT |
| I19 | 29 | Bấm **Thu hồi bước** và bỏ trống lý do | Bị chặn cho tới khi nhập lý do; thu hồi xong hồ sơ quay lại bước trước | — |
| I20 | 35 | Mở tab **Ma trận điểm** của hội đồng | Hàng = hồ sơ, cột = thành viên; điểm chỉ hiện ở ô **Đã gửi** | — |
| I21 | 35 | Bấm **Mở lại** trên một ô đã gửi | Ô chuyển sang *Đang chấm*, số phiếu đã chấm giảm; thành viên sửa và gửi lại được | — |
| I22 | — | Đăng nhập `hoidong01` (thành viên hội đồng) rồi mở Trang chủ | Hiện trang chủ rút gọn với lối tắt theo quyền, **không** có thông báo lỗi thiếu quyền | — |
| I23 | — | Đăng nhập `admin` rồi mở Trang chủ | Vẫn đầy đủ 4 chỉ số + 3 biểu đồ + top đơn vị + cảnh báo | KB |
| I24 | — | Gọi thẳng `GET /api/v1/bao-cao/tong-quan` bằng token tác giả | HTTP 403 — máy chủ vẫn chặn, giao diện chỉ xử lý mềm phía người dùng | IT |

## K. Biên bản, cấu hình quy trình và nhật ký lỗi

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| K1 | 19 | Mở phiên họp **chưa kết thúc** → tab Biên bản → bấm Lập biên bản | Nút bị vô hiệu hoá; gọi thẳng API trả 422 "phiên họp đã kết thúc" | IT |
| K2 | 19 | Kết thúc phiên rồi bấm **Lập biên bản** | Biên bản có số `BB-…`, ghi đúng số người có mặt, danh sách hồ sơ kèm số phiếu và tỷ lệ đồng ý, kết luận của phiên | IT |
| K3 | 19 | Bấm **Xuất PDF** biên bản | Tải về PDF theo mẫu văn bản hành chính: thành phần dự họp, thành viên vắng, kết quả từng hồ sơ, nơi ký của chủ tịch | IT |
| K4 | 19 | Chủ tịch bấm **Ký nhận biên bản** | Dòng chữ ký của chủ tịch chuyển sang *Đã ký* kèm thời gian; đủ chữ ký thì biên bản chuyển trạng thái *Đã ký đủ* | IT |
| K5 | 19 | Người không thuộc hội đồng gọi API ký biên bản | HTTP 403 "Bạn không phải thành viên của hội đồng này" | IT |
| K6 | 49 | Bấm **Ký số** biên bản | Sinh PDF của biên bản hiện hành rồi ký; lịch sử ký số hiện serial chứng thư và người ký | — |
| K7 | 13 | Vào Quy trình → *Thành phần hồ sơ*, sửa một dòng rồi Lưu | Lưu thành công; mở lại wizard nộp hồ sơ thấy checklist đổi theo | — |
| K8 | 13 | Đặt hai thành phần trùng mã rồi Lưu | Cảnh báo đỏ nêu rõ mã trùng, nút Lưu bị khoá | — |
| K9 | 16 | Vào Quy trình → *Liên thông*, gắn một hệ thống vào bước với sự kiện *Khi hoàn thành* | Cấu hình hiện trong bảng kèm tên bước và tên hệ thống | IT |
| K10 | 16 | Gọi API gắn liên thông với `buocId` của quy trình khác | HTTP 422 "Bước được chọn không thuộc quy trình này" | IT |
| K11 | 5 | Vào Danh mục → *Cấp phê duyệt*, thêm cấp 1 và cấp 2 cho cùng một đợt | Hai dòng hiển thị theo thứ tự cấp | — |
| K12 | 5 | Thêm cấp trùng thứ tự trong cùng phạm vi | HTTP 409, nêu rõ đã có cấp đó cho phạm vi này | IT |
| K13 | — | Vào Nhật ký → tab *Lỗi hệ thống* | Danh sách lỗi 5xx, lọc theo mức độ và trạng thái; mở rộng dòng xem stack trace | IT |
| K14 | — | Đăng nhập tác giả rồi gọi API nhật ký lỗi | HTTP 403 — chỉ vai trò có quyền xem nhật ký mới đọc được | IT |
| K15 | — | Mở hai trình duyệt, tài khoản A gửi thông báo tới B | Chuông của B tăng số **ngay** (không chờ 60 giây) nhờ SignalR | — |
| K16 | — | Ngắt mạng rồi bật lại | Realtime tự kết nối lại; trong lúc mất kết nối, chuông vẫn cập nhật theo nhịp 60 giây | — |
| K17 | 9 | Trong trình thiết kế bấm **Xuất PNG** | Tải về ảnh sơ đồ, không dính thanh điều khiển của canvas | — |

## L. Danh mục còn lại, cấu hình bước và quản trị (bổ sung cho đủ 51 chức năng)

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| L1 | 2 | Danh mục → **Đối tượng**, thêm mới rồi sửa, xoá | Bản ghi hiện đúng sau mỗi thao tác; mã trùng bị chặn 409; xoá mềm (GET sau xoá trả 404) | IT |
| L2 | 2 | Bấm **Nhập từ Excel**, tải tệp có mã đã tồn tại | Chạy thử báo rõ dòng nào cập nhật, dòng nào tạo mới; không tạo bản trùng mã | IT |
| L3 | 7 | Danh mục → **Biểu mẫu thống kê**, tạo mẫu với 3 cột và nhóm theo đơn vị | Mẫu lưu được; mở `/bao-cao/tuy-bien` chạy ra bảng đúng cột đã khai, có dòng tổng | IT |
| L4 | 7 | Khai một cột trỏ tới nguồn dữ liệu không có trong danh sách trắng | Bị chặn ngay khi lưu, nêu rõ nguồn không hợp lệ | IT |
| L5 | 8 | Danh mục → **Quyết định**, tạo quyết định rồi gắn sáng kiến đủ điều kiện | Chỉ hiện sáng kiến đã phê duyệt chưa gắn quyết định; gắn trùng bị chặn | IT |
| L6 | 8 | Sửa quyết định **đã ký số** | Bị chặn: quyết định đã ký số không sửa/xoá được | IT |
| L7 | 11 | Trình thiết kế quy trình → chọn một node bước → panel phải | Sửa được tên, loại bước, số ngày xử lý, cờ bắt buộc ý kiến / đính kèm / cho phép uỷ quyền; Lưu rồi mở lại giữ nguyên | — |
| L8 | 11 | Đặt **cảnh báo trước hạn** = 4 giờ cho một bước | Job nhắc hạn dùng đúng ngưỡng của bước đó thay vì cấu hình chung | UT |
| L9 | 12 | Trình thiết kế → tab **Chức năng bổ sung**, bật `KY_SO` và `GUI_EMAIL` cho một bước | Cấu hình lưu; khi thực thi bước, hệ thống chỉ chạy đúng các chức năng đã bật | IT |
| L10 | 14 | Khai một trạng thái bước với **Hiển thị cho tác giả = tắt** | Tác giả mở hồ sơ chỉ thấy nhãn trung tính "Đang xử lý"; tiến độ không hiện ý kiến nội bộ của bước đó | IT |
| L11 | 14 | Cán bộ xử lý mở cùng hồ sơ | Vẫn thấy đầy đủ tên trạng thái và ý kiến nội bộ | IT |
| L12 | 31, 36 | Hồ sơ đã phê duyệt → tab **Quyết định** → gắn quyết định | `ngay_cong_nhan` của sáng kiến được cập nhật theo quyết định | IT |
| L13 | 36 | Danh mục → Quyết định → **Xuất PDF** | Tải về PDF theo mẫu văn bản hành chính, có số quyết định và danh sách sáng kiến | IT |
| L14 | 42 | Mở hệ thống trên trình duyệt điện thoại (hoặc thu cửa sổ còn 320px) | Dùng được đủ luồng tác giả: xem hồ sơ, nộp, theo dõi tiến độ — **phương án web responsive thay cho ứng dụng cài đặt, cần văn bản chấp thuận của Chủ đầu tư** | — |
| L15 | 46 | Cấu hình hệ thống → đổi **màu chủ đạo** và tải logo | Giao diện đổi màu ngay sau khi lưu; logo hiện trên thanh tiêu đề và trang đăng nhập | IT |
| L16 | 46 | Khai một **ngày nghỉ lễ** rồi tạo hồ sơ mới | Hạn xử lý bỏ qua ngày lễ vừa khai và hai ngày cuối tuần | UT |
| L17 | 47 | Mở đơn vị → **Cấu hình đơn vị**, đặt tiêu đề văn bản và người ký mặc định | Văn bản xuất ra của đơn vị đó dùng đúng tiêu đề và người ký đã khai | IT |
| L18 | 50 | Cấu hình email & SMS → bấm **Gửi thử** | Có bản ghi trong hàng đợi; mật khẩu SMTP **không** bao giờ trả về giao diện | IT |
| L19 | 50 | Mẫu thông báo → xem trước với biến thiếu dữ liệu | Biến chưa có dữ liệu hiển thị `[tên_biến]` thay vì để trống im lặng | IT |
| L20 | 51 | Cấu hình thông tin sáng kiến → đổi ngưỡng cảnh báo trùng lặp đỏ | Đọc lại thấy giá trị mới; tab Trùng lặp đổi mức cảnh báo theo ngưỡng mới | IT |

## M. Luồng bổ sung sau rà soát

| # | Chức năng | Các bước kiểm chứng | Kết quả mong đợi | Tự động |
|---|---|---|---|---|
| M1 | 26 | Mở hồ sơ → tab **Kiểm tra trùng lặp** → nhập ý kiến → **Đánh dấu đã xem xét** | Thẻ chuyển sang *Đã xem xét*; mở lại thấy nguyên ý kiến; nhật ký hệ thống ghi người và thời điểm | IT, E2E |
| M2 | 26 | Đăng nhập tác giả rồi gọi API ghi ý kiến xem xét | HTTP 403 — chỉ vai trò có `TRUNG_LAP.XEM_XET` mới kết luận được | IT, E2E |
| M3 | 26 | Bấm **Xuất báo cáo PDF** ở tab Trùng lặp | Tải về PDF có tỷ lệ tổng hợp, danh sách hồ sơ đối chiếu, **trích dẫn đoạn trùng** và ý kiến hội đồng | IT, E2E |
| M4 | 13 | Bỏ tick *dùng để kiểm tra trùng lặp* ở thành phần Phụ lục, chạy lại kiểm tra | Nội dung phụ lục **không** còn đi vào so khớp; thành phần được tick vẫn vào bình thường | IT |
| M5 | 39 | Báo cáo → Sáng kiến chưa đạt → **Xuất PDF** | Tải về PDF đúng danh sách chưa đạt kèm cột Lý do (trước đây nút này tải nhầm danh sách đạt) | IT, E2E |
| M6 | 40 | Báo cáo → Theo đơn vị → **Xuất PDF** | Tải về PDF có tỷ lệ đạt từng đơn vị | IT, E2E |
| M7 | — | Báo cáo → **Xuất nền** | Trả về ngay HTTP 202; khi xong có thông báo trong chuông kèm liên kết tải tệp | IT, E2E |
| M8 | 21, 43 | Menu người dùng → **Thông tin cá nhân**, sửa họ tên / điện thoại / chức vụ rồi Lưu | Lưu thành công, tên trên thanh tiêu đề đổi theo | IT, E2E |
| M9 | 21, 43 | Gửi kèm `donViId` và `vaiTro` khi cập nhật thông tin cá nhân | Hai trường đó bị bỏ qua — đơn vị và vai trò chỉ quản trị viên đổi được | IT |
| M10 | 21 | Menu người dùng → **Bảo mật tài khoản** | Vào được trang bật/tắt MFA và xem mã khôi phục — lối tắt mới cạnh *Đổi mật khẩu*, bên cạnh mục sẵn có ở menu trái | E2E |
| M11 | 15, 29 | Mở hộp thoại xử lý của bước có bật *cho phép uỷ quyền* | Hiện ô **Xử lý thay cho**, danh sách chỉ gồm tác nhân của bước đó | E2E |
| M12 | 15, 29 | Gọi API thực thi với `nguoiUyQuyenId` là người không phải tác nhân của bước | Bị chặn `KHONG_CO_QUYEN_XU_LY_BUOC` | IT |
| M13 | 35 | Xuất phiếu chấm → chọn **Word (.docx)** | Tải về tệp .docx mở được bằng Word, có bảng điểm đủ dòng tiêu chí | IT, E2E, UT |
| M14 | 49 | Ký số một tệp **XML** rồi xác minh | Ký theo chuẩn XAdES-BES (chữ ký nằm trong tệp); sửa một ký tự rồi xác minh lại → báo không hợp lệ | UT |
| M15 | 49 | Ký số một tệp **PDF** rồi bấm **Xác minh** | Báo chữ ký hợp lệ kèm serial chứng thư (trước đây xác minh bản PAdES luôn báo "không có chữ ký") | UT |

## H. Phi chức năng

| # | Yêu cầu | Các bước kiểm chứng | Kết quả mong đợi |
|---|---|---|---|
| H1 | Tiếng Việt | Kiểm tra toàn bộ nhãn, thông báo lỗi | 100 % tiếng Việt có dấu, chuẩn NFC |
| H2 | Responsive (chức năng 42) | Mở trên điện thoại hoặc thu cửa sổ xuống 320px | Menu chuyển thành Drawer, bảng cuộn ngang trong khung riêng, không vỡ bố cục — đây là cách hệ thống đáp ứng yêu cầu dùng trên thiết bị di động |
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
