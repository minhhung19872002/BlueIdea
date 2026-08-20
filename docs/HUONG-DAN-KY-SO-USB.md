# Ký số bằng USB token — hướng dẫn cài đặt và sử dụng

Chức năng 49. Áp dụng cho **quyết định công nhận** và **biên bản họp hội đồng**.

## Vì sao phải có công cụ ở máy trạm

Khoá bí mật nằm trong USB token và **không bao giờ rời khỏi thiết bị** — đó là lý do tồn tại của
token. Máy chủ vì vậy không ký thay được, và luồng bắt buộc phải đi ba nhịp:

| Nhịp | Ai làm | Việc |
|---|---|---|
| 1 | Máy chủ | Chốt nội dung cần ký, trả về **giá trị băm SHA-256** của đúng nội dung đó |
| 2 | Máy trạm | Token ký giá trị băm (người ký nhập mã PIN) |
| 3 | Máy chủ | Xác minh chữ ký **đối chiếu với chính giá trị băm đã phát ở nhịp 1**, rồi mới ghi nhận |

Nhịp 3 cố ý không tin giá trị băm do máy trạm gửi lên: nếu tin, bất kỳ ai cũng "ký" được một nội
dung tuỳ ý rồi gán vào văn bản khác.

Trình duyệt không với tới được USB token, nên nhịp 2 phải do một chương trình chạy trên máy người
ký thực hiện. Đó là `blueidea-kyso`.

## Cài đặt

Yêu cầu: Windows, đã cài driver/middleware của nhà cung cấp token (token phải xuất hiện trong
`certmgr.msc` → *Personal* → *Certificates*).

```bash
dotnet publish client-tools/BlueIdea.KySoUsb -c Release -r win-x64 --self-contained false
```

Kết quả nằm ở `client-tools/BlueIdea.KySoUsb/bin/Release/net8.0-windows/win-x64/publish/blueidea-kyso.exe`.
Chép cả thư mục publish sang máy người ký; máy đó cần .NET 8 Runtime.

## Kiểm tra token đã sẵn sàng

```bash
blueidea-kyso liet-ke
```

In ra mọi chứng thư có khoá bí mật kèm vân tay (thumbprint), nơi cấp và hạn hiệu lực. Lệnh này
**không** chạm vào khoá bí mật nên không hỏi PIN — chạm vào khoá là token đòi PIN ngay, kể cả khi
ta chỉ muốn đọc thông tin.

Không thấy chứng thư nào: token chưa cắm, hoặc driver của nhà cung cấp chưa nạp.

## Ký một văn bản

```bash
blueidea-kyso tu-dong ^
  --api https://sangkien.donvi.gov.vn ^
  --tai-khoan nguoiky --mat-khau ****** ^
  --tep <id tệp văn bản> ^
  --doi-tuong QUYET_DINH --doi-tuong-id <id quyết định> ^
  --van-tay <thumbprint chứng thư>
```

Công cụ chạy cả ba nhịp. Ở nhịp 2, driver của token bật hộp thoại nhập PIN.

- `--van-tay` bỏ được nếu máy chỉ có **đúng một** chứng thư còn hạn. Có nhiều hơn thì công cụ
  **không tự đoán**: ký nhầm danh tính không sửa được bằng một lần bấm nút.
- `--token <jwt>` dùng thay cho `--tai-khoan/--mat-khau` nếu đã có sẵn JWT.
- `--pin <mã>` nạp PIN thẳng vào khoá, không hiện hộp thoại — chỉ dùng cho chạy tự động. **Không**
  ghi PIN vào tệp cấu hình hay tệp .bat: nó sẽ nằm lại trong lịch sử lệnh.

Muốn tự dán tay (ví dụ máy ký tách khỏi mạng của hệ thống):

```bash
blueidea-kyso ky --hash <base64 lấy từ màn hình ký> --van-tay <thumbprint>
```

In ra JSON gồm `chuKyBase64` và `chungThuBase64` để dán vào màn hình `/quan-tri/chu-ky-so`.

## Lỗi thường gặp

| Hiện tượng | Nguyên nhân |
|---|---|
| Lệnh **treo im lặng** ở nhịp 2 | Chạy từ dịch vụ nền, tác vụ theo lịch hoặc phiên SSH không có desktop. Hộp thoại PIN không hiện được nên tiến trình đứng chờ mãi. Chạy trong cửa sổ dòng lệnh của chính người ký, hoặc dùng `--pin`. |
| `Token từ chối ký` | Sai PIN, bấm huỷ hộp thoại, hoặc token đã khoá sau nhiều lần nhập sai. Số lần cho phép do nhà cung cấp quy định — thường 5 lần, khoá rồi phải mang tới nhà cung cấp mở. |
| `Chữ ký không khớp với nội dung cần ký` | Phiên ký đã hết hạn (10 phút) và tệp được thay giữa chừng, hoặc ký nhầm giá trị băm của phiên khác. Bấm ký lại để lấy phiên mới. |
| `Chứng thư hết hiệu lực` | Chứng thư trong token quá hạn. Gia hạn với nhà cung cấp. |
| Không thấy chứng thư khi `liet-ke` | Chưa cắm token, hoặc thiếu middleware của nhà cung cấp. |

## Đã kiểm chứng với

| Ngày | Token / CA | Kết quả |
|---|---|---|
| 20/08/2026 | Chứng thư tổ chức do **WINCA** (WINGROUP) cấp, khoá nằm sau *WINCA Key Storage Provider v6.0*, RSA 2048 | Liệt kê, ký và máy chủ xác minh — xem mục "Nhật ký kiểm chứng" bên dưới |

Chuẩn ký: **RSA PKCS#1 v1.5 trên SHA-256**. Máy chủ chấp nhận thêm RSA-PSS và ECDSA, nên token
của nhà cung cấp khác vẫn dùng được miễn là chứng thư nằm trong kho chứng thư Windows.

## Nhật ký kiểm chứng

Ghi lại tại đây mỗi lần kiểm chứng với một token thật (loại token, ngày, kết quả nhịp 3). Mục này
là bằng chứng cho hồ sơ nghiệm thu chức năng 49 — chứng thư tự ký không thay thế được.

### 20/08/2026 — token WINCA, chứng thư tổ chức

| Mục | Giá trị |
|---|---|
| Chứng thư | `CN=CÔNG TY TNHH KỸ THUẬT CÔNG NGHỆ BLUESTAR, O=…, S=Hồ Chí Minh, C=VN`, `MST:0318811225` |
| Nơi cấp | `C=VN, O=WINGROUP, CN=WINCA` (chuỗi tin cậy về `rootca.gov.vn`) |
| Serial | `5401160B56525DDB1B2B5CA3F7C5D9ED` |
| Hiệu lực | 20/01/2025 → 15/06/2027 |
| Khoá | RSA 2048, nằm sau *WINCA Key Storage Provider v6.0* |
| Văn bản ký | `qd-kiem-chung.pdf` của quyết định `KS-KIEMCHUNG-01` |

Kết quả:

- **Nhịp 1** — máy chủ phát băm SHA-256 của tệp, mở phiên ký 10 phút.
- **Nhịp 2** — token ký, chữ ký RSA PKCS#1 v1.5 dài 256 byte.
- **Nhịp 3** — máy chủ **xác minh thành công** và ghi nhật ký ký số: nơi cấp `WINCA`, serial khớp
  chứng thư trong token, `nguonKhoa = USB_TOKEN`, `chuanKy = CMS_DETACHED`, `trangThaiKy = THANH_CONG`.
- **Phép thử ngược** — lấy đúng chữ ký hợp lệ đó, lật một byte rồi gửi lại: máy chủ trả
  **HTTP 422 `DU_LIEU_KHONG_HOP_LE`**. Nhịp 3 xác minh thật, không phải chỉ ghi nhận.

### Bài học rút ra khi kiểm chứng

Lần chạy đầu tiên **treo im lặng**, và nguyên nhân đáng ghi lại vì nó sẽ lặp lại với người khác:

Công cụ được gọi từ một tiến trình **không gắn console tương tác**. Driver WINCA vẫn tạo hộp thoại
"Xác nhận PIN" — enumerate cửa sổ ra thì thấy nó, đúng lớp `#32770`, đúng session, toạ độ nằm gọn
trong màn hình — nhưng `IsWindowVisible = false` và ép `ShowWindow` cũng vô ích. Lý do: luồng tạo
ra hộp thoại chính là luồng đang bị chặn trong lệnh ký, nên vòng lặp thông điệp của nó không bao
giờ chạy và cửa sổ không bao giờ được vẽ. Tiến trình đó còn giữ token, làm mọi lệnh sau xếp hàng
chờ theo.

Vì vậy:

1. Công cụ ký **phải** chạy trong cửa sổ dòng lệnh thật của người ký.
2. Chạy nền bắt buộc thì dùng `--pin`, khi đó không cần hộp thoại nào.
3. Công cụ nay có `--cho <giây>` (mặc định 120) và báo đúng nguyên nhân này khi hết giờ, thay vì
   đứng im. Treo im lặng là kiểu hỏng tệ nhất: không ai biết phải sửa gì.
