using System.Text;

namespace BlueIdea.Ai.ThuatToan;

/// <summary>
/// Ham bam 64-bit tat dinh (FNV-1a) dung cho SimHash / MinHash.
/// Bat buoc tat dinh giua cac lan chay va giua cac may -> KHONG dung string.GetHashCode()
/// (co randomized hashing trong .NET).
/// </summary>
public static class HamBam
{
    private const ulong FnvOffsetBasis = 14695981039346656037UL;
    private const ulong FnvPrime = 1099511628211UL;

    public static ulong Fnv1a64(ReadOnlySpan<byte> duLieu)
    {
        var hash = FnvOffsetBasis;
        foreach (var b in duLieu)
        {
            hash ^= b;
            hash *= FnvPrime;
        }

        return hash;
    }

    public static ulong Fnv1a64(string vanBan)
    {
        var bytes = Encoding.UTF8.GetBytes(vanBan);
        return Fnv1a64(bytes);
    }

    /// <summary>Bam co seed - dung tao K ham bam doc lap cho MinHash.</summary>
    public static ulong Fnv1a64(string vanBan, uint seed)
    {
        var bytes = Encoding.UTF8.GetBytes(vanBan);
        var hash = FnvOffsetBasis ^ seed;
        foreach (var b in bytes)
        {
            hash ^= b;
            hash *= FnvPrime;
        }

        // Tron them de cac seed khac nhau cho phan bo doc lap hon.
        hash ^= hash >> 33;
        hash *= 0xFF51AFD7ED558CCDUL;
        hash ^= hash >> 33;
        return hash;
    }

    /// <summary>Khoang cach Hamming giua 2 gia tri 64-bit.</summary>
    public static int KhoangCachHamming(long a, long b)
        => System.Numerics.BitOperations.PopCount((ulong)(a ^ b));
}
