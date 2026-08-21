using System.Security.Cryptography;
using System.Security.Cryptography.Pkcs;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using BlueIdea.Application.KySo;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using PdfSharp.Pdf.Signatures;

namespace BlueIdea.Infrastructure.KySo;

/// <summary>
/// Nhung chu ky vao BEN TRONG tep PDF theo chuan PAdES (PDF Advanced Electronic Signatures).
///
/// Khac chu ky detached PKCS#7 (<see cref="BoKySoPkcs7"/>): ban detached sinh ra MOT tep .p7s
/// nam ngoai, ai nhan van ban cung phai duoc gui kem ca hai tep va phai co cong cu rieng de doi
/// chieu. PAdES dat chu ky ngay trong PDF, nen Adobe Reader / trinh doc PDF cua he thong mot cua
/// mo len la thay ngay dong "Signed by ..." — dung cach can bo va nguoi dan thuc te kiem tra.
///
/// Ban detached VAN GIU: mot so quy trinh trao doi van ban giua co quan yeu cau nop kem tep .p7s
/// rieng, va chu ky detached ky duoc ca tep khong phai PDF.
/// </summary>
public sealed class BoKySoPades
{
    /// <summary>
    /// Nhung chu ky vao PDF.
    ///
    /// Tra ve tep PDF MOI; khong ghi de tep goc — tranh chap ve sau can doi chieu duoc ban chua
    /// ky voi ban da ky.
    /// </summary>
    public static async Task<byte[]> KyAsync(
        byte[] pdfGoc,
        X509Certificate2 chungThu,
        string? nguoiKy,
        string? lyDo,
        string? diaDiem,
        CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(pdfGoc);
        ArgumentNullException.ThrowIfNull(chungThu);

        if (!chungThu.HasPrivateKey)
        {
            throw new InvalidOperationException(
                "Chứng thư không kèm khoá bí mật nên không ký nhúng vào PDF được.");
        }

        using var vao = new MemoryStream(pdfGoc, writable: false);

        // Import de sinh lai tep sach: mot so PDF sinh tu nguon khac co cau truc khien PDFsharp
        // khong mo o che do Modify duoc, luc do ky se vo ngay o buoc mo tep.
        using var tai = PdfReader.Open(vao, PdfDocumentOpenMode.Modify);

        var bo = new PdfSharpDefaultSigner(chungThu, PdfMessageDigestType.SHA256, null);

        var tuyChon = new DigitalSignatureOptions
        {
            AppName = "BlueIdea",
            Reason = lyDo ?? "Ký số văn bản điện tử",
            Location = diaDiem ?? string.Empty,
            ContactInfo = nguoiKy ?? string.Empty,
            // Khong ve o chu ky nhin thay duoc: van ban hanh chinh Viet Nam da co phan chu ky va
            // dau o cuoi trang; chen them mot o do len tren de che mat noi dung.
            PageIndex = 0,
            Rectangle = new PdfSharp.Drawing.XRect(0, 0, 0, 0),
        };

        _ = DigitalSignatureHandler.ForDocument(tai, bo, tuyChon);

        using var ra = new MemoryStream();

        // PDFsharp ky trong luc ghi tep (tinh ByteRange roi thay cho giu cho chu ky), nen phai
        // Save vao luong chu khong the ky sau khi da co mang byte.
        tai.Save(ra);

        await Task.CompletedTask.ConfigureAwait(false);
        ct.ThrowIfCancellationRequested();

        return ra.ToArray();
    }

    /// <summary>Tep co phai PDF khong — PAdES chi ap dung cho PDF.</summary>
    public static bool LaPdf(byte[] noiDung)
        => noiDung is { Length: > 4 }
           && noiDung[0] == (byte)'%' && noiDung[1] == (byte)'P'
           && noiDung[2] == (byte)'D' && noiDung[3] == (byte)'F';

    /// <summary>
    /// Xac minh chu ky NHUNG trong PDF.
    ///
    /// PDF ky theo PAdES chua khoi PKCS#7 o <c>/Contents</c> va vung du lieu duoc ky o
    /// <c>/ByteRange</c> — chinh la ca tep TRU doan hex chua chu ky. Phai dung dung hai doan do
    /// lam noi dung doi chieu; lay ca tep se luon sai vi tep da doi sau khi chen chu ky vao.
    /// </summary>
    public static (bool CoChuKy, bool HopLe, string? Serial, string? NguoiKy,
        DateTimeOffset? ThoiGianKy, string? Loi) XacMinh(byte[] pdfDaKy)
    {
        ArgumentNullException.ThrowIfNull(pdfDaKy);

        try
        {
            var vung = DocByteRange(pdfDaKy);

            if (vung is null)
            {
                return (false, false, null, null, null,
                    "Tệp PDF không có chữ ký số nhúng (không tìm thấy /ByteRange).");
            }

            var (batDau1, dai1, batDau2, dai2) = vung.Value;

            var chuKyHex = DocContents(pdfDaKy, batDau1 + dai1, batDau2);

            if (chuKyHex is null)
            {
                return (false, false, null, null, null,
                    "Không đọc được khối chữ ký /Contents trong PDF.");
            }

            var duLieuKy = new byte[dai1 + dai2];
            Buffer.BlockCopy(pdfDaKy, batDau1, duLieuKy, 0, dai1);
            Buffer.BlockCopy(pdfDaKy, batDau2, duLieuKy, dai1, dai2);

            var cms = new SignedCms(new ContentInfo(duLieuKy), detached: true);
            cms.Decode(chuKyHex);
            cms.CheckSignature(verifySignatureOnly: true);

            var nguoiKy = cms.SignerInfos.Count > 0 ? cms.SignerInfos[0] : null;

            var thoiGianKy = nguoiKy?.SignedAttributes
                .Cast<CryptographicAttributeObject>()
                .Where(x => x.Oid.Value == "1.2.840.113549.1.9.5")
                .Select(x => new Pkcs9SigningTime(x.Values[0].RawData).SigningTime)
                .Cast<DateTime?>()
                .FirstOrDefault();

            return (true, true, nguoiKy?.Certificate?.SerialNumber, nguoiKy?.Certificate?.Subject,
                thoiGianKy.HasValue
                    ? new DateTimeOffset(thoiGianKy.Value.ToUniversalTime(), TimeSpan.Zero)
                    : null,
                null);
        }
        catch (CryptographicException ex)
        {
            // Doc duoc cau truc nhung chu ky khong khop: van ban da bi sua sau khi ky.
            return (true, false, null, null, null, ex.Message);
        }
        catch (Exception ex) when (ex is FormatException or ArgumentException or OverflowException)
        {
            return (false, false, null, null, null, ex.Message);
        }
    }

    private static (int BatDau1, int Dai1, int BatDau2, int Dai2)? DocByteRange(byte[] pdf)
    {
        var viTri = TimChuoi(pdf, "/ByteRange");

        if (viTri < 0) return null;

        var mo = Array.IndexOf(pdf, (byte)'[', viTri);
        var dong = mo < 0 ? -1 : Array.IndexOf(pdf, (byte)']', mo);

        if (mo < 0 || dong < 0) return null;

        var noiDung = Encoding.ASCII.GetString(pdf, mo + 1, dong - mo - 1);

        var so = noiDung
            .Split(new[] { ' ', '\r', '\n', '\t' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(x => int.TryParse(x, out var n) ? n : -1)
            .ToArray();

        if (so.Length < 4 || so.Any(x => x < 0)) return null;

        if (so[0] + so[1] > pdf.Length || so[2] + so[3] > pdf.Length) return null;

        return (so[0], so[1], so[2], so[3]);
    }

    private static byte[]? DocContents(byte[] pdf, int tu, int den)
    {
        if (tu < 0 || den > pdf.Length || tu >= den) return null;

        var doan = Encoding.ASCII.GetString(pdf, tu, den - tu);

        var mo = doan.IndexOf('<');
        var dong = doan.LastIndexOf('>');

        if (mo < 0 || dong <= mo) return null;

        var hex = doan[(mo + 1)..dong].Trim().TrimEnd('0');

        // Chieu dai le nghia la mot nua byte cuoi bi cat khi trim '0' — bu lai de Convert doc duoc.
        if (hex.Length % 2 == 1) hex += "0";

        return Convert.FromHexString(hex);
    }

    private static int TimChuoi(byte[] nguon, string can)
    {
        var mau = Encoding.ASCII.GetBytes(can);

        for (var i = 0; i <= nguon.Length - mau.Length; i++)
        {
            var khop = true;

            for (var j = 0; j < mau.Length; j++)
            {
                if (nguon[i + j] != mau[j])
                {
                    khop = false;
                    break;
                }
            }

            if (khop) return i;
        }

        return -1;
    }
}
