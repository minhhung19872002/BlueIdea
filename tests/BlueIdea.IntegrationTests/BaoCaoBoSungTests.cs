using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu dot 3: thong ke theo tac gia, thoi gian xu ly trung binh, bao cao tong hop nam
/// va bo loc nam / cap xet duyet (truoc day khai bao nhung khong duoc ap dung).
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class BaoCaoBoSungTests
{
    private readonly UngDungKiemThu _ungDung;

    public BaoCaoBoSungTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Thong_Ke_Theo_Tac_Gia_Gom_Dung_So_Ho_So_Moi_Nguoi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/bao-cao/theo-tac-gia");
        phanHoi.EnsureSuccessStatusCode();

        var dong = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().ToList();

        dong.Should().NotBeEmpty("dữ liệu mẫu có hồ sơ kèm tác giả");

        foreach (var x in dong)
        {
            var tong = x.GetProperty("tongSo").GetInt32();
            var dat = x.GetProperty("soDat").GetInt32();
            var chinh = x.GetProperty("soLaTacGiaChinh").GetInt32();

            dat.Should().BeLessThanOrEqualTo(tong);
            chinh.Should().BeLessThanOrEqualTo(tong);
            x.GetProperty("hoTen").GetString().Should().NotBeNullOrWhiteSpace();
        }

        // Moi (ho ten + don vi cong tac) chi xuat hien mot lan, neu khong bao cao dem trung nguoi.
        dong.Select(x => (
                x.GetProperty("hoTen").GetString(),
                x.TryGetProperty("donViCongTac", out var dv) ? dv.GetString() : null))
            .Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Thoi_Gian_Xu_Ly_Chi_Tinh_Cac_Luot_Da_Hoan_Thanh()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/bao-cao/thoi-gian-xu-ly");
        phanHoi.EnsureSuccessStatusCode();

        var dong = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().ToList();

        foreach (var x in dong)
        {
            x.GetProperty("soLuot").GetInt32().Should().BeGreaterThan(0);
            x.GetProperty("soNgayTrungBinh").GetDecimal().Should().BeGreaterThanOrEqualTo(0);

            // Trung binh khong the lon hon lan lau nhat — sai o day la gom nhom sai.
            x.GetProperty("soNgayTrungBinh").GetDecimal()
                .Should().BeLessThanOrEqualTo(x.GetProperty("soNgayLauNhat").GetDecimal());
        }
    }

    [Fact]
    public async Task Bao_Cao_Tong_Hop_Nam_Khop_Voi_Danh_Sach_Chi_Tiet()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var dot = await admin.GetAsync("/api/v1/danh-muc/dot-de-nghi/quan-ly?soDong=1");
        dot.EnsureSuccessStatusCode();

        var nam = (await dot.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu")[0].GetProperty("nam").GetInt32();

        var phanHoi = await admin.GetAsync($"/api/v1/bao-cao/tong-hop-nam/{nam}");
        phanHoi.EnsureSuccessStatusCode();

        var b = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        b.GetProperty("nam").GetInt32().Should().Be(nam);

        var tong = b.GetProperty("tongHoSo").GetInt32();
        tong.Should().BeGreaterThan(0);

        // Tong theo linh vuc phai bang tong ho so — lech nghia la co ho so bi rot khoi nhom.
        b.GetProperty("theoLinhVuc").EnumerateArray()
            .Sum(x => x.GetProperty("soLuong").GetInt32()).Should().Be(tong);

        b.GetProperty("theoDot").EnumerateArray()
            .Sum(x => x.GetProperty("soLuong").GetInt32()).Should().Be(tong);

        var pdf = await admin.GetAsync($"/api/v1/bao-cao/tong-hop-nam/{nam}/xuat-pdf");
        pdf.EnsureSuccessStatusCode();
        pdf.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
    }

    [Fact]
    public async Task Loc_Theo_Nam_Thuc_Su_Doi_So_Lieu_Tong_Quan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var khongLoc = await LayTongHoSoAsync(admin, "/api/v1/bao-cao/tong-quan");

        // Nam khong co dot nao => phai ve 0. Truoc day tham so nam bi bo qua nen ket qua giu
        // nguyen bang tong toan he thong, nguoi dung tuong loc khong chay.
        var namTrong = await LayTongHoSoAsync(admin, "/api/v1/bao-cao/tong-quan?nam=1990");

        khongLoc.Should().BeGreaterThan(0);
        namTrong.Should().Be(0);
    }

    private static async Task<int> LayTongHoSoAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("tongHoSo").GetInt32();
    }
}
