# Nhật ký thay đổi

Định dạng theo [Keep a Changelog](https://keepachangelog.com/vi/1.1.0/).

---

## [1.6.0] — 2026-08-20

Rà soát lại theo đúng chiều của bản 1.5.0 — **cấu hình khai báo được nhưng không dòng logic nào
đọc tới** — lần này quét riêng khối hội đồng. Ba thiếu sót được xử lý.

### Sửa

- **Chức năng 19 — phiếu kín không kín.** Ô tick *Phiếu kín* được lưu xuống CSDL nhưng
  `GET /api/v1/hoi-dong/phien-hop/{id}` vẫn trả nguyên danh sách phiếu kèm `thanhVienId` và ghi
  chú cho **mọi người có quyền `HOI_DONG.XEM`**. Giao diện không vẽ ra, nhưng gọi thẳng API là
  đọc được ai bỏ phiếu gì. Nay máy chủ xoá danh tính và ghi chú khỏi các lá phiếu kín trước khi
  trả về; chính chủ vẫn thấy lại phiếu của mình, số liệu kiểm phiếu tổng hợp không đổi.
- **Chức năng 20 — hai ô tick quyền thành viên không có tác dụng.** `QuyenNhanXet` và
  `QuyenKetLuan` sửa được trên bảng thành viên nhưng không nơi nào kiểm (trong khi `QuyenBoPhieu`
  và `QuyenChamDiem` thì có). Nay ghi ý kiến cho hồ sơ trong phiên cần *Nhận xét*, chốt kết quả
  xét của hồ sơ và kết thúc phiên cần *Kết luận*. Chỉ so sánh trên trường thực sự thay đổi, nên
  người chỉ có quyền nhận xét vẫn sửa được ý kiến của mình mà không vướng ô kết quả.
  Người **không phải thành viên** hội đồng (quản trị viên, thư ký hệ thống nhập hộ) vẫn đi tiếp
  bằng quyền vai trò như trước — hai ô tick chỉ ràng buộc người đang ngồi trong hội đồng.
- Giao diện phòng họp mờ nút theo đúng hai lớp quyền trên để không bấm rồi mới ăn lỗi. Đây là
  cải thiện thao tác; chặn thật vẫn nằm ở máy chủ.

### Chức năng 13 — màn hình thành phần hồ sơ nay dùng đúng API riêng của nó

Năm endpoint CRUD riêng từng thành phần (`POST/PUT/DELETE /quy-trinh/{id}/thanh-phan-ho-so` và
`PUT .../sap-xep`) đã có từ trước nhưng **không màn hình nào gọi** — đây là năm endpoint duy nhất
trong toàn bộ API không có lời gọi từ giao diện. Màn hình vẫn lưu bằng `PUT /so-do`, tức gửi lại
cả sơ đồ quy trình, nên hai người cùng mở một quy trình (một người sửa bước, một người thêm thành
phần) sẽ ghi đè lên nhau — đúng thứ mà tài liệu ghi là đã tránh được.

- Bấm Lưu nay chỉ gửi **những dòng thực sự đổi**: thêm dòng nào thì POST dòng đó, sửa dòng nào thì
  PUT dòng đó, xoá dòng nào thì DELETE dòng đó.
- Khối cảnh báo nói rõ sắp gửi gì ("1 dòng thêm mới, 2 dòng đã sửa, đổi thứ tự"), mỗi dòng có nhãn
  *Mới* / *Đã sửa*; máy chủ từ chối dòng nào thì báo đúng tên dòng đó thay vì một câu lỗi chung.
- **Đổi thứ tự thành phần** bằng nút lên/xuống — trước đây không có cách nào đổi, thứ tự này chính
  là thứ tự checklist hiện ra cho tác giả lúc nộp hồ sơ.

### Kiểm thử

- `ThanhPhanVaBoNhoDemTests.Sap_Xep_Thanh_Phan_Doi_Thu_Tu_Checklist`
- `ThanhPhanVaBoNhoDemTests.Sap_Xep_Thanh_Phan_Doi_Hoi_Quyen_Cau_Hinh_Quy_Trinh`
- 4 test E2E `REQ-13: Thành phần hồ sơ` — trong đó một test khẳng định lúc Lưu có
  `POST .../thanh-phan-ho-so` và **không** có `PUT .../so-do`
- `HoiDongTests.Phieu_Kin_Khong_Lo_Danh_Tinh_Nguoi_Bo_Phieu`
- `HoiDongTests.Thanh_Vien_Bi_Tat_Quyen_Ket_Luan_Khong_Ket_Thuc_Duoc_Phien`
- `HoiDongTests.Thanh_Vien_Bi_Tat_Quyen_Nhan_Xet_Khong_Ghi_Y_Kien_Duoc`

227/227 kiểm thử tích hợp, 512/512 kiểm thử đơn vị, 405/405 E2E đều đạt.

---

## [1.7.0] — 2026-08-20

Dọn nốt nhóm **cấu hình bật/tắt được nhưng không có tác dụng** (TD-011). Nguyên tắc áp dụng cho
từng cờ: hoặc nối vào hành vi thật, hoặc bỏ khỏi giao diện và API — không để tồn tại một ô tick
mà bật hay tắt cũng như nhau.

### Nối vào hành vi thật

- **Chức năng 14 — trạng thái kết thúc.** Trước đây máy chạy quy trình chỉ nhìn cờ *bước* kết
  thúc; tick "là trạng thái kết thúc" trên một trạng thái không đổi gì cả. Nay trường hợp nào gán
  trạng thái đó cho hồ sơ thì hồ sơ dừng hẳn: không sang bước kế tiếp, không đặt hạn xử lý mới,
  ghi ngày hoàn thành. Trạng thái không tick thì đi tiếp y như cũ (có test chặn hồi quy).
- **Chức năng 5 — đơn vị phê duyệt.** Máy chủ từ chối khai cấp phê duyệt trỏ vào đơn vị chưa bật
  ô *Là đơn vị phê duyệt* (422), và ô chọn trên màn hình chỉ liệt kê đơn vị đã bật — không bày ra
  những lựa chọn chắc chắn bị từ chối.
- **Chức năng 18 — tự động tổng hợp điểm.** Người cuối cùng trong danh sách phân công gửi phiếu
  là hệ thống tổng hợp ngay, thư ký không phải bấm nút nữa; điều kiện chuyển bước theo tổng điểm
  nhờ đó dùng được luôn. Bản tổng hợp tự động **không** ghi "người kết luận" — máy làm thay, không
  phải ai đó quyết định. Hai công tắc *Tự động tổng hợp* và *Loại điểm cao nhất/thấp nhất* nay có
  trên form tạo bộ tiêu chí; riêng cái sau vốn có tác dụng thật trong công thức tính nhưng không
  có chỗ đặt, nên bộ tiêu chí tạo từ màn hình luôn bị đưa về mặc định.
- **Chức năng 48 — mở tab mới.** Mục menu khai "mở tab mới" nay mở thật ở tab mới.

### Bỏ khỏi giao diện và API

- **Đồng bộ hai chiều** (cấu hình liên thông theo bước): chiều nhận về đòi một đường GHI từ hệ
  thống ngoài vào, mà `/api/public/v1` cố ý chỉ cho ĐỌC. Mở đường ghi phải có đặc tả thật của IOC
  / Thi đua khen thưởng (REQ-41 đang chờ bên thành phố). Ô chọn cũ hứa một hành vi không tồn tại
  nên đã gỡ; cột trong CSDL giữ lại kèm chú thích, không phải viết migration.
- **Cho phép chấm độc lập** (bộ tiêu chí): hội đồng luôn chấm độc lập — điểm từng người chỉ lộ ra
  sau khi gửi phiếu — nên vị trí "tắt" không có nghĩa gì để hiện thực. Đã bỏ khỏi API.

### Kiểm thử

- `BoMayQuyTrinhTests.Trang_Thai_Ket_Thuc_Dung_Ho_So_Lai_Du_Con_Buoc_Ke_Tiep`
- `BoMayQuyTrinhTests.Trang_Thai_Thuong_Van_Cho_Ho_So_Di_Tiep`
- `BienBanVaCauHinhTests.Don_Vi_Chua_Danh_Dau_Phe_Duyet_Khong_Khai_Lam_Cap_Xet_Duoc`
- `LuongNghiepVuTests` — khẳng định điểm đã lên hồ sơ **trước** khi có ai bấm "Tổng hợp điểm"

228/228 kiểm thử tích hợp, 514/514 kiểm thử đơn vị, 405/405 E2E đều đạt.

---

## [1.5.0] — 2026-08-19

Rà soát độc lập lại 51 chức năng theo một chiều khác: **cấu hình nào khai báo được trên giao diện
nhưng không một dòng logic nào đọc tới**, và **dịch vụ nào có mã nhưng không có endpoint hay lối
vào**. Cách quét này bắt được nhóm thiếu sót nguy hiểm nhất khi nghiệm thu — quản trị viên bật một
tuỳ chọn, hệ thống báo lưu thành công, nhưng hành vi không đổi.

### Cấu hình có khai mà không có tác dụng — nay đã có

- **Chức năng 13** — cờ *dùng để kiểm tra trùng lặp* của từng thành phần hồ sơ: pipeline trước đây
  gom **toàn bộ** nội dung và tệp, bỏ qua cờ này. Nay chỉ thành phần được tick mới đi vào so khớp,
  nên phụ lục và biểu mẫu hành chính giống nhau giữa mọi hồ sơ không còn đẩy tỷ lệ trùng lên.
- **Chức năng 14** — cờ *hiển thị cho tác giả* của trạng thái bước: nay tác giả chỉ thấy nhãn
  trung tính "Đang xử lý" với những trạng thái tắt cờ, và tiến độ ẩn ý kiến nội bộ của bước đó.
  Máy chủ **không trả dữ liệu đó về**, không phải ẩn ở giao diện.
- **Chức năng 11** — *cảnh báo trước hạn (giờ)* khai theo từng bước: job nhắc hạn trước đây chỉ
  dùng cấu hình chung `SO_NGAY_NHAC_TRUOC_HAN`. Nay đọc ngưỡng của chính bước đó từ snapshot quy trình.
- **Chức năng 5** — bảng `cau_hinh_cap_phe_duyet` trước đây chỉ có CRUD, không logic nào đọc. Nay
  sinh ra 4 biến ngữ cảnh (`so_cap_phe_duyet`, `cap_phe_duyet_hien_tai`, `con_cap_phe_duyet_cao_hon`,
  `don_vi_phe_duyet_ke_tiep`) để khai nhánh "Chuyển cấp cao hơn" bằng **điều kiện cấu hình** thay vì sửa mã.

### Chức năng có mã nhưng không có lối vào — nay đã nối

- **Chức năng 26** — *Đánh dấu đã xem xét* kèm ý kiến hội đồng: hàm xử lý đã tồn tại nhưng không có
  endpoint, không có nút, không có kiểm thử. Nay có `POST /api/v1/sang-kien/{id}/trung-lap/xem-xet`
  (quyền mới `TRUNG_LAP.XEM_XET`, ghi nhật ký ai/khi nào) và khối ghi ý kiến trên tab Trùng lặp.
- **Chức năng 29** — *uỷ quyền xử lý bước*: máy chạy quy trình đã kiểm tra cờ `cho_phep_uy_quyen`
  và API đã nhận `nguoiUyQuyenId`, nhưng không màn hình nào cho chọn. Nay hộp thoại xử lý có ô
  "Xử lý thay cho" lấy từ `GET /api/v1/xu-ly/tac-nhan-buoc/{id}`, và **máy chủ chặn** uỷ quyền cho
  người không phải tác nhân của bước.
- **Chức năng 21** — thêm lối tắt tới trang *Bảo mật tài khoản* (bật MFA, mã khôi phục) ngay trong
  menu người dùng, cạnh *Đổi mật khẩu*. Mục này vốn đã có ở menu bên trái (nạp từ `cau_hinh_menu`)
  nên đây là cải thiện thao tác, không phải bù chức năng thiếu.

### Bổ sung còn thiếu so với đặc tả

- **Chức năng 26** — xuất báo cáo trùng lặp ra **PDF**, in cả trích dẫn đoạn trùng để đính kèm hồ sơ
  hội đồng; một con số phần trăm không tự bảo vệ được trước tác giả bị kết luận.
- **Chức năng 39, 40** — xuất **PDF** cho báo cáo chưa đạt, theo đơn vị, theo tác giả, thời gian xử
  lý. Trước đây nút *Xuất PDF* ở mọi tab đều tải nhầm **danh sách sáng kiến đạt**.
- **Chức năng 35** — xuất phiếu chấm bản **Word (.docx)** để thư ký biên tập trước khi đóng hồ sơ.
- **Chức năng 49** — ký **XAdES-BES** cho tệp XML; đồng thời sửa lỗi **xác minh bản PAdES luôn báo
  "không có chữ ký"** (bản PDF đã ký bị đọc nhầm như một khối CMS tách rời).
- **Chức năng 21, 43** — `PUT /api/v1/xac-thuc/toi` và trang *Thông tin cá nhân*: người dùng tự sửa
  họ tên, email, điện thoại, chức vụ, ngày sinh. Đơn vị và vai trò vẫn chỉ quản trị viên đổi được —
  cho tự đổi là mở đường leo thang đặc quyền.
- **Nhóm X** — `POST /api/v1/bao-cao/{loai}/xuat-nen`: báo cáo lớn chạy ở tiến trình nền rồi gửi
  thông báo kèm liên kết tải về, thay vì giữ người dùng chờ trên một request dễ time-out.

### Hạ tầng và tài liệu

- **WAL archiving bật sẵn** trong `docker-compose.prod.yml` (đẩy WAL mỗi 5 phút) — trước đây chỉ có
  bản dump hằng ngày nên điểm khôi phục gần nhất là 1h sáng hôm trước. Kèm quy trình khôi phục theo
  thời điểm (PITR) trong tài liệu vận hành.
- **`docs/API.md`** sinh từ mã nguồn (174 endpoint), **`docs/KE-HOACH-CONG-TAC.md`**,
  **`docs/DEPLOYMENT.md`** — ba tài liệu bàn giao đặc tả yêu cầu nhưng chưa có.
- **`docs/KICH-BAN-NGHIEM-THU.md`**: bổ sung 35 kịch bản, phủ đủ **51/51** chức năng (trước đây
  thiếu hẳn dòng cho 13 chức năng: 2, 7, 8, 11, 12, 14, 31, 36, 42, 46, 47, 50, 51).
- Đính chính ghi chú "dự án dùng antd Form thay vì react-hook-form + zod" — sai: toàn bộ 30 màn hình
  có biểu mẫu đều đi qua `useBieuMau` (react-hook-form + zodResolver), không tệp nào gọi thẳng `<Form>`.

### Kiểm thử

- Unit: 512 (thêm XAdES ký/xác minh/phát hiện sửa nội dung, xuất Word, xuất PDF trùng lặp).
- Integration trên PostgreSQL thật: 222 (thêm 17 cho các luồng trên).
- E2E Playwright: 401 (thêm 21).

---

## [1.4.0] — 2026-08-18

Quét lại theo một chiều chưa từng kiểm: **bảng cơ sở dữ liệu nào không một dòng code nào chạm
tới**, rồi đối chiếu ngược với đặc tả. Cách này bắt được nhóm thiếu sót mà quét từ phía API không
bao giờ thấy — thiết kế CSDL có bảng nhưng chưa ai làm nghiệp vụ. Bản này làm nốt 7 hạng mục đó.

### Biên bản họp hội đồng (nhóm IV)

- Hai bảng `bien_ban_hop` và `bien_ban_chu_ky` trước đây **không có dịch vụ, API hay giao diện
  nào** — thành viên có checkbox quyền *Ký biên bản* nhưng không có biên bản nào để ký.
- Thêm `DichVuBienBanHop`: biên bản **sinh từ dữ liệu có thật** của phiên họp (điểm danh, phiếu
  bầu, kết luận) và chụp lại thành JSON, không phải form nhập tay — biên bản là căn cứ hành chính,
  gõ tay lại số liệu là mở đường cho sai lệch với dữ liệu hệ thống.
- API: lập/làm mới biên bản, xem theo phiên, ký nhận theo chức danh, xuất PDF theo mẫu văn bản
  hành chính, ký số và xem lịch sử ký số.
- Giao diện: tab **Biên bản** trong màn hình điều hành phiên họp — trạng thái, số chữ ký, bảng kết
  quả từng hồ sơ, bảng chữ ký, các nút xuất PDF / ký nhận / ký số / lập lại.
- Chỉ thành viên của chính hội đồng đó và có quyền ký biên bản mới ký được; biên bản đã đủ chữ ký
  thì không lập lại được.

### Cấu hình thành phần hồ sơ (chức năng 13)

- Trình thiết kế trước đây chỉ **hiển thị con số** "n thành phần hồ sơ" — không sửa được, dù API
  lưu sơ đồ vẫn nhận trường này. Thêm màn hình `/quan-tri/quy-trinh/:id/thanh-phan` sửa đầy đủ:
  mã, tên, bắt buộc, kiểu dữ liệu, định dạng cho phép, dung lượng, số tệp, số ký tự tối thiểu và
  cờ dùng cho kiểm tra trùng lặp.
- Cảnh báo ngay khi mã bị trùng — mã là khoá lưu dữ liệu nộp nên trùng là hỏng dữ liệu.

### Liên thông theo bước quy trình (chức năng 16)

- Bảng `quy_trinh_lien_thong` chưa được dùng: hệ thống mới chỉ khai báo được *hệ thống ngoài*, chưa
  quyết định được **khi nào** gọi. Thêm API và màn hình `/quan-tri/quy-trinh/:id/lien-thong` gắn
  hệ thống vào từng bước theo sự kiện (vào bước / hoàn thành / được phê duyệt).
- Máy chủ chặn gắn bước của quy trình khác — sai chỗ này thì luồng đồng bộ không bao giờ chạy mà
  không ai hiểu tại sao.

### Cấp phê duyệt theo đợt / lĩnh vực (chức năng 5)

- Bảng `cau_hinh_cap_phe_duyet` chưa được dùng. Thêm API và tab **Cấp phê duyệt** trong Danh mục:
  khai báo thứ tự các cấp xét cho từng phạm vi, bỏ trống đợt/lĩnh vực nghĩa là áp dụng chung.
- Chặn trùng thứ tự cấp trong cùng phạm vi — hai đơn vị cùng cấp thì không biết hồ sơ qua ai trước.

### Nhật ký lỗi

- Bảng `nhat_ky_loi` chưa từng được ghi. Middleware xử lý lỗi nay ghi lại lỗi **5xx** (kèm stack
  trace, đường dẫn, người dùng, IP); lỗi nghiệp vụ 4xx không ghi vì đó là hành vi bình thường và
  chỉ làm nhiễu.
- Việc ghi nhật ký được bọc try/catch riêng: mất kết nối CSDL lúc ghi log không được làm hỏng phản
  hồi lỗi gửi về người dùng.
- Thêm tab **Lỗi hệ thống** trong Nhật ký: lọc theo mức độ và trạng thái xử lý, mở rộng dòng để xem
  stack trace, đánh dấu đã xử lý.

### Thông báo realtime (SignalR)

- Hub `/hubs/thong-bao` đã có từ trước nhưng **web không cài `@microsoft/signalr`** — không ai kết
  nối. Nay web kết nối và nhận tín hiệu, chuông cập nhật tức thì thay vì chờ nhịp 60 giây.
- Tầng Application/Infrastructure chỉ biết giao diện `IBoDayRealtime`, không phụ thuộc SignalR —
  đổi công nghệ realtime về sau chỉ phải thay bản cài đặt ở tầng Api.
- Máy chủ chỉ đẩy **tín hiệu**, không kèm nội dung nghiệp vụ: client nhận tín hiệu rồi gọi API lấy
  dữ liệu theo đúng quyền của mình, nên không có đường nào rò dữ liệu qua kênh realtime.
- Vẫn giữ nhịp hỏi lại 60 giây làm lưới an toàn khi realtime rớt kết nối.

### Xuất sơ đồ quy trình

- Nút **Xuất PNG** trong trình thiết kế: chụp đúng khung canvas, ẩn phần điều khiển của ReactFlow
  để ảnh đưa vào hồ sơ trình ký chỉ còn sơ đồ.

### Dọn dẹp

- Gỡ `i18next`, `react-i18next`, `react-hook-form` — cài trong `package.json` nhưng không dùng ở
  bất kỳ tệp nào.

### Kiểm thử

- Thêm `BienBanVaCauHinhTests` (8 test): chặn lập biên bản khi phiên chưa kết thúc, lập biên bản
  sinh đúng số liệu và xuất được PDF, chặn người ngoài hội đồng ký, chặn gắn liên thông vào bước
  của quy trình khác, vòng lưu–đọc liên thông, chặn trùng thứ tự cấp phê duyệt, đọc nhật ký lỗi và
  chặn tác giả xem nhật ký lỗi.
- Tổng: 267 unit test + 85 integration test, tất cả đều pass.

---

## [1.3.0] — 2026-08-18

Rà soát lại toàn bộ 206 endpoint và đối chiếu với những gì màn hình thực sự gọi. Đợt trước chỉ tìm
theo đường dẫn nên bỏ sót nhiều chỗ; lần này đối chiếu theo **tên hàm trong lớp API client**, phát
hiện 12 hàm khai báo mà không màn hình nào dùng. Bản này nối hết chúng vào giao diện.

### Ký số văn bản (chức năng 49)

- Màn hình **Quản trị → Chữ ký số**: khai báo nhà cung cấp (Ban Cơ yếu, VNPT-CA, Viettel-CA…),
  hình thức ký (USB token / HSM / ký từ xa / SmartCA), serial chứng thư, thuật toán. Có thẻ trạng
  thái cho biết hệ thống đã sẵn sàng ký hay còn thiếu gì.
- Bổ sung API CRUD cho bảng `cau_hinh_chu_ky_so` — trước đây bảng này không có endpoint nào, chứng
  thư chỉ chèn được thẳng vào cơ sở dữ liệu.
- Trang **Quyết định**: thêm ô tải tệp văn bản quyết định (đối tượng được ký), nút **Ký số**, nút
  **Lịch sử ký số** kèm **Xác minh chữ ký** từng lần ký và liên kết tải bản gốc / tệp chữ ký.
- `QuyetDinhDto` và `LuuQuyetDinhDto` bổ sung `TepTinId`: trước đây cột `tep_tin_id` của quyết định
  không có đường nào ghi vào, nên nút ký số sẽ không có tệp để ký.
- Khoá bí mật vẫn **không** nằm trong cơ sở dữ liệu: đường dẫn PFX và mật khẩu đọc từ biến môi
  trường `KYSO_PFX` / `KYSO_MAT_KHAU_PFX`, đã thêm vào cả hai tệp compose.

### Thông báo trong ứng dụng

- Chuông trên thanh trên cùng trước đây chỉ hiện badge đếm — **bấm vào không mở gì**. Nay mở ngăn
  kéo danh sách 20 thông báo gần nhất, đánh dấu đã đọc khi bấm vào, mở thẳng đối tượng liên quan
  nếu thông báo có đường dẫn, kèm nút **Đọc tất cả**.
- **Sửa lỗ hổng IDOR**: endpoint đánh dấu đã đọc chỉ tìm theo Id nên bất kỳ ai đăng nhập cũng đánh
  dấu được thông báo của người khác, và thông báo lỗi còn để lộ Id nào có thật. Nay truy vấn lọc
  luôn theo người nhận. Bổ sung endpoint đánh dấu đã đọc tất cả.

### Hội đồng và chấm điểm (chức năng 32, 33, 35)

- Tab **Ma trận điểm** trong trang hội đồng: hàng là hồ sơ, cột là thành viên, điểm chỉ hiện sau khi
  phiếu đã gửi. Thư ký bấm **Mở lại** ngay trên ô để mở khoá phiếu cho thành viên sửa — trước đây
  tài liệu hướng dẫn có mô tả nút này nhưng giao diện không hề có.
- `ODiemMaTran` bổ sung `PhieuId` để thao tác mở lại phiếu có đối tượng cụ thể.
- Trang chi tiết hồ sơ thêm khối **Nghiệp vụ hội đồng**: phân công chấm điểm (chọn hội đồng, thành
  viên, hạn hoàn thành, chia đều), tổng hợp điểm (hiện bảng kết quả kèm cảnh báo), thu hồi bước xử
  lý (bắt buộc nhập lý do).

### Đợt đề nghị, quy trình, tiêu chí, tra cứu, danh mục

- **Đợt đề nghị** tách thành màn hình riêng trong tab Danh mục: bảng hiện trạng thái vòng đời và
  cảnh báo thiếu quy trình / bộ tiêu chí, có nút **Mở / Đóng / Khoá / Sao chép** và form thêm–sửa
  đầy đủ (hạn nộp, hạn chấm, quy trình, bộ tiêu chí, đơn vị áp dụng). Bổ sung endpoint
  `GET /danh-muc/dot-de-nghi/quan-ly` vì danh sách danh mục chung không trả trạng thái vòng đời.
- **Quy trình**: thêm nút *Sao chép thành quy trình mới*, phân biệt rõ với *Tạo phiên bản mới*.
- **Bộ tiêu chí**: khối **Mức công nhận theo khoảng điểm** sửa trực tiếp trên màn hình.
- **Tra cứu**: khối *Tìm theo ý nghĩa* trả kết quả kèm độ tương đồng và đoạn khớp nhất, tách riêng
  khỏi bảng kết quả tìm từ khoá.
- **Danh mục lĩnh vực**: đổi thứ tự bằng nút lên/xuống (không dùng kéo–thả để thao tác được cả trên
  điện thoại), lưu ngay sau mỗi lần đổi.

### Sửa lỗi

- **Trang chủ báo lỗi đỏ với vai trò không có quyền xem báo cáo.** Dashboard luôn gọi API tổng quan,
  mà API đó đòi `BAO_CAO.XEM`; thành viên hội đồng và tác giả không có quyền nên vừa đăng nhập đã
  thấy "Bạn không có quyền thực hiện chức năng này". Nay giao diện kiểm tra quyền trước: không có
  thì **không gọi API** và hiển thị trang chủ rút gọn gồm các lối tắt lọc theo đúng quyền của người
  dùng (Việc đánh giá, Hội đồng, Việc cần xử lý, Hồ sơ của tôi, Tra cứu…). Phía máy chủ giữ nguyên
  kiểm tra quyền — gọi thẳng API vẫn trả 403.
- Bốn bảng mới bị bóp cột do có `scroll.x` nhỏ hơn tổng bề rộng các cột: đặt bề rộng cho mọi cột và
  nâng `scroll.x` cho khớp.

### Kiểm thử

- Thêm kiểm thử: quyết định lưu và trả về tệp văn bản; cấu hình chữ ký số không trả bí mật và chỉ
  giữ một cấu hình mặc định; không đánh dấu được thông báo của người khác; đọc tất cả chỉ ảnh hưởng
  thông báo của chính mình.
- Tổng: 267 unit test + 76 integration test, tất cả đều pass.

---

## [1.2.0] — 2026-08-18

Hoàn thiện phần giao diện cho các chức năng trước đây mới có API, và chốt phương án cho chức năng
ứng dụng di động. Sau bản này cả 51 chức năng đều có đủ API + nghiệp vụ + giao diện.

### Hội đồng sáng kiến (chức năng 19–20)

- Màn hình `/hoi-dong`: danh sách, thành lập, sửa, xoá hội đồng kèm cấp xét duyệt, đợt, lĩnh vực
  phụ trách, số thành viên tối thiểu và tỷ lệ thông qua.
- Trang chi tiết có ba tab: Thông tin chung, Thành viên, Phiên họp.
- Tab **Thành viên** sửa trực tiếp trên bảng: chọn tài khoản có sẵn hoặc nhập tay người ngoài hệ
  thống, đặt chức danh và 5 nhóm quyền. Nút Lưu bị khoá khi danh sách chưa hợp lệ (không đúng một
  chủ tịch, hoặc ít hơn số thành viên tối thiểu) — chặn ngay trên giao diện thay vì để người dùng
  bấm rồi mới nhận lỗi từ máy chủ.
- Tab **Phiên họp**: tạo phiên kèm hồ sơ đưa ra xét, điểm danh, bỏ phiếu (đồng ý / không đồng ý /
  ý kiến khác, hỗ trợ phiếu kín), kiểm phiếu realtime so với ngưỡng thông qua của hội đồng, nhập
  kết luận và kết thúc phiên. Phiên đã kết thúc khoá toàn bộ thao tác bỏ phiếu và điểm danh.
- Nút **Xuất phiếu chấm** xuất PDF gộp toàn bộ phiếu chấm của hội đồng (chức năng 35) — trước đây
  endpoint đã có nhưng không màn hình nào gọi tới.

### Đăng nhập một lần SSO trên giao diện (chức năng 21, 41)

- Trang đăng nhập hỏi máy chủ `sso/trang-thai` và chỉ hiện nút **Đăng nhập một lần (SSO)** khi đã
  cấu hình nhà cung cấp — không dẫn người dùng vào một luồng chắc chắn lỗi.
- Trang `/dang-nhap/sso` nhận mã trả về, so `state` với giá trị đã lưu (chống CSRF), đổi mã lấy
  token rồi đưa người dùng về đúng trang họ định vào. Effect chạy hai lần trong StrictMode được
  chặn vì authorization code chỉ đổi được một lần.
- Đăng xuất kiểm tra phiên có phải đăng nhập bằng SSO không; nếu đúng thì lấy `end_session_endpoint`
  **trước** khi xoá token (endpoint này cần đăng nhập) rồi chuyển hướng sang nhà cung cấp.
- Bổ sung `SSO_ISSUER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET`, `SSO_SCOPE` vào `.env.example` và cả
  hai tệp compose — trước đây bản triển khai Docker không có chỗ nào để bật SSO.

### Liên thông hệ thống ngoài (chức năng 16, 41)

- Màn hình `/quan-tri/lien-thong`: khai báo hệ thống (mã, endpoint, kiểu xác thực, client id/secret,
  scope, tần suất), ánh xạ tên trường sang tên mà hệ thống ngoài yêu cầu.
- Ô bí mật để trống khi sửa = giữ nguyên giá trị đang lưu; giao diện chỉ hiển thị "Đã đặt bí mật"
  chứ không bao giờ nhận lại giá trị thật.
- **Xem trước dữ liệu** trước khi đẩy đi, chạy **đồng bộ** theo đợt/năm, và tab **Nhật ký đồng bộ**
  hiển thị số bản ghi thành công/thất bại kèm thông báo lỗi của hệ thống ngoài.

### Biểu mẫu xuất (chức năng 6)

- Thêm tab **Biểu mẫu xuất** vào màn hình Danh mục: CRUD biểu mẫu, tải tệp `.docx` mẫu và quét
  placeholder ngay khi tải lên, ánh xạ từng placeholder sang nguồn dữ liệu kèm kiểu và định dạng
  hiển thị.
- Đổi tệp mẫu giữ nguyên ánh xạ đã cấu hình cho các placeholder trùng tên.

### Bộ lọc yêu thích (chức năng 28)

- Thanh bộ lọc yêu thích dùng chung cho màn hình danh sách: chọn bộ lọc đã lưu, lưu bộ lọc hiện
  tại, đặt mặc định, xoá.
- Bộ lọc mặc định chỉ tự áp dụng khi mở màn hình mà URL **chưa** có tiêu chí nào — liên kết chia sẻ
  luôn thắng bộ lọc cá nhân.
- Máy chủ lưu tiêu chí dưới dạng đối tượng JSON còn màn hình làm việc bằng chuỗi query, nên thanh
  lọc chuyển đổi hai chiều; thêm tiêu chí lọc mới cho màn hình không phải sửa thanh lọc.

### Ứng dụng di động (chức năng 42)

- Chốt phương án **web responsive** thay cho ứng dụng đóng gói: giao diện chạy tốt từ 320px, thanh
  điều hướng chuyển thành Drawer, bảng cuộn ngang trong khung riêng. Ghi rõ giới hạn kèm theo
  (không có thông báo đẩy, không dùng ngoại tuyến) trong `TRANG-THAI-TRIEN-KHAI.md`.

### Sửa lỗi

- Bảng danh sách hồ sơ: cột "Tên sáng kiến" không đặt bề rộng nên khi tổng bề rộng các cột cố định
  vượt khung, phần dư còn lại âm và tên sáng kiến bị bóp thành một ký tự mỗi dòng.
- Menu điều hướng có sẵn mục "Hội đồng sáng kiến" trỏ tới `/hoi-dong` nhưng không có route tương
  ứng, bấm vào ra trang 404. Bổ sung mục "Liên thông hệ thống ngoài" vào nhóm Quản trị.

### Kiểm thử

- Thêm `HoiDongTests`: ràng buộc đúng một chủ tịch, số thành viên tối thiểu, luồng phiên họp đầy đủ
  (tạo phiên → điểm danh → bỏ phiếu → kiểm phiếu → kết luận → khoá bỏ phiếu), chặn người ngoài hội
  đồng bỏ phiếu, xuất phiếu chấm PDF.
- Thêm kiểm thử lưu bộ lọc trùng tên (ghi đè, giữ đúng một bộ lọc mặc định) và vòng lưu–đọc biểu
  mẫu xuất kèm tệp mẫu và bảng ánh xạ placeholder.
- Tổng: 267 unit test + 72 integration test, tất cả đều pass.

---

## [1.1.0] — 2026-08-17

Bổ sung bốn nhóm chức năng còn thiếu sau bản 1.0.0.

### Công việc nền (Hangfire)

- Hàng đợi job lưu trong chính PostgreSQL, chạy trong tiến trình API, tắt được hoàn toàn bằng
  `CongViecNen:BatHangfire=false`.
- Bốn công việc định kỳ: nhắc hạn xử lý và hạn chấm điểm (7h hằng ngày), tự đóng đợt đề nghị quá
  hạn nộp (mỗi giờ), rút hàng đợi email/SMS (mỗi 5 phút), quét bù kiểm tra trùng lặp (mỗi 15 phút).
  Biểu thức cron đọc từ cấu hình nên đổi được tần suất mà không phải build lại.
- Hai công việc theo sự kiện: trích xuất văn bản khi tải tệp, kiểm tra trùng lặp khi nộp hồ sơ.
- Dashboard `/hangfire` chỉ vai trò Quản trị hệ thống mở được — dashboard hiển thị tham số job
  (Id hồ sơ, địa chỉ email) nên không được để mở như trang tĩnh.
- Nhắc hạn chống trùng bằng chính bảng thông báo (không nhắc lại trong 20 giờ), không phải thêm
  cột trạng thái riêng.

### Gửi email và SMS thật (chức năng 50)

- Gửi email qua SMTP bằng MailKit, gửi SMS qua API nhà cung cấp; mật khẩu và API key giải mã
  AES-256-GCM khi dùng.
- Nội dung email gửi dạng `text/plain` chứ không phải HTML — mẫu thông báo do quản trị viên nhập
  nên gửi văn bản thuần loại bỏ hoàn toàn nguy cơ HTML injection.
- Bản tin lỗi được thử lại tối đa 5 lượt. Riêng trường hợp **chưa cấu hình** máy chủ gửi tin thì
  giữ nguyên trạng thái `CHO_GUI` và không tăng số lần thử, để khi quản trị viên cấu hình xong
  hàng đợi tự chạy tiếp thay vì đã cháy hết lượt thử.

### OCR nội bộ nối vào luồng nộp hồ sơ (chức năng 26)

- Tải tệp PDF/ảnh lên sẽ xếp lịch trích xuất văn bản qua dịch vụ Tesseract nội bộ, kết quả lưu vào
  `noi_dung_trich_xuat` để kiểm tra trùng lặp đọc được cả nội dung tệp scan.
- Thứ tự được bảo đảm: nộp hồ sơ chỉ chạy kiểm tra trùng lặp khi không còn tệp nào chờ OCR; tệp
  cuối cùng OCR xong sẽ tự đẩy sang kiểm tra trùng lặp; và một vòng quét định kỳ dọn nốt hồ sơ mắc
  kẹt khi OCR thất bại hẳn.
- Dịch vụ OCR chết hoặc quá thời gian chờ thì suy giảm mềm — hồ sơ vẫn nộp được bình thường.

### Ban hành quyết định và công bố kết quả (chức năng 8, 31, 32, 36)

- Màn hình ban hành quyết định: chọn sáng kiến đủ điều kiện theo đợt, sửa, xoá, xuất PDF theo mẫu
  văn bản hành chính.
- Ràng buộc nghiệp vụ: một sáng kiến chỉ nằm trong **đúng một** quyết định công nhận; quyết định
  đã ký số không sửa/xoá được; quyết định đã công bố kết quả không xoá được.
- Công bố kết quả hàng loạt: đánh dấu đã công bố, mở hiển thị công khai và gửi thông báo tới toàn
  bộ tác giả có tài khoản.

### Giao diện quản trị (chức năng 43, 44, 45, 47)

- Người dùng: thêm, sửa, gán vai trò, đặt lại mật khẩu. Mật khẩu tạm sinh bằng nguồn ngẫu nhiên
  mật mã và chỉ hiển thị đúng một lần.
- Đặt lại mật khẩu thu hồi toàn bộ refresh token đang mở của tài khoản đó.
- Chặn tự khoá mình ra khỏi hệ thống: không được bỏ quyền quản trị hoặc khoá tài khoản khi đó là
  quản trị viên đang hoạt động cuối cùng.
- Vai trò: ma trận phân quyền sửa trực tiếp trên bảng, chọn cả cột, gom thay đổi rồi lưu một lần.
  Vai trò hệ thống không đổi được mã vì mã được mã nguồn tham chiếu trực tiếp.
- Đơn vị: cây tổ chức kèm panel chi tiết, thêm đơn vị con, sửa, xoá; cấu hình tiêu đề văn bản và
  người ký mặc định của đơn vị.

### Sửa lỗi

- **Tệp trùng nội dung không được trích xuất văn bản**: điều kiện xếp lịch OCR dựa trên "tệp mới"
  thay vì trạng thái OCR, nên một tệp được dùng lại theo hash mà chưa từng trích xuất sẽ bị bỏ
  qua vĩnh viễn.
- **Hồ sơ mất ngày công nhận khi ban hành quyết định mới**: hàm gắn sáng kiến đọc ngày ban hành
  lại từ cơ sở dữ liệu, trong khi bản ghi quyết định mới chỉ đang nằm trong change tracker.
- **Trạng thái công bố bị mất với hồ sơ không có bản ghi hội đồng**: trạng thái công bố trước đây
  chỉ lưu ở `ket_qua_xet_duyet`, nhưng không phải hồ sơ nào cũng có bản ghi đó. Chuyển sang lưu
  trên chính hồ sơ.
- **Mã HTTP không đúng ngữ nghĩa**: đăng nhập sai và token hết hạn trả 400 thay vì 401; các lỗi
  xung đột trạng thái trả 400 thay vì 409. Đã gom lại theo đúng nhóm 401 / 403 / 404 / 409 / 422.

### Thay đổi khác

- Ngưỡng rate limit đọc từ cấu hình `GioiHanTruyCap:*` thay vì cố định trong mã nguồn.
- Thêm cột `da_cong_bo_ket_qua` và `ngay_cong_bo_ket_qua` cho bảng `sang_kien` (migration
  `ThemTrangThaiCongBoKetQua`).

---

## [1.0.0] — 2026-08-17

Bản đầu tiên: xây dựng nền tảng theo đặc tả `docs/00-MASTER-SPEC.md`.

### Nền tảng và kiến trúc

- Khởi tạo solution .NET 8 gồm 9 dự án theo Clean Architecture, bật `TreatWarningsAsErrors`
  và kiểm tra lỗ hổng gói NuGet ở mức lỗi build.
- Mô hình dữ liệu đầy đủ ~55 bảng PostgreSQL 16 với quy ước dùng chung: khóa `uuid`, audit,
  soft delete toàn hệ thống, `timestamptz` lưu UTC, `jsonb` cho dữ liệu bán cấu trúc.
- Cột `*_khong_dau` đồng bộ tự động qua interceptor, phục vụ tìm kiếm tiếng Việt không dấu.
- Sắp xếp theo collation ICU `vi-VN`.

### Engine nghiệp vụ

- **Engine quy trình động**: rule evaluator tự viết (`= != > >= < <= IN CONTAINS BETWEEN`,
  `AND/OR/NOT` lồng nhau, giới hạn độ sâu), validator 7 quy tắc bắt buộc, tính hạn xử lý theo
  ngày làm việc và ngày nghỉ lễ, quy tắc tác nhân MỘT_NGƯỜI / TẤT_CẢ / ĐA_SỐ.
- **Snapshot quy trình**: hồ sơ chạy theo cấu hình đóng băng lúc nộp; quy trình đang có hồ sơ
  chạy dở bị chặn sửa, buộc tạo phiên bản mới.
- **Engine tính điểm**: 3 cách tính, loại điểm cao/thấp khi ≥ 5 phiếu, làm tròn cấu hình được,
  xác định mức công nhận theo khoảng điểm, kiểm tra chồng lấn khoảng điểm.
- **Engine kiểm tra trùng lặp** chạy hoàn toàn nội bộ: SimHash, MinHash/LSH, TF-IDF cosine,
  Jaccard, vector nhúng; điểm tổng hợp theo hệ số cấu hình được; trả về từng cặp đoạn trùng
  kèm vị trí ký tự để giao diện highlight.

### API và hạ tầng

- Xác thực Argon2id + JWT (access 15 phút) + refresh token 7 ngày xoay vòng, thu hồi được;
  phát hiện tái sử dụng token đã thu hồi thì thu hồi cả chuỗi phiên.
- Phân quyền trên từng chức năng qua pipeline MediatR, kèm phạm vi dữ liệu theo đơn vị.
- Mã hóa AES-256-GCM cho số CCCD và secret tích hợp.
- Tải tệp kiểm tra magic number, chặn tệp thực thi, tính SHA-256, chống path traversal.
- Rate limit 100 req/phút/IP và 5 lần đăng nhập/phút/IP; security headers đầy đủ.
- Swagger tiếng Việt, SignalR hub thông báo realtime, health check `/health` và `/health/ready`.
- Xuất Excel (ClosedXML) và PDF (QuestPDF) theo mẫu văn bản hành chính Việt Nam.

### Giao diện web

- React 18 + TypeScript + Vite + Ant Design 5, chia gói theo route.
- Menu và màu chủ đạo đọc động từ cấu hình hệ thống, lọc theo quyền người dùng.
- Wizard nộp hồ sơ 6 bước có tự lưu nháp 30 giây và checklist thành phần trực quan.
- Màn hình chấm điểm 2 panel với phiếu chấm sinh động từ bộ tiêu chí, tính điểm realtime.
- Trình thiết kế quy trình trên ReactFlow.
- Giao diện đối chiếu trùng lặp 2 cột có highlight đoạn trùng.
- Dashboard ECharts; responsive từ 320 px; hỗ trợ in ấn qua `@media print`.

### Dữ liệu mẫu

9 vai trò với ma trận phân quyền đầy đủ, 22 đơn vị 3 cấp, 8 lĩnh vực, quy trình mẫu 6 bước,
bộ tiêu chí 100 điểm, hội đồng 7 thành viên, 30 tài khoản, 40 hồ sơ ở đủ trạng thái — trong đó
có một cặp cố ý trùng lặp để demo chức năng AI.

### Triển khai

`docker compose` gồm PostgreSQL + pgvector, Redis, MinIO, Seq, dịch vụ OCR, API và web.
Container chạy user không phải root; dịch vụ dữ liệu chỉ bind `127.0.0.1`.

### Kiểm thử

166 unit test, 6 integration test trên PostgreSQL thật qua Testcontainers, và kịch bản
end-to-end 29 bước chạy qua API thật với 8 tài khoản thuộc 6 vai trò.

### Quyết định bảo mật đáng chú ý

- **Loại bỏ gói Scriban** khỏi dự án do có lỗ hổng critical
  ([GHSA-5wr9-m6jw-xx44](https://github.com/advisories/GHSA-5wr9-m6jw-xx44)) chưa có bản vá.
  Thay bằng bộ thay thế placeholder tự viết chỉ xử lý văn bản thuần — đồng thời loại bỏ hoàn
  toàn nguy cơ template injection từ mẫu thông báo do quản trị viên nhập.
- **Danh sách stopword tiếng Việt được thu hẹp có chủ đích**: do so khớp chạy trên văn bản đã bỏ
  dấu, các hư từ như `hồ`, `số`, `vị`, `trọng`, `văn`, `quả` trùng với thuật ngữ nghiệp vụ quan
  trọng sau khi bỏ dấu, nên bị loại khỏi danh sách để không phá hủy ngữ nghĩa khi so khớp.

### Lỗi đã phát hiện và sửa trong quá trình xây dựng

- Npgsql chỉ chấp nhận `timestamptz` với offset UTC → thêm value converter chuẩn hóa UTC cho
  toàn bộ cột `DateTimeOffset`.
- EF Core sinh `UPDATE` thay vì `INSERT` cho thực thể con mới của bản ghi cha đã lưu, do khóa
  chính được đánh dấu `ValueGenerated.OnAdd` → chuyển sang `ValueGeneratedNever()` và thêm trực
  tiếp vào `DbSet`.
- Với minimal hosting, cấu hình do `WebApplicationFactory` thêm bị `appsettings.json` ghi đè,
  khiến integration test nối nhầm sang cơ sở dữ liệu cục bộ → chuyển sang biến môi trường.
- Giao diện chỉ kiểm tra `=== null` trong khi API bỏ hẳn trường `null` khỏi JSON → sửa sang
  optional chaining và bổ sung error boundary cho toàn bộ route.
- Thiếu `.dockerignore` khiến thư mục `obj/` của máy host bị copy vào image, ghi đè kết quả
  `dotnet restore` và làm hỏng bước publish.
- API thoát ngay khi lần kết nối cơ sở dữ liệu đầu tiên thất bại, dẫn tới vòng lặp khởi động lại
  trong docker-compose khi DNS nội bộ chưa sẵn sàng → thêm cơ chế thử lại có backoff, vẫn giữ
  fail-fast cho lỗi migration thật.
- Cổng máy chủ trong docker-compose bị cố định → tham số hóa toàn bộ qua biến `*_PORT` để triển
  khai được trên máy đã dùng sẵn các cổng mặc định.
- Container web luôn báo `unhealthy` dù phục vụ HTTP 200: health check dùng `localhost`, tên này
  phân giải ra `::1` trước trong khi Nginx chỉ lắng nghe IPv4, và BusyBox `wget` không tự chuyển
  sang địa chỉ còn lại → đổi sang `127.0.0.1` và thêm `--start-period`.
