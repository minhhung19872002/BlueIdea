using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu rang o cau hinh hien tren man hinh quan tri THAT SU co tac dung.
///
/// Mot o cau hinh sua duoc, luu duoc, nhung khong nhanh code nao doc la loai loi kho phat hien
/// nhat: khong bao loi, khong ai biet, chi lang le khong lam gi ca cho toi khi co su co.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class CauHinhCoHieuLucTests
{
    private readonly UngDungKiemThu _ungDung;

    public CauHinhCoHieuLucTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    /// <summary>
    /// Hai cong tac chinh sach duoi day duoc code doc tu truoc nhung khong duoc seed, nen khong
    /// hien tren man hinh cau hinh: muon bat/tat phai sua thang co so du lieu.
    /// </summary>
    [Theory]
    [InlineData("SO_LAN_SAI_CAN_CAPTCHA")]
    [InlineData("SSO_TU_DONG_TAO_TAI_KHOAN")]
    [InlineData("SO_TEP_TOI_DA")]
    public async Task Khoa_Cau_Hinh_Hien_Tren_Man_Hinh_Quan_Tri(string khoa)
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/he-thong/cau-hinh");
        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        duLieu.EnumerateArray()
            .Select(x => x.GetProperty("khoa").GetString())
            .Should().Contain(khoa,
                "khoá này được code đọc nên phải bật/tắt được trên giao diện, "
                + "không phải sửa thẳng cơ sở dữ liệu");
    }

    [Fact]
    public async Task Gioi_Han_So_Tep_Moi_Ho_So_Duoc_Ap_Dung()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        // Ha gioi han xuong 1 tep de kiem chung nhanh, roi tra lai gia tri cu.
        var cu = await LayGiaTriAsync(admin, "SO_TEP_TOI_DA");
        await DatGiaTriAsync(admin, "SO_TEP_TOI_DA", "1");

        try
        {
            var hoSoId = await TaoHoSoNhapAsync(admin);

            (await TaiTepAsync(admin, hoSoId, "minh-chung-1.pdf")).EnsureSuccessStatusCode();

            var lanHai = await TaiTepAsync(admin, hoSoId, "minh-chung-2.pdf");

            lanHai.IsSuccessStatusCode.Should().BeFalse(
                "đặt giới hạn 1 tệp thì tệp thứ hai phải bị từ chối");

            var loi = await lanHai.Content.ReadFromJsonAsync<JsonElement>();
            loi.GetProperty("thongBao").GetString().Should().Contain("tối đa 1 tệp");
        }
        finally
        {
            await DatGiaTriAsync(admin, "SO_TEP_TOI_DA", cu ?? "20");
        }
    }

    // ---------------------------------------------------------------------------------

    private static async Task<string?> LayGiaTriAsync(HttpClient client, string khoa)
    {
        var phanHoi = await client.GetAsync("/api/v1/he-thong/cau-hinh");
        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        return duLieu.EnumerateArray()
            .FirstOrDefault(x => x.GetProperty("khoa").GetString() == khoa)
            .TryGetProperty("giaTri", out var g) ? g.GetString() : null;
    }

    private static async Task DatGiaTriAsync(HttpClient client, string khoa, string giaTri)
    {
        var phanHoi = await client.PutAsJsonAsync("/api/v1/he-thong/cau-hinh",
            new[] { new { khoa, giaTri } });

        phanHoi.EnsureSuccessStatusCode();
    }

    private static async Task<string> TaoHoSoNhapAsync(HttpClient client)
    {
        var dot = await LayMotIdAsync(client, "/api/v1/danh-muc/dot-de-nghi/dang-mo");
        var linhVuc = await LayMotIdAsync(client, "/api/v1/danh-muc/linh-vuc/chon");

        var tao = await client.PostAsJsonAsync("/api/v1/sang-kien", new
        {
            tenSangKien = $"Hồ sơ kiểm thử giới hạn tệp {Guid.NewGuid():N}",
            dotDeNghiId = dot,
            linhVucId = linhVuc,
            moTaGiaiPhap = string.Concat(Enumerable.Repeat(
                "Mô tả chi tiết giải pháp phục vụ kiểm thử giới hạn số tệp. ", 8)),
            tinhTrangTruocKhiApDung = string.Concat(Enumerable.Repeat(
                "Trước khi áp dụng phải thao tác thủ công rất mất thời gian. ", 4)),
            noiDungGiaiPhap = string.Concat(Enumerable.Repeat(
                "Nội dung chi tiết của giải pháp được trình bày đầy đủ theo từng bước. ", 10)),
            tinhMoi = string.Concat(Enumerable.Repeat(
                "Tính mới của giải pháp so với cách làm cũ tại đơn vị. ", 4)),
            khaNangApDung = string.Concat(Enumerable.Repeat(
                "Khả năng áp dụng rộng rãi cho các đơn vị tương tự. ", 4)),
            danhSachTacGia = new[]
            {
                new { hoTen = "Nguyễn Văn A", tyLeDongGop = 100, laTacGiaChinh = true }
            }
        });

        tao.EnsureSuccessStatusCode();

        return (await tao.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu").GetString()!;
    }

    private static async Task<HttpResponseMessage> TaiTepAsync(
        HttpClient client, string hoSoId, string tenTep)
    {
        // Tep PDF toi thieu hop le de qua duoc buoc kiem tra magic number.
        var noiDungPdf = "%PDF-1.4\n1 0 obj<</Type/Catalog>>endobj\ntrailer<</Root 1 0 R>>\n%%EOF"u8
            .ToArray();

        using var noiDung = new MultipartFormDataContent();
        var tep = new ByteArrayContent(noiDungPdf);
        tep.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/pdf");

        noiDung.Add(tep, "tep", tenTep);
        noiDung.Add(new StringContent(hoSoId), "sangKienId");
        noiDung.Add(new StringContent("MINH_CHUNG"), "thanhPhanHoSoMa");

        return await client.PostAsync("/api/v1/tep-tin/tai-len", noiDung);
    }

    private static async Task<string> LayMotIdAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        var goc = await phanHoi.Content.ReadFromJsonAsync<JsonElement>();

        return goc.GetProperty("duLieu").EnumerateArray().First().GetProperty("id").GetString()!;
    }
}
