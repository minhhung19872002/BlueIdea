using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace BlueIdea.Shared.TiengViet;

/// <summary>
/// Tien ich xu ly van ban tieng Viet dung chung toan he thong.
/// Bao gom: chuan hoa NFC, bo dau (unaccent), tao slug, chuan hoa cho so khop van ban.
/// Cac ham nay phai cho ket qua GIONG HET ham <c>unaccent()</c> cua PostgreSQL de tim kiem
/// phia client va phia server tra ve cung ket qua.
/// </summary>
public static partial class VanBanTiengViet
{
    // Bang anh xa ky tu co dau -> khong dau. Bao phu day du bo chu cai tieng Viet.
    private const string CoDau =
        "àáạảãâầấậẩẫăằắặẳẵ" +
        "èéẹẻẽêềếệểễ" +
        "ìíịỉĩ" +
        "òóọỏõôồốộổỗơờớợởỡ" +
        "ùúụủũưừứựửữ" +
        "ỳýỵỷỹ" +
        "đ" +
        "ÀÁẠẢÃÂẦẤẬẨẪĂẰẮẶẲẴ" +
        "ÈÉẸẺẼÊỀẾỆỂỄ" +
        "ÌÍỊỈĨ" +
        "ÒÓỌỎÕÔỒỐỘỔỖƠỜỚỢỞỠ" +
        "ÙÚỤỦŨƯỪỨỰỬỮ" +
        "ỲÝỴỶỸ" +
        "Đ";

    private const string KhongDau =
        "aaaaaaaaaaaaaaaaa" +
        "eeeeeeeeeee" +
        "iiiii" +
        "ooooooooooooooooo" +
        "uuuuuuuuuuu" +
        "yyyyy" +
        "d" +
        "AAAAAAAAAAAAAAAAA" +
        "EEEEEEEEEEE" +
        "IIIII" +
        "OOOOOOOOOOOOOOOOO" +
        "UUUUUUUUUUU" +
        "YYYYY" +
        "D";

    private static readonly Dictionary<char, char> BangAnhXa = TaoBangAnhXa();

    private static Dictionary<char, char> TaoBangAnhXa()
    {
        if (CoDau.Length != KhongDau.Length)
        {
            throw new InvalidOperationException(
                $"Bảng ánh xạ bỏ dấu không khớp độ dài: {CoDau.Length} vs {KhongDau.Length}");
        }

        var bang = new Dictionary<char, char>(CoDau.Length);
        for (var i = 0; i < CoDau.Length; i++)
        {
            bang[CoDau[i]] = KhongDau[i];
        }

        return bang;
    }

    [GeneratedRegex(@"\s+", RegexOptions.Compiled)]
    private static partial Regex KhoangTrangThua();

    [GeneratedRegex(@"[^a-z0-9\s]", RegexOptions.Compiled)]
    private static partial Regex KyTuKhongPhaiChuSo();

    [GeneratedRegex(@"[^a-zA-Z0-9]+", RegexOptions.Compiled)]
    private static partial Regex KyTuNgoaiSlug();

    /// <summary>
    /// Chuan hoa Unicode ve dang NFC (yeu cau bat buoc cua dac ta - Muc 7).
    /// Du lieu nhap tu nhieu nguon (macOS dung NFD) phai duoc chuan hoa truoc khi luu.
    /// </summary>
    public static string ChuanHoaNfc(string? vanBan)
        => string.IsNullOrEmpty(vanBan) ? string.Empty : vanBan.Normalize(NormalizationForm.FormC);

    /// <summary>
    /// Bo dau tieng Viet, giu nguyen hoa/thuong. Tuong duong <c>unaccent()</c> cua PostgreSQL.
    /// </summary>
    public static string BoDau(string? vanBan)
    {
        if (string.IsNullOrEmpty(vanBan))
        {
            return string.Empty;
        }

        // Chuan hoa NFC truoc de moi ky tu co dau la 1 code point duy nhat.
        var nguon = ChuanHoaNfc(vanBan);
        var ketQua = new StringBuilder(nguon.Length);

        foreach (var kyTu in nguon)
        {
            if (BangAnhXa.TryGetValue(kyTu, out var thay))
            {
                ketQua.Append(thay);
                continue;
            }

            if (char.IsSurrogate(kyTu)) continue;

            var tach = kyTu.ToString().Normalize(NormalizationForm.FormD);
            foreach (var c in tach)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    ketQua.Append(c);
                }
            }
        }

        return ketQua.ToString();
    }

    /// <summary>
    /// Sinh gia tri cho cot <c>*_khong_dau</c>: bo dau + chu thuong + gom khoang trang.
    /// </summary>
    public static string TaoKhongDau(string? vanBan)
        => string.IsNullOrWhiteSpace(vanBan)
            ? string.Empty
            : KhoangTrangThua().Replace(BoDau(vanBan).ToLowerInvariant(), " ").Trim();

    /// <summary>
    /// Chuan hoa van ban phuc vu so khop trung lap: bo dau, chu thuong,
    /// loai bo toan bo ky tu khong phai chu/so, gom khoang trang.
    /// </summary>
    public static string ChuanHoaDeSoKhop(string? vanBan)
    {
        if (string.IsNullOrWhiteSpace(vanBan))
        {
            return string.Empty;
        }

        var khongDau = BoDau(vanBan).ToLowerInvariant();
        var sach = KyTuKhongPhaiChuSo().Replace(khongDau, " ");
        return KhoangTrangThua().Replace(sach, " ").Trim();
    }

    /// <summary>Tach van ban thanh danh sach tu (da chuan hoa de so khop).</summary>
    public static string[] TachTu(string? vanBan)
    {
        var chuanHoa = ChuanHoaDeSoKhop(vanBan);
        return chuanHoa.Length == 0
            ? Array.Empty<string>()
            : chuanHoa.Split(' ', StringSplitOptions.RemoveEmptyEntries);
    }

    /// <summary>Tach tu va loai bo stopwords tieng Viet.</summary>
    public static string[] TachTuBoStopword(string? vanBan)
        => TachTu(vanBan).Where(tu => !StopwordTiengViet.LaStopword(tu)).ToArray();

    /// <summary>Tao slug dung cho URL / ma tu dong: "Sáng kiến mới" -&gt; "sang-kien-moi".</summary>
    public static string TaoSlug(string? vanBan)
    {
        if (string.IsNullOrWhiteSpace(vanBan))
        {
            return string.Empty;
        }

        var khongDau = BoDau(vanBan).ToLowerInvariant();
        return KyTuNgoaiSlug().Replace(khongDau, "-").Trim('-');
    }

    /// <summary>
    /// So sanh 2 chuoi theo kieu "tim kiem tieng Viet": khong phan biet hoa thuong va dau.
    /// Dung cho loc phia bo nho (danh muc nho); truy van lon dung cot *_khong_dau tren DB.
    /// </summary>
    public static bool ChuaKhongDau(string? nguon, string? tuKhoa)
    {
        if (string.IsNullOrWhiteSpace(tuKhoa))
        {
            return true;
        }

        return TaoKhongDau(nguon).Contains(TaoKhongDau(tuKhoa), StringComparison.Ordinal);
    }

    /// <summary>Cat bot van ban va them dau "…" - dung cho hien thi trich doan.</summary>
    public static string CatNgan(string? vanBan, int doDaiToiDa)
    {
        if (string.IsNullOrEmpty(vanBan) || doDaiToiDa <= 0)
        {
            return string.Empty;
        }

        return vanBan.Length <= doDaiToiDa ? vanBan : vanBan[..doDaiToiDa].TrimEnd() + "…";
    }
}
