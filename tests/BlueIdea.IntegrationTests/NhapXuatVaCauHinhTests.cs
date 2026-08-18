using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;
using ClosedXML.Excel;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu tich hop cho chuc nang 6 (quet placeholder .docx), 7 (bao cao tuy bien),
/// 43 (nhap nguoi dung tu Excel) va 50 (cau hinh gui tin).
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class NhapXuatVaCauHinhTests
{
    private readonly UngDungKiemThu _ungDung;

    public NhapXuatVaCauHinhTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ----------------------------------------------- Chuc nang 43: nhap tu Excel

    [Fact]
    public async Task Tai_Duoc_Tep_Mau_Nhap_Nguoi_Dung()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/nhap-xuat/nguoi-dung/mau-nhap");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadAsByteArrayAsync();
        noiDung.Should().NotBeEmpty();

        // Tep mau phai doc lai duoc va co dung hang tieu de.
        using var bo = new MemoryStream(noiDung);
        using var workbook = new XLWorkbook(bo);

        workbook.Worksheet(1).Cell(1, 1).GetString().Should().Be("Tên đăng nhập");
        workbook.Worksheet(1).Cell(1, 7).GetString().Should().Be("Mã vai trò");
    }

    [Fact]
    public async Task Chay_Thu_Bao_Loi_Tung_Dong_Va_Khong_Ghi_Gi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tenHopLe = $"kt.{Guid.NewGuid():N}"[..14];

        var tep = TaoExcelNguoiDung(new[]
        {
            new[] { tenHopLe, "Người Hợp Lệ", "hople@kiemthu.local", "0912345678", "Chuyên viên", "", "TAC_GIA" },
            new[] { "admin", "Trùng tài khoản", "", "", "", "", "TAC_GIA" },
            new[] { "kt.thieu.vaitro", "Thiếu vai trò", "", "", "", "", "" },
            new[] { "kt nguyễn a", "Sai định dạng tên", "", "", "", "", "TAC_GIA" },
            new[] { "kt.sai.donvi", "Sai mã đơn vị", "", "", "", "KHONG_TON_TAI", "TAC_GIA" }
        });

        var ketQua = await NhapAsync(admin, tep, chayThu: true);

        ketQua.GetProperty("chayThu").GetBoolean().Should().BeTrue();
        ketQua.GetProperty("tongDong").GetInt32().Should().Be(5);
        ketQua.GetProperty("soHopLe").GetInt32().Should().Be(1);
        ketQua.GetProperty("soLoi").GetInt32().Should().Be(4);

        var loi = ketQua.GetProperty("chiTiet").EnumerateArray()
            .Where(x => !x.GetProperty("hopLe").GetBoolean())
            .Select(x => x.GetProperty("loi").GetString()!)
            .ToList();

        loi.Should().ContainSingle(x => x.Contains("đã tồn tại"));
        loi.Should().ContainSingle(x => x.Contains("Thiếu mã vai trò"));
        loi.Should().ContainSingle(x => x.Contains("chữ cái không dấu"));
        loi.Should().ContainSingle(x => x.Contains("Mã đơn vị"));

        // Chay thu tuyet doi khong duoc tao tai khoan nao.
        var timThay = await TimTaiKhoanAsync(admin, tenHopLe);
        timThay.Should().BeFalse("chạy thử không được ghi vào cơ sở dữ liệu");
    }

    [Fact]
    public async Task Con_Dong_Loi_Thi_Khong_Tao_Tai_Khoan_Nao()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tenHopLe = $"kt.{Guid.NewGuid():N}"[..14];

        var tep = TaoExcelNguoiDung(new[]
        {
            new[] { tenHopLe, "Người Hợp Lệ", "", "", "", "", "TAC_GIA" },
            new[] { "admin", "Trùng tài khoản", "", "", "", "", "TAC_GIA" }
        });

        // Goi voi chayThu=false nhung van con loi -> phai tu chuyen ve che do bao cao.
        var ketQua = await NhapAsync(admin, tep, chayThu: false);

        ketQua.GetProperty("chayThu").GetBoolean().Should()
            .BeTrue("còn dòng lỗi thì hệ thống phải trả về báo cáo thay vì ghi một phần");
        ketQua.GetProperty("soLoi").GetInt32().Should().Be(1);

        (await TimTaiKhoanAsync(admin, tenHopLe)).Should().BeFalse();
    }

    [Fact]
    public async Task Nhap_That_Tao_Duoc_Tai_Khoan_Va_Dang_Nhap_Duoc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var ten1 = $"kt.{Guid.NewGuid():N}"[..14];
        var ten2 = $"kt.{Guid.NewGuid():N}"[..14];

        var tep = TaoExcelNguoiDung(new[]
        {
            new[] { ten1, "Nguyễn Văn Một", $"{ten1}@kiemthu.local", "0912345678", "Chuyên viên", "", "TAC_GIA" },
            new[] { ten2, "Trần Thị Hai", "", "", "", "", "TAC_GIA" }
        });

        var ketQua = await NhapAsync(admin, tep, chayThu: false);

        ketQua.GetProperty("chayThu").GetBoolean().Should().BeFalse();
        ketQua.GetProperty("soHopLe").GetInt32().Should().Be(2);
        ketQua.GetProperty("soLoi").GetInt32().Should().Be(0);

        var dong = ketQua.GetProperty("chiTiet").EnumerateArray()
            .First(x => x.GetProperty("tenDangNhap").GetString() == ten1);

        var matKhauTam = dong.GetProperty("matKhauTam").GetString()!;
        matKhauTam.Should().NotBeNullOrEmpty();

        // Tai khoan vua nhap phai dang nhap duoc ngay bang mat khau tam.
        var client = _ungDung.CreateClient();
        var dangNhap = await client.PostAsJsonAsync("/api/v1/xac-thuc/dang-nhap", new
        {
            tenDangNhap = ten1,
            matKhau = matKhauTam
        });

        dangNhap.EnsureSuccessStatusCode();
        (await dangNhap.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("buocDoiMatKhau").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Trung_Ten_Ngay_Trong_Chinh_Tep_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var ten = $"kt.{Guid.NewGuid():N}"[..14];

        var tep = TaoExcelNguoiDung(new[]
        {
            new[] { ten, "Bản ghi một", "", "", "", "", "TAC_GIA" },
            new[] { ten, "Bản ghi hai", "", "", "", "", "TAC_GIA" }
        });

        var ketQua = await NhapAsync(admin, tep, chayThu: true);

        ketQua.GetProperty("soLoi").GetInt32().Should().Be(1);
        // API bo truong null khoi JSON nen phai dung TryGetProperty cho dong hop le.
        ketQua.GetProperty("chiTiet").EnumerateArray()
            .Select(x => x.TryGetProperty("loi", out var l) ? l.GetString() : null)
            .Should().ContainSingle(x => x != null && x.Contains("lặp lại trong chính tệp"));
    }

    [Fact]
    public async Task Tac_Gia_Khong_Duoc_Nhap_Nguoi_Dung()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var tep = TaoExcelNguoiDung(new[]
        {
            new[] { "kt.khong.duoc", "Không được phép", "", "", "", "", "TAC_GIA" }
        });

        using var form = new MultipartFormDataContent();
        var noiDung = new ByteArrayContent(tep);
        noiDung.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(noiDung, "tep", "nguoi-dung.xlsx");

        var phanHoi = await tacGia.PostAsync("/api/v1/nhap-xuat/nguoi-dung?chayThu=true", form);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ----------------------------------------------- Chuc nang 7: bao cao tuy bien

    [Fact]
    public async Task Chay_Duoc_Bao_Cao_Tuy_Bien_Tu_Cau_Hinh()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var bieuMau = await LayMangAsync(admin, "/api/v1/danh-muc/bieu-mau-thong-ke/chon");
        bieuMau.Should().NotBeEmpty("dữ liệu mẫu có sẵn các biểu mẫu thống kê");

        var id = bieuMau.First(x => x.GetProperty("ma").GetString() == "BCTK_TONG_HOP")
            .GetProperty("id").GetString()!;

        var bc = await LayDuLieuAsync(admin, $"/api/v1/nhap-xuat/bao-cao-tuy-bien/{id}");

        bc.GetProperty("tenBaoCao").GetString().Should().NotBeNullOrEmpty();
        bc.GetProperty("tieuDeCot").GetArrayLength().Should().Be(7);
        bc.GetProperty("tongSo").GetInt32().Should().BeGreaterThan(0);

        // Cau hinh co ham tong hop -> phai co dong TONG CONG o cuoi.
        var cacDong = bc.GetProperty("cacDong").EnumerateArray().ToList();
        cacDong.Should().NotBeEmpty();

        cacDong[^1].EnumerateArray().Select(x => x.GetString())
            .Should().Contain("TỔNG CỘNG");
    }

    [Fact]
    public async Task Xuat_Duoc_Bao_Cao_Tuy_Bien_Ra_Excel()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var bieuMau = await LayMangAsync(admin, "/api/v1/danh-muc/bieu-mau-thong-ke/chon");
        var id = bieuMau[0].GetProperty("id").GetString()!;

        var phanHoi = await admin.GetAsync($"/api/v1/nhap-xuat/bao-cao-tuy-bien/{id}/xuat-excel");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadAsByteArrayAsync();

        using var bo = new MemoryStream(noiDung);
        using var workbook = new XLWorkbook(bo);
        workbook.Worksheets.Count.Should().Be(1);
    }

    [Fact]
    public async Task Cau_Hinh_Cot_Sai_Bi_Chan_Ngay_Khi_Luu()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/danh-muc/bieu-mau-thong-ke", new
        {
            ma = $"BCTK_SAI_{Guid.NewGuid():N}"[..16],
            ten = "Biểu mẫu cấu hình sai",
            thuTu = 99,
            trangThai = 1,
            cauHinhCot = new[]
            {
                new { ma = "x", tieuDe = "Cột lạ", nguon = "matKhauHash", hamTongHop = (string?)null, thuTu = 1 }
            }
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("thongBao").GetString().Should().Contain("không nằm trong danh sách cho phép");
    }

    // ----------------------------------------------- Chuc nang 6: quet placeholder

    [Fact]
    public async Task Quet_Duoc_Placeholder_Trong_Tep_Docx()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var docx = TaoDocx("Kính gửi {{ tenDonVi }}", "Hồ sơ ", "{{", "maHo", "So", "}}", " đã duyệt.");

        using var form = new MultipartFormDataContent();
        var noiDung = new ByteArrayContent(docx);
        noiDung.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        form.Add(noiDung, "tep", "mau.docx");

        var phanHoi = await admin.PostAsync("/api/v1/nhap-xuat/bieu-mau/quet-placeholder", form);
        phanHoi.EnsureSuccessStatusCode();

        var ketQua = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        var placeholder = ketQua.GetProperty("placeholder").EnumerateArray()
            .Select(x => x.GetString()).ToList();

        placeholder.Should().Contain("tenDonVi");
        placeholder.Should().Contain("maHoSo", "placeholder bị Word cắt thành nhiều run vẫn phải nhận ra");

        ketQua.GetProperty("nguonGoiY").GetArrayLength().Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Tep_Khong_Phai_Docx_Bi_Tu_Choi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        using var form = new MultipartFormDataContent();
        form.Add(new ByteArrayContent("%PDF-1.4"u8.ToArray()), "tep", "mau.pdf");

        var phanHoi = await admin.PostAsync("/api/v1/nhap-xuat/bieu-mau/quet-placeholder", form);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Bieu_Mau_Xuat_Luu_Duoc_Tep_Mau_Va_Anh_Xa_Placeholder()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        // 1. Tai tep mau len kho dung chung — man hinh Bieu mau xuat lam dung viec nay.
        var docx = TaoDocx("Số quyết định {{ soQuyetDinh }}", "Hồ sơ {{ maHoSo }}");

        using var form = new MultipartFormDataContent();
        var noiDungTep = new ByteArrayContent(docx);
        noiDungTep.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.wordprocessingml.document");
        form.Add(noiDungTep, "tep", "mau-quyet-dinh.docx");

        var taiLen = await admin.PostAsync("/api/v1/tep-tin/tai-len", form);
        taiLen.EnsureSuccessStatusCode();

        var tepTinId = (await taiLen.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        // 2. Luu bieu mau kem anh xa placeholder -> nguon du lieu.
        var tao = await admin.PostAsJsonAsync("/api/v1/danh-muc/bieu-mau-xuat", new
        {
            ma = "BM_KIEM_THU_ANH_XA",
            ten = "Biểu mẫu kiểm thử ánh xạ",
            thuTu = 99,
            trangThai = 1,
            loai = "QUYET_DINH",
            dinhDang = "DOCX",
            fileTemplateId = tepTinId,
            cauHinhTruong = new[]
            {
                new { placeholder = "soQuyetDinh", nguon = "soQuyetDinh", kieu = "text" },
                new { placeholder = "maHoSo", nguon = "maHoSo", kieu = "text" }
            }
        });

        tao.EnsureSuccessStatusCode();

        var bieuMauId = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        try
        {
            var chiTiet = await admin.GetAsync($"/api/v1/danh-muc/bieu-mau-xuat/{bieuMauId}");
            chiTiet.EnsureSuccessStatusCode();

            var duLieu = (await chiTiet.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("duLieu");

            duLieu.GetProperty("fileTemplateId").GetString().Should().Be(tepTinId);
            duLieu.GetProperty("dinhDang").GetString().Should().Be("DOCX");

            var anhXa = duLieu.GetProperty("cauHinhTruong").EnumerateArray().ToList();

            anhXa.Should().HaveCount(2);
            anhXa.Select(x => x.GetProperty("placeholder").GetString())
                .Should().BeEquivalentTo(new[] { "soQuyetDinh", "maHoSo" });
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/danh-muc/bieu-mau-xuat/{bieuMauId}");
        }
    }

    // ----------------------------------------------- Chuc nang 50: cau hinh gui tin

    [Fact]
    public async Task Cau_Hinh_Gui_Tin_Khong_Bao_Gio_Tra_Ve_Mat_Khau()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tao = await admin.PostAsJsonAsync("/api/v1/cau-hinh-gui-tin", new
        {
            loai = "EMAIL",
            nhaCungCap = "SMTP kiểm thử",
            host = "smtp.kiemthu.local",
            port = 587,
            tenDangNhap = "no-reply",
            matKhau = "mat-khau-bi-mat-khong-duoc-lo",
            suDungSsl = true,
            emailGuiDi = "no-reply@kiemthu.local",
            tenHienThi = "Kiểm thử",
            trangThai = 1,
            laMacDinh = true
        });

        tao.EnsureSuccessStatusCode();

        var phanHoi = await admin.GetAsync("/api/v1/cau-hinh-gui-tin");
        phanHoi.EnsureSuccessStatusCode();

        var chuoi = await phanHoi.Content.ReadAsStringAsync();

        chuoi.Should().NotContain("mat-khau-bi-mat-khong-duoc-lo",
            "mật khẩu SMTP tuyệt đối không được trả về giao diện");

        var ds = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        var muc = ds.EnumerateArray().First(x => x.GetProperty("host").GetString() == "smtp.kiemthu.local");

        muc.GetProperty("daDatMatKhau").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task Cau_Hinh_Email_Thieu_Host_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/cau-hinh-gui-tin", new
        {
            loai = "EMAIL",
            port = 587,
            emailGuiDi = "a@b.vn",
            suDungSsl = true,
            trangThai = 1,
            laMacDinh = false
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
        (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("thongBao").GetString().Should().Contain("máy chủ SMTP");
    }

    [Fact]
    public async Task Chi_Co_Mot_Cau_Hinh_Mac_Dinh_Cho_Moi_Loai()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        for (var i = 1; i <= 2; i++)
        {
            var phanHoi = await admin.PostAsJsonAsync("/api/v1/cau-hinh-gui-tin", new
            {
                loai = "SMS",
                nhaCungCap = $"Nhà cung cấp {i}",
                apiEndpoint = $"https://sms{i}.kiemthu.local/send",
                apiKey = $"key-{i}",
                brandname = "SANGKIEN",
                suDungSsl = true,
                trangThai = 1,
                laMacDinh = true
            });

            phanHoi.EnsureSuccessStatusCode();
        }

        var ds = await LayDuLieuAsync(admin, "/api/v1/cau-hinh-gui-tin");

        var soMacDinhSms = ds.EnumerateArray()
            .Count(x => x.GetProperty("loai").GetString() == "SMS"
                        && x.GetProperty("laMacDinh").GetBoolean());

        soMacDinhSms.Should().Be(1, "đặt mặc định mới phải bỏ mặc định cũ của cùng loại");
    }

    [Fact]
    public async Task Tac_Gia_Khong_Xem_Duoc_Cau_Hinh_Gui_Tin()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.GetAsync("/api/v1/cau-hinh-gui-tin");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ------------------------------------------------------------------------------------

    private static async Task<JsonElement> NhapAsync(HttpClient client, byte[] tep, bool chayThu)
    {
        using var form = new MultipartFormDataContent();
        var noiDung = new ByteArrayContent(tep);
        noiDung.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");
        form.Add(noiDung, "tep", "nguoi-dung.xlsx");

        var phanHoi = await client.PostAsync($"/api/v1/nhap-xuat/nguoi-dung?chayThu={chayThu}", form);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<bool> TimTaiKhoanAsync(HttpClient client, string tenDangNhap)
    {
        var phanHoi = await client.GetAsync(
            $"/api/v1/he-thong/nguoi-dung?tuKhoa={Uri.EscapeDataString(tenDangNhap)}");

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetArrayLength() > 0;
    }

    /// <summary>Dung tep Excel dung dinh dang cot ma API mong doi.</summary>
    private static byte[] TaoExcelNguoiDung(IReadOnlyList<string[]> cacDong)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Người dùng");

        var tieuDe = new[]
        {
            "Tên đăng nhập", "Họ và tên", "Email", "Điện thoại", "Chức vụ", "Mã đơn vị", "Mã vai trò"
        };

        for (var i = 0; i < tieuDe.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = tieuDe[i];
        }

        for (var d = 0; d < cacDong.Count; d++)
        {
            for (var c = 0; c < cacDong[d].Length; c++)
            {
                sheet.Cell(d + 2, c + 1).Value = cacDong[d][c];
            }
        }

        using var bo = new MemoryStream();
        workbook.SaveAs(bo);
        return bo.ToArray();
    }

    /// <summary>Doan dau la mot doan rieng; cac chuoi con lai la nhieu run trong doan thu hai.</summary>
    private static byte[] TaoDocx(string doanDau, params string[] cacRun)
    {
        using var bo = new MemoryStream();

        using (var tep = WordprocessingDocument.Create(bo, WordprocessingDocumentType.Document, true))
        {
            var phan = tep.AddMainDocumentPart();
            phan.Document = new Document();
            var than = phan.Document.AppendChild(new Body());

            than.AppendChild(new Paragraph(new Run(new Text(doanDau))));

            var doan = than.AppendChild(new Paragraph());
            foreach (var run in cacRun)
            {
                doan.AppendChild(new Run(new Text(run) { Space = SpaceProcessingModeValues.Preserve }));
            }

            phan.Document.Save();
        }

        return bo.ToArray();
    }

    private static async Task<JsonElement> LayDuLieuAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<List<JsonElement>> LayMangAsync(HttpClient client, string duongDan)
        => (await LayDuLieuAsync(client, duongDan)).EnumerateArray().ToList();
}
