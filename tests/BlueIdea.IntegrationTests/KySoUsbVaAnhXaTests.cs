using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu dot 11: ky so bang USB token (may chu phat hash, may tram ky, may chu xac minh).
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class KySoUsbVaAnhXaTests
{
    private readonly UngDungKiemThu _ungDung;

    public KySoUsbVaAnhXaTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Ky_Bang_Usb_Token_Xac_Minh_Duoc_Va_Ghi_Nhat_Ky()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var tepId = await TaiTepAsync(admin);

        var (phienId, hash) = await ChuanBiAsync(admin, tepId);

        // "USB token" trong kiem thu: mot cap khoa RSA tao tai cho. Diem cua luong nay la KHOA
        // KHONG O MAY CHU — may chu chi thay chu ky va chung thu cong khai.
        using var rsa = RSA.Create(2048);
        var yeuCau = new CertificateRequest(
            "CN=Nguyen Van Ky, O=Kiem thu", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        using var chungThu = yeuCau.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        var chuKy = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var hoanTat = await admin.PostAsJsonAsync($"/api/v1/ky-so-usb/{phienId}/hoan-tat", new
        {
            chuKyBase64 = Convert.ToBase64String(chuKy),
            chungThuBase64 = Convert.ToBase64String(chungThu.RawData)
        });

        hoanTat.EnsureSuccessStatusCode();

        var kq = (await hoanTat.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        kq.GetProperty("chuTheChungThu").GetString().Should().Contain("Nguyen Van Ky");
        kq.GetProperty("tepChuKyId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task Chu_Ky_Cua_Noi_Dung_Khac_Bi_Tu_Choi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var tepId = await TaiTepAsync(admin);

        var (phienId, _) = await ChuanBiAsync(admin, tepId);

        using var rsa = RSA.Create(2048);
        var yeuCau = new CertificateRequest(
            "CN=Ke gia mao", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        using var chungThu = yeuCau.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        // Ky mot noi dung KHAC roi gan vao phien nay. Neu may chu tin gia tri bam do client gui
        // len thay vi dung gia tri no da phat, cu nay se lot.
        var hashKhac = SHA256.HashData("noi dung hoan toan khac"u8.ToArray());
        var chuKy = rsa.SignHash(hashKhac, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var hoanTat = await admin.PostAsJsonAsync($"/api/v1/ky-so-usb/{phienId}/hoan-tat", new
        {
            chuKyBase64 = Convert.ToBase64String(chuKy),
            chungThuBase64 = Convert.ToBase64String(chungThu.RawData)
        });

        hoanTat.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Chung_Thu_Het_Han_Bi_Tu_Choi()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var tepId = await TaiTepAsync(admin);

        var (phienId, hash) = await ChuanBiAsync(admin, tepId);

        using var rsa = RSA.Create(2048);
        var yeuCau = new CertificateRequest(
            "CN=Chung thu het han", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        using var chungThu = yeuCau.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddYears(-3), DateTimeOffset.UtcNow.AddYears(-1));

        var chuKy = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var hoanTat = await admin.PostAsJsonAsync($"/api/v1/ky-so-usb/{phienId}/hoan-tat", new
        {
            chuKyBase64 = Convert.ToBase64String(chuKy),
            chungThuBase64 = Convert.ToBase64String(chungThu.RawData)
        });

        hoanTat.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);

        var loi = await hoanTat.Content.ReadFromJsonAsync<JsonElement>();
        loi.GetProperty("thongBao").GetString().Should().Contain("hiệu lực");
    }

    [Fact]
    public async Task Khong_Gan_Duoc_Chu_Ky_Vao_Phien_Cua_Nguoi_Khac()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var nguoiKhac = await _ungDung.TaoClientDaDangNhapAsync("lanhdao");

        var tepId = await TaiTepAsync(admin);
        var (phienId, hash) = await ChuanBiAsync(admin, tepId);

        using var rsa = RSA.Create(2048);
        var yeuCau = new CertificateRequest(
            "CN=Nguoi khac", rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        using var chungThu = yeuCau.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        var chuKy = rsa.SignHash(hash, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var hoanTat = await nguoiKhac.PostAsJsonAsync($"/api/v1/ky-so-usb/{phienId}/hoan-tat", new
        {
            chuKyBase64 = Convert.ToBase64String(chuKy),
            chungThuBase64 = Convert.ToBase64String(chungThu.RawData)
        });

        hoanTat.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private static async Task<(Guid PhienId, byte[] Hash)> ChuanBiAsync(
        HttpClient client, string tepId)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/ky-so-usb/chuan-bi", new
        {
            tepTinId = tepId,
            doiTuong = "QUYET_DINH",
            doiTuongId = Guid.NewGuid()
        });

        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        duLieu.GetProperty("thuatToanBam").GetString().Should().Be("SHA-256");

        return (
            Guid.Parse(duLieu.GetProperty("phienId").GetString()!),
            Convert.FromBase64String(duLieu.GetProperty("hashBase64").GetString()!));
    }

    private static async Task<string> TaiTepAsync(HttpClient client)
    {
        var noiDung = "%PDF-1.4\nvan ban can ky\n%%EOF\n"u8.ToArray();

        using var form = new MultipartFormDataContent();
        var tep = new ByteArrayContent(noiDung);
        tep.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
        form.Add(tep, "tep", $"van-ban-{Guid.NewGuid():N}.pdf");

        var phanHoi = await client.PostAsync("/api/v1/tep-tin/tai-len", form);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;
    }
}
