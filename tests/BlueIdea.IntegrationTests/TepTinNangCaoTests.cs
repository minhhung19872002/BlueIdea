using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu dot 10: tai tep theo manh, xem truoc trong trinh duyet va lien ket tai xuong
/// co thoi han (co ky HMAC voi kho dia cuc bo).
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class TepTinNangCaoTests
{
    private readonly UngDungKiemThu _ungDung;

    public TepTinNangCaoTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    /// <summary>PDF nho nhat hop le — du de qua kiem tra magic number.</summary>
    private static byte[] TaoPdf(int soByteDem)
    {
        var dau = "%PDF-1.4\n"u8.ToArray();
        var than = new byte[soByteDem];
        Array.Fill(than, (byte)'A');
        var cuoi = "\n%%EOF\n"u8.ToArray();

        return dau.Concat(than).Concat(cuoi).ToArray();
    }

    [Fact]
    public async Task Tai_Tep_Theo_Manh_Roi_Ghep_Lai_Dung_Noi_Dung()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var noiDung = TaoPdf(300_000);

        var batDau = await admin.PostAsJsonAsync("/api/v1/tep-tin/manh/bat-dau", new
        {
            tenTep = "tep-lon-kiem-thu.pdf",
            tongKichThuoc = noiDung.Length,
            soManh = 3
        });

        batDau.EnsureSuccessStatusCode();
        var phienId = (await batDau.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("phienId").GetString()!;

        var kichThuocManh = (int)Math.Ceiling(noiDung.Length / 3.0);

        for (var i = 0; i < 3; i++)
        {
            var tu = i * kichThuocManh;
            var dai = Math.Min(kichThuocManh, noiDung.Length - tu);

            var gui = await GuiManhAsync(admin, phienId, i, noiDung.Skip(tu).Take(dai).ToArray());
            gui.EnsureSuccessStatusCode();
        }

        var hoanTat = await admin.PostAsync($"/api/v1/tep-tin/manh/{phienId}/hoan-tat", null);
        hoanTat.EnsureSuccessStatusCode();

        var tep = (await hoanTat.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");
        tep.GetProperty("kichThuoc").GetInt64().Should().Be(noiDung.Length);

        // Tai ve va doi chieu tung byte: ghep sai thu tu manh se lam tep hong ma kich thuoc van dung.
        var id = tep.GetProperty("id").GetString()!;
        var taiVe = await admin.GetAsync($"/api/v1/tep-tin/{id}/tai-ve");
        taiVe.EnsureSuccessStatusCode();

        (await taiVe.Content.ReadAsByteArrayAsync()).Should().Equal(noiDung);
    }

    [Fact]
    public async Task Thieu_Manh_Thi_Khong_Ghep_Duoc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var batDau = await admin.PostAsJsonAsync("/api/v1/tep-tin/manh/bat-dau", new
        {
            tenTep = "thieu-manh.pdf",
            tongKichThuoc = 1000,
            soManh = 3
        });

        batDau.EnsureSuccessStatusCode();
        var phienId = (await batDau.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("phienId").GetString()!;

        (await GuiManhAsync(admin, phienId, 0, TaoPdf(100))).EnsureSuccessStatusCode();

        var hoanTat = await admin.PostAsync($"/api/v1/tep-tin/manh/{phienId}/hoan-tat", null);

        hoanTat.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var loi = await hoanTat.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("thiếu");
    }

    [Fact]
    public async Task Khong_Chen_Duoc_Manh_Vao_Phien_Cua_Nguoi_Khac()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var nguoiKhac = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");

        var batDau = await admin.PostAsJsonAsync("/api/v1/tep-tin/manh/bat-dau", new
        {
            tenTep = "cua-admin.pdf",
            tongKichThuoc = 1000,
            soManh = 2
        });

        batDau.EnsureSuccessStatusCode();
        var phienId = (await batDau.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("phienId").GetString()!;

        // Doan duoc Id phien cung khong chen duoc manh vao tep nguoi khac dang tai.
        var chen = await GuiManhAsync(nguoiKhac, phienId, 0, TaoPdf(50));

        chen.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task Chi_Xem_Truoc_Duoc_Dinh_Dang_Trinh_Duyet_Khong_Chay_Ma()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var pdfId = await TaiTepAsync(admin, "xem-truoc.pdf", TaoPdf(500));

        var xemPdf = await admin.GetAsync($"/api/v1/tep-tin/{pdfId}/xem-truoc");
        xemPdf.EnsureSuccessStatusCode();

        xemPdf.Content.Headers.ContentType!.MediaType.Should().Be("application/pdf");
        xemPdf.Content.Headers.ContentDisposition!.DispositionType.Should().Be("inline");

        // nosniff bat buoc: mot tep bi trinh duyet doan thanh text/html se duoc chay nhu trang web.
        xemPdf.Headers.TryGetValues("X-Content-Type-Options", out var giaTri).Should().BeTrue();
        giaTri!.Should().Contain("nosniff");

        var docxId = await TaiTepAsync(admin, "khong-xem-truoc.docx",
            new byte[] { 0x50, 0x4B, 0x03, 0x04 }.Concat(new byte[200]).ToArray());

        var xemDocx = await admin.GetAsync($"/api/v1/tep-tin/{docxId}/xem-truoc");
        xemDocx.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Lien_Ket_Tai_Xuong_Co_Chu_Ky_Va_Chu_Ky_Sai_Thi_Khong_Tai_Duoc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var id = await TaiTepAsync(admin, "lien-ket.pdf", TaoPdf(400));

        var phanHoi = await admin.GetAsync($"/api/v1/tep-tin/{id}/lien-ket-tai-xuong?soPhut=5");
        phanHoi.EnsureSuccessStatusCode();

        var url = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("url").GetString()!;

        url.Should().Contain("chuKy=");

        var khach = _ungDung.CreateClient();

        // Lien ket dung: tai duoc ma khong can dang nhap — do chinh la diem cua lien ket co han.
        var tai = await khach.GetAsync(url);
        tai.EnsureSuccessStatusCode();

        // Sua chu ky: phai bi tu choi, neu khong "co thoi han" khong con y nghia gi.
        var urlHong = url[..^4] + "AAAA";
        var taiHong = await khach.GetAsync(urlHong);

        taiHong.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<HttpResponseMessage> GuiManhAsync(
        HttpClient client, string phienId, int chiSo, byte[] duLieu)
    {
        using var noiDung = new MultipartFormDataContent();
        var manh = new ByteArrayContent(duLieu);
        manh.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        noiDung.Add(manh, "manh", $"manh-{chiSo}.bin");

        return await client.PostAsync($"/api/v1/tep-tin/manh/{phienId}/{chiSo}", noiDung);
    }

    private static async Task<string> TaiTepAsync(HttpClient client, string tenTep, byte[] duLieu)
    {
        using var noiDung = new MultipartFormDataContent();
        var tep = new ByteArrayContent(duLieu);
        tep.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
        noiDung.Add(tep, "tep", tenTep);

        var phanHoi = await client.PostAsync("/api/v1/tep-tin/tai-len", noiDung);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;
    }
}
