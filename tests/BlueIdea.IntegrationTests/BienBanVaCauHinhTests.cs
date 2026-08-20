using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu tich hop cho cac hang muc bo sung dot cuoi: bien ban phien hop (nhom IV),
/// lien thong theo buoc quy trinh (chuc nang 16), cap phe duyet (chuc nang 5) va nhat ky loi.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class BienBanVaCauHinhTests
{
    private readonly UngDungKiemThu _ungDung;

    public BienBanVaCauHinhTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ------------------------------------------------------------- Bien ban hop

    [Fact]
    public async Task Chua_Ket_Thuc_Phien_Thi_Khong_Lap_Duoc_Bien_Ban()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var phienId = await TaoPhienAsync(admin);

        var phanHoi = await admin.PostAsync($"/api/v1/bien-ban-hop/phien-hop/{phienId}", null);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var loi = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("đã kết thúc");
    }

    [Fact]
    public async Task Lap_Bien_Ban_Sinh_Du_Lieu_Tu_Phien_Hop_Va_Xuat_Duoc_Pdf()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");

        var (phienId, sangKienId) = await TaoPhienDayDuAsync(admin);

        // Chu tich bo phieu roi ket thuc phien — bien ban phai chup lai dung so lieu nay.
        (await chuTich.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop/bo-phieu", new
        {
            phienHopId = phienId,
            sangKienId,
            yKien = "DONG_Y",

        })).EnsureSuccessStatusCode();

        (await chuTich.PostAsJsonAsync($"/api/v1/hoi-dong/phien-hop/{phienId}/ket-thuc",
            new { ketLuan = "Thông qua toàn bộ hồ sơ." })).EnsureSuccessStatusCode();

        var lap = await admin.PostAsync($"/api/v1/bien-ban-hop/phien-hop/{phienId}", null);
        lap.EnsureSuccessStatusCode();

        var bienBan = (await lap.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        var bienBanId = bienBan.GetProperty("id").GetString()!;

        bienBan.GetProperty("soBienBan").GetString().Should().StartWith("BB-");
        bienBan.GetProperty("ketLuanChung").GetString().Should().Contain("Thông qua");

        var dongHoSo = bienBan.GetProperty("danhSachHoSo").EnumerateArray().ToList();
        dongHoSo.Should().ContainSingle();
        dongHoSo[0].GetProperty("soPhieuDongY").GetInt32().Should().Be(1);
        dongHoSo[0].GetProperty("tyLeDongY").GetDecimal().Should().Be(100m);
        dongHoSo[0].GetProperty("datNguong").GetBoolean().Should().BeTrue();

        // Moi thanh vien co quyen ky bien ban phai co san mot dong chu ky cho.
        bienBan.GetProperty("chuKy").GetArrayLength().Should().BeGreaterThan(0);

        // Xuat PDF that.
        var pdf = await admin.GetAsync($"/api/v1/bien-ban-hop/{bienBanId}/xuat-pdf");
        pdf.EnsureSuccessStatusCode();
        pdf.Content.Headers.ContentType?.MediaType.Should().Be("application/pdf");

        var noiDung = await pdf.Content.ReadAsByteArrayAsync();
        System.Text.Encoding.ASCII.GetString(noiDung, 0, 4).Should().Be("%PDF");

        // Chu tich ky nhan.
        (await chuTich.PostAsync($"/api/v1/bien-ban-hop/{bienBanId}/ky", null))
            .EnsureSuccessStatusCode();

        var sauKy = await LayDuLieuAsync(admin, $"/api/v1/bien-ban-hop/{bienBanId}");

        sauKy.GetProperty("chuKy").EnumerateArray()
            .Count(x => x.GetProperty("daKy").GetBoolean())
            .Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task Nguoi_Ngoai_Hoi_Dong_Khong_Ky_Duoc_Bien_Ban()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");
        var nguoiNgoai = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var (phienId, sangKienId) = await TaoPhienDayDuAsync(admin);

        (await chuTich.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop/bo-phieu", new
        {
            phienHopId = phienId,
            sangKienId,
            yKien = "DONG_Y",

        })).EnsureSuccessStatusCode();

        (await chuTich.PostAsJsonAsync($"/api/v1/hoi-dong/phien-hop/{phienId}/ket-thuc",
            new { ketLuan = "Kết luận kiểm thử." })).EnsureSuccessStatusCode();

        var lap = await admin.PostAsync($"/api/v1/bien-ban-hop/phien-hop/{phienId}", null);
        lap.EnsureSuccessStatusCode();

        var bienBanId = (await lap.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        var phanHoi = await nguoiNgoai.PostAsync($"/api/v1/bien-ban-hop/{bienBanId}/ky", null);

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // -------------------------------------------- Lien thong theo buoc (chuc nang 16)

    [Fact]
    public async Task Gan_Lien_Thong_Vao_Buoc_Cua_Quy_Trinh_Khac_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var quyTrinhId = await LayQuyTrinhAsync(admin);
        var heThongId = await TaoHeThongLienThongAsync(admin);

        try
        {
            // Buoc gia (khong thuoc quy trinh nao) — phai bi chan.
            var phanHoi = await admin.PostAsJsonAsync(
                $"/api/v1/quy-trinh/{quyTrinhId}/lien-thong",
                new
                {
                    buocId = Guid.NewGuid(),
                    heThongTichHopId = heThongId,
                    suKien = "KHI_HOAN_THANH",
                    trangThai = 1
                });

            phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

            var loi = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
            loi.GetProperty("thongBao").GetString().Should().Contain("không thuộc quy trình");
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/tich-hop/he-thong/{heThongId}");
        }
    }

    [Fact]
    public async Task Them_Va_Doc_Lai_Lien_Thong_Cua_Quy_Trinh()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var quyTrinhId = await LayQuyTrinhAsync(admin);
        var heThongId = await TaoHeThongLienThongAsync(admin);

        var tao = await admin.PostAsJsonAsync($"/api/v1/quy-trinh/{quyTrinhId}/lien-thong", new
        {
            buocId = (Guid?)null,
            heThongTichHopId = heThongId,
            suKien = "KHI_PHE_DUYET",
            loaiDuLieu = "SANG_KIEN_DUOC_CONG_NHAN",
            trangThai = 1
        });

        tao.EnsureSuccessStatusCode();

        var id = (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu").GetGuid();

        try
        {
            var ds = await LayMangAsync(admin, $"/api/v1/quy-trinh/{quyTrinhId}/lien-thong");

            var dong = ds.Single(x => x.GetProperty("id").GetGuid() == id);
            dong.GetProperty("suKien").GetString().Should().Be("KHI_PHE_DUYET");
            dong.GetProperty("tenHeThong").GetString().Should().NotBeNullOrEmpty();
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/quy-trinh/lien-thong/{id}");
            await admin.DeleteAsync($"/api/v1/tich-hop/he-thong/{heThongId}");
        }
    }

    // ------------------------------------------------ Cap phe duyet (chuc nang 5)

    [Fact]
    public async Task Trung_Thu_Tu_Cap_Trong_Cung_Pham_Vi_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var donVi = await LayMangAsync(admin, "/api/v1/don-vi/chon?chiDonViPheDuyet=true");
        var donViId = donVi[0].GetProperty("id").GetGuid();

        var mot = await admin.PostAsJsonAsync("/api/v1/cap-phe-duyet", new
        {
            donViPheDuyetId = donViId,
            thuTuCap = 9,
            ghiChu = "Kiểm thử cấp 9"
        });

        mot.EnsureSuccessStatusCode();
        var id = (await mot.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu").GetGuid();

        try
        {
            var hai = await admin.PostAsJsonAsync("/api/v1/cap-phe-duyet", new
            {
                donViPheDuyetId = donViId,
                thuTuCap = 9,
                ghiChu = "Trùng cấp"
            });

            hai.StatusCode.Should().Be(HttpStatusCode.Conflict);
        }
        finally
        {
            await admin.DeleteAsync($"/api/v1/cap-phe-duyet/{id}");
        }
    }

    /// <summary>
    /// O tick "La don vi phe duyet" tren ho so don vi phai co hieu luc: don vi khong bat o do thi
    /// khong khai lam cap xet duoc, va danh sach chon cua man hinh cung khong bay no ra.
    /// </summary>
    [Fact]
    public async Task Don_Vi_Chua_Danh_Dau_Phe_Duyet_Khong_Khai_Lam_Cap_Xet_Duoc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tatCa = await LayMangAsync(admin, "/api/v1/don-vi/chon");
        var chiPheDuyet = await LayMangAsync(admin, "/api/v1/don-vi/chon?chiDonViPheDuyet=true");

        var idPheDuyet = chiPheDuyet.Select(x => x.GetProperty("id").GetGuid()).ToHashSet();

        idPheDuyet.Should().NotBeEmpty("dữ liệu mẫu phải có ít nhất một đơn vị phê duyệt");
        chiPheDuyet.Count.Should().BeLessThan(tatCa.Count,
            "danh sách lọc phải hẹp hơn danh sách đầy đủ, nếu bằng nhau thì bộ lọc không chạy");

        var donViThuong = tatCa
            .Select(x => x.GetProperty("id").GetGuid())
            .First(x => !idPheDuyet.Contains(x));

        var phanHoi = await admin.PostAsJsonAsync("/api/v1/cap-phe-duyet", new
        {
            donViPheDuyetId = donViThuong,
            thuTuCap = 8,
            ghiChu = "Đơn vị không có thẩm quyền ký"
        });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var loi = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("chưa được đánh dấu");
    }

    // ---------------------------------------------------------------- Nhat ky loi

    [Fact]
    public async Task Doc_Duoc_Nhat_Ky_Loi_He_Thong()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/he-thong/nhat-ky/loi?soDong=5");

        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        noiDung.GetProperty("duLieu").ValueKind.Should().Be(JsonValueKind.Array);
    }

    [Fact]
    public async Task Tac_Gia_Khong_Xem_Duoc_Nhat_Ky_Loi()
    {
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var phanHoi = await tacGia.GetAsync("/api/v1/he-thong/nhat-ky/loi");

        phanHoi.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    // ---------------------------------------------------------------------------------

    private async Task<Guid> TaoHeThongLienThongAsync(HttpClient client)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/tich-hop/he-thong", new
        {
            ma = $"KT{Guid.NewGuid():N}"[..10].ToUpperInvariant(),
            ten = "Hệ thống kiểm thử liên thông theo bước",
            endpointBase = "http://127.0.0.1:9",
            loaiXacThuc = "API_KEY",
            clientSecret = "bi-mat",
            tanSuatDongBo = "THU_CONG",
            trangThai = 1
        });

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetGuid();
    }

    private static async Task<Guid> LayQuyTrinhAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/quy-trinh?soDong=1");
        phanHoi.EnsureSuccessStatusCode();

        var noiDung = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        return noiDung.GetProperty("duLieu")[0].GetProperty("id").GetGuid();
    }

    private static async Task<string> TaoPhienAsync(HttpClient client)
    {
        var (phienId, _) = await TaoPhienDayDuAsync(client);
        return phienId;
    }

    private static async Task<(string PhienId, string SangKienId)> TaoPhienDayDuAsync(
        HttpClient client)
    {
        var hoiDong = await LayDuLieuAsync(client, "/api/v1/hoi-dong/chon");
        var hoiDongId = hoiDong[0].GetProperty("id").GetString()!;

        var dsHoSo = await client.GetAsync("/api/v1/sang-kien?soDong=1");
        dsHoSo.EnsureSuccessStatusCode();

        var sangKienId = (await dsHoSo.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu")[0].GetProperty("id").GetString()!;

        var tao = await client.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop", new
        {
            hoiDongId,
            tenPhien = "Phiên họp kiểm thử biên bản",
            thoiGianBatDau = DateTimeOffset.UtcNow,
            hinhThuc = "TRUC_TIEP",
            sangKienIds = new[] { sangKienId }
        });

        tao.EnsureSuccessStatusCode();

        var phienId = (await tao.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;

        return (phienId, sangKienId);
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
