using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu chuc nang 44 (cay to chuc: chuyen cha, gop don vi) va chuc nang 48
/// (quan tri menu: CRUD, sap xep keo tha, tach Web/Mobile).
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class ToChucVaMenuTests
{
    private readonly UngDungKiemThu _ungDung;

    public ToChucVaMenuTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    // ------------------------------------------------------------ Cay to chuc

    [Fact]
    public async Task Khong_Chuyen_Don_Vi_Vao_Cap_Duoi_Cua_Chinh_No()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var cay = await LayCayAsync(admin);
        var goc = cay.First(x => x.GetProperty("con").GetArrayLength() > 0);
        var gocId = goc.GetProperty("id").GetString()!;
        var conId = goc.GetProperty("con")[0].GetProperty("id").GetString()!;

        var phanHoi = await admin.PostAsJsonAsync(
            $"/api/v1/don-vi/{gocId}/chuyen-cha", new { donViChaMoiId = conId });

        // Cho phep se cat ca nhanh khoi cay va tao vong lap cha-con.
        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Khong_Dat_Don_Vi_Lam_Cap_Tren_Cua_Chinh_No()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var cay = await LayCayAsync(admin);
        var id = cay[0].GetProperty("id").GetString()!;

        var phanHoi = await admin.PostAsJsonAsync(
            $"/api/v1/don-vi/{id}/chuyen-cha", new { donViChaMoiId = id });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Gop_Don_Vi_Chuyen_Ho_So_Va_Tai_Khoan_Sang_Don_Vi_Dich()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var nguon = await TaoDonViAsync(admin, "KT_GOP_NGUON");
        var dich = await TaoDonViAsync(admin, "KT_GOP_DICH");

        var gop = await admin.PostAsync($"/api/v1/don-vi/{nguon}/gop-vao/{dich}", null);
        gop.EnsureSuccessStatusCode();

        // Don vi nguon bi xoa mem, khong con trong cay.
        var conTrongDanhSach = (await LayPhangAsync(admin))
            .Any(x => x.GetProperty("id").GetString() == nguon);

        conTrongDanhSach.Should().BeFalse();

        var gopChinhNo = await admin.PostAsync($"/api/v1/don-vi/{dich}/gop-vao/{dich}", null);
        gopChinhNo.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    // ------------------------------------------------------------------- Menu

    [Fact]
    public async Task Menu_Web_Va_Mobile_La_Hai_Cay_Doc_Lap()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var ma = $"KT_MENU_{Guid.NewGuid():N}"[..18].ToUpperInvariant();

        var them = await admin.PostAsJsonAsync("/api/v1/cau-hinh-menu", new
        {
            ma,
            ten = "Mục kiểm thử mobile",
            duongDan = "/kiem-thu",
            thuTu = 99,
            loai = "MOBILE",
            hienThi = true,
            moTabMoi = false
        });

        them.EnsureSuccessStatusCode();

        var web = await LayMenuAsync(admin, "WEB");
        var mobile = await LayMenuAsync(admin, "MOBILE");

        mobile.Select(x => x.GetProperty("ma").GetString()).Should().Contain(ma);
        web.Select(x => x.GetProperty("ma").GetString()).Should().NotContain(ma);

        // Cung ma nhung khac loai thi hop le — hai cay doc lap nhau.
        var trungMaKhacLoai = await admin.PostAsJsonAsync("/api/v1/cau-hinh-menu", new
        {
            ma,
            ten = "Mục kiểm thử web",
            duongDan = "/kiem-thu",
            thuTu = 99,
            loai = "WEB",
            hienThi = true,
            moTabMoi = false
        });

        trungMaKhacLoai.EnsureSuccessStatusCode();

        var trungMaCungLoai = await admin.PostAsJsonAsync("/api/v1/cau-hinh-menu", new
        {
            ma,
            ten = "Mục trùng",
            thuTu = 99,
            loai = "WEB",
            hienThi = true,
            moTabMoi = false
        });

        trungMaCungLoai.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Sap_Xep_Ghi_Lai_Thu_Tu_Va_Cap_Cha_Theo_Cay_Gui_Len()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var cha = await TaoMucMenuAsync(admin, "Nhóm kiểm thử");
        var con = await TaoMucMenuAsync(admin, "Mục con kiểm thử");

        var sapXep = await admin.PutAsJsonAsync("/api/v1/cau-hinh-menu/sap-xep?loai=WEB",
            new[] { new { id = cha, con = new[] { new { id = con, con = (object?)null } } } });

        sapXep.EnsureSuccessStatusCode();

        var menu = await LayMenuAsync(admin, "WEB");
        var mucCon = menu.Single(x => x.GetProperty("id").GetString() == con);

        mucCon.GetProperty("menuChaId").GetString().Should().Be(cha);
        mucCon.GetProperty("thuTu").GetInt32().Should().Be(0);

        // Xoa mục cha khi còn mục con phải bị chặn, nếu không mục con thành mồ côi.
        var xoaCha = await admin.DeleteAsync($"/api/v1/cau-hinh-menu/{cha}");
        xoaCha.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    private static async Task<string> TaoDonViAsync(HttpClient client, string tienTo)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/don-vi", new
        {
            ma = $"{tienTo}_{Guid.NewGuid():N}"[..24].ToUpperInvariant(),
            ten = $"Đơn vị kiểm thử {tienTo}",
            loai = "DON_VI",
            trangThai = 1,
            thuTu = 99
        });

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;
    }

    private static async Task<string> TaoMucMenuAsync(HttpClient client, string ten)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/cau-hinh-menu", new
        {
            ma = $"KT_{Guid.NewGuid():N}"[..16].ToUpperInvariant(),
            ten,
            thuTu = 50,
            loai = "WEB",
            hienThi = true,
            moTabMoi = false
        });

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetString()!;
    }

    private static async Task<List<JsonElement>> LayMenuAsync(HttpClient client, string loai)
        => await LayMangAsync(client, $"/api/v1/cau-hinh-menu?loai={loai}");

    private static async Task<List<JsonElement>> LayCayAsync(HttpClient client)
        => await LayMangAsync(client, "/api/v1/don-vi/cay");

    private static async Task<List<JsonElement>> LayPhangAsync(HttpClient client)
        => await LayMangAsync(client, "/api/v1/don-vi/chon");

    private static async Task<List<JsonElement>> LayMangAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().ToList();
    }
}
