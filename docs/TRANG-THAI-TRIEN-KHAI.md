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
| 1 | Lĩnh vực | ✅ | CRUD, cây phân cấp, tìm không dấu, **đổi thứ tự bằng kéo–thả (dnd-kit) hoặc nút lên/xuống**, **nhập từ Excel có chạy thử trước**, chặn xóa khi đang tham chiếu, xuất Excel |
| 2 | Đối tượng | ✅ | CRUD đầy đủ, **nhập từ Excel** (mã đã có thì cập nhật, không tạo bản trùng) |
| 3 | Đợt đề nghị | ✅ | CRUD + **Mở / Đóng / Khoá + Sao chép đợt**; **màn hình chi tiết đợt 6 tab** (thông tin, đơn vị áp dụng, quy trình & tiêu chí, hồ sơ, hội đồng & tiến độ chấm, quyết định) gom số liệu trong một lần gọi; tự động đóng đợt quá hạn nộp mỗi giờ |
| 4 | Loại tác giả | ✅ | Kèm ràng buộc số tác giả tối đa, áp dụng khi nộp hồ sơ; **nhập từ Excel** |
| 5 | Đơn vị phê duyệt | ✅ | Cây tổ chức, đường dẫn cây phục vụ phạm vi dữ liệu; **tab Cấp phê duyệt** khai báo thứ tự các cấp xét theo đợt / lĩnh vực, chặn trùng thứ tự cấp trong cùng phạm vi |
| 6 | Biểu mẫu xuất | ✅ | CRUD, tải tệp `.docx` mẫu, quét placeholder `{{ }}` và ánh xạ sang nguồn dữ liệu. **Cấu hình này thực sự sinh ra văn bản**: phiếu tiếp nhận và biên bản họp dùng bố cục đã khai báo, có **Xem trước** bằng dữ liệu mẫu (JSON + PDF); placeholder trỏ sai nguồn được báo lại thay vì in ra ô trống im lặng |
| 7 | Biểu mẫu thống kê | ✅ | Báo cáo tuỳ biến sinh động từ cấu hình cột, có dòng tổng hợp; nguồn dữ liệu chặn bằng bảng trắng, cấu hình sai bị chặn ngay khi lưu |
| 8 | Quyết định | ✅ | CRUD + chọn sáng kiến đủ điều kiện + xuất PDF; chặn gán trùng và chặn sửa khi đã ký số |

## Nhóm II — Quy trình động

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 9 | Cấu hình quy trình | ✅ | CRUD, **sao chép**, tạo phiên bản mới, kích hoạt, ngừng áp dụng, **xuất sơ đồ ra PNG và PDF**, **xem toàn màn hình** — tất cả có nút trên màn hình |
| 10 | Cấu hình trường hợp | ✅ | Nhánh rẽ có điều kiện jsonb; rule evaluator đầy đủ toán tử; xem trên designer |
| 11 | Cấu hình bước xử lý | ✅ | Panel cấu hình bước trên trình thiết kế ReactFlow |
| 12 | Chức năng bổ sung | ✅ | Bật/tắt 9 chức năng ngay trên trình thiết kế quy trình |
| 13 | Thành phần hồ sơ | ✅ | **Màn hình cấu hình riêng** + **API CRUD riêng từng thành phần** (không phải gửi lại cả sơ đồ, tránh hai người sửa ghi đè nhau); chặn cấu hình số ký tự tối thiểu > tối đa và chặn sửa khi quy trình đang áp dụng; checklist kiểm tra khi nộp |
| 14 | Trạng thái bước | ✅ | Trạng thái theo bước và trạng thái toàn cục |
| 15 | Tác nhân xử lý | ✅ | 7 loại tác nhân, quy tắc MỘT_NGƯỜI / TẤT_CẢ / ĐA_SỐ đã kiểm chứng |
| 16 | Cấu hình liên thông | ✅ | Hai lớp: `/quan-tri/lien-thong` khai báo **hệ thống ngoài** (3 kiểu xác thực, ánh xạ trường, xem trước, đồng bộ, nhật ký, **thử kết nối** và **gửi lại lần đồng bộ lỗi**); `/quan-tri/quy-trinh/:id/lien-thong` gắn **bước nào gọi hệ thống nào** theo sự kiện. Bí mật mã hoá khi lưu, không bao giờ trả về giao diện |

## Nhóm III — Tiêu chí động

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 17 | Nhóm tiêu chí | ✅ | Cây 2 cấp, kiểm tra tổng trọng số và tổng điểm realtime |
| 18 | Cấu hình tiêu chí | ✅ | 4 kiểu nhập; **khối mức công nhận theo khoảng điểm sửa trực tiếp trên màn hình**, máy chủ chặn khoảng chồng lấn |

## Nhóm IV — Hội đồng

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 19 | Danh sách hội đồng | ✅ | Màn hình `/hoi-dong`: CRUD hội đồng; trang chi tiết có tab Phiên họp — tạo phiên, chọn hồ sơ đưa ra xét, điểm danh, bỏ phiếu và kiểm phiếu theo ngưỡng thông qua, kết luận và kết thúc phiên. **Tab Biên bản**: lập biên bản sinh từ dữ liệu phiên họp, ký nhận theo chức danh, xuất PDF và ký số biên bản |
| 20 | Thành viên hội đồng | ✅ | Tab Thành viên sửa trực tiếp trên bảng; chặn lưu khi không đúng 1 chủ tịch hoặc thiếu số thành viên tối thiểu. **Phòng họp realtime**: điểm danh / bỏ phiếu / ghi ý kiến của một người hiện ngay trên màn hình những người đang mở phiên, không phải bấm tải lại |

## Nhóm V — Đăng ký nộp hồ sơ

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 21 | Đăng nhập | ✅ | Argon2id, JWT + refresh xoay vòng, khoá tài khoản, buộc đổi mật khẩu lần đầu, **SSO qua OIDC** (Authorization Code + PKCE), **MFA TOTP** (RFC 6238, chống dùng lại mã, 10 mã khôi phục), **CAPTCHA** ảnh SVG tự sinh sau 3 lần sai, **quên mật khẩu qua OTP email**. Nút "Đăng nhập một lần (SSO)" hiện trên trang đăng nhập khi máy chủ đã cấu hình nhà cung cấp, kèm trang nhận mã trả về `/dang-nhap/sso` |
| 22 | Đăng ký nộp sáng kiến | ✅ | Wizard 6 bước, tự lưu nháp 30 giây, kiểm tra tỷ lệ đóng góp 100%, **phiếu tiếp nhận PDF** in được ngay sau khi nộp |
| 23 | Quản lý hồ sơ sáng kiến | ✅ | Danh sách, sửa, rút, tab lịch sử chỉnh sửa có diff trước/sau |
| 24 | Thành phần hồ sơ | ✅ | Checklist trực quan ✓/✗/⚠, chặn nộp và nêu rõ mục còn thiếu. **Ô soạn nội dung dài có thanh định dạng** (đậm/nghiêng/danh sách), đếm ký tự + từ, thanh tiến độ số ký tự tối thiểu và tab xem trước — lưu văn bản thường, không lưu HTML |
| 25 | Tệp tin đính kèm | ✅ | Magic number, chặn tệp thực thi, SHA-256, **quét mã độc ClamAV trước khi ghi xuống kho**. **Tải tệp lớn theo mảnh 5MB** (rớt mạng chỉ gửi lại mảnh hỏng), **xem trước PDF/ảnh ngay trong trình duyệt** (cố ý không mở inline .html/.svg), **liên kết tải xuống có thời hạn và có ký HMAC**; kho lưu trữ chọn được đĩa cục bộ hoặc MinIO (presigned URL) |
| 26 | Kiểm tra trùng lặp | ✅ | Pipeline đầy đủ, giao diện đối chiếu 2 cột highlight. OCR đã nối vào luồng nộp: tệp PDF/ảnh tự trích xuất văn bản rồi mới chạy so khớp |

## Nhóm VI — Tiếp nhận và xử lý

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 27 | Tiếp nhận hồ sơ | ✅ | Nút hành động sinh động theo quy trình |
| 28 | Danh sách hồ sơ | ✅ | Bộ lọc đa tiêu chí, lưu trong URL, **lưu bộ lọc yêu thích**, **chọn cột hiển thị** (nhớ lựa chọn cho lần sau), chọn nhiều, xuất Excel |
| 29 | Xử lý hồ sơ | ✅ | Thực thi bước, xử lý hàng loạt, **thu hồi bước có nút và bắt buộc nhập lý do**, Idempotency-Key |
| 30 | Theo dõi hồ sơ | ✅ | Timeline đầy đủ, badge quá hạn. Job nhắc hạn tự động chạy 7h hằng ngày, chống nhắc trùng trong 20 giờ |
| 31/36 | Đính kèm quyết định | ✅ | Màn hình ban hành quyết định, chọn sáng kiến đủ điều kiện, xuất PDF theo mẫu hành chính |
| 32 | Kết quả sáng kiến | ✅ | Công bố kết quả hàng loạt theo quyết định, mở hiển thị công khai và gửi thông báo tới tác giả |

## Nhóm VII — Đánh giá

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 33 | Danh sách hồ sơ đánh giá | ✅ | "Việc của tôi", đếm ngược hạn, **màn hình phân công chấm điểm** (chọn hội đồng, thành viên, hạn, chia đều) loại trừ xung đột lợi ích |
| 34 | Đánh giá hồ sơ | ✅ | Giao diện 2 panel, phiếu chấm render động, tính điểm realtime |
| 35 | Phiếu đánh giá | ✅ | Lưu/gửi phiếu; **tab Ma trận điểm** kèm nút **Mở lại phiếu** cho thư ký; **ký số từng phiếu** (ký trên bản PDF chốt tại thời điểm ký) kèm lịch sử ký; xuất **một PDF liền mạch hoặc ZIP mỗi phiếu một tệp** |

## Nhóm IX–X — Tra cứu, báo cáo

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 37 | Tra cứu, tìm kiếm | ✅ | Tìm không dấu, tìm nâng cao, chia sẻ link, trang công khai, **khối "Tìm theo ý nghĩa"**; **gợi ý từ khoá khi gõ** (chỉ trong phạm vi dữ liệu người dùng được xem), **tô đậm phần khớp** trong kết quả, **lưu truy vấn** bằng bộ lọc yêu thích |
| 38 | DS sáng kiến đạt | ✅ | Bảng + xuất Excel + xuất PDF mẫu văn bản hành chính |
| 39 | DS sáng kiến chưa đạt | ✅ | Kèm lý do và điểm đánh giá |
| 40 | DS theo đơn vị | ✅ | Kèm tỷ lệ đạt, dòng tổng cộng. Bổ sung **thống kê theo tác giả**, **thời gian xử lý trung bình theo bước** (kèm số lượt quá hạn) và **báo cáo tổng hợp năm** (xuất PDF). Dashboard có bộ lọc năm/đợt/lĩnh vực/đơn vị và **bấm vào biểu đồ để mở đúng danh sách hồ sơ đứng sau con số**. Kết quả báo cáo đệm 5 phút, khoá đệm gồm danh tính người gọi |
| — | Dashboard | ✅ | 4 chỉ số + 3 biểu đồ ECharts + top đơn vị + cảnh báo trùng lặp |

## Nhóm XI–XIII — Tích hợp, di động, quản trị

| # | Chức năng | Mức | Ghi chú |
|---|---|---|---|
| 41 | Tích hợp SSO/IOC/TĐKT | ✅ | SSO OIDC có nút trên trang đăng nhập + **single logout** khi đăng xuất, cấu hình bằng `SSO_ISSUER`/`SSO_CLIENT_ID`/`SSO_CLIENT_SECRET` trong `.env`, đẩy danh sách sáng kiến đã công bố sang hệ thống ngoài qua REST kèm nhật ký đồng bộ, **API `/api/public/v1` cho hệ thống ngoài gọi vào** (khoá API băm + danh sách IP/CIDR + giới hạn tần suất riêng). Cần thông tin endpoint thật của thành phố để đấu nối |
| 42 | Ứng dụng di động | ✅ | **Đáp ứng bằng web responsive** theo quyết định của chủ đầu tư: giao diện chạy tốt từ 320px, thanh điều hướng chuyển thành Drawer, bảng cuộn ngang trong khung riêng — dùng trực tiếp trên trình duyệt điện thoại, không cần cài đặt. **Không** có ứng dụng đóng gói cho App Store / CH Play |
| 43 | Quản lý người dùng | ✅ | Thêm/sửa/gán vai trò, đặt lại mật khẩu (thu hồi phiên cũ), khoá/mở khoá, **nhập từ Excel** (chạy thử trước, toàn bộ hoặc không) |
| 44 | Quản lý đơn vị | ✅ | Cây tổ chức + panel chi tiết, thêm/sửa/xoá; **kéo–thả đổi cấp trên**, **gộp đơn vị khi sáp nhập** (chuyển hồ sơ, tài khoản, đơn vị con sang đích rồi mới xoá mềm nguồn), **xuất sơ đồ tổ chức ra PNG** |
| 45 | Quản lý vai trò | ✅ | Ma trận phân quyền sửa trực tiếp trên bảng, chọn cả cột, thêm/sửa/xoá vai trò, **sao chép vai trò** (giữ nguyên quyền và phạm vi dữ liệu; bản sao luôn là vai trò thường) |
| 46 | Cấu hình hệ thống | ✅ | Đọc/ghi theo nhóm, có kiểu dữ liệu, màu chủ đạo áp dụng ngay lên giao diện; **khai báo ngày nghỉ lễ** (trừ khi tính hạn xử lý); **màn hình theo dõi sao lưu** (liệt kê bản sao, cảnh báo bản gần nhất quá 48 giờ hoặc thiếu thành phần — chỉ đọc, không có nút khôi phục trên web) |
| 47 | Cấu hình đơn vị | ✅ | Sửa được tiêu đề văn bản, người ký mặc định và chức vụ ngay trong form đơn vị |
| 48 | Cấu hình menu | ✅ | Menu render động từ CSDL và lọc theo quyền; **màn hình quản trị menu**: thêm/sửa/xoá, **kéo–thả cả cây rồi lưu một lần**, tách riêng hai cây **Web** và **Mobile** |
| 49 | Cấu hình chữ ký số | ✅ | Màn hình `/quan-tri/chu-ky-so` khai báo nhà cung cấp, hình thức ký, chứng thư và báo hệ thống đã sẵn sàng ký hay chưa. Ký số áp dụng cho **quyết định** và **biên bản họp**, kèm lịch sử ký và xác minh chữ ký từng lần. Ký PKCS#7 detached, giữ nguyên bản gốc. Khoá bí mật đọc từ tệp PFX của máy chủ (`KYSO_PFX`), không lưu trong CSDL. Cần chứng thư thật của CA để dùng chính thức |
| 50 | Cấu hình email & SMS | ✅ | Màn hình cấu hình SMTP/SMS có nút gửi thử và thống kê hàng đợi; worker gửi thật rút hàng đợi mỗi 5 phút. **Mẫu thông báo theo sự kiện và kênh gửi** (Email/SMS/trong ứng dụng) có xem trước, biến chưa có dữ liệu hiện `[tên_biến]` |
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

Luồng thông báo: chuông trên thanh trên đếm số chưa đọc, mở ngăn kéo xem danh sách, bấm vào một
thông báo là đánh dấu đã đọc và mở thẳng đối tượng liên quan, có nút "Đọc tất cả". Thông báo là
dữ liệu cá nhân — không ai đọc hay đánh dấu được thông báo của người khác.

Luồng ký số: khai báo cấu hình chữ ký số → gắn tệp văn bản vào quyết định → ký số → xem lịch sử ký
→ xác minh chữ ký (đối chiếu bản gốc với tệp chữ ký PKCS#7 detached). Biên bản họp ký số theo cùng
cơ chế: hệ thống sinh PDF của biên bản hiện hành rồi ký, nên chữ ký luôn gắn với đúng nội dung.

Luồng biên bản: phiên họp kết thúc → lập biên bản (tự lấy điểm danh, phiếu bầu, kết luận) → thành
viên có quyền ký nhận → xuất PDF → ký số. Người ngoài hội đồng gọi thẳng API ký cũng bị chặn.

Realtime: thông báo mới được đẩy xuống trình duyệt qua SignalR (`/hubs/thong-bao`); máy chủ chỉ gửi
tín hiệu, client gọi lại API để lấy dữ liệu theo đúng quyền. Mất kết nối realtime thì chuông vẫn
cập nhật nhờ nhịp hỏi lại 60 giây.

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
| 49 — Ký số | Ba hình thức đều chạy và có kiểm thử: **PAdES** (chữ ký nhúng trong PDF, trình đọc PDF thông thường kiểm tra được), **PKCS#7 detached** cho tệp không phải PDF, và **ký bằng USB token** (máy chủ phát giá trị băm, máy trạm ký, máy chủ xác minh). Mới kiểm chứng bằng chứng thư tự ký. Chưa có đóng dấu thời gian từ TSA | Chứng thư số thật của Ban Cơ yếu / CA được cấp phép; địa chỉ máy chủ cấp dấu thời gian (TSA) nếu văn bản đòi PAdES-T; công cụ ký ở máy trạm của nhà cung cấp token để nối vào `/api/v1/ky-so-usb` |
| 37 — Tìm ngữ nghĩa | Vector nhúng hiện là "hashing trick" từ vựng nên bắt quan hệ **từ vựng**, chưa bắt được quan hệ ngữ nghĩa xa (ví dụ "tiết kiệm điện" ~ "cảm biến ánh sáng") | Nạp mô hình sentence-transformer tiếng Việt dạng ONNX chạy nội bộ (không dùng API bên thứ ba) |
| 42 — Di động | Đáp ứng bằng **web responsive** chứ không phải ứng dụng cài từ store: không có thông báo đẩy, không dùng được ngoại tuyến, không truy cập máy ảnh/chữ ký trên thiết bị. Menu Mobile đã cấu hình riêng được ở màn hình cấu hình menu | Nếu chủ đầu tư yêu cầu bản cài đặt: làm ứng dụng React Native dùng lại hợp đồng API hiện có |
| Sao lưu / phục hồi | Màn hình trên web **chỉ theo dõi** (liệt kê bản sao, cảnh báo bản cũ hoặc thiếu thành phần). Tạo bản sao và khôi phục chạy bằng `deploy/sao-luu-blueidea.sh` trên máy chủ | Đây là lựa chọn có chủ đích: khôi phục ghi đè toàn bộ CSDL đang chạy nên không đưa lên web; và để API tự tạo bản sao thì phải cho ứng dụng web gọi được docker trên máy chủ, tức trao quyền tương đương root của máy đó |
| Kho tệp | Mặc định lưu trên **đĩa của máy chủ**; adapter MinIO đã có và chọn bằng `LuuTru:Loai = MINIO` nhưng chưa bật ở bản triển khai hiện tại | Máy chủ sản xuất hiện chỉ có 1 vCPU / 2GB RAM nên không chạy thêm MinIO. Khi chạy nhiều bản API thì bắt buộc chuyển sang MinIO vì tệp không còn nằm trên đĩa của một máy cụ thể |

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
