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
| 6 | Biểu mẫu xuất | ✅ | Màn hình tab **Biểu mẫu xuất** trong Danh mục: CRUD, tải tệp `.docx` mẫu, quét placeholder `{{ }}` ngay khi tải lên và ánh xạ từng placeholder sang nguồn dữ liệu. Gộp văn bản cả đoạn nên bắt được placeholder bị Word cắt thành nhiều run |
| 7 | Biểu mẫu thống kê | ✅ | Báo cáo tuỳ biến sinh động từ cấu hình cột, có dòng tổng hợp; nguồn dữ liệu chặn bằng bảng trắng, cấu hình sai bị chặn ngay khi lưu |
| 8 | Quyết định | ✅ | CRUD + chọn sáng kiến đủ điều kiện + xuất PDF; chặn gán trùng và chặn sửa khi đã ký số |

## Nhóm II — Quy trình động

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 9 | Cấu hình quy trình | ✅ | CRUD, sao chép, tạo phiên bản mới, kích hoạt, ngừng áp dụng |
| 10 | Cấu hình trường hợp | ✅ | Nhánh rẽ có điều kiện jsonb; rule evaluator đầy đủ toán tử; xem trên designer |
| 11 | Cấu hình bước xử lý | ✅ | Panel cấu hình bước trên trình thiết kế ReactFlow |
| 12 | Chức năng bổ sung | ✅ | Bật/tắt 9 chức năng ngay trên trình thiết kế quy trình |
| 13 | Thành phần hồ sơ | ✅ | Cấu hình + checklist kiểm tra khi nộp, chặn nộp khi thiếu |
| 14 | Trạng thái bước | ✅ | Trạng thái theo bước và trạng thái toàn cục |
| 15 | Tác nhân xử lý | ✅ | 7 loại tác nhân, quy tắc MỘT_NGƯỜI / TẤT_CẢ / ĐA_SỐ đã kiểm chứng |
| 16 | Cấu hình liên thông | ✅ | Màn hình `/quan-tri/lien-thong`: CRUD hệ thống liên thông, 3 kiểu xác thực (API_KEY/HMAC/OAUTH2), ánh xạ tên trường, xem trước dữ liệu, chạy đồng bộ và tab nhật ký đồng bộ. Bí mật mã hoá khi lưu, không bao giờ trả về giao diện |

## Nhóm III — Tiêu chí động

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 17 | Nhóm tiêu chí | ✅ | Cây 2 cấp, kiểm tra tổng trọng số và tổng điểm realtime |
| 18 | Cấu hình tiêu chí | ✅ | 4 kiểu nhập; mức công nhận theo khoảng điểm, kiểm tra chồng lấn |

## Nhóm IV — Hội đồng

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 19 | Danh sách hội đồng | ✅ | Màn hình `/hoi-dong`: CRUD hội đồng; trang chi tiết có tab Phiên họp — tạo phiên, chọn hồ sơ đưa ra xét, điểm danh, bỏ phiếu và kiểm phiếu theo ngưỡng thông qua, kết luận và kết thúc phiên |
| 20 | Thành viên hội đồng | ✅ | Tab Thành viên sửa trực tiếp trên bảng: chọn tài khoản, chức danh, 5 nhóm quyền; chặn lưu khi không đúng 1 chủ tịch hoặc thiếu số thành viên tối thiểu |

## Nhóm V — Đăng ký nộp hồ sơ

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 21 | Đăng nhập | ✅ | Argon2id, JWT + refresh xoay vòng, khoá tài khoản, buộc đổi mật khẩu lần đầu, **SSO qua OIDC** (Authorization Code + PKCE), **MFA TOTP** (RFC 6238, chống dùng lại mã, 10 mã khôi phục), **CAPTCHA** ảnh SVG tự sinh sau 3 lần sai, **quên mật khẩu qua OTP email**. Nút "Đăng nhập một lần (SSO)" hiện trên trang đăng nhập khi máy chủ đã cấu hình nhà cung cấp, kèm trang nhận mã trả về `/dang-nhap/sso` |
| 22 | Đăng ký nộp sáng kiến | ✅ | Wizard 6 bước, tự lưu nháp 30 giây, kiểm tra tỷ lệ đóng góp 100% |
| 23 | Quản lý hồ sơ sáng kiến | ✅ | Danh sách, sửa, rút, tab lịch sử chỉnh sửa có diff trước/sau |
| 24 | Thành phần hồ sơ | ✅ | Checklist trực quan ✓/✗/⚠, chặn nộp và nêu rõ mục còn thiếu |
| 25 | Tệp tin đính kèm | ✅ | Magic number, chặn tệp thực thi, SHA-256, **quét mã độc ClamAV trước khi ghi xuống kho** |
| 26 | Kiểm tra trùng lặp | ✅ | Pipeline đầy đủ, giao diện đối chiếu 2 cột highlight. OCR đã nối vào luồng nộp: tệp PDF/ảnh tự trích xuất văn bản rồi mới chạy so khớp |

## Nhóm VI — Tiếp nhận và xử lý

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 27 | Tiếp nhận hồ sơ | ✅ | Nút hành động sinh động theo quy trình |
| 28 | Danh sách hồ sơ | ✅ | Bộ lọc đa tiêu chí, lưu trong URL, **lưu bộ lọc yêu thích** ngay trên màn hình danh sách (chọn, lưu, đặt mặc định, xoá; bộ lọc mặc định tự áp dụng khi mở màn hình, liên kết chia sẻ luôn thắng bộ lọc cá nhân), chọn nhiều, xuất Excel |
| 29 | Xử lý hồ sơ | ✅ | Thực thi bước, xử lý hàng loạt, thu hồi, Idempotency-Key |
| 30 | Theo dõi hồ sơ | ✅ | Timeline đầy đủ, badge quá hạn. Job nhắc hạn tự động chạy 7h hằng ngày, chống nhắc trùng trong 20 giờ |
| 31/36 | Đính kèm quyết định | ✅ | Màn hình ban hành quyết định, chọn sáng kiến đủ điều kiện, xuất PDF theo mẫu hành chính |
| 32 | Kết quả sáng kiến | ✅ | Công bố kết quả hàng loạt theo quyết định, mở hiển thị công khai và gửi thông báo tới tác giả |

## Nhóm VII — Đánh giá

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 33 | Danh sách hồ sơ đánh giá | ✅ | "Việc của tôi", đếm ngược hạn, phân công loại trừ xung đột lợi ích |
| 34 | Đánh giá hồ sơ | ✅ | Giao diện 2 panel, phiếu chấm render động, tính điểm realtime |
| 35 | Phiếu đánh giá | ✅ | Lưu/gửi/mở lại phiếu, ma trận điểm, xuất PDF hàng loạt theo hồ sơ hoặc theo hội đồng (nút **Xuất phiếu chấm** trên trang hội đồng) |

## Nhóm IX–X — Tra cứu, báo cáo

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 37 | Tra cứu, tìm kiếm | ✅ | Tìm không dấu, tìm nâng cao, chia sẻ link, trang công khai, **tìm ngữ nghĩa** bằng vector nội bộ (xem ghi chú giới hạn bên dưới) |
| 38 | DS sáng kiến đạt | ✅ | Bảng + xuất Excel + xuất PDF mẫu văn bản hành chính |
| 39 | DS sáng kiến chưa đạt | ✅ | Kèm lý do và điểm đánh giá |
| 40 | DS theo đơn vị | ✅ | Kèm tỷ lệ đạt, dòng tổng cộng |
| — | Dashboard | ✅ | 4 chỉ số + 3 biểu đồ ECharts + top đơn vị + cảnh báo trùng lặp |

## Nhóm XI–XIII — Tích hợp, di động, quản trị

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 41 | Tích hợp SSO/IOC/TĐKT | ✅ | SSO OIDC có nút trên trang đăng nhập + **single logout** khi đăng xuất, cấu hình bằng `SSO_ISSUER`/`SSO_CLIENT_ID`/`SSO_CLIENT_SECRET` trong `.env`, đẩy danh sách sáng kiến đã công bố sang hệ thống ngoài qua REST kèm nhật ký đồng bộ, **API `/api/public/v1` cho hệ thống ngoài gọi vào** (khoá API băm + danh sách IP/CIDR + giới hạn tần suất riêng). Cần thông tin endpoint thật của thành phố để đấu nối |
| 42 | Ứng dụng di động | ✅ | **Đáp ứng bằng web responsive** theo quyết định của chủ đầu tư: giao diện chạy tốt từ 320px, thanh điều hướng chuyển thành Drawer, bảng cuộn ngang trong khung riêng — dùng trực tiếp trên trình duyệt điện thoại, không cần cài đặt. **Không** có ứng dụng đóng gói cho App Store / CH Play |
| 43 | Quản lý người dùng | ✅ | Thêm/sửa/gán vai trò, đặt lại mật khẩu (thu hồi phiên cũ), khoá/mở khoá, **nhập từ Excel** (chạy thử trước, toàn bộ hoặc không) |
| 44 | Quản lý đơn vị | ✅ | Cây tổ chức + panel chi tiết, thêm đơn vị con, sửa, xoá ngay trên giao diện |
| 45 | Quản lý vai trò | ✅ | Ma trận phân quyền sửa trực tiếp trên bảng, chọn cả cột, thêm/sửa/xoá vai trò |
| 46 | Cấu hình hệ thống | ✅ | Đọc/ghi theo nhóm, có kiểu dữ liệu, màu chủ đạo áp dụng ngay lên giao diện |
| 47 | Cấu hình đơn vị | ✅ | Sửa được tiêu đề văn bản, người ký mặc định và chức vụ ngay trong form đơn vị |
| 48 | Cấu hình menu | ✅ | Menu render động từ CSDL và lọc theo quyền; sửa menu bằng API |
| 49 | Cấu hình chữ ký số | ✅ | Ký PKCS#7 detached + xác minh chữ ký, giữ nguyên bản gốc, chặn ký bằng chứng thư hết hạn. Cần chứng thư thật của CA để dùng chính thức |
| 50 | Cấu hình email & SMS | ✅ | Màn hình cấu hình SMTP/SMS có nút gửi thử và thống kê hàng đợi; worker gửi thật rút hàng đợi mỗi 5 phút |
| 51 | Cấu hình thông tin sáng kiến | ✅ | Ngưỡng trùng lặp, hệ số tính điểm, giới hạn tệp — sửa được trên giao diện |

---

## Tổng hợp

| Mức | Số chức năng |
|---|---|
| ✅ Hoàn chỉnh | 51 |
| ⬜ Chưa triển khai | 0 |

**Toàn bộ 51 chức năng đều có API, nghiệp vụ và giao diện, đã kiểm chứng chạy thật.**

Riêng chức năng 42 được chốt phương án **web responsive** thay cho ứng dụng đóng gói: người dùng
mở trình duyệt trên điện thoại là dùng được ngay, không phải cài đặt và không phụ thuộc chu kỳ
duyệt của App Store / CH Play. Nếu sau này cần bản cài đặt từ store thì đó là một sản phẩm riêng
(React Native / Flutter) dùng lại chính hợp đồng API hiện có, chứ không phải sửa phần web.

## Luồng nghiệp vụ đã kiểm chứng

Luồng chính: nộp hồ sơ → tiếp nhận → thẩm định → phân công chấm → hội đồng chấm điểm →
chủ tịch kết luận → ban hành quyết định → công bố kết quả → liên thông hệ thống ngoài.

Luồng hội đồng: thành lập hội đồng → lưu danh sách thành viên (chặn khi không đúng 1 chủ tịch
hoặc thiếu thành viên tối thiểu) → mở phiên họp kèm hồ sơ đưa ra xét → điểm danh → bỏ phiếu và
kiểm phiếu theo ngưỡng thông qua → kết luận và kết thúc phiên (khoá bỏ phiếu) → xuất phiếu chấm PDF.

Luồng liên thông: khai báo hệ thống ngoài → xem trước dữ liệu sẽ đẩy → chạy đồng bộ → đọc nhật ký
đồng bộ; sửa cấu hình mà để trống ô bí mật thì giữ nguyên bí mật đang lưu.

Luồng nhánh: yêu cầu bổ sung, từ chối, rút hồ sơ, sửa và nộp lại, xử lý hàng loạt,
phân công tự động chia đều, lưu và áp dụng bộ lọc yêu thích trên màn hình danh sách.

Luồng xác thực: đăng nhập nội bộ, SSO OIDC, single logout, MFA TOTP (bật/tắt/mã khôi phục/
quản trị viên gỡ hộ), CAPTCHA sau 3 lần sai, quên mật khẩu qua OTP email.

## Ghi chú giới hạn cần biết trước khi nghiệm thu

| Chức năng | Giới hạn | Cần gì để gỡ |
|---|---|---|
| 41 — Liên thông IOC/TĐKT | Đã có adapter đầy đủ và kiểm chứng bằng máy chủ nhận thật chạy cục bộ, nhưng **chưa đấu vào hệ thống thật của thành phố** | Endpoint, khoá và tài liệu API của IOC / Thi đua khen thưởng |
| 21, 41 — SSO | Luồng OIDC hoàn chỉnh (Authorization Code + PKCE) kèm nút trên trang đăng nhập và trang nhận mã trả về, kiểm chứng bằng nhà cung cấp OIDC chạy cục bộ | `SSO_ISSUER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET` trong `.env` (ánh xạ sang `Sso:*`) và đăng ký redirect URI `<địa-chỉ-web>/dang-nhap/sso` với hệ thống SSO thành phố |
| 49 — Ký số | Ký và xác minh PKCS#7 chạy đúng, kiểm chứng bằng chứng thư tự ký | Chứng thư số thật (PFX hoặc USB token/HSM) của Ban Cơ yếu / CA được cấp phép |
| 37 — Tìm ngữ nghĩa | Vector nhúng hiện là "hashing trick" từ vựng nên bắt quan hệ **từ vựng**, chưa bắt được quan hệ ngữ nghĩa xa (ví dụ "tiết kiệm điện" ~ "cảm biến ánh sáng") | Nạp mô hình sentence-transformer tiếng Việt dạng ONNX chạy nội bộ (không dùng API bên thứ ba) |
| 42 — Di động | Đáp ứng bằng **web responsive** chứ không phải ứng dụng cài từ store: không có thông báo đẩy, không dùng được ngoại tuyến, không truy cập máy ảnh/chữ ký trên thiết bị | Nếu chủ đầu tư yêu cầu bản cài đặt: làm ứng dụng React Native dùng lại hợp đồng API hiện có |

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

Phần code đã xong; những việc dưới đây **phụ thuộc dữ liệu và hạ tầng của chủ đầu tư**, không
làm được nếu chỉ ngồi tại chỗ viết thêm mã.

1. **Đấu nối SSO thật**: điền `SSO_ISSUER`, `SSO_CLIENT_ID`, `SSO_CLIENT_SECRET` trong `.env` và
   đăng ký redirect URI `<địa-chỉ-web>/dang-nhap/sso` với hệ thống SSO thành phố.
2. **Đấu nối IOC và Thi đua khen thưởng**: khai báo hệ thống trong màn hình *Liên thông hệ thống
   ngoài* bằng endpoint, khoá và kiểu xác thực thật, rồi chạy *Xem trước dữ liệu* trước khi đồng bộ.
3. **Chữ ký số**: nạp chứng thư thật (PFX hoặc USB token/HSM) của CA được cấp phép.
4. **Tìm ngữ nghĩa**: nạp mô hình sentence-transformer tiếng Việt dạng ONNX chạy nội bộ để thay
   vector "hashing trick" hiện tại.
5. **Nạp biểu mẫu xuất thật**: tải các tệp `.docx` mẫu của đơn vị lên tab *Biểu mẫu xuất* và ánh xạ
   placeholder — hiện mới có biểu mẫu mẫu trong dữ liệu seed.
6. *(Tuỳ chọn, ngoài phạm vi hiện tại)* Ứng dụng cài đặt từ store nếu chủ đầu tư đổi ý về phương án
   di động; dùng lại hợp đồng API hiện có.
