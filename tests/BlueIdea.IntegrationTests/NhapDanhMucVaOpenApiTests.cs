using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;
using ClosedXML.Excel;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu dot 8: nhap danh muc tu Excel va tai lieu OpenAPI rieng cho API cong khai.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class NhapDanhMucVaOpenApiTests
{
    private readonly UngDungKiemThu _ungDung;

    public NhapDanhMucVaOpenApiTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Chay_Thu_Khong_Ghi_Gi_Vao_Danh_Muc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var ma = $"KT{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        var ketQua = await NhapAsync(admin, "linh-vuc", chayThu: true,
            (ma, "Lĩnh vực kiểm thử", "Mô tả", "5"));

        ketQua.GetProperty("chayThu").GetBoolean().Should().BeTrue();
        ketQua.GetProperty("soHopLe").GetInt32().Should().Be(1);
        ketQua.GetProperty("soThemMoi").GetInt32().Should().Be(1);

        // Chay thu chi bao truoc ket qua, khong duoc dong vao du lieu that.
        (await TimTheoMaAsync(admin, ma)).Should().BeFalse();
    }

    [Fact]
    public async Task Nhap_That_Them_Moi_Roi_Lan_Sau_La_Cap_Nhat()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var ma = $"KT{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        var lan1 = await NhapAsync(admin, "linh-vuc", chayThu: false, (ma, "Tên lần đầu", null, "1"));
        lan1.GetProperty("soThemMoi").GetInt32().Should().Be(1);

        (await TimTheoMaAsync(admin, ma)).Should().BeTrue();

        // Nhap lai cung ma = CAP NHAT, khong bao trung va khong tao ban thu hai: cac don vi
        // thuong gui lai ca bang danh muc moi khi co thay doi.
        var lan2 = await NhapAsync(admin, "linh-vuc", chayThu: false, (ma, "Tên đã sửa", null, "2"));

        lan2.GetProperty("soThemMoi").GetInt32().Should().Be(0);
        lan2.GetProperty("soCapNhat").GetInt32().Should().Be(1);

        var ds = await LayDanhMucAsync(admin);
        ds.Count(x => x.GetProperty("ma").GetString() == ma).Should().Be(1);
        ds.Single(x => x.GetProperty("ma").GetString() == ma)
            .GetProperty("ten").GetString().Should().Be("Tên đã sửa");
    }

    [Fact]
    public async Task Bao_Loi_Tung_Dong_Va_Van_Ghi_Cac_Dong_Hop_Le()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var maTot = $"KT{Guid.NewGuid():N}"[..12].ToUpperInvariant();

        var ketQua = await NhapAsync(admin, "linh-vuc", chayThu: true,
            (maTot, "Dòng hợp lệ", null, "1"),
            ("", "Thiếu mã", null, "2"),
            ("MA SAI", "Mã có dấu cách", null, "3"),
            (maTot, "Trùng mã ngay trong tệp", null, "4"));

        ketQua.GetProperty("soHopLe").GetInt32().Should().Be(1);
        ketQua.GetProperty("soLoi").GetInt32().Should().Be(3);

        var chiTiet = ketQua.GetProperty("chiTiet").EnumerateArray().ToList();
        chiTiet.Should().HaveCount(4);
        chiTiet.Where(x => !x.GetProperty("hopLe").GetBoolean())
            .Should().OnlyContain(x => x.GetProperty("loi").GetString()!.Length > 0);
    }

    [Fact]
    public async Task Loai_Danh_Muc_Khong_Ho_Tro_Thi_Bao_Loi_Ro()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await GuiTepAsync(admin, "khong-ton-tai", true, ("A", "B", null, "1"));

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Tai_Lieu_Openapi_Cong_Khai_Tach_Rieng_Khoi_Tai_Lieu_Noi_Bo()
    {
        var client = _ungDung.CreateClient();

        var congKhai = await client.GetAsync("/swagger/cong-khai/swagger.json");
        var noiBo = await client.GetAsync("/swagger/v1/swagger.json");

        congKhai.EnsureSuccessStatusCode();
        noiBo.EnsureSuccessStatusCode();

        var duongDanCongKhai = (await congKhai.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("paths").EnumerateObject().Select(x => x.Name).ToList();

        var duongDanNoiBo = (await noiBo.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("paths").EnumerateObject().Select(x => x.Name).ToList();

        duongDanCongKhai.Should().NotBeEmpty();
        duongDanCongKhai.Should().OnlyContain(x => x.StartsWith("/api/public/", StringComparison.Ordinal));

        // Tai lieu giao cho ben thu ba KHONG duoc chua be mat quan tri: dua ca danh sach endpoint
        // nguoi dung / phan quyen cho ho la lo cau truc he thong cho doi tuong khong dung den.
        duongDanCongKhai.Should().NotContain(x => x.Contains("he-thong", StringComparison.Ordinal));
        duongDanCongKhai.Should().NotContain(x => x.Contains("xac-thuc", StringComparison.Ordinal));

        duongDanNoiBo.Should().NotContain(x => x.StartsWith("/api/public/", StringComparison.Ordinal));
        duongDanNoiBo.Should().Contain(x => x.Contains("sang-kien", StringComparison.Ordinal));
    }

    private static async Task<JsonElement> NhapAsync(
        HttpClient client, string loai, bool chayThu,
        params (string Ma, string Ten, string? MoTa, string ThuTu)[] dong)
    {
        var phanHoi = await GuiTepAsync(client, loai, chayThu, dong);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
    }

    private static async Task<HttpResponseMessage> GuiTepAsync(
        HttpClient client, string loai, bool chayThu,
        params (string Ma, string Ten, string? MoTa, string ThuTu)[] dong)
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Danh muc");

        sheet.Cell(1, 1).Value = "Mã";
        sheet.Cell(1, 2).Value = "Tên";
        sheet.Cell(1, 3).Value = "Mô tả";
        sheet.Cell(1, 4).Value = "Thứ tự";

        for (var i = 0; i < dong.Length; i++)
        {
            sheet.Cell(i + 2, 1).Value = dong[i].Ma;
            sheet.Cell(i + 2, 2).Value = dong[i].Ten;
            sheet.Cell(i + 2, 3).Value = dong[i].MoTa ?? string.Empty;
            sheet.Cell(i + 2, 4).Value = dong[i].ThuTu;
        }

        using var bo = new MemoryStream();
        workbook.SaveAs(bo);

        using var noiDung = new MultipartFormDataContent();
        var tep = new ByteArrayContent(bo.ToArray());
        tep.Headers.ContentType = new MediaTypeHeaderValue(
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

        noiDung.Add(tep, "tep", "danh-muc.xlsx");

        return await client.PostAsync(
            $"/api/v1/nhap-xuat/danh-muc?loai={loai}&chayThu={chayThu}", noiDung);
    }

    private static async Task<bool> TimTheoMaAsync(HttpClient client, string ma)
        => (await LayDanhMucAsync(client)).Any(x => x.GetProperty("ma").GetString() == ma);

    private static async Task<List<JsonElement>> LayDanhMucAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/danh-muc/linh-vuc?soDong=500");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().ToList();
    }
}
