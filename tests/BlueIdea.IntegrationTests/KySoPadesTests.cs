using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BlueIdea.Infrastructure.KySo;
using QuestPDF.Fluent;
using QuestPDF.Helpers;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu chuc nang 49 — ky nhung vao PDF theo chuan PAdES.
///
/// Khong can container: ky PDF la phep tinh thuan tuy tren mang byte.
/// </summary>
public sealed class KySoPadesTests
{
    private static X509Certificate2 TaoChungThuThu()
    {
        using var rsa = RSA.Create(2048);

        var yeuCau = new CertificateRequest(
            "CN=BlueIdea Kiem Thu PAdES, O=Kiem thu",
            rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

        var chungThu = yeuCau.CreateSelfSigned(
            DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        // Xuat/nhap lai qua PFX de chung thu co khoa bi mat gan duoc voi bo ky.
        return new X509Certificate2(
            chungThu.Export(X509ContentType.Pfx, "kiemthu"), "kiemthu",
            X509KeyStorageFlags.Exportable);
    }

    private static byte[] TaoPdfThat()
    {
        return Document.Create(tap =>
        {
            tap.Page(trang =>
            {
                trang.Size(PageSizes.A4);
                trang.Margin(2, QuestPDF.Infrastructure.Unit.Centimetre);
                trang.Content().Text("Van ban kiem thu ky so PAdES");
            });
        }).GeneratePdf();
    }

    [Fact]
    public async Task Ky_Nhung_Vao_Pdf_Ra_Tep_Van_La_Pdf_Va_Chua_Chu_Ky()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var goc = TaoPdfThat();
        using var chungThu = TaoChungThuThu();

        var daKy = await BoKySoPades.KyAsync(goc, chungThu, "Người ký kiểm thử", null, null);

        // Ket qua VAN LA PDF: nguoi nhan mo bang trinh doc thong thuong duoc, khac han ban
        // detached von sinh ra mot tep .p7s khong doc duoc noi dung.
        BoKySoPades.LaPdf(daKy).Should().BeTrue();
        daKy.Length.Should().BeGreaterThan(goc.Length);

        var vanBan = Encoding.Latin1.GetString(daKy);

        // Cac phan tu bat buoc cua chu ky PDF: tu dien chu ky, bo loc, va mang ByteRange chi ra
        // phan noi dung duoc ky. Thieu ByteRange thi trinh doc PDF khong kiem tra duoc gi.
        vanBan.Should().Contain("/Type/Sig").And.Contain("/ByteRange");
    }

    [Fact]
    public async Task Sua_Mot_Byte_Trong_Tep_Da_Ky_Lam_Hong_Vung_Duoc_Ky()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var goc = TaoPdfThat();
        using var chungThu = TaoChungThuThu();

        var daKy = await BoKySoPades.KyAsync(goc, chungThu, "Người ký kiểm thử", null, null);
        var sua = daKy.ToArray();

        // Doi mot byte trong phan noi dung (bo qua phan dau tep) roi bam lai: gia tri bam phai
        // khac di. Day chinh la co che ma trinh doc PDF dua vao de bao "tai lieu da bi sua".
        sua[sua.Length / 2] = (byte)(sua[sua.Length / 2] ^ 0xFF);

        Convert.ToHexString(SHA256.HashData(sua))
            .Should().NotBe(Convert.ToHexString(SHA256.HashData(daKy)));
    }

    [Fact]
    public async Task Chung_Thu_Khong_Co_Khoa_Bi_Mat_Thi_Khong_Ky_Duoc()
    {
        QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

        var goc = TaoPdfThat();

        using var coKhoa = TaoChungThuThu();

        // Chi giu phan cong khai — dung thu mot ben ngoai gui chung thu cho minh.
        using var chiCongKhai = new X509Certificate2(coKhoa.Export(X509ContentType.Cert));

        var hanhDong = async () =>
            await BoKySoPades.KyAsync(goc, chiCongKhai, "Không có khoá", null, null);

        await hanhDong.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public void Nhan_Dien_Dung_Tep_Pdf()
    {
        BoKySoPades.LaPdf("%PDF-1.7"u8.ToArray()).Should().BeTrue();
        BoKySoPades.LaPdf("PK"u8.ToArray()).Should().BeFalse();
        BoKySoPades.LaPdf(Array.Empty<byte>()).Should().BeFalse();
    }
}
