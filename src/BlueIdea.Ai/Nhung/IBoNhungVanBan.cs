using BlueIdea.Ai.ThuatToan;
using BlueIdea.Shared.TiengViet;

namespace BlueIdea.Ai.Nhung;

/// <summary>
/// Sinh vector nhung (embedding) cho van ban tieng Viet.
/// BAT BUOC chay noi bo (ONNX Runtime tren may chu cua don vi) - khong goi API AI ben thu ba.
/// Xem docs/ADR/0001-ai-noi-bo.md.
/// </summary>
public interface IBoNhungVanBan
{
    /// <summary>So chieu cua vector (pgvector cot embedding phai khop so nay).</summary>
    int SoChieu { get; }

    /// <summary>Ten mo hinh dang su dung - ghi vao ket qua kiem tra de truy vet.</summary>
    string TenMoHinh { get; }

    Task<float[]> TaoVectorAsync(string? vanBan, CancellationToken ct = default);

    Task<IReadOnlyList<float[]>> TaoVectorHangLoatAsync(
        IReadOnlyList<string> cacVanBan, CancellationToken ct = default);
}

/// <summary>
/// Bo nhung du phong chay hoan toan trong .NET bang ky thuat "hashing trick":
/// moi tu / bi-gram duoc bam vao mot chieu co dinh, trong so TF-IDF cuc bo, sau do chuan hoa L2.
///
/// Dung khi chua nap duoc mo hinh ONNX (yeu cau "graceful degradation" - Muc 7 dac ta):
/// he thong van phat hien duoc trung lap tu vung, chi giam do nhay ve ngu nghia.
/// Tat dinh tuyet doi giua cac lan chay va giua cac may.
/// </summary>
public sealed class BoNhungBamTuVung : IBoNhungVanBan
{
    public BoNhungBamTuVung(int soChieu = 768)
    {
        if (soChieu <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(soChieu), "Số chiều phải lớn hơn 0.");
        }

        SoChieu = soChieu;
    }

    public int SoChieu { get; }

    public string TenMoHinh => $"bam-tu-vung-{SoChieu}";

    public Task<float[]> TaoVectorAsync(string? vanBan, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.FromResult(TaoVector(vanBan));
    }

    public Task<IReadOnlyList<float[]>> TaoVectorHangLoatAsync(
        IReadOnlyList<string> cacVanBan, CancellationToken ct = default)
    {
        ArgumentNullException.ThrowIfNull(cacVanBan);
        ct.ThrowIfCancellationRequested();

        IReadOnlyList<float[]> ketQua = cacVanBan.Select(TaoVector).ToList();
        return Task.FromResult(ketQua);
    }

    public float[] TaoVector(string? vanBan)
    {
        var vector = new float[SoChieu];
        var tu = VanBanTiengViet.TachTuBoStopword(vanBan);

        if (tu.Length == 0)
        {
            return vector;
        }

        void Cong(string dacTrung, float trongSo)
        {
            var hash = HamBam.Fnv1a64(dacTrung);
            var chieu = (int)(hash % (ulong)SoChieu);

            // Bit dau quyet dinh dau (+/-) giup giam va cham cung dau.
            var dau = (hash >> 63 & 1UL) == 1UL ? -1f : 1f;
            vector[chieu] += dau * trongSo;
        }

        foreach (var t in tu)
        {
            Cong(t, 1f);
        }

        // Bi-gram giup giu mot phan thong tin thu tu tu -> nhay hon voi cach dien dat.
        for (var i = 0; i + 1 < tu.Length; i++)
        {
            Cong($"{tu[i]}_{tu[i + 1]}", 0.5f);
        }

        // Chuan hoa L2 de cosine chi phu thuoc huong, khong phu thuoc do dai van ban.
        var chuan = (float)Math.Sqrt(vector.Sum(v => (double)v * v));
        if (chuan > 0f)
        {
            for (var i = 0; i < vector.Length; i++)
            {
                vector[i] /= chuan;
            }
        }

        return vector;
    }
}
