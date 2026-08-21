using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Xml;
using BlueIdea.Infrastructure.KySo;

namespace BlueIdea.UnitTests.KySo;

/// <summary>
/// Kiem thu ky XAdES-BES tren tep XML bang chung thu tu ky sinh trong kiem thu.
///
/// Ba dieu phai dung thi chu ky moi co gia tri doi chieu: ky duoc, xac minh lai duoc, va SUA MOT
/// KY TU trong noi dung thi xac minh phai bao khong hop le.
/// </summary>
public sealed class BoKySoXadesTests
{
    private const string XmlMau =
        """<?xml version="1.0" encoding="utf-8"?><QuyetDinh><So>125/QĐ-UBND</So><TrichYeu>Công nhận sáng kiến cấp cơ sở</TrichYeu></QuyetDinh>""";

    [Fact]
    public void Nhan_Dien_Dung_Tep_Xml()
    {
        BoKySoXades.LaXml(Encoding.UTF8.GetBytes(XmlMau)).Should().BeTrue();
        BoKySoXades.LaXml(Encoding.UTF8.GetBytes("   \n<a/>")).Should().BeTrue();
        BoKySoXades.LaXml(Encoding.UTF8.GetBytes("%PDF-1.7")).Should().BeFalse();
        BoKySoXades.LaXml(Array.Empty<byte>()).Should().BeFalse();
    }

    [Fact]
    public void Ky_Roi_Xac_Minh_Lai_Duoc()
    {
        using var chungThu = TaoChungThuTuKy();
        var goc = Encoding.UTF8.GetBytes(XmlMau);

        var daKy = BoKySoXades.Ky(goc, chungThu, DateTimeOffset.Now);

        var ketQua = BoKySoXades.XacMinh(daKy);

        ketQua.CoChuKy.Should().BeTrue();
        ketQua.HopLe.Should().BeTrue(ketQua.Loi);
        ketQua.Serial.Should().Be(chungThu.SerialNumber);
        ketQua.ThoiGianKy.Should().NotBeNull(
            "XAdES-BES bắt buộc có SigningTime nằm trong phạm vi ký");
    }

    [Fact]
    public void Chu_Ky_Nam_Trong_Chinh_Tep_Xml_Va_Giu_Nguyen_Noi_Dung_Goc()
    {
        using var chungThu = TaoChungThuTuKy();

        var daKy = BoKySoXades.Ky(Encoding.UTF8.GetBytes(XmlMau), chungThu, DateTimeOffset.Now);

        var tai = new XmlDocument { XmlResolver = null };
        tai.LoadXml(Encoding.UTF8.GetString(daKy));

        tai.DocumentElement!.Name.Should().Be("QuyetDinh");
        tai.GetElementsByTagName("Signature", "http://www.w3.org/2000/09/xmldsig#")
            .Count.Should().Be(1);

        // Khoi thuoc tinh XAdES phai co, neu khong day chi la XML-DSig thuong.
        tai.GetElementsByTagName("SignedProperties", "http://uri.etsi.org/01903/v1.3.2#")
            .Count.Should().Be(1);

        tai.GetElementsByTagName("So")[0]!.InnerText.Should().Be("125/QĐ-UBND");
    }

    [Fact]
    public void Sua_Noi_Dung_Sau_Khi_Ky_Thi_Xac_Minh_Bao_Khong_Hop_Le()
    {
        using var chungThu = TaoChungThuTuKy();

        var daKy = BoKySoXades.Ky(Encoding.UTF8.GetBytes(XmlMau), chungThu, DateTimeOffset.Now);

        var vanBan = Encoding.UTF8.GetString(daKy)
            .Replace("125/QĐ-UBND", "999/QĐ-UBND", StringComparison.Ordinal);

        var ketQua = BoKySoXades.XacMinh(Encoding.UTF8.GetBytes(vanBan));

        ketQua.CoChuKy.Should().BeTrue("khối chữ ký vẫn còn trong tệp");
        ketQua.HopLe.Should().BeFalse("nội dung đã bị sửa sau khi ký");
    }

    [Fact]
    public void Tep_Xml_Khong_Co_Chu_Ky_Thi_Bao_Chua_Ky()
    {
        var ketQua = BoKySoXades.XacMinh(Encoding.UTF8.GetBytes(XmlMau));

        ketQua.CoChuKy.Should().BeFalse();
        ketQua.HopLe.Should().BeFalse();
    }

    private static X509Certificate2 TaoChungThuTuKy()
    {
        using var khoa = RSA.Create(2048);

        var yeuCau = new CertificateRequest(
            "CN=Kiem thu XAdES, O=BlueIdea, C=VN",
            khoa,
            HashAlgorithmName.SHA256,
            RSASignaturePadding.Pkcs1);

        yeuCau.CertificateExtensions.Add(
            new X509KeyUsageExtension(
                X509KeyUsageFlags.DigitalSignature | X509KeyUsageFlags.NonRepudiation, true));

        using var chungThu = yeuCau.CreateSelfSigned(
            DateTimeOffset.Now.AddDays(-1), DateTimeOffset.Now.AddYears(1));

        // Xuat/nap lai qua PFX de chac chan khoa bi mat dung duoc tren moi nen tang.
        return new X509Certificate2(
            chungThu.Export(X509ContentType.Pfx, "kiem-thu"),
            "kiem-thu",
            X509KeyStorageFlags.Exportable);
    }
}
