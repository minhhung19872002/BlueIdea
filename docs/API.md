# Tài liệu API — BlueIdea

Tài liệu này **sinh từ mã nguồn** (các controller trong `src/BlueIdea.Api/Controllers/`) nên luôn
khớp với hệ thống đang chạy. Bản đặc tả máy đọc được (OpenAPI 3.0) nằm tại `/swagger/v1/swagger.json`
và giao diện thử API tại `/swagger` khi chạy môi trường phát triển.

## Quy ước chung

| Mục | Quy ước |
|---|---|
| Đường dẫn gốc | `/api/v1` (API cho hệ thống ngoài: `/api/public/v1`) |
| Định dạng | JSON, tên trường `camelCase`, thời gian ISO-8601 kèm offset |
| Vỏ phản hồi | `{ "thanhCong": true, "duLieu": {}, "thongBao": "", "maLoi": null, "chiTietLoi": [] }` |
| Phân trang | `?trang=1&soDong=20&sapXep=ngayTao&huong=desc` → thêm `tongSo`, `trang`, `soDong`, `tongTrang` |
| Xác thực | `Authorization: Bearer <access token>`; access token 15 phút, refresh token 7 ngày xoay vòng |
| Lỗi xác thực dữ liệu | HTTP 422 kèm `chiTietLoi: [{ truong, thongBao }]` |
| Chưa đăng nhập / hết hạn | HTTP 401, mã lỗi `CHUA_XAC_THUC` |
| Không đủ quyền | HTTP 403, mã lỗi `KHONG_CO_QUYEN` |
| Chống gửi trùng | Header `Idempotency-Key` cho các thao tác thực thi bước |
| Giới hạn tần suất | 100 request/phút/IP; riêng đăng nhập 5 lần/phút/IP |

Cột **Quyền** ghi mã quyền mà endpoint yêu cầu (`[Authorize(Policy = ...)]`). Endpoint không ghi
quyền nghĩa là chỉ cần đăng nhập, hoặc là endpoint công khai có đánh dấu riêng trong mô tả.


## API công khai cho hệ thống ngoài (chức năng 41)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/public/v1/sang-kien` | Danh sách sáng kiến đã được công nhận và công bố công khai. | — |
| GET | `/api/public/v1/thong-ke` | Chỉ số tổng hợp phục vụ hệ thống IOC. | — |
| GET | `/api/public/v1/linh-vuc` | Danh mục lĩnh vực kèm số lượng sáng kiến công khai. | — |

## Khác

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| POST | `/api/v1/khoa-api-ngoai` | Cấp khoá mới. Khoá gốc chỉ hiển thị đúng một lần trong phản hồi này. | — |
| GET | `/api/v1/cap-phe-duyet` | Danh sách cấp phê duyệt, lọc được theo đợt và lĩnh vực. | `DanhMucXem` |

## Thống kê báo cáo (chức năng 38–40)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| POST | `/api/v1/bao-cao/{loai}/xuat-nen` | Cac loai bao cao xuat nen duoc — chan bang danh sach trang, khong nhan chuoi tuy y. | `BaoCaoXuat` |
| GET | `/api/v1/bao-cao/tong-quan` | Dashboard tổng quan (số liệu + dữ liệu biểu đồ). | `BaoCaoXem` |
| GET | `/api/v1/bao-cao/sang-kien-dat` | Chức năng 38 — Danh sách sáng kiến đạt. | `BaoCaoXem` |
| GET | `/api/v1/bao-cao/sang-kien-chua-dat` | Chức năng 39 — Danh sách sáng kiến chưa đạt (kèm lý do và điểm). | `BaoCaoXem` |
| GET | `/api/v1/bao-cao/theo-don-vi` | Chức năng 40 — Thống kê theo đơn vị (phục vụ đánh giá thi đua). | `BaoCaoXem` |
| GET | `/api/v1/bao-cao/theo-tac-gia` | Thống kê theo tác giả — ai có bao nhiêu sáng kiến, bao nhiêu đạt. | `BaoCaoXem` |
| GET | `/api/v1/bao-cao/thoi-gian-xu-ly` | Thời gian xử lý trung bình theo từng bước quy trình. | `BaoCaoXem` |
| GET | `/api/v1/bao-cao/tong-hop-nam/{nam:int}` | Báo cáo tổng hợp một năm — số liệu cho báo cáo cuối năm. | `BaoCaoXem` |
| GET | `/api/v1/bao-cao/tong-hop-nam/{nam:int}/xuat-pdf` | Xuất báo cáo tổng hợp năm ra PDF theo mẫu văn bản hành chính. | `BaoCaoXuat` |
| GET | `/api/v1/bao-cao/sang-kien-dat/xuat-pdf` | Xuất báo cáo tổng hợp ra PDF (mẫu văn bản hành chính). | `BaoCaoXuat` |
| GET | `/api/v1/bao-cao/sang-kien-chua-dat/xuat-pdf` | Chức năng 39 — Xuất PDF danh sách sáng kiến chưa đạt (kèm lý do và điểm). | `BaoCaoXuat` |
| GET | `/api/v1/bao-cao/theo-don-vi/xuat-pdf` | Chức năng 40 — Xuất PDF thống kê theo đơn vị (phục vụ đánh giá thi đua). | `BaoCaoXuat` |
| GET | `/api/v1/bao-cao/theo-tac-gia/xuat-pdf` | Xuất PDF thống kê theo tác giả. | `BaoCaoXuat` |
| GET | `/api/v1/bao-cao/thoi-gian-xu-ly/xuat-pdf` | Xuất PDF thời gian xử lý trung bình theo bước. | `BaoCaoXuat` |

## Biên bản họp (chức năng 19)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| POST | `/api/v1/bien-ban-hop/phien-hop/{phienHopId:guid}` | Lập (hoặc làm mới) biên bản của một phiên họp đã kết thúc. | `HoiDongHopPhien` |
| GET | `/api/v1/bien-ban-hop/phien-hop/{phienHopId:guid}` | Biên bản của một phiên họp — trả null khi chưa lập. | `HoiDongXem` |
| POST | `/api/v1/bien-ban-hop/{id:guid}/ky` | Thành viên ký nhận nội dung biên bản (khác với ký số bằng chứng thư). | `HoiDongXem` |
| GET | `/api/v1/bien-ban-hop/{id:guid}/xuat-pdf` | Xuất biên bản ra PDF theo mẫu văn bản hành chính. | `HoiDongXem` |
| POST | `/api/v1/bien-ban-hop/{id:guid}/ky-so` | /// Ký số biên bản: sinh PDF, lưu thành tệp rồi ký bằng chứng thư đang cấu hình. /// /// Sinh PDF ngay tại thời điểm ký (không dùng lại tệp cũ) để chữ | `QuyetDinhKySo` |
| GET | `/api/v1/bien-ban-hop/{id:guid}/lich-su-ky-so` | Lịch sử ký số của biên bản. | `HoiDongXem` |

## Danh mục hệ thống (chức năng 1–8)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/danh-muc/bieu-mau-xuat/truong-kha-dung` | Danh sách trường dữ liệu dùng được cho một loại biểu mẫu. | `DanhMucXem` |
| GET | `/api/v1/danh-muc/bieu-mau-xuat/{id:guid}/xem-truoc` | /// Xem trước biểu mẫu. Không truyền hoSoId thì dựng bằng dữ liệu mẫu, để người cấu hình /// kiểm tra bố cục ngay lúc soạn mà không cần đi tìm một hồ  | `DanhMucXem` |
| GET | `/api/v1/danh-muc/bieu-mau-xuat/{id:guid}/xem-truoc.pdf` | Xem trước biểu mẫu dưới dạng PDF đúng bố cục văn bản hành chính. | `DanhMucXem` |
| GET | `/api/v1/danh-muc/linh-vuc` | Danh sách lĩnh vực có phân trang, tìm kiếm không dấu. | `DanhMucXem` |
| GET | `/api/v1/danh-muc/linh-vuc/chon` | Danh sách rút gọn cho dropdown. | — |
| GET | `/api/v1/danh-muc/linh-vuc/cay` | Cây lĩnh vực (hiển thị dạng Tree trên giao diện). | `DanhMucXem` |
| GET | `/api/v1/danh-muc/linh-vuc/xuat-excel` | Xuất danh sách ra Excel theo bộ lọc hiện tại. | `DanhMucXuat` |
| GET | `/api/v1/danh-muc/dot-de-nghi/quan-ly` | /// Danh sách đợt kèm trạng thái vòng đời — màn hình quản trị đợt dùng để bật/tắt nút /// Mở / Đóng / Khoá đợt. /// | `DanhMucXem` |
| GET | `/api/v1/danh-muc/dot-de-nghi/chon` | /// Danh sách đợt để đổ vào ô chọn — dùng ở mọi màn hình LỌC theo đợt. /// /// Khác dang-mo: ô lọc phải liệt kê cả những đợt đã đóng, vì người dùng th | — |
| GET | `/api/v1/danh-muc/dot-de-nghi/dang-mo` | Các đợt đang mở và còn hạn nộp — dùng ở bước 1 của wizard nộp hồ sơ. | — |
| POST | `/api/v1/danh-muc/dot-de-nghi/{id:guid}/mo-dot` | Mở đợt — bắt đầu nhận hồ sơ. | `DanhMucSua` |
| POST | `/api/v1/danh-muc/dot-de-nghi/{id:guid}/dong-dot` | Đóng đợt — ngừng nhận hồ sơ mới nhưng vẫn xử lý hồ sơ đã nộp. | `DanhMucSua` |
| POST | `/api/v1/danh-muc/dot-de-nghi/{id:guid}/khoa-dot` | Khóa đợt — toàn bộ dữ liệu chỉ đọc. | `DanhMucSua` |
| GET | `/api/v1/danh-muc/dot-de-nghi/{id:guid}/tong-quan` | Sao chép cấu hình đợt từ năm trước. | `DanhMucXem` |

## Bộ lọc yêu thích (chức năng 28)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/bo-loc-yeu-thich` | Bộ lọc đã lưu của tôi trên một màn hình. | — |
| POST | `/api/v1/bo-loc-yeu-thich` | Lưu bộ lọc mới, hoặc ghi đè bộ lọc cùng tên trên cùng màn hình. | — |
| POST | `/api/v1/bo-loc-yeu-thich/{id:guid}/mac-dinh` | Đặt một bộ lọc làm mặc định khi mở màn hình. | — |

## Cấu hình chữ ký số (chức năng 49)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/cau-hinh-chu-ky-so/trang-thai` | /// Máy chủ đã sẵn sàng ký hay chưa — giao diện dùng để cảnh báo trước khi người dùng bấm /// Ký số rồi mới nhận lỗi. /// | `CauHinhXem` |

## Cấu hình email & SMS (chức năng 50)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/cau-hinh-gui-tin` | /// Danh sách cấu hình. Mật khẩu và API key không bao giờ được trả về — /// chỉ có cờ báo đã đặt hay chưa. /// | `CauHinhXem` |
| GET | `/api/v1/cau-hinh-gui-tin/thong-ke-hang-doi` | Thống kê hàng đợi gửi tin — cho biết cấu hình có thực sự hoạt động không. | `CauHinhXem` |
| POST | `/api/v1/cau-hinh-gui-tin/{id:guid}/gui-thu` | Gửi thử ngay bằng cấu hình đang lưu để kiểm chứng trước khi đưa vào vận hành. | `CauHinhSua` |

## Cấu hình menu (chức năng 48)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/cau-hinh-menu` | Toàn bộ mục menu của một loại (WEB hoặc MOBILE), kể cả mục đang ẩn. | — |
| PUT | `/api/v1/cau-hinh-menu/sap-xep` | /// Lưu lại toàn bộ cây sau khi kéo thả: thứ tự là vị trí trong mảng, cấp cha là vị trí lồng. /// /// Nhận nguyên cây thay vì từng thao tác kéo lẻ: ké | — |

## Quy trình động (chức năng 9–16)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/quy-trinh/{quyTrinhId:guid}/lien-thong` | Danh sách cấu hình liên thông của một quy trình. | `QuyTrinhXem` |
| GET | `/api/v1/quy-trinh/{id:guid}/so-do` | Sơ đồ đầy đủ để render trên ReactFlow. | `QuyTrinhXem` |
| PUT | `/api/v1/quy-trinh/{id:guid}/so-do` | Lưu toàn bộ sơ đồ (bước + trường hợp + tác nhân + trạng thái) trong một giao dịch. | `QuyTrinhCauHinh` |
| GET | `/api/v1/quy-trinh/{id:guid}/thanh-phan-ho-so` | Kiểm tra tính hợp lệ theo 7 rule bắt buộc. | `QuyTrinhXem` |
| POST | `/api/v1/quy-trinh/{id:guid}/kich-hoat` | Kích hoạt quy trình (chỉ khi validator không còn lỗi). | `QuyTrinhCauHinh` |
| POST | `/api/v1/quy-trinh/{id:guid}/phien-ban-moi` | Tạo phiên bản mới — hồ sơ cũ vẫn chạy theo snapshot của phiên bản cũ. | `QuyTrinhCauHinh` |

## Tra cứu công khai không cần đăng nhập (chức năng 37)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/cong-khai/sang-kien` | Danh sách sáng kiến đã được công nhận và công bố công khai. | — |
| GET | `/api/v1/cong-khai/thong-ke` | Ba số liệu trên dải thống kê đầu trang. | — |
| GET | `/api/v1/cong-khai/linh-vuc` | Danh sách lĩnh vực dùng cho chip lọc. | — |

## Đánh giá và phiếu chấm (chức năng 33–35)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| POST | `/api/v1/danh-gia/phieu/{id:guid}/ky-so` | /// Chức năng 35 — Ký số một phiếu chấm đã gửi. /// /// Ký trên bản PDF chốt tại thời điểm ký (không ký trên dữ liệu bảng): chữ ký phải gắn với /// đú | `DanhGiaXem` |
| GET | `/api/v1/danh-gia/phieu/{id:guid}/lich-su-ky-so` | Lịch sử ký số của một phiếu chấm. | `DanhGiaXem` |
| GET | `/api/v1/danh-gia/viec-cua-toi` | Chức năng 33 — Danh sách hồ sơ được phân công cho tôi ("Việc của tôi"). | `DanhGiaChamDiem` |
| POST | `/api/v1/danh-gia/phan-cong` | Phân công thành viên hội đồng chấm hồ sơ (loại trừ xung đột lợi ích). | `DanhGiaPhanCong` |
| GET | `/api/v1/danh-gia/phieu` | Chức năng 34 — Lấy phiếu chấm (kèm bộ tiêu chí render động). | `DanhGiaChamDiem` |
| POST | `/api/v1/danh-gia/phieu/luu-nhap` | Lưu nháp phiếu chấm. | `DanhGiaChamDiem` |
| POST | `/api/v1/danh-gia/phieu/gui` | Gửi phiếu chấm chính thức (sau khi gửi chỉ thư ký mới mở lại được). | `DanhGiaChamDiem` |
| POST | `/api/v1/danh-gia/phieu/{id:guid}/mo-lai` | Thư ký mở lại phiếu đã gửi để thành viên sửa. | `DanhGiaMoLaiPhieu` |
| POST | `/api/v1/danh-gia/tong-hop` | Chức năng 32 — Tổng hợp điểm của hội đồng cho một hồ sơ. | `DanhGiaTongHop` |
| GET | `/api/v1/danh-gia/ma-tran-diem` | Chức năng 35 — Bảng ma trận điểm (hàng = hồ sơ, cột = thành viên). | `DanhGiaTongHop` |

## Đơn vị và cấp phê duyệt (chức năng 5, 44, 47)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/don-vi/cay` | Cây tổ chức đầy đủ. | — |
| GET | `/api/v1/don-vi/{id:guid}/logo` | /// Doc logo rieng cua mot don vi (chuc nang 47). /// /// Phai co duong rieng chu khong dung duoc endpoint xem truoc tep dung chung: tep khong gan /// | — |
| POST | `/api/v1/don-vi` | Khong nhan SVG: tep SVG chua duoc ma script chay trong goc cua ung dung. | `DonViCauHinh` |
| POST | `/api/v1/don-vi/{id:guid}/chuyen-cha` | Chức năng 44 — Chuyển đơn vị sang cấp trên khác (kéo thả trên cây tổ chức). | `DonViCauHinh` |
| POST | `/api/v1/don-vi/{id:guid}/gop-vao/{dichId:guid}` | Chức năng 44 — Gộp đơn vị khi sáp nhập. | `DonViCauHinh` |

## Quản trị hệ thống (chức năng 43–46, 51)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/he-thong/anh-thuong-hieu/{loai}` | Các khoá cấu hình được phép đọc khi chưa đăng nhập (dùng cho trang đăng nhập). | — |
| GET | `/api/v1/he-thong/cau-hinh-cong-khai` | Cấu hình hiển thị công khai — không cần đăng nhập. | — |
| GET | `/api/v1/he-thong/cau-hinh` | Toàn bộ cấu hình hệ thống (theo nhóm) — cần quyền CAU_HINH.XEM. | `CauHinhXem` |
| PUT | `/api/v1/he-thong/cau-hinh` | Cập nhật nhiều khoá cấu hình cùng lúc. | `CauHinhSua` |
| GET | `/api/v1/he-thong/vai-tro` | Chức năng 45 — Danh sách vai trò kèm ma trận phân quyền. | `VaiTroXem` |
| GET | `/api/v1/he-thong/nguoi-dung` | Chức năng 43 — Danh sách người dùng. | `NguoiDungXem` |
| PATCH | `/api/v1/he-thong/nguoi-dung/{id:guid}/trang-thai` | Chức năng 43 — Khoá / mở khoá tài khoản. | `NguoiDungSua` |
| GET | `/api/v1/he-thong/nhat-ky/he-thong` | Nhật ký hệ thống (audit log) có lọc. | `NhatKyXem` |
| GET | `/api/v1/he-thong/nhat-ky/dang-nhap` | Nhật ký đăng nhập. | `NhatKyXem` |
| POST | `/api/v1/he-thong/vai-tro/{id:guid}/sao-chep` | /// Chức năng 45 — Sao chép vai trò kèm toàn bộ ma trận phân quyền. /// /// Vai trò mới thường chỉ khác vai trò cũ vài quyền; bắt tick lại từ đầu hàng | `VaiTroCauHinh` |
| GET | `/api/v1/he-thong/nhat-ky/loi` | /// Chức năng nhật ký — lỗi hệ thống (5xx) đã ghi lại, để quản trị viên xem ngay trên giao /// diện thay vì phải mở log container. /// | `NhatKyXem` |
| POST | `/api/v1/he-thong/nhat-ky/loi/{id:guid}/da-xu-ly` | Đánh dấu một lỗi đã được xử lý để không lẫn với lỗi mới. | `NhatKyXem` |
| GET | `/api/v1/he-thong/thong-bao` | Thông báo trong ứng dụng của người dùng hiện tại. | — |
| POST | `/api/v1/he-thong/thong-bao/{id:guid}/da-doc` | /// Đánh dấu đã đọc thông báo. /// /// Truy vấn lọc luôn theo người nhận: chỉ tìm theo Id rồi kiểm tra chủ sở hữu sau thì người /// dùng vẫn đánh dấu  | — |
| POST | `/api/v1/he-thong/thong-bao/doc-tat-ca` | Đánh dấu đã đọc TẤT CẢ thông báo chưa đọc của người đăng nhập. | — |
| GET | `/api/v1/he-thong/nguoi-dung/{id:guid}` | Chi tiết một tài khoản kèm vai trò đang gán. | `NguoiDungXem` |
| POST | `/api/v1/he-thong/nguoi-dung` | /// Tạo tài khoản mới. Hệ thống sinh mật khẩu tạm và trả về đúng MỘT LẦN trong phản hồi này — /// mật khẩu chỉ lưu dưới dạng băm Argon2id nên không th | `NguoiDungThem` |
| POST | `/api/v1/he-thong/nguoi-dung/{id:guid}/dat-lai-mat-khau` | Đặt lại mật khẩu, thu hồi mọi phiên đang mở và bắt đổi ở lần đăng nhập kế tiếp. | `NguoiDungDatLaiMatKhau` |

## Hội đồng và phiên họp (chức năng 19–20)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/hoi-dong/{id:guid}` | Chi tiết hội đồng kèm thành viên và phiên họp. | `HoiDongXem` |
| PUT | `/api/v1/hoi-dong/{id:guid}/thanh-vien` | Chức năng 20 — Lưu danh sách thành viên (bắt buộc đúng 1 chủ tịch). | `HoiDongCauHinh` |
| POST | `/api/v1/hoi-dong/phien-hop/{id:guid}/y-kien-ho-so` | Ghi ý kiến / kết luận riêng cho một hồ sơ trong phiên họp. | `HoiDongHopPhien` |
| POST | `/api/v1/hoi-dong/phien-hop/bo-phieu` | Bỏ phiếu (kín hoặc công khai) cho một hồ sơ trong phiên họp. | `HoiDongBoPhieu` |

## Ký số bằng USB token (chức năng 49)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| POST | `/api/v1/ky-so-usb/chuan-bi` | Nhịp 1 — lấy giá trị băm của nội dung cần ký. | — |
| POST | `/api/v1/ky-so-usb/{phienId:guid}/hoan-tat` | Nhịp 3 — gửi chữ ký và chứng thư từ công cụ ký ở máy trạm về để xác minh. | — |
| DELETE | `/api/v1/ky-so-usb/{phienId:guid}` | Huỷ phiên ký (người dùng đóng hộp thoại giữa chừng). | — |

## Mẫu thông báo và ngày nghỉ lễ (chức năng 46, 50)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/mau-thong-bao/su-kien` | Danh sách sự kiện hệ thống có thể gắn mẫu — giao diện đổ vào ô chọn. | `CauHinhXem` |
| POST | `/api/v1/mau-thong-bao/{id:guid}/xem-truoc` | /// Kết xuất thử mẫu với biến mẫu — xem trước đúng cái người nhận sẽ đọc, trước khi bật. /// | `CauHinhXem` |

## Xác thực và tài khoản (chức năng 21)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/xac-thuc/mfa/trang-thai` | Trạng thái MFA của tài khoản đang đăng nhập. | — |
| POST | `/api/v1/xac-thuc/mfa/bat-dau-ghi-danh` | Bước 1 — sinh bí mật và chuỗi otpauth để quét mã QR. | — |
| POST | `/api/v1/xac-thuc/mfa/xac-nhan-ghi-danh` | /// Bước 2 — nhập mã từ ứng dụng xác thực để bật MFA. Trả về bộ mã khôi phục, /// và đây là lần duy nhất chúng được hiển thị. /// | — |
| POST | `/api/v1/xac-thuc/mfa/tat` | Tắt xác thực hai lớp. | — |
| POST | `/api/v1/xac-thuc/mfa/tao-lai-ma-khoi-phuc` | Sinh lại bộ mã khôi phục, huỷ toàn bộ bộ cũ. | — |
| POST | `/api/v1/xac-thuc/mfa/go/{nguoiDungId:guid}` | /// Quản trị viên gỡ MFA cho tài khoản khác — dùng khi người dùng mất thiết bị và hết mã /// khôi phục. Ghi nhật ký, thu hồi phiên, kiểm tra phạm vi đ | `NguoiDungDatLaiMatKhau` |
| GET | `/api/v1/xac-thuc/captcha` | Sinh ảnh CAPTCHA (SVG) cho trang đăng nhập. | — |
| POST | `/api/v1/xac-thuc/quen-mat-khau` | /// Bước 1 quên mật khẩu — gửi mã OTP qua email. /// /// LUÔN trả về thành công, kể cả khi tài khoản không tồn tại: phản hồi khác nhau sẽ biến /// end | — |
| POST | `/api/v1/xac-thuc/dat-lai-mat-khau` | Bước 2 quên mật khẩu — đổi mã OTP lấy mật khẩu mới. | — |
| POST | `/api/v1/xac-thuc/dang-nhap` | Đăng nhập bằng tài khoản nội bộ. Giới hạn 5 lần/phút/IP. | — |
| GET | `/api/v1/xac-thuc/sso/trang-thai` | Làm mới access token bằng refresh token (token cũ bị thu hồi). | — |
| GET | `/api/v1/xac-thuc/sso/bat-dau` | /// Bắt đầu luồng SSO: sinh state và cặp PKCE, gửi về cho client rồi chuyển hướng /// sang nhà cung cấp. /// /// state được lưu server-side (IDistribu | — |
| POST | `/api/v1/xac-thuc/sso/doi-ma` | Đổi authorization code lấy token của hệ thống. | — |
| POST | `/api/v1/xac-thuc/lam-moi-token` | /// Chan open redirect: chi chap nhan duong dan tra ve co scheme+host+port trung voi /// Cors:NguonChoPhep hoac Sso:AllowedRedirectHosts. /// | — |
| POST | `/api/v1/xac-thuc/dang-xuat` | Đăng xuất và thu hồi refresh token. | — |
| POST | `/api/v1/xac-thuc/sso/dia-chi-dang-xuat` | /// Chức năng 41 — Single logout: địa chỉ để kết thúc luôn phiên bên nhà cung cấp SSO. /// /// Trả về null khi hệ thống không dùng SSO hoặc nhà cung c | — |
| POST | `/api/v1/xac-thuc/doi-mat-khau` | Đổi mật khẩu (áp dụng chính sách mật khẩu cấu hình được). | — |
| GET | `/api/v1/xac-thuc/toi` | Thông tin người dùng đang đăng nhập (dùng khi tải lại trang). | — |
| PUT | `/api/v1/xac-thuc/toi` | /// Chức năng 21, 43 — Người dùng tự cập nhật thông tin cá nhân. /// /// Không sửa được đơn vị, vai trò hay trạng thái tài khoản: đó là quyết định của | — |
| GET | `/api/v1/xac-thuc/menu` | Chức năng 48 — Menu đã lọc theo quyền của người dùng hiện tại. | — |

## Nhập/xuất dữ liệu và biểu mẫu (chức năng 6, 7, 35, 43)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/nhap-xuat/nguoi-dung/mau-nhap` | Thứ tự cột bắt buộc của tệp nhập người dùng. | `NguoiDungThem` |
| POST | `/api/v1/nhap-xuat/nguoi-dung` | /// Nhập người dùng từ Excel. Mặc định chayThu=true: chỉ kiểm tra và báo lỗi từng dòng, /// không ghi gì — để quản trị viên xem trước rồi mới xác nhận | `NguoiDungThem` |
| GET | `/api/v1/nhap-xuat/phieu-cham/ho-so/{sangKienId:guid}` | /// Xuất toàn bộ phiếu chấm đã gửi của một hồ sơ. /// /// dinhDang: bỏ trống = PDF liền mạch (mặc định), ZIP = mỗi phiếu một tệp PDF, /// DOCX = bản W | `DanhGiaXem` |
| GET | `/api/v1/nhap-xuat/phieu-cham/hoi-dong/{hoiDongId:guid}` | Xuất phiếu chấm hàng loạt của một hội đồng — dùng khi in hồ sơ phiên họp. | `DanhGiaXem` |
| GET | `/api/v1/nhap-xuat/bao-cao-tuy-bien/nguon-du-lieu` | Danh sách trường có thể đưa vào báo cáo — màn hình cấu hình đọc từ đây. | `BaoCaoXem` |
| GET | `/api/v1/nhap-xuat/bao-cao-tuy-bien/{bieuMauId:guid}` | Chạy một biểu mẫu thống kê đã cấu hình. | `BaoCaoXem` |
| GET | `/api/v1/nhap-xuat/bao-cao-tuy-bien/{bieuMauId:guid}/xuat-excel` | Xuất báo cáo tuỳ biến ra Excel. | `BaoCaoXuat` |
| GET | `/api/v1/nhap-xuat/bao-cao-tuy-bien/{bieuMauId:guid}/xuat-pdf` | Xuất báo cáo tuỳ biến ra PDF. | `BaoCaoXuat` |
| GET | `/api/v1/nhap-xuat/danh-muc/mau` | /// Chan xuat khi quan tri vien khong cho phep dinh dang do. /// /// Truoc day o "Dinh dang cho phep xuat" duoc luu nhung khong nhanh code nao doc: bo | `DanhMucSua` |
| POST | `/api/v1/nhap-xuat/danh-muc` | /// Nhập danh mục từ Excel. Mặc định chayThu=true để xem trước kết quả rồi mới ghi. /// | `DanhMucSua` |
| POST | `/api/v1/nhap-xuat/bieu-mau/quet-placeholder` | /// Quét placeholder {{ tenBien }} trong tệp mẫu .docx để cấu hình ánh xạ trường. /// Chỉ đọc, không lưu tệp. /// | `DanhMucSua` |

## Tiêu chí động (chức năng 17–18)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/tieu-chi/{id:guid}` | Bộ tiêu chí đầy đủ (cây 2 cấp) để render màn hình cấu hình và chấm điểm. | `TieuChiXem` |
| POST | `/api/v1/tieu-chi/{id:guid}/kiem-tra` | Kiểm tra tổng trọng số, tổng điểm và khoảng điểm mức công nhận. | `TieuChiXem` |
| PUT | `/api/v1/tieu-chi/{id:guid}/cay` | Lưu toàn bộ cây nhóm/tiêu chí (kéo thả sắp xếp trên giao diện). | `TieuChiCauHinh` |
| PUT | `/api/v1/tieu-chi/{id:guid}/muc-cong-nhan` | Lưu danh sách mức công nhận theo khoảng điểm. | `TieuChiCauHinh` |

## Quyết định công nhận (chức năng 8, 31, 32, 36)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/quyet-dinh` | Danh sách quyết định đã ban hành. | `QuyetDinhXem` |
| GET | `/api/v1/quyet-dinh/{id:guid}` | Chi tiết quyết định kèm danh sách sáng kiến được công nhận. | `QuyetDinhXem` |
| GET | `/api/v1/quyet-dinh/ho-so-du-dieu-kien` | /// Sáng kiến đủ điều kiện đưa vào quyết định: đã có kết quả Đạt và chưa nằm trong quyết định nào. /// | `QuyetDinhBanHanh` |
| POST | `/api/v1/quyet-dinh/{id:guid}/cong-bo` | Chức năng 32 — Công bố kết quả hàng loạt cho toàn bộ sáng kiến trong quyết định. | `QuyetDinhBanHanh` |
| POST | `/api/v1/quyet-dinh/{id:guid}/ky-so` | /// Ký số một tệp đính kèm của quyết định. /// /// Bản gốc được GIỮ NGUYÊN, chữ ký lưu thành tệp riêng dạng PKCS#7 detached — tranh chấp /// về sau đố | `QuyetDinhKySo` |
| GET | `/api/v1/quyet-dinh/{id:guid}/lich-su-ky-so` | Lịch sử ký số của một quyết định. | `QuyetDinhXem` |
| GET | `/api/v1/quyet-dinh/xac-minh-chu-ky/{nhatKyKySoId:guid}` | Xác minh chữ ký của một lần ký — đối chiếu bản gốc với tệp chữ ký. | `QuyetDinhXem` |
| GET | `/api/v1/quyet-dinh/{id:guid}/xuat-pdf` | Xuất quyết định ra PDF theo mẫu văn bản hành chính. | `QuyetDinhXem` |

## Hồ sơ sáng kiến (chức năng 22–32)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/sang-kien` | Chức năng 28 — Danh sách hồ sơ với bộ lọc đa tiêu chí. | `SangKienXem` |
| GET | `/api/v1/sang-kien/goi-y` | Chức năng 37 — Gợi ý từ khoá khi gõ ở ô tìm kiếm. | `SangKienXem` |
| GET | `/api/v1/sang-kien/tim-ngu-nghia` | /// Chức năng 26, 37 — Tìm kiếm ngữ nghĩa. /// /// Khác tìm theo từ khoá: câu hỏi "giải pháp tiết kiệm điện ở trường học" vẫn tìm ra sáng kiến /// đặt | `SangKienXem` |
| GET | `/api/v1/sang-kien/cua-toi` | Chức năng 23 — Hồ sơ của tôi. | `SangKienXem` |
| GET | `/api/v1/sang-kien/{id:guid}` | Chi tiết hồ sơ kèm checklist thành phần và tệp đính kèm. | `SangKienXem` |
| GET | `/api/v1/sang-kien/{id:guid}/tien-do` | Chức năng 30 — Timeline tiến độ xử lý. | `SangKienXem` |
| GET | `/api/v1/sang-kien/{id:guid}/lich-su` | Chức năng 23 — Lịch sử chỉnh sửa (diff giá trị trước/sau). | `SangKienXem` |
| GET | `/api/v1/sang-kien/{id:guid}/hanh-dong` | Chức năng 29 — Danh sách hành động khả dụng (frontend render nút động). | `SangKienXem` |
| GET | `/api/v1/sang-kien/{id:guid}/trung-lap` | Chức năng 26 — Kết quả kiểm tra trùng lặp gần nhất. | `TrungLapXem` |
| POST | `/api/v1/sang-kien/{id:guid}/trung-lap/xem-xet` | /// Chức năng 26 — Đánh dấu "Đã xem xét" kết quả trùng lặp và ghi ý kiến hội đồng. /// /// Kết quả AI chỉ là cảnh báo; kết luận cuối cùng thuộc hội đồ | `TrungLapXemXet` |
| GET | `/api/v1/sang-kien/{id:guid}/trung-lap/xuat-pdf` | Chức năng 26 — Xuất báo cáo kiểm tra trùng lặp ra PDF. | `TrungLapXem` |
| POST | `/api/v1/sang-kien/{id:guid}/trung-lap/chay-lai` | Chạy lại kiểm tra trùng lặp thủ công. | `TrungLapChayLai` |
| POST | `/api/v1/sang-kien` | Chức năng 22 — Tạo hồ sơ nháp. | `SangKienThem` |
| PUT | `/api/v1/sang-kien/{id:guid}` | Cập nhật hồ sơ (chỉ khi ở trạng thái Nháp hoặc Yêu cầu bổ sung). | `SangKienSua` |
| POST | `/api/v1/sang-kien/{id:guid}/nop` | Chức năng 22 — Nộp hồ sơ chính thức, khởi tạo quy trình xử lý. | `SangKienNop` |
| GET | `/api/v1/sang-kien/{id:guid}/phieu-tiep-nhan` | /// Chức năng 22 — Phiếu tiếp nhận hồ sơ (PDF) để tác giả in làm bằng chứng đã nộp. /// /// Dùng bố cục do quản trị viên cấu hình ở biểu mẫu loại PHIE | `SangKienXem` |
| POST | `/api/v1/sang-kien/{id:guid}/rut` | Chức năng 23 — Rút hồ sơ (chỉ khi chưa vào bước chấm điểm). | `SangKienRut` |
| GET | `/api/v1/sang-kien/xuat-excel` | Xuất danh sách hồ sơ ra Excel theo bộ lọc hiện tại. | `SangKienXuat` |

## Tiếp nhận và xử lý (chức năng 27–30)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/xu-ly/tac-nhan-buoc/{sangKienId:guid}` | /// Chức năng 15, 29 — Danh sách người có thể xử lý bước hiện tại của hồ sơ. /// /// Dùng cho ô chọn "xử lý thay cho ai" khi bước cho phép uỷ quyền. / | `XuLyThucThi` |
| POST | `/api/v1/xu-ly/thuc-thi` | Thực thi một bước xử lý trên hồ sơ. | `XuLyThucThi` |
| POST | `/api/v1/xu-ly/thuc-thi-hang-loat` | Xử lý hàng loạt nhiều hồ sơ cùng bước. | `XuLyThucThi` |
| POST | `/api/v1/xu-ly/thu-hoi` | Thu hồi bước đã xử lý (nếu bước cho phép). | `XuLyThuHoi` |

## Theo dõi sao lưu

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/sao-luu` | Quá mốc này mà chưa có bản sao lưu mới thì coi là bất thường. | — |

## Tệp tin đính kèm (chức năng 25)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| POST | `/api/v1/tep-tin/tai-len` | /// "Magic number" của các định dạng được phép — KHÔNG tin phần mở rộng do client gửi /// (yêu cầu Mục 5 — chức năng 25). /// | — |
| GET | `/api/v1/tep-tin/{id:guid}/lien-ket-tai-xuong` | /// Sinh liên kết tải xuống có thời hạn cho một tệp. /// /// Với kho MinIO đây là presigned URL: trình duyệt tải thẳng từ kho đối tượng, luồng tệp /// | — |
| GET | `/api/v1/tep-tin/tai-xuong` | /// Tải tệp bằng liên kết đã ký — dùng cho kho đĩa cục bộ. /// /// Không đòi đăng nhập vì liên kết tự mang chữ ký và thời hạn: đó chính là điểm của li | — |
| GET | `/api/v1/tep-tin/{id:guid}/xem-truoc` | /// Xem trước tệp ngay trong trình duyệt (không tải về). /// /// Chỉ mở inline các định dạng trình duyệt tự dựng được và KHÔNG chạy mã của tệp: PDF và | — |

## Liên thông hệ thống ngoài (chức năng 16, 41)

| Phương thức | Đường dẫn | Mô tả | Quyền |
|---|---|---|---|
| GET | `/api/v1/tich-hop/he-thong` | Danh sách hệ thống liên thông. Bí mật chỉ báo "đã đặt", không trả giá trị. | `TichHopCauHinh` |
| GET | `/api/v1/tich-hop/xem-truoc` | Xem trước dữ liệu sẽ đẩy đi — không gửi gì cả. | `TichHopDongBo` |
| POST | `/api/v1/tich-hop/he-thong/{id:guid}/dong-bo` | Đẩy danh sách sáng kiến đã công bố sang hệ thống ngoài. | `TichHopDongBo` |
| POST | `/api/v1/tich-hop/he-thong/{id:guid}/thu-ket-noi` | /// Thử kết nối tới hệ thống ngoài mà KHÔNG gửi dữ liệu nghiệp vụ. /// /// Gửi một gói rỗng có cờ laThuKetNoi: đủ để biết endpoint sống, khoá đúng và  | `TichHopCauHinh` |
| POST | `/api/v1/tich-hop/nhat-ky-dong-bo/{nhatKyId:guid}/gui-lai` | Gửi lại một lần đồng bộ đã thất bại. | `TichHopDongBo` |

## Cách sinh lại tài liệu này

```bash
# Bản OpenAPI đầy đủ (kèm schema request/response)
curl -s http://localhost:8080/swagger/v1/swagger.json -o docs/openapi.json
```

Bảng trên quét trực tiếp thuộc tính `[Http*]`, `[Route]`, `[Authorize(Policy = ...)]` và khối
`<summary>` trong mã nguồn controller, nên khi thêm endpoint mới chỉ cần chạy lại bước quét là
tài liệu khớp trở lại.
