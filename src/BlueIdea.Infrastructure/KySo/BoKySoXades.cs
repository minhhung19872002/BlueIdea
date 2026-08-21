using System.Globalization;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Security.Cryptography.Xml;
using System.Text;
using System.Xml;

namespace BlueIdea.Infrastructure.KySo;

/// <summary>
/// Chuc nang 49 - Ky van ban XML theo chuan XAdES-BES (enveloped signature).
///
/// Vi sao can rieng bo nay: van ban trao doi lien thong voi he thong ngoai (Thi dua khen thuong,
/// IOC, mot cua) thuong la XML, va ben nhan kiem tra bang cong cu XML-DSig chuan chu khong mo
/// tep .p7s roi doi chieu tay. Chu ky PKCS#7 tach roi van dung duoc nhung ben nhan phai duoc gui
/// KEM hai tep — thuc te rat de that lac mot trong hai.
///
/// XAdES-BES = XML-DSig + khoi <c>QualifyingProperties</c> mang thoi diem ky va dau van tay chung
/// thu, ca hai deu nam TRONG pham vi ky nen khong sua duoc ma chu ky con hop le.
/// </summary>
public static class BoKySoXades
{
    /// <summary>Khong gian ten XAdES v1.3.2 (TS 101 903).</summary>
    private const string NamespaceXades = "http://uri.etsi.org/01903/v1.3.2#";

    private const string LoaiSignedProperties = "http://uri.etsi.org/01903#SignedProperties";

    private const string IdChuKy = "chu-ky-blueidea";

    private const string IdSignedProperties = "signed-properties-blueidea";

    /// <summary>
    /// Nhan dien tep XML.
    ///
    /// Bo qua BOM va khoang trang dau tep roi kiem tra ky tu '&lt;': du de phan biet voi PDF/DOCX
    /// ma khong phai parse ca tep chi de biet co nen ky XAdES hay khong.
    /// </summary>
    public static bool LaXml(byte[] noiDung)
    {
        if (noiDung is null || noiDung.Length == 0) return false;

        var batDau = 0;

        // BOM UTF-8.
        if (noiDung.Length >= 3 && noiDung[0] == 0xEF && noiDung[1] == 0xBB && noiDung[2] == 0xBF)
        {
            batDau = 3;
        }

        for (var i = batDau; i < Math.Min(noiDung.Length, batDau + 64); i++)
        {
            var c = (char)noiDung[i];

            if (char.IsWhiteSpace(c)) continue;

            return c == '<';
        }

        return false;
    }

    /// <summary>
    /// Ky nhung (enveloped) vao chinh tep XML, tra ve tep XML moi da co khoi &lt;Signature&gt;.
    /// Khong sua tep goc: tranh chap ve sau can doi chieu ban chua ky voi ban da ky.
    /// </summary>
    public static byte[] Ky(
        byte[] xmlGoc, X509Certificate2 chungThu, DateTimeOffset thoiDiemKy)
    {
        ArgumentNullException.ThrowIfNull(xmlGoc);
        ArgumentNullException.ThrowIfNull(chungThu);

        if (!chungThu.HasPrivateKey)
        {
            throw new InvalidOperationException(
                "Chứng thư không kèm khoá bí mật nên không ký XML được.");
        }

        var taiLieu = TaiLieuAnToan();

        using (var luong = new MemoryStream(xmlGoc, writable: false))
        {
            taiLieu.Load(luong);
        }

        var khoa = chungThu.GetRSAPrivateKey()
                   ?? throw new InvalidOperationException(
                       "Chỉ hỗ trợ chứng thư RSA cho chữ ký XAdES.");

        var chuKy = new SignedXmlCoIdTuyY(taiLieu)
        {
            SigningKey = khoa
        };

        chuKy.Signature.Id = IdChuKy;
        chuKy.SignedInfo!.SignatureMethod = SignedXml.XmlDsigRSASHA256Url;

        // Tham chieu 1: toan bo tai lieu (tru chinh khoi chu ky).
        var thamChieuTaiLieu = new Reference(string.Empty)
        {
            DigestMethod = SignedXml.XmlDsigSHA256Url
        };
        thamChieuTaiLieu.AddTransform(new XmlDsigEnvelopedSignatureTransform());
        thamChieuTaiLieu.AddTransform(new XmlDsigExcC14NTransform());
        chuKy.AddReference(thamChieuTaiLieu);

        // Tham chieu 2: khoi thuoc tinh XAdES — dieu bat buoc de goi la XAdES-BES chu khong
        // chi la XML-DSig thuong.
        var doiTuong = TaoQualifyingProperties(taiLieu, chungThu, thoiDiemKy);
        chuKy.AddObject(doiTuong);

        var thamChieuThuocTinh = new Reference($"#{IdSignedProperties}")
        {
            Type = LoaiSignedProperties,
            DigestMethod = SignedXml.XmlDsigSHA256Url
        };
        thamChieuThuocTinh.AddTransform(new XmlDsigExcC14NTransform());
        chuKy.AddReference(thamChieuThuocTinh);

        var thongTinKhoa = new KeyInfo();
        thongTinKhoa.AddClause(new KeyInfoX509Data(chungThu, X509IncludeOption.WholeChain));
        chuKy.KeyInfo = thongTinKhoa;

        chuKy.ComputeSignature();

        taiLieu.DocumentElement!.AppendChild(
            taiLieu.ImportNode(chuKy.GetXml(), deep: true));

        using var ra = new MemoryStream();
        using (var bo = XmlWriter.Create(ra, new XmlWriterSettings
        {
            Encoding = new UTF8Encoding(false),
            Indent = false
        }))
        {
            taiLieu.Save(bo);
        }

        return ra.ToArray();
    }

    /// <summary>
    /// Xac minh chu ky nhung trong tep XML.
    ///
    /// Chi kiem tra ve mat MAT MA (chu ky khop noi dung va khoa cong khai trong tep). Khong kiem
    /// chuoi tin cay CA: may chu noi bo thuong khong cai san CA goc cua nha cung cap, giong cach
    /// <see cref="BoKySoPkcs7"/> dang lam voi CMS.
    /// </summary>
    public static (bool CoChuKy, bool HopLe, string? Serial, string? NguoiKy,
        DateTimeOffset? ThoiGianKy, string? Loi) XacMinh(byte[] xmlDaKy)
    {
        try
        {
            var taiLieu = TaiLieuAnToan();

            using (var luong = new MemoryStream(xmlDaKy, writable: false))
            {
                taiLieu.Load(luong);
            }

            var nut = taiLieu.GetElementsByTagName("Signature", SignedXml.XmlDsigNamespaceUrl);

            if (nut.Count == 0)
            {
                return (false, false, null, null, null, "Tệp XML không có khối chữ ký.");
            }

            var chuKy = new SignedXml(taiLieu);
            chuKy.LoadXml((XmlElement)nut[0]!);

            var hopLe = chuKy.CheckSignature();

            var chungThu = chuKy.KeyInfo
                .OfType<KeyInfoX509Data>()
                .SelectMany(x => (x.Certificates ?? new System.Collections.ArrayList())
                    .OfType<X509Certificate2>())
                .FirstOrDefault();

            var thoiGian = DocThoiDiemKy(taiLieu);

            return (true, hopLe, chungThu?.SerialNumber, chungThu?.Subject, thoiGian,
                hopLe ? null : "Chữ ký không khớp nội dung tệp.");
        }
        catch (Exception ex) when (ex is XmlException or CryptographicException)
        {
            return (false, false, null, null, null, ex.Message);
        }
    }

    /// <summary>
    /// XmlDocument voi DTD tat han: tep XML den tu ben ngoai, mo DTD la mo luon cua XXE
    /// (doc tep tren may chu, goi mang noi bo).
    /// </summary>
    private static XmlDocument TaiLieuAnToan()
        => new()
        {
            PreserveWhitespace = true,
            XmlResolver = null
        };

    private static DataObject TaoQualifyingProperties(
        XmlDocument taiLieu, X509Certificate2 chungThu, DateTimeOffset thoiDiemKy)
    {
        var khoi = taiLieu.CreateElement("xades", "QualifyingProperties", NamespaceXades);
        khoi.SetAttribute("Target", $"#{IdChuKy}");

        var signedProperties = taiLieu.CreateElement("xades", "SignedProperties", NamespaceXades);
        signedProperties.SetAttribute("Id", IdSignedProperties);

        var signedSignatureProperties =
            taiLieu.CreateElement("xades", "SignedSignatureProperties", NamespaceXades);

        var signingTime = taiLieu.CreateElement("xades", "SigningTime", NamespaceXades);
        signingTime.InnerText = thoiDiemKy.ToUniversalTime()
            .ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture);

        var signingCertificate = taiLieu.CreateElement("xades", "SigningCertificate", NamespaceXades);
        var cert = taiLieu.CreateElement("xades", "Cert", NamespaceXades);
        var certDigest = taiLieu.CreateElement("xades", "CertDigest", NamespaceXades);

        var digestMethod = taiLieu.CreateElement("ds", "DigestMethod", SignedXml.XmlDsigNamespaceUrl);
        digestMethod.SetAttribute("Algorithm", SignedXml.XmlDsigSHA256Url);

        var digestValue = taiLieu.CreateElement("ds", "DigestValue", SignedXml.XmlDsigNamespaceUrl);
        digestValue.InnerText = Convert.ToBase64String(SHA256.HashData(chungThu.RawData));

        certDigest.AppendChild(digestMethod);
        certDigest.AppendChild(digestValue);

        var issuerSerial = taiLieu.CreateElement("xades", "IssuerSerial", NamespaceXades);
        var issuerName = taiLieu.CreateElement("ds", "X509IssuerName", SignedXml.XmlDsigNamespaceUrl);
        issuerName.InnerText = chungThu.Issuer;
        var serialNumber = taiLieu.CreateElement("ds", "X509SerialNumber", SignedXml.XmlDsigNamespaceUrl);
        serialNumber.InnerText = SoSerialThapPhan(chungThu.SerialNumber);

        issuerSerial.AppendChild(issuerName);
        issuerSerial.AppendChild(serialNumber);

        cert.AppendChild(certDigest);
        cert.AppendChild(issuerSerial);
        signingCertificate.AppendChild(cert);

        signedSignatureProperties.AppendChild(signingTime);
        signedSignatureProperties.AppendChild(signingCertificate);
        signedProperties.AppendChild(signedSignatureProperties);
        khoi.AppendChild(signedProperties);

        // Bao khoi vao mot fragment roi lay ChildNodes: DataObject can mot XmlNodeList, va cach
        // nay giu nguyen tien to namespace "xades" khi ky — doi tien to la doi ca gia tri bam.
        var boc = taiLieu.CreateDocumentFragment();
        boc.AppendChild(khoi);

        return new DataObject { Data = boc.ChildNodes };
    }

    private static string SoSerialThapPhan(string serialHex)
    {
        try
        {
            var so = System.Numerics.BigInteger.Parse(
                "0" + serialHex, NumberStyles.HexNumber, CultureInfo.InvariantCulture);

            return so.ToString(CultureInfo.InvariantCulture);
        }
        catch (FormatException)
        {
            return serialHex;
        }
    }

    private static DateTimeOffset? DocThoiDiemKy(XmlDocument taiLieu)
    {
        var nut = taiLieu.GetElementsByTagName("SigningTime", NamespaceXades);

        if (nut.Count == 0) return null;

        return DateTimeOffset.TryParse(
            nut[0]!.InnerText, CultureInfo.InvariantCulture,
            DateTimeStyles.AdjustToUniversal | DateTimeStyles.AssumeUniversal, out var thoiGian)
            ? thoiGian
            : null;
    }
}

/// <summary>
/// <see cref="SignedXml"/> co the tim phan tu theo thuoc tinh <c>Id</c> o BAT KY dau, ke ca ben
/// trong khoi &lt;ds:Object&gt;.
///
/// Ban mac dinh chi tra cuu Id trong tai lieu goc nen tham chieu
/// <c>#signed-properties</c> cua XAdES khong phan giai duoc va ComputeSignature nem
/// "Malformed reference element".
/// </summary>
internal sealed class SignedXmlCoIdTuyY : SignedXml
{
    public SignedXmlCoIdTuyY(XmlDocument taiLieu) : base(taiLieu)
    {
    }

    public override XmlElement? GetIdElement(XmlDocument? taiLieu, string idValue)
    {
        var mac = base.GetIdElement(taiLieu, idValue);

        if (mac is not null) return mac;

        foreach (DataObject doiTuong in Signature.ObjectList)
        {
            foreach (XmlNode nut in doiTuong.Data)
            {
                var tim = TimTheoId(nut, idValue);

                if (tim is not null) return tim;
            }
        }

        return null;
    }

    private static XmlElement? TimTheoId(XmlNode nut, string idValue)
    {
        if (nut is XmlElement phanTu && phanTu.GetAttribute("Id") == idValue)
        {
            return phanTu;
        }

        foreach (XmlNode con in nut.ChildNodes)
        {
            var tim = TimTheoId(con, idValue);

            if (tim is not null) return tim;
        }

        return null;
    }
}
