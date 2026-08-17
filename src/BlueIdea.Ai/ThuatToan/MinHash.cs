namespace BlueIdea.Ai.ThuatToan;

/// <summary>
/// MinHash + LSH banding - loc ung vien trung lap voi recall cao, chi phi thap.
/// Chu ky MinHash uoc luong he so Jaccard: P(minhash_a[i] == minhash_b[i]) = Jaccard(A, B).
/// </summary>
public sealed class MinHash
{
    public const int SoHamMacDinh = 128;

    private readonly int _soHam;

    public MinHash(int soHam = SoHamMacDinh)
    {
        if (soHam <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(soHam), "Số hàm băm phải lớn hơn 0.");
        }

        _soHam = soHam;
    }

    public int SoHam => _soHam;

    /// <summary>Tinh chu ky MinHash tu tap shingle.</summary>
    public ulong[] TinhChuKy(IEnumerable<string> cacShingle)
    {
        var chuKy = new ulong[_soHam];
        Array.Fill(chuKy, ulong.MaxValue);

        var coPhanTu = false;
        foreach (var shingle in cacShingle)
        {
            coPhanTu = true;
            for (var i = 0; i < _soHam; i++)
            {
                var hash = HamBam.Fnv1a64(shingle, (uint)(i + 1));
                if (hash < chuKy[i])
                {
                    chuKy[i] = hash;
                }
            }
        }

        if (!coPhanTu)
        {
            Array.Fill(chuKy, 0UL);
        }

        return chuKy;
    }

    public ulong[] TinhChuKyTuVanBan(string? vanBan, int kichThuocShingle = Shingle.KichThuocMacDinh)
        => TinhChuKy(Shingle.TaoTapShingle(vanBan, kichThuocShingle));

    /// <summary>Uoc luong he so Jaccard tu 2 chu ky MinHash.</summary>
    public static double UocLuongJaccard(ulong[] chuKyA, ulong[] chuKyB)
    {
        ArgumentNullException.ThrowIfNull(chuKyA);
        ArgumentNullException.ThrowIfNull(chuKyB);

        if (chuKyA.Length != chuKyB.Length)
        {
            throw new ArgumentException("Hai chữ ký MinHash phải cùng độ dài.", nameof(chuKyB));
        }

        if (chuKyA.Length == 0)
        {
            return 0d;
        }

        var trung = 0;
        for (var i = 0; i < chuKyA.Length; i++)
        {
            if (chuKyA[i] == chuKyB[i])
            {
                trung++;
            }
        }

        return trung / (double)chuKyA.Length;
    }

    /// <summary>
    /// Sinh khoa LSH theo band: chia chu ky thanh <paramref name="soBand"/> dai,
    /// hai van ban chi can trung 1 band la tro thanh ung vien.
    /// </summary>
    public IReadOnlyList<string> SinhKhoaLsh(ulong[] chuKy, int soBand = 16)
    {
        ArgumentNullException.ThrowIfNull(chuKy);

        if (soBand <= 0 || soBand > chuKy.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(soBand),
                "Số band phải nằm trong khoảng 1..số hàm băm.");
        }

        var soDong = chuKy.Length / soBand;
        var khoa = new List<string>(soBand);

        for (var band = 0; band < soBand; band++)
        {
            var batDau = band * soDong;
            var noiDung = string.Join('-', chuKy.Skip(batDau).Take(soDong));
            khoa.Add($"{band}:{HamBam.Fnv1a64(noiDung):x16}");
        }

        return khoa;
    }
}
