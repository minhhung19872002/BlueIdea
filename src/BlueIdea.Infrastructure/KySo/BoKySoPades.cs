using System.Security.Cryptography.X509Certificates;
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
}
