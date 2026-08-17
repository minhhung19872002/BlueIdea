using BlueIdea.Shared.TiengViet;

namespace BlueIdea.Ai.ThuatToan;

/// <summary>Cac phep do tuong dong van ban chay thuan .NET (khong goi API ngoai).</summary>
public static class DoTuongDong
{
    /// <summary>He so Jaccard giua 2 tap hop: |A ∩ B| / |A ∪ B|.</summary>
    public static double Jaccard<T>(IReadOnlyCollection<T> tapA, IReadOnlyCollection<T> tapB)
    {
        ArgumentNullException.ThrowIfNull(tapA);
        ArgumentNullException.ThrowIfNull(tapB);

        if (tapA.Count == 0 && tapB.Count == 0)
        {
            return 1d;
        }

        if (tapA.Count == 0 || tapB.Count == 0)
        {
            return 0d;
        }

        var a = tapA as ISet<T> ?? tapA.ToHashSet();
        var b = tapB as ISet<T> ?? tapB.ToHashSet();

        var giao = a.Count <= b.Count ? a.Count(x => b.Contains(x)) : b.Count(x => a.Contains(x));
        var hop = a.Count + b.Count - giao;

        return hop == 0 ? 0d : giao / (double)hop;
    }

    /// <summary>He so Jaccard tren shingle cua 2 van ban.</summary>
    public static double JaccardVanBan(string? vanBanA, string? vanBanB, int kichThuocShingle = Shingle.KichThuocMacDinh)
        => Jaccard(
            Shingle.TaoTapShingle(vanBanA, kichThuocShingle),
            Shingle.TaoTapShingle(vanBanB, kichThuocShingle));

    /// <summary>Cosine giua 2 vector thua (tu -&gt; trong so).</summary>
    public static double CosineThua(
        IReadOnlyDictionary<string, double> vectorA, IReadOnlyDictionary<string, double> vectorB)
    {
        ArgumentNullException.ThrowIfNull(vectorA);
        ArgumentNullException.ThrowIfNull(vectorB);

        if (vectorA.Count == 0 || vectorB.Count == 0)
        {
            return 0d;
        }

        var tich = 0d;
        // Duyet vector nho hon de giam so lan tra cuu.
        var (nho, lon) = vectorA.Count <= vectorB.Count ? (vectorA, vectorB) : (vectorB, vectorA);
        foreach (var (tu, trongSo) in nho)
        {
            if (lon.TryGetValue(tu, out var trongSoKhac))
            {
                tich += trongSo * trongSoKhac;
            }
        }

        var chuanA = Math.Sqrt(vectorA.Values.Sum(v => v * v));
        var chuanB = Math.Sqrt(vectorB.Values.Sum(v => v * v));

        return chuanA == 0d || chuanB == 0d ? 0d : tich / (chuanA * chuanB);
    }

    /// <summary>Cosine giua 2 vector dac (embedding).</summary>
    public static double CosineDac(IReadOnlyList<float> a, IReadOnlyList<float> b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Count == 0 || b.Count == 0)
        {
            return 0d;
        }

        if (a.Count != b.Count)
        {
            throw new ArgumentException("Hai vector phải cùng số chiều.", nameof(b));
        }

        double tich = 0, chuanA = 0, chuanB = 0;
        for (var i = 0; i < a.Count; i++)
        {
            tich += (double)a[i] * b[i];
            chuanA += (double)a[i] * a[i];
            chuanB += (double)b[i] * b[i];
        }

        return chuanA == 0d || chuanB == 0d ? 0d : tich / (Math.Sqrt(chuanA) * Math.Sqrt(chuanB));
    }

    /// <summary>Khoang cach Levenshtein - dung doi chieu doan ngan khi highlight.</summary>
    public static int Levenshtein(string a, string b)
    {
        ArgumentNullException.ThrowIfNull(a);
        ArgumentNullException.ThrowIfNull(b);

        if (a.Length == 0)
        {
            return b.Length;
        }

        if (b.Length == 0)
        {
            return a.Length;
        }

        var truoc = new int[b.Length + 1];
        var hienTai = new int[b.Length + 1];

        for (var j = 0; j <= b.Length; j++)
        {
            truoc[j] = j;
        }

        for (var i = 1; i <= a.Length; i++)
        {
            hienTai[0] = i;
            for (var j = 1; j <= b.Length; j++)
            {
                var chiPhi = a[i - 1] == b[j - 1] ? 0 : 1;
                hienTai[j] = Math.Min(Math.Min(hienTai[j - 1] + 1, truoc[j] + 1), truoc[j - 1] + chiPhi);
            }

            (truoc, hienTai) = (hienTai, truoc);
        }

        return truoc[b.Length];
    }
}

/// <summary>
/// Mo hinh TF-IDF tren mot tap tai lieu. Xay dung IDF tu corpus roi tinh cosine giua cac tai lieu.
/// </summary>
public sealed class MoHinhTfIdf
{
    private readonly Dictionary<string, double> _idf;
    private readonly int _soTaiLieu;

    private MoHinhTfIdf(Dictionary<string, double> idf, int soTaiLieu)
    {
        _idf = idf;
        _soTaiLieu = soTaiLieu;
    }

    public int SoTaiLieu => _soTaiLieu;

    public IReadOnlyDictionary<string, double> Idf => _idf;

    /// <summary>Huan luyen IDF tu corpus (moi phan tu la mot van ban tho).</summary>
    public static MoHinhTfIdf HuanLuyen(IEnumerable<string> corpus, bool boStopword = true)
    {
        ArgumentNullException.ThrowIfNull(corpus);

        var danhSachTu = corpus
            .Select(vb => boStopword ? VanBanTiengViet.TachTuBoStopword(vb) : VanBanTiengViet.TachTu(vb))
            .ToList();

        var df = new Dictionary<string, int>(StringComparer.Ordinal);
        foreach (var tu in danhSachTu)
        {
            foreach (var t in tu.Distinct(StringComparer.Ordinal))
            {
                df[t] = df.GetValueOrDefault(t) + 1;
            }
        }

        var n = danhSachTu.Count;
        var idf = new Dictionary<string, double>(df.Count, StringComparer.Ordinal);
        foreach (var (tu, soTaiLieuChua) in df)
        {
            // IDF lam min (smooth) de tranh chia 0 va tranh trong so am.
            idf[tu] = Math.Log((n + 1d) / (soTaiLieuChua + 1d)) + 1d;
        }

        return new MoHinhTfIdf(idf, n);
    }

    /// <summary>Tinh vector TF-IDF (chuan hoa L2) cua mot van ban.</summary>
    public IReadOnlyDictionary<string, double> TinhVector(string? vanBan, bool boStopword = true)
    {
        var tu = boStopword ? VanBanTiengViet.TachTuBoStopword(vanBan) : VanBanTiengViet.TachTu(vanBan);
        if (tu.Length == 0)
        {
            return new Dictionary<string, double>();
        }

        var tf = new Dictionary<string, double>(StringComparer.Ordinal);
        foreach (var t in tu)
        {
            tf[t] = tf.GetValueOrDefault(t) + 1d;
        }

        var vector = new Dictionary<string, double>(tf.Count, StringComparer.Ordinal);
        foreach (var (t, soLan) in tf)
        {
            // Tu chua tung xuat hien trong corpus van duoc tinh voi IDF mac dinh cao nhat.
            var idf = _idf.TryGetValue(t, out var giaTri)
                ? giaTri
                : Math.Log((_soTaiLieu + 1d) / 1d) + 1d;

            vector[t] = soLan / tu.Length * idf;
        }

        var chuan = Math.Sqrt(vector.Values.Sum(v => v * v));
        if (chuan > 0d)
        {
            foreach (var t in vector.Keys.ToList())
            {
                vector[t] /= chuan;
            }
        }

        return vector;
    }

    /// <summary>Cosine TF-IDF giua 2 van ban.</summary>
    public double Cosine(string? vanBanA, string? vanBanB)
        => DoTuongDong.CosineThua(TinhVector(vanBanA), TinhVector(vanBanB));
}
