using BlueIdea.Shared.TiengViet;

namespace BlueIdea.Ai.ThuatToan;

/// <summary>
/// SimHash 64-bit (Charikar) - loc tho near-duplicate voi chi phi rat thap.
/// Hai van ban gan giong nhau se co khoang cach Hamming nho (nguong mac dinh &lt;= 8).
///
/// Dac trung dung de bam la TU DON (trong so 2) ket hop BI-GRAM (trong so 1):
/// - Tu don giu do on dinh khi sua vai tu trong doan dai (n-gram dai lam nhieu dac trung
///   thay doi cung luc, khien khoang cach Hamming vot len qua nguong).
/// - Bi-gram giu lai mot phan thong tin thu tu tu, tranh viec dao tron cau van cho
///   cung mot SimHash.
/// </summary>
public static class SimHash
{
    public const int NguongHammingMacDinh = 8;

    private const int SoBit = 64;
    private const int TrongSoTuDon = 2;
    private const int TrongSoBiGram = 1;

    /// <summary>Tinh SimHash tu van ban tho (tu dong chuan hoa tieng Viet + bo stopword).</summary>
    public static long Tinh(string? vanBan) => TinhTuDanhSachTu(VanBanTiengViet.TachTuBoStopword(vanBan));

    /// <summary>Tinh SimHash tu danh sach tu da chuan hoa.</summary>
    public static long TinhTuDanhSachTu(IReadOnlyList<string> cacTu)
    {
        ArgumentNullException.ThrowIfNull(cacTu);

        if (cacTu.Count == 0)
        {
            return 0L;
        }

        var trongSo = new Dictionary<string, int>(StringComparer.Ordinal);

        foreach (var tu in cacTu)
        {
            trongSo[tu] = trongSo.GetValueOrDefault(tu) + TrongSoTuDon;
        }

        for (var i = 0; i + 1 < cacTu.Count; i++)
        {
            var biGram = $"{cacTu[i]}_{cacTu[i + 1]}";
            trongSo[biGram] = trongSo.GetValueOrDefault(biGram) + TrongSoBiGram;
        }

        var vector = new int[SoBit];
        foreach (var (dacTrung, w) in trongSo)
        {
            var hash = HamBam.Fnv1a64(dacTrung);
            for (var bit = 0; bit < SoBit; bit++)
            {
                var batBit = (hash >> bit & 1UL) == 1UL;
                vector[bit] += batBit ? w : -w;
            }
        }

        var ketQua = 0UL;
        for (var bit = 0; bit < SoBit; bit++)
        {
            if (vector[bit] > 0)
            {
                ketQua |= 1UL << bit;
            }
        }

        return (long)ketQua;
    }

    /// <summary>Hai van ban duoc coi la ung vien trung lap khi Hamming &lt;= nguong.</summary>
    public static bool LaUngVien(long simHashA, long simHashB, int nguong = NguongHammingMacDinh)
        => HamBam.KhoangCachHamming(simHashA, simHashB) <= nguong;

    /// <summary>Quy doi khoang cach Hamming ve do tuong dong 0..1 (dung de xep hang ung vien).</summary>
    public static double DoTuongDong(long simHashA, long simHashB)
        => 1.0 - HamBam.KhoangCachHamming(simHashA, simHashB) / (double)SoBit;
}
