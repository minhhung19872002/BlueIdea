using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.Application.XacThuc;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu tich hop cho chuc nang 21 va 41 — MFA, CAPTCHA, quen mat khau, API cho he thong ngoai.
///
/// Day la nhung duong DUY NHAT de vao he thong tu ben ngoai, nen moi phep kiem o day deu la
/// phep kiem an toan chu khong phai phep kiem tinh nang. Dac biet nhom MFA: truoc khi co lop
/// nay, he thong bao "da bat xac thuc hai lop" nhung luong dang nhap khong he kiem tra ma —
/// mot lo hong khong lo ra khi bam thu tren giao dien.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class XacThucNangCaoTests
{
    private const string MatKhau = "Sk@2026";

    private readonly UngDungKiemThu _ungDung;

    public XacThucNangCaoTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ---------------------------------------------------------------------------- MFA

    [Fact]
    public async Task Bat_Mfa_Roi_Thi_Dung_Mat_Khau_Van_Khong_Vao_Duoc()
    {
        var (client, biMat) = await BatMfaAsync("cb.mai");

        try
        {
            var phanHoi = await _ungDung.CreateClient().PostAsJsonAsync(
                "/api/v1/xac-thuc/dang-nhap", new { tenDangNhap = "cb.mai", matKhau = MatKhau });

            phanHoi.StatusCode.Should().NotBe(HttpStatusCode.OK,
                "day chinh la lo hong cu: dung mat khau la duoc cap token du da bat MFA");

            (await MaLoiAsync(phanHoi)).Should().Be("CAN_XAC_THUC_MFA");
        }
        finally
        {
            await TatMfaAsync(client, biMat);
        }
    }

    [Fact]
    public async Task Sai_Mat_Khau_Voi_Mfa_Van_Tra_Ve_Can_Xac_Thuc_Mfa()
    {
        var (client, biMat) = await BatMfaAsync("cb.linh");

        try
        {
            var dungMk = await _ungDung.CreateClient().PostAsJsonAsync(
                "/api/v1/xac-thuc/dang-nhap", new { tenDangNhap = "cb.linh", matKhau = MatKhau });

            var saiMk = await _ungDung.CreateClient().PostAsJsonAsync(
                "/api/v1/xac-thuc/dang-nhap",
                new { tenDangNhap = "cb.linh", matKhau = "MatKhauSai!123" });

            var maDung = await MaLoiAsync(dungMk);
            var maSai = await MaLoiAsync(saiMk);

            maDung.Should().Be("CAN_XAC_THUC_MFA");
            maSai.Should().Be("CAN_XAC_THUC_MFA",
                "sai mat khau phai tra ve cung ma loi de khong lo thong tin mat khau dung/sai");

            dungMk.StatusCode.Should().Be(saiMk.StatusCode,
                "HTTP status code phai dong nhat giua dung va sai mat khau");
        }
        finally
        {
            await TatMfaAsync(client, biMat);
        }
    }

    [Fact]
    public async Task Sai_Mat_Khau_Voi_Totp_Hop_Le_Bi_Tu_Choi()
    {
        var (client, biMat) = await BatMfaAsync("cb.trang");

        try
        {
            var ma = BoTotp.SinhMa(biMat, BoTotp.TinhBuoc(DateTimeOffset.UtcNow) + 1);

            var phanHoi = await _ungDung.CreateClient().PostAsJsonAsync(
                "/api/v1/xac-thuc/dang-nhap",
                new { tenDangNhap = "cb.trang", matKhau = "MatKhauSai!999", maMfa = ma });

            phanHoi.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
            (await MaLoiAsync(phanHoi)).Should().Be("SAI_TAI_KHOAN_MAT_KHAU");
        }
        finally
        {
            await TatMfaAsync(client, biMat);
        }
    }

    [Fact]
    public async Task Dang_Nhap_Duoc_Khi_Nhap_Dung_Ma()
    {
        var (client, biMat) = await BatMfaAsync("cb.long");

        try
        {
            // Buoc ghi danh vua "tieu" mat buoc thoi gian hien tai nen phai dung buoc ke tiep.
            var ma = BoTotp.SinhMa(biMat, BoTotp.TinhBuoc(DateTimeOffset.UtcNow) + 1);

            var phanHoi = await _ungDung.CreateClient().PostAsJsonAsync(
                "/api/v1/xac-thuc/dang-nhap",
                new { tenDangNhap = "cb.long", matKhau = MatKhau, maMfa = ma });

            phanHoi.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await TatMfaAsync(client, biMat);
        }
    }

    [Fact]
    public async Task Khong_Dung_Lai_Duoc_Ma_Da_Dung()
    {
        var (client, biMat) = await BatMfaAsync("cb.ngan");

        try
        {
            var ma = BoTotp.SinhMa(biMat, BoTotp.TinhBuoc(DateTimeOffset.UtcNow) + 1);

            var lan1 = await _ungDung.CreateClient().PostAsJsonAsync(
                "/api/v1/xac-thuc/dang-nhap",
                new { tenDangNhap = "cb.ngan", matKhau = MatKhau, maMfa = ma });

            lan1.StatusCode.Should().Be(HttpStatusCode.OK);

            var lan2 = await _ungDung.CreateClient().PostAsJsonAsync(
                "/api/v1/xac-thuc/dang-nhap",
                new { tenDangNhap = "cb.ngan", matKhau = MatKhau, maMfa = ma });

            lan2.StatusCode.Should().NotBe(HttpStatusCode.OK,
                "ma TOTP con hieu luc toi 90 giay; khong chan thi nguoi nhin trom man hinh "
                + "dien thoai van kip mo phien thu hai");
        }
        finally
        {
            await TatMfaAsync(client, biMat);
        }
    }

    [Fact]
    public async Task Ma_Khoi_Phuc_Chi_Dung_Duoc_Mot_Lan()
    {
        var (client, biMat, maKhoiPhuc) = await BatMfaDayDuAsync("cb.phuc");

        try
        {
            var ma = maKhoiPhuc[0];

            var lan1 = await _ungDung.CreateClient().PostAsJsonAsync(
                "/api/v1/xac-thuc/dang-nhap",
                new { tenDangNhap = "cb.phuc", matKhau = MatKhau, maMfa = ma });

            lan1.StatusCode.Should().Be(HttpStatusCode.OK);

            var lan2 = await _ungDung.CreateClient().PostAsJsonAsync(
                "/api/v1/xac-thuc/dang-nhap",
                new { tenDangNhap = "cb.phuc", matKhau = MatKhau, maMfa = ma });

            lan2.StatusCode.Should().NotBe(HttpStatusCode.OK);
        }
        finally
        {
            await TatMfaAsync(client, biMat);
        }
    }

    [Fact]
    public async Task Tat_Mfa_Phai_Nhap_Dung_Mat_Khau()
    {
        var (client, biMat) = await BatMfaAsync("cb.tam");

        try
        {
            var ma = BoTotp.SinhMa(biMat, BoTotp.TinhBuoc(DateTimeOffset.UtcNow) + 1);

            var phanHoi = await client.PostAsJsonAsync(
                "/api/v1/xac-thuc/mfa/tat", new { matKhau = "mat-khau-sai", ma });

            phanHoi.StatusCode.Should().NotBe(HttpStatusCode.OK,
                "neu chi can phien dang mo la tat duoc thi mot may bo quen khoa man hinh "
                + "du de vo hieu hoa lop bao ve nay");
        }
        finally
        {
            await TatMfaAsync(client, biMat);
        }
    }

    // ------------------------------------------------------------------ Quen mat khau

    [Theory]
    [InlineData("khong-ton-tai-abcxyz")]
    [InlineData("admin")]
    public async Task Quen_Mat_Khau_Khong_Tiet_Lo_Tai_Khoan_Co_Ton_Tai(string dinhDanh)
    {
        var khach = _ungDung.CreateClient();

        var phanHoi = await khach.PostAsJsonAsync(
            "/api/v1/xac-thuc/quen-mat-khau", new { dinhDanh });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.OK,
            "phan hoi khac nhau se bien endpoint nay thanh cong cu do danh sach tai khoan");
    }

    [Fact]
    public async Task Quen_Mat_Khau_Tra_Cung_Mot_Thong_Bao_Cho_Moi_Truong_Hop()
    {
        var khach = _ungDung.CreateClient();

        var coThat = await ThongBaoAsync(await khach.PostAsJsonAsync(
            "/api/v1/xac-thuc/quen-mat-khau", new { dinhDanh = "admin" }));

        var khongCo = await ThongBaoAsync(await khach.PostAsJsonAsync(
            "/api/v1/xac-thuc/quen-mat-khau", new { dinhDanh = "khong-he-ton-tai-zzz" }));

        coThat.Should().Be(khongCo);
    }

    [Fact]
    public async Task Otp_Sai_Bi_Tu_Choi()
    {
        var khach = _ungDung.CreateClient();

        var phanHoi = await khach.PostAsJsonAsync("/api/v1/xac-thuc/dat-lai-mat-khau",
            new { tenDangNhap = "admin", ma = "000000", matKhauMoi = "MatKhauMoi@2026" });

        phanHoi.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ------------------------------------------------------------------------ CAPTCHA

    [Fact]
    public async Task Sinh_Duoc_Anh_Captcha()
    {
        var khach = _ungDung.CreateClient();

        var phanHoi = await khach.GetAsync("/api/v1/xac-thuc/captcha");
        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu");

        duLieu.GetProperty("id").GetString().Should().NotBeNullOrEmpty();

        var svg = duLieu.GetProperty("anhSvg").GetString()!;

        svg.Should().StartWith("<svg").And.EndWith("</svg>");

        // Anh tu sinh trong he thong, khong nhung tai nguyen ngoai nao — rang buoc Muc 3.2
        // E-HSMT cam goi dich vu AI/CAPTCHA cua ben thu ba.
        //
        // KHONG kiem bang "khong chua http://": thuoc tinh xmlns cua SVG bat buoc phai la
        // "http://www.w3.org/2000/svg", do la dinh danh khong gian ten chu khong phai mot
        // dia chi duoc tai ve. Kiem dung cac cach nhung tai nguyen that.
        svg.Should().NotContain("<image").And.NotContain("xlink:href").And.NotContain("url(http");

        // Va toan bo ky tu cua ma nam ngay trong anh, khong tro sang tep ben ngoai nao.
        svg.Should().Contain("<text");
    }

    // --------------------------------------------------------- API cho he thong ngoai

    [Fact]
    public async Task Api_He_Thong_Ngoai_Chan_Khi_Thieu_Hoac_Sai_Khoa()
    {
        var khach = _ungDung.CreateClient();

        (await khach.GetAsync("/api/public/v1/sang-kien"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        khach.DefaultRequestHeaders.Add("X-Api-Key", "bik_khoa-bia-dat");

        (await khach.GetAsync("/api/public/v1/sang-kien"))
            .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Api_He_Thong_Ngoai_Cho_Qua_Khi_Khoa_Dung()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var khoa = await CapKhoaAsync(admin, "Kiem thu tich hop", new List<string>());

        try
        {
            var khach = _ungDung.CreateClient();
            khach.DefaultRequestHeaders.Add("X-Api-Key", khoa.Khoa);

            var phanHoi = await khach.GetAsync("/api/public/v1/sang-kien");

            phanHoi.StatusCode.Should().Be(HttpStatusCode.OK);
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/khoa-api-ngoai/{khoa.Id}");
        }
    }

    [Fact]
    public async Task Danh_Sach_Khoa_Khong_Bao_Gio_Kem_Khoa_Goc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var khoa = await CapKhoaAsync(admin, "Kiem thu lo khoa", new List<string>());

        try
        {
            var phanHoi = await admin.GetAsync("/api/v1/khoa-api-ngoai");
            var noiDung = await phanHoi.Content.ReadAsStringAsync();

            // Khoa goc chi duoc hien dung mot lan luc cap. Neu no con xuat hien o bat ky
            // phan hoi nao khac, mot lan lo man hinh quan tri la lo quyen truy cap doi tac.
            noiDung.Should().NotContain(khoa.Khoa);
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/khoa-api-ngoai/{khoa.Id}");
        }
    }

    [Fact]
    public async Task Khoa_Bi_Tam_Dung_Thi_Tu_Choi_Ngay()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var khoa = await CapKhoaAsync(admin, "Kiem thu tam dung", new List<string>());

        try
        {
            await admin.PostAsync($"/api/v1/khoa-api-ngoai/{khoa.Id}/trang-thai?bat=false", null);

            var khach = _ungDung.CreateClient();
            khach.DefaultRequestHeaders.Add("X-Api-Key", khoa.Khoa);

            (await khach.GetAsync("/api/public/v1/sang-kien"))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/khoa-api-ngoai/{khoa.Id}");
        }
    }

    [Fact]
    public async Task Khoa_Bi_Chan_Khi_Ip_Khong_Nam_Trong_Danh_Sach()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        // Dai nay khong the la dia chi cua may chay kiem thu (RFC 5737 danh cho tai lieu).
        var khoa = await CapKhoaAsync(admin, "Kiem thu IP", new List<string> { "203.0.113.7" });

        try
        {
            var khach = _ungDung.CreateClient();
            khach.DefaultRequestHeaders.Add("X-Api-Key", khoa.Khoa);

            (await khach.GetAsync("/api/public/v1/sang-kien"))
                .StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/khoa-api-ngoai/{khoa.Id}");
        }
    }

    [Fact]
    public async Task Ip_Sai_Dinh_Dang_Bi_Chan_Ngay_Khi_Luu()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/khoa-api-ngoai", new
        {
            ten = "Khoa sai IP",
            danhSachIp = new[] { "999.1.1.1" },
            ngayHetHan = (DateTimeOffset?)null,
            ghiChu = (string?)null
        });

        // Bat luc luu chu khong de den luc goi that: mot dong sai chinh ta se lam he thong
        // doi tac bi tu choi ma khong ai biet vi sao.
        phanHoi.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    // ---------------------------------------------------------------- Bo loc yeu thich

    [Fact]
    public async Task Bo_Loc_Cua_Nguoi_Nay_Khong_Hien_Cho_Nguoi_Khac()
    {
        var mot = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var hai = await _ungDung.TaoClientDaDangNhapAsync("cb.khoa");

        var tao = await mot.PostAsJsonAsync("/api/v1/bo-loc-yeu-thich", new
        {
            manHinh = "KIEM_THU_RIENG_TU",
            ten = "Bộ lọc riêng của admin",
            thamSo = "{\"a\":1}",
            macDinh = false
        });

        tao.EnsureSuccessStatusCode();
        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetGuid();

        try
        {
            var cuaNguoiKhac = await hai.GetAsync(
                "/api/v1/bo-loc-yeu-thich?manHinh=KIEM_THU_RIENG_TU");

            var duLieu = (await cuaNguoiKhac.Content.ReadFromJsonAsync<JsonElement>())
                .GetProperty("duLieu");

            duLieu.GetArrayLength().Should().Be(0, "bo loc la du lieu ca nhan");

            // Va cung khong xoa duoc bo loc cua nguoi khac du biet dinh danh.
            (await hai.DeleteAsync($"/api/v1/bo-loc-yeu-thich/{id}"))
                .StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
        finally
        {
            await mot.DeleteAsync($"/api/v1/bo-loc-yeu-thich/{id}");
        }
    }

    [Fact]
    public async Task Tham_So_Khong_Phai_Json_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/bo-loc-yeu-thich", new
        {
            manHinh = "KIEM_THU",
            ten = "Tham số hỏng",
            thamSo = "khong-phai-json",
            macDinh = false
        });

        // Cot khai bao kieu jsonb: khong bat o tang ung dung thi nguoi dung nhan loi 500.
        phanHoi.StatusCode.Should().NotBe(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Luu_Trung_Ten_Thi_Ghi_De_Va_Chi_Mot_Bo_Loc_Mac_Dinh()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        const string manHinh = "KIEM_THU_GHI_DE";

        var mot = await LuuBoLocAsync(admin, manHinh, "Hồ sơ quá hạn", """{"chiQuaHan":"true"}""", true);
        var hai = await LuuBoLocAsync(admin, manHinh, "Hồ sơ quá hạn",
            """{"chiQuaHan":"true","trangThaiTong":"DANG_XU_LY"}""", true);

        try
        {
            // Trung ten = cap nhat chinh bo loc cu, khong tao them ban ghi thu hai.
            hai.Should().Be(mot);

            var ba = await LuuBoLocAsync(admin, manHinh, "Chờ tiếp nhận", """{"trangThaiTong":"DA_NOP"}""", true);

            try
            {
                var phanHoi = await admin.GetAsync($"/api/v1/bo-loc-yeu-thich?manHinh={manHinh}");
                phanHoi.EnsureSuccessStatusCode();

                var danhSach = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
                    .GetProperty("duLieu").EnumerateArray().ToList();

                danhSach.Should().HaveCount(2);

                // Dat mac dinh bo loc moi phai BO mac dinh cua bo loc cu.
                danhSach.Count(x => x.GetProperty("macDinh").GetBoolean()).Should().Be(1);

                danhSach.Single(x => x.GetProperty("macDinh").GetBoolean())
                    .GetProperty("ten").GetString().Should().Be("Chờ tiếp nhận");

                // Tham so cua lan luu sau phai duoc giu lai.
                danhSach.Single(x => x.GetProperty("id").GetGuid() == mot)
                    .GetProperty("thamSo").GetString().Should().Contain("DANG_XU_LY");
            }
            finally
            {
                await admin.DeleteAsync($"/api/v1/bo-loc-yeu-thich/{ba}");
            }
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/bo-loc-yeu-thich/{mot}");
        }
    }

    private static async Task<Guid> LuuBoLocAsync(
        HttpClient client, string manHinh, string ten, string thamSo, bool macDinh)
    {
        var phanHoi = await client.PostAsJsonAsync(
            "/api/v1/bo-loc-yeu-thich", new { manHinh, ten, thamSo, macDinh });

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetGuid();
    }

    // ------------------------------------------------------------------------ Ham phu tro

    private async Task<(HttpClient Client, string BiMat)> BatMfaAsync(string tenDangNhap)
    {
        var (client, biMat, _) = await BatMfaDayDuAsync(tenDangNhap);
        return (client, biMat);
    }

    private async Task<(HttpClient Client, string BiMat, List<string> MaKhoiPhuc)>
        BatMfaDayDuAsync(string tenDangNhap)
    {
        var client = await _ungDung.TaoClientDaDangNhapAsync(tenDangNhap);

        var batDau = await client.PostAsync("/api/v1/xac-thuc/mfa/bat-dau-ghi-danh", null);
        batDau.EnsureSuccessStatusCode();

        var biMat = (await batDau.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("biMat").GetString()!;

        var ma = BoTotp.SinhMa(biMat, BoTotp.TinhBuoc(DateTimeOffset.UtcNow));

        var xacNhan = await client.PostAsJsonAsync(
            "/api/v1/xac-thuc/mfa/xac-nhan-ghi-danh", new { ma });

        xacNhan.EnsureSuccessStatusCode();

        var maKhoiPhuc = (await xacNhan.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("maKhoiPhuc")
            .EnumerateArray().Select(x => x.GetString()!).ToList();

        return (client, biMat, maKhoiPhuc);
    }

    /// <summary>
    /// Tat MFA de tra tai khoan ve trang thai ban dau.
    ///
    /// Thu lan luot cac buoc thoi gian lan can: buoc hien tai co the da bi "tieu" boi chinh
    /// phep kiem vua chay, va kiem thu khong duoc phep phu thuoc vao thoi diem no chay.
    /// </summary>
    private static async Task TatMfaAsync(HttpClient client, string biMat)
    {
        var buoc = BoTotp.TinhBuoc(DateTimeOffset.UtcNow);

        foreach (var lech in new[] { 1, 0, -1 })
        {
            var phanHoi = await client.PostAsJsonAsync("/api/v1/xac-thuc/mfa/tat", new
            {
                matKhau = MatKhau,
                ma = BoTotp.SinhMa(biMat, buoc + lech)
            });

            if (phanHoi.IsSuccessStatusCode)
            {
                return;
            }
        }

        throw new InvalidOperationException("Không tắt được MFA để dọn dẹp sau kiểm thử.");
    }

    private static async Task<(Guid Id, string Khoa)> CapKhoaAsync(
        HttpClient admin, string ten, List<string> danhSachIp)
    {
        var phanHoi = await admin.PostAsJsonAsync("/api/v1/khoa-api-ngoai", new
        {
            ten,
            danhSachIp,
            ngayHetHan = (DateTimeOffset?)null,
            ghiChu = (string?)null
        });

        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu");

        return (duLieu.GetProperty("id").GetGuid(), duLieu.GetProperty("khoa").GetString()!);
    }

    private static async Task<string?> MaLoiAsync(HttpResponseMessage phanHoi)
    {
        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();

        return noiDung.TryGetProperty("maLoi", out var ma) ? ma.GetString() : null;
    }

    private static async Task<string?> ThongBaoAsync(HttpResponseMessage phanHoi)
    {
        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();

        return noiDung.TryGetProperty("thongBao", out var tb) ? tb.GetString() : null;
    }
}
