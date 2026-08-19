# Kế hoạch công tác

> Tài liệu phục vụ **Mục 4 E-HSMT (Giải pháp và phương pháp luận → Kế hoạch công tác)**.
>
> Các ô đánh dấu `«cần điền»` phụ thuộc hợp đồng và nhân sự thực tế của nhà thầu, không suy ra
> được từ mã nguồn — phải điền trước khi nộp hồ sơ. Phần còn lại phản ánh đúng hiện trạng kho mã.

## 1. Phạm vi công việc

Xây dựng, triển khai và chuyển giao **Nền tảng số dùng chung phục vụ hoạt động sáng kiến**, đáp ứng
51 chức năng tại Chương V E-HSMT, kèm yêu cầu an toàn thông tin cấp độ 2 và các yêu cầu phi chức năng.

| Nhóm công việc | Nội dung | Sản phẩm bàn giao |
|---|---|---|
| Khảo sát & chuẩn hoá yêu cầu | Đối chiếu 51 chức năng, chốt quy trình và bộ tiêu chí của đơn vị | `docs/00-MASTER-SPEC.md`, biên bản khảo sát |
| Xây dựng phần mềm | Backend .NET 8, frontend React 18, AI nội bộ, tích hợp | Mã nguồn + Docker image |
| Kiểm thử | Unit, integration trên PostgreSQL thật, E2E Playwright | Báo cáo kiểm thử, `docs/KICH-BAN-NGHIEM-THU.md` |
| Triển khai | Cài đặt hạ tầng, nạp danh mục, tài khoản, phân quyền | `docs/DEPLOYMENT.md`, biên bản triển khai |
| Đào tạo | Theo từng nhóm vai trò | Tài liệu + video hướng dẫn |
| Nghiệm thu | Chạy kịch bản nghiệm thu với Chủ đầu tư | Biên bản nghiệm thu |
| Bảo hành, hỗ trợ | «cần điền: thời hạn bảo hành theo hợp đồng» | Nhật ký hỗ trợ |

## 2. Tiến độ theo giai đoạn

Mốc tính theo **tuần kể từ ngày hợp đồng có hiệu lực** (T0 = ngày ký hợp đồng).

| Giai đoạn | Nội dung chính | Mốc | Điều kiện hoàn thành |
|---|---|---|---|
| GĐ1 | Khảo sát, chốt quy trình xử lý và bộ tiêu chí chấm điểm của đơn vị | T0 → T0+2 | Biên bản chốt quy trình, bộ tiêu chí |
| GĐ2 | Nền tảng: hạ tầng, xác thực, phân quyền, danh mục (chức năng 1–8, 43–48) | T0+2 → T0+6 | Quản trị viên nhập được toàn bộ danh mục |
| GĐ3 | Quy trình động + tiêu chí động + hội đồng (chức năng 9–20) | T0+6 → T0+10 | Thiết kế và kích hoạt được quy trình thật của đơn vị |
| GĐ4 | Nộp hồ sơ, tiếp nhận, xử lý, đánh giá (chức năng 21–36) | T0+10 → T0+15 | Chạy trọn một hồ sơ từ nộp đến ban hành quyết định |
| GĐ5 | AI kiểm tra trùng lặp, tra cứu, báo cáo (chức năng 26, 37–40) | T0+15 → T0+18 | Phát hiện đúng cặp hồ sơ trùng; đủ 3 báo cáo bắt buộc |
| GĐ6 | Tích hợp SSO/IOC/TĐKT, chữ ký số, email/SMS (chức năng 41, 49, 50) | T0+18 → T0+21 | Thử kết nối thành công với hệ thống thật của thành phố |
| GĐ7 | Kiểm thử tổng thể, an toàn thông tin, tối ưu hiệu năng | T0+21 → T0+23 | Báo cáo kiểm thử, hồ sơ ATTT cấp độ 2 |
| GĐ8 | Đào tạo, nghiệm thu, bàn giao | T0+23 → T0+25 | Biên bản nghiệm thu, bàn giao mã nguồn và tài liệu |

> Mốc cụ thể theo ngày: «cần điền sau khi ký hợp đồng».

## 3. Nhân sự

| Vai trò | Số lượng | Trách nhiệm chính | Nhân sự |
|---|---|---|---|
| Quản trị dự án | 1 | Điều phối tiến độ, làm việc với Chủ đầu tư, quản lý rủi ro | «cần điền» |
| Kiến trúc sư giải pháp | 1 | Kiến trúc hệ thống, quyết định kỹ thuật (ghi vào `docs/ADR/`) | «cần điền» |
| Lập trình viên backend | «cần điền» | .NET 8, CQRS, PostgreSQL, workflow/scoring engine | «cần điền» |
| Lập trình viên frontend | «cần điền» | React 18, TypeScript, Ant Design | «cần điền» |
| Kỹ sư AI/OCR | 1 | Tesseract, pipeline trùng lặp chạy nội bộ | «cần điền» |
| Kỹ sư kiểm thử | 1 | Kịch bản nghiệm thu, kiểm thử tự động | «cần điền» |
| Kỹ sư hệ thống/ATTT | 1 | Triển khai, sao lưu, cấu hình an toàn thông tin | «cần điền» |
| Chuyên viên nghiệp vụ | 1 | Chuẩn hoá quy trình, đào tạo người dùng | «cần điền» |

## 4. Phương pháp quản lý chất lượng

- **Truy vết yêu cầu**: mỗi chức năng trong 51 chức năng có dòng trong `docs/requirements/traceability.yaml`,
  ghi rõ đường dẫn giao diện, endpoint, bảng dữ liệu, tệp kiểm thử và trạng thái xác minh.
- **Định nghĩa "hoàn thành"**: chức năng chỉ được coi là xong khi có API + phân quyền + giao diện +
  kiểm thử chạy trên hạ tầng thật + dòng tương ứng trong `docs/KICH-BAN-NGHIEM-THU.md`.
- **Kiểm thử tự động** chạy trên mỗi lần thay đổi:
  - unit test cho quy tắc nghiệp vụ (rule evaluator, scoring engine, hạn xử lý theo ngày làm việc, ký số);
  - integration test chạy trên **PostgreSQL thật** (Testcontainers), không dùng cơ sở dữ liệu giả lập;
  - E2E Playwright trên giao diện thật, không chặn/giả lập API nghiệp vụ.
- **Rà soát mã nguồn**: mỗi thay đổi đi qua nhánh riêng và pull request, có rà soát bảo mật cho phần
  liên quan tới xác thực, phân quyền, tệp tin và ký số.
- **Nhật ký quyết định kiến trúc**: mọi lựa chọn ảnh hưởng lâu dài ghi vào `docs/ADR/`.

## 5. Quản lý rủi ro

| Rủi ro | Ảnh hưởng | Cách xử lý |
|---|---|---|
| Chưa có endpoint/khoá thật của SSO, IOC, Thi đua khen thưởng | Không đấu nối liên thông đúng hạn | Adapter đã hoàn thiện và kiểm chứng bằng máy chủ mô phỏng; đấu nối thật chỉ là khai cấu hình. Đề nghị Chủ đầu tư cung cấp trong GĐ6 |
| Chưa có chứng thư số của CA được cấp phép | Chữ ký chưa có giá trị pháp lý | Chạy bằng chứng thư tự ký trong kiểm thử; nạp chứng thư thật khi bàn giao |
| Quy trình xử lý của đơn vị thay đổi giữa chừng | Ảnh hưởng tiến độ GĐ3–GĐ4 | Quy trình là cấu hình, không phải mã nguồn; hồ sơ đang chạy giữ snapshot quy trình cũ (`docs/ADR/0002`) |
| Dữ liệu sáng kiến các năm trước không sẵn sàng | Kiểm tra trùng lặp thiếu kho đối chiếu | Nhập từ Excel; kho đối chiếu bổ sung dần không cần sửa mã |
| Hạ tầng máy chủ của đơn vị hạn chế | Ảnh hưởng hiệu năng, không chạy được MinIO | Đã có cấu hình chạy lưu trữ trên đĩa máy chủ; chuyển sang MinIO chỉ đổi cấu hình |

## 6. Bàn giao và bảo hành

**Sản phẩm bàn giao**: mã nguồn đầy đủ, Docker image, cơ sở dữ liệu mẫu, và bộ tài liệu:

| Tài liệu | Tệp |
|---|---|
| Mô tả giải pháp | `docs/TAI-LIEU-MO-TA-GIAI-PHAP.md` |
| Hướng dẫn sử dụng theo vai trò | `docs/TAI-LIEU-HUONG-DAN-SU-DUNG.md` |
| Quản trị vận hành | `docs/TAI-LIEU-QUAN-TRI-VAN-HANH.md` |
| Triển khai | `docs/DEPLOYMENT.md` |
| An toàn thông tin cấp độ 2 | `docs/AN-TOAN-THONG-TIN.md` |
| Quy chế sử dụng hệ thống | `docs/QUY-CHE-SU-DUNG-HE-THONG.md` |
| Tài liệu API | `docs/API.md` |
| Kịch bản nghiệm thu | `docs/KICH-BAN-NGHIEM-THU.md` |
| Trạng thái triển khai từng chức năng | `docs/TRANG-THAI-TRIEN-KHAI.md` |

**Đào tạo**: theo nhóm vai trò — quản trị hệ thống, cán bộ tiếp nhận/xử lý, thư ký và thành viên
hội đồng, tác giả. Thời lượng và số lớp: «cần điền theo hợp đồng».

**Bảo hành**: «cần điền thời hạn». Trong thời gian bảo hành, nhà thầu xử lý lỗi theo mức độ:

| Mức | Ví dụ | Thời gian phản hồi | Thời gian khắc phục |
|---|---|---|---|
| Nghiêm trọng | Không đăng nhập được, không nộp được hồ sơ, mất dữ liệu | «cần điền» | «cần điền» |
| Cao | Một chức năng nghiệp vụ không dùng được, có cách làm thay thế | «cần điền» | «cần điền» |
| Trung bình/thấp | Lỗi hiển thị, sai chính tả, đề nghị cải tiến | «cần điền» | «cần điền» |
