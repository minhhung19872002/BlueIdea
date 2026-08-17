using System.Text;
using System.Text.RegularExpressions;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BlueIdea.Reporting;

/// <summary>
/// Quet cac placeholder dang <c>{{ ten_bien }}</c> trong tep mau .docx (chuc nang 6).
///
/// Word thuong CAT MOT PLACEHOLDER thanh nhieu run rieng biet (do sua chinh ta, doi dinh dang,
/// kiem tra ngu phap), vi du "{{" o run nay va "maHoSo}}" o run khac. Vi vay phai gop van ban cua
/// ca doan roi moi so khop, khong the quet tung run.
/// </summary>
public static partial class BoQuetPlaceholderDocx
{
    /// <summary>Chan tep qua lon de mot lan quet khong ngon het bo nho may chu.</summary>
    private const long DungLuongToiDaByte = 20L * 1024 * 1024;

    [GeneratedRegex(@"\{\{\s*([\w\.]+)\s*\}\}", RegexOptions.Compiled, matchTimeoutMilliseconds: 2000)]
    private static partial Regex MauBien();

    public sealed record KetQuaQuet(
        IReadOnlyList<string> Placeholder,
        int SoDoanVan,
        int SoBang,
        string? CanhBao);

    /// <summary>Doc tep .docx va tra ve danh sach placeholder duy nhat, giu nguyen thu tu xuat hien.</summary>
    public static KetQuaQuet Quet(Stream noiDung)
    {
        ArgumentNullException.ThrowIfNull(noiDung);

        if (noiDung.CanSeek && noiDung.Length > DungLuongToiDaByte)
        {
            throw new InvalidOperationException(
                $"Tệp mẫu vượt quá {DungLuongToiDaByte / 1024 / 1024}MB.");
        }

        // OpenXml can stream doc-ghi va seek duoc; tep tai len co the la stream chi doc mot chieu.
        using var bo = new MemoryStream();
        noiDung.CopyTo(bo);
        bo.Position = 0;

        using var tep = WordprocessingDocument.Open(bo, false);

        var than = tep.MainDocumentPart?.Document?.Body;

        if (than is null)
        {
            return new KetQuaQuet(
                Array.Empty<string>(), 0, 0, "Tệp .docx không có nội dung đọc được.");
        }

        var tim = new List<string>();
        var daCo = new HashSet<string>(StringComparer.Ordinal);
        var soDoan = 0;

        foreach (var doan in than.Descendants<Paragraph>())
        {
            soDoan++;

            // Gop toan bo van ban cua doan de bat duoc placeholder bi Word cat thanh nhieu run.
            var vanBan = new StringBuilder();
            foreach (var chu in doan.Descendants<Text>())
            {
                vanBan.Append(chu.Text);
            }

            foreach (Match khop in MauBien().Matches(vanBan.ToString()))
            {
                var ten = khop.Groups[1].Value;
                if (daCo.Add(ten))
                {
                    tim.Add(ten);
                }
            }
        }

        var soBang = than.Descendants<Table>().Count();

        var canhBao = tim.Count == 0
            ? "Không tìm thấy placeholder nào. Placeholder phải viết dạng {{ tenBien }}."
            : null;

        return new KetQuaQuet(tim, soDoan, soBang, canhBao);
    }
}
