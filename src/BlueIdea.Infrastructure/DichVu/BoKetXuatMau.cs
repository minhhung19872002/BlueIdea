using System.Globalization;
using System.Text.RegularExpressions;

namespace BlueIdea.Infrastructure.DichVu;

/// <summary>
/// Bo ket xuat mau thong bao dang <c>{{ ten_bien }}</c>.
///
/// KHONG dung template engine day du (Scriban/Handlebars) vi:
/// - Mau thong bao do quan tri vien nhap tren giao dien -&gt; neu engine ho tro bieu thuc/goi ham
///   thi tro thanh be mat tan cong template injection (thuc thi ma tuy y tren may chu).
/// - Nghiep vu chi can thay the bien, khong can vong lap hay dieu kien.
/// Bo ket xuat nay chi thay the van ban thuan, khong bao gio thuc thi ma.
/// </summary>
public static partial class BoKetXuatMau
{
    private const int DoDaiToiDa = 100_000;

    [GeneratedRegex(@"\{\{\s*([\w\.]+)\s*\}\}", RegexOptions.Compiled, matchTimeoutMilliseconds: 1000)]
    private static partial Regex MauBien();

    /// <summary>
    /// Thay the moi placeholder bang gia tri tuong ung. Bien khong ton tai duoc thay bang chuoi rong
    /// de nguoi nhan khong thay ky tu ky thuat trong email.
    /// </summary>
    public static string KetXuat(string? mau, IDictionary<string, object?> bien)
    {
        if (string.IsNullOrEmpty(mau))
        {
            return string.Empty;
        }

        if (mau.Length > DoDaiToiDa)
        {
            mau = mau[..DoDaiToiDa];
        }

        return MauBien().Replace(mau, khop =>
        {
            var ten = khop.Groups[1].Value;
            return bien.TryGetValue(ten, out var giaTri) ? DinhDang(giaTri) : string.Empty;
        });
    }

    /// <summary>Liet ke cac bien duoc dung trong mau - phuc vu man hinh cau hinh mau thong bao.</summary>
    public static IReadOnlyList<string> LayDanhSachBien(string? mau)
    {
        if (string.IsNullOrEmpty(mau))
        {
            return Array.Empty<string>();
        }

        return MauBien().Matches(mau)
            .Select(m => m.Groups[1].Value)
            .Distinct(StringComparer.Ordinal)
            .ToList();
    }

    private static string DinhDang(object? giaTri) => giaTri switch
    {
        null => string.Empty,
        DateTimeOffset dto => dto.ToOffset(TimeSpan.FromHours(7))
            .ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN")),
        DateTime dt => dt.ToString("dd/MM/yyyy HH:mm", CultureInfo.GetCultureInfo("vi-VN")),
        DateOnly d => d.ToString("dd/MM/yyyy", CultureInfo.GetCultureInfo("vi-VN")),
        decimal m => m.ToString("#,##0.##", CultureInfo.GetCultureInfo("vi-VN")),
        double db => db.ToString("#,##0.##", CultureInfo.GetCultureInfo("vi-VN")),
        bool b => b ? "Có" : "Không",
        IFormattable f => f.ToString(null, CultureInfo.GetCultureInfo("vi-VN")),
        _ => giaTri.ToString() ?? string.Empty
    };
}
