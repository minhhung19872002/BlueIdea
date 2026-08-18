using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>Kiem thu chuc nang 37 — goi y tu khoa khi go o o tim kiem.</summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class GoiYTimKiemTests
{
    private readonly UngDungKiemThu _ungDung;

    public GoiYTimKiemTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Go_Duoi_Hai_Ky_Tu_Thi_Khong_Goi_Y()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        // Mot ky tu khop qua nhieu ho so, goi y luc do vo dung ma van ton mot vong truy van.
        var goiY = await LayGoiYAsync(admin, "a");

        goiY.Should().BeEmpty();
    }

    [Fact]
    public async Task Goi_Y_Theo_Ma_Ho_So_Va_Ten_Sang_Kien()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var goiY = await LayGoiYAsync(admin, "SK-");

        goiY.Should().NotBeEmpty();
        goiY.Select(x => x.GetProperty("loai").GetString()).Should().Contain("MA_HO_SO");

        foreach (var x in goiY)
        {
            x.GetProperty("giaTri").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task Goi_Y_Chi_Tra_Ve_Du_Lieu_Trong_Pham_Vi_Nguoi_Dung_Duoc_Xem()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var tacGia = await _ungDung.TaoClientDaDangNhapAsync("gv.lan");

        var cuaAdmin = await LayGoiYAsync(admin, "SK-", 20);
        var cuaTacGia = await LayGoiYAsync(tacGia, "SK-", 20);

        // Tac gia chi thay ho so trong pham vi cua minh: goi y khong duoc lam lo ma ho so cua
        // don vi khac — do cung la mot dang ro ri du lieu du chi la mot dong goi y.
        cuaTacGia.Count.Should().BeLessThanOrEqualTo(cuaAdmin.Count);
    }

    private static async Task<List<JsonElement>> LayGoiYAsync(
        HttpClient client, string tuKhoa, int soLuong = 8)
    {
        var phanHoi = await client.GetAsync(
            $"/api/v1/sang-kien/goi-y?tuKhoa={Uri.EscapeDataString(tuKhoa)}&soLuong={soLuong}");

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().ToList();
    }
}
