using BlueIdea.Shared.TiengViet;

namespace BlueIdea.Ai.ThuatToan;

/// <summary>Sinh shingle (n-gram tu) - don vi dac trung dung cho SimHash, MinHash va Jaccard.</summary>
public static class Shingle
{
    public const int KichThuocMacDinh = 5;

    /// <summary>Tao shingle tu van ban tho (chuan hoa tieng Viet truoc).</summary>
    public static IReadOnlyList<string> TaoShingle(string? vanBan, int kichThuoc = KichThuocMacDinh)
        => TaoShingleTu(VanBanTiengViet.TachTu(vanBan), kichThuoc);

    /// <summary>Tao shingle tu danh sach tu da chuan hoa.</summary>
    public static IReadOnlyList<string> TaoShingleTu(IReadOnlyList<string> cacTu, int kichThuoc = KichThuocMacDinh)
    {
        if (kichThuoc <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(kichThuoc), "Kích thước shingle phải lớn hơn 0.");
        }

        if (cacTu.Count == 0)
        {
            return Array.Empty<string>();
        }

        if (cacTu.Count < kichThuoc)
        {
            return new[] { string.Join(' ', cacTu) };
        }

        var ketQua = new List<string>(cacTu.Count - kichThuoc + 1);
        for (var i = 0; i + kichThuoc <= cacTu.Count; i++)
        {
            ketQua.Add(string.Join(' ', cacTu.Skip(i).Take(kichThuoc)));
        }

        return ketQua;
    }

    /// <summary>Tap shingle duy nhat - dung tinh Jaccard.</summary>
    public static HashSet<string> TaoTapShingle(string? vanBan, int kichThuoc = KichThuocMacDinh)
        => TaoShingle(vanBan, kichThuoc).ToHashSet(StringComparer.Ordinal);
}
