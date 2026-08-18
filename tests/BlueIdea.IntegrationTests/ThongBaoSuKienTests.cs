using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu cac su kien thong bao truoc day duoc khai bao nhung khong noi nao phat: giay moi hop
/// hoi dong va thong bao ho so bi tu choi.
///
/// Cach kiem: goi dung endpoint ma man hinh goi, roi doc chuong thong bao cua nguoi nhan — day la
/// thu nguoi dung that su nhin thay, khong phai trang thai noi bo cua dich vu.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class ThongBaoSuKienTests
{
    private readonly UngDungKiemThu _ungDung;

    public ThongBaoSuKienTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Tao_Phien_Hop_Gui_Giay_Moi_Cho_Thanh_Vien_Hoi_Dong()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");

        var truocKhiMoi = await DemThongBaoAsync(chuTich, "MOI_HOP_HOI_DONG");

        var hoiDongId = await LayHoiDongMauAsync(admin);

        var taoPhien = await admin.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop", new
        {
            hoiDongId,
            tenPhien = "Phiên họp kiểm thử giấy mời",
            thoiGianBatDau = DateTimeOffset.UtcNow.AddDays(3),
            hinhThuc = "TRUC_TUYEN",
            diaDiem = "Phòng họp trực tuyến",
            sangKienIds = Array.Empty<string>()
        });

        taoPhien.EnsureSuccessStatusCode();

        var sauKhiMoi = await DemThongBaoAsync(chuTich, "MOI_HOP_HOI_DONG");

        sauKhiMoi.Should().BeGreaterThan(truocKhiMoi,
            "tạo phiên họp phải gửi giấy mời cho thành viên hội đồng");
    }

    [Fact]
    public async Task Giay_Moi_Neu_Ro_Thoi_Gian_Va_Dia_Diem()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");

        var hoiDongId = await LayHoiDongMauAsync(admin);
        var diaDiem = $"Hội trường {Guid.NewGuid():N}"[..28];

        var taoPhien = await admin.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop", new
        {
            hoiDongId,
            tenPhien = "Phiên họp kiểm thử nội dung giấy mời",
            thoiGianBatDau = DateTimeOffset.UtcNow.AddDays(5),
            hinhThuc = "TRUC_TIEP",
            diaDiem,
            sangKienIds = Array.Empty<string>()
        });

        taoPhien.EnsureSuccessStatusCode();

        var thongBao = await LayThongBaoAsync(chuTich);

        // Giay moi khong neu dia diem va gio thi nguoi nhan van phai vao he thong tra cuu,
        // tuc la thong bao khong lam duoc viec cua no.
        var giayMoi = thongBao
            .Where(x => x.GetProperty("loaiSuKien").GetString() == "MOI_HOP_HOI_DONG")
            .Select(x => x.GetProperty("noiDung").GetString() ?? string.Empty)
            .ToList();

        giayMoi.Should().NotBeEmpty();
        giayMoi.Should().Contain(x => x.Contains(diaDiem, StringComparison.Ordinal));
    }

    /// <summary>
    /// Bam vao thong bao phai mo duoc thu no dang noi toi.
    ///
    /// Truoc day ca ba lien ket deu tro toi route khong ton tai va nhom thong bao thuong gap nhat
    /// thi khong co lien ket, nen chuong thong bao chi de doc chu khong bam duoc.
    /// </summary>
    [Fact]
    public async Task Giay_Moi_Co_Lien_Ket_Mo_Duoc_Hoi_Dong()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var chuTich = await _ungDung.TaoClientDaDangNhapAsync("chutich");

        var hoiDongId = await LayHoiDongMauAsync(admin);

        var taoPhien = await admin.PostAsJsonAsync("/api/v1/hoi-dong/phien-hop", new
        {
            hoiDongId,
            tenPhien = "Phiên họp kiểm thử liên kết",
            thoiGianBatDau = DateTimeOffset.UtcNow.AddDays(7),
            hinhThuc = "TRUC_TIEP",
            diaDiem = "Phòng họp kiểm thử liên kết",
            sangKienIds = Array.Empty<string>()
        });

        taoPhien.EnsureSuccessStatusCode();

        var giayMoi = (await LayThongBaoAsync(chuTich))
            .First(x => x.GetProperty("loaiSuKien").GetString() == "MOI_HOP_HOI_DONG");

        giayMoi.GetProperty("duongDan").GetString().Should().Be($"/hoi-dong/{hoiDongId}");
    }

    [Fact]
    public async Task Mau_Thong_Bao_Tu_Choi_Va_Moi_Hop_Deu_Duoc_Khai_Bao()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/mau-thong-bao/su-kien");
        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        var ma = duLieu.EnumerateArray()
            .Select(x => x.GetProperty("ma").GetString())
            .ToList();

        ma.Should().Contain("HO_SO_BI_TU_CHOI");
        ma.Should().Contain("MOI_HOP_HOI_DONG");
    }

    // ---------------------------------------------------------------------------------

    private static async Task<int> DemThongBaoAsync(HttpClient client, string loaiSuKien)
    {
        var ds = await LayThongBaoAsync(client);
        return ds.Count(x => x.GetProperty("loaiSuKien").GetString() == loaiSuKien);
    }

    private static async Task<List<JsonElement>> LayThongBaoAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/he-thong/thong-bao?soDong=100");
        phanHoi.EnsureSuccessStatusCode();

        var goc = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();
        var duLieu = goc.GetProperty("duLieu");

        // Endpoint tra ve dang phan trang hoac mang tuy phien ban — nhan ca hai.
        var mang = duLieu.ValueKind == JsonValueKind.Array
            ? duLieu
            : duLieu.GetProperty("duLieu");

        return mang.EnumerateArray().ToList();
    }

    private static async Task<string> LayHoiDongMauAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/hoi-dong?trang=1&soDong=1");
        phanHoi.EnsureSuccessStatusCode();

        var goc = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();

        return goc.GetProperty("duLieu").EnumerateArray().First()
            .GetProperty("id").GetString()!;
    }
}
