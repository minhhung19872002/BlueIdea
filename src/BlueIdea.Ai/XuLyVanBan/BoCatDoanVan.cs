using BlueIdea.Ai.ThuatToan;
using BlueIdea.Shared.TiengViet;

namespace BlueIdea.Ai.XuLyVanBan;

/// <summary>Mot doan van sau khi cat, kem vi tri trong van ban goc de highlight tren UI.</summary>
public sealed record DoanVanCat(
    int ChiMuc,
    string NoiDung,
    string NoiDungChuanHoa,
    int SoTu,
    int ViTriBatDau,
    int ViTriKetThuc,
    long SimHash);

/// <summary>Tham so cat doan van (cua so truot).</summary>
public sealed class ThamSoCatDoan
{
    /// <summary>So tu moi doan (mac dinh 200 theo dac ta Muc 5 - chuc nang 26).</summary>
    public int SoTuMoiDoan { get; init; } = 200;

    /// <summary>So tu chong lan giua 2 doan lien tiep (mac dinh 50).</summary>
    public int SoTuChongLan { get; init; } = 50;

    /// <summary>Doan ngan hon nguong nay se bi bo qua (nhieu, khong du thong tin).</summary>
    public int SoTuToiThieu { get; init; } = 20;

    public static ThamSoCatDoan MacDinh => new();
}

/// <summary>
/// Cat van ban thanh cac doan bang cua so truot theo tu, co chong lan.
/// Giu lai vi tri ky tu trong van ban goc de UI doi chieu 2 cot highlight dung vi tri.
/// </summary>
public static class BoCatDoanVan
{
    public static IReadOnlyList<DoanVanCat> Cat(string? vanBan, ThamSoCatDoan? thamSo = null)
    {
        thamSo ??= ThamSoCatDoan.MacDinh;

        if (thamSo.SoTuMoiDoan <= 0)
        {
            throw new ArgumentException("Số từ mỗi đoạn phải lớn hơn 0.", nameof(thamSo));
        }

        if (thamSo.SoTuChongLan >= thamSo.SoTuMoiDoan)
        {
            throw new ArgumentException("Số từ chồng lấn phải nhỏ hơn số từ mỗi đoạn.", nameof(thamSo));
        }

        var chuanHoaNfc = VanBanTiengViet.ChuanHoaNfc(vanBan);
        if (string.IsNullOrWhiteSpace(chuanHoaNfc))
        {
            return Array.Empty<DoanVanCat>();
        }

        var tu = TachTuKemViTri(chuanHoaNfc);
        if (tu.Count == 0)
        {
            return Array.Empty<DoanVanCat>();
        }

        var buoc = thamSo.SoTuMoiDoan - thamSo.SoTuChongLan;
        var ketQua = new List<DoanVanCat>();
        var chiMuc = 0;

        for (var batDau = 0; batDau < tu.Count; batDau += buoc)
        {
            var soLay = Math.Min(thamSo.SoTuMoiDoan, tu.Count - batDau);

            // Doan cuoi qua ngan thi gop vao doan truoc thay vi tao doan rieng.
            if (soLay < thamSo.SoTuToiThieu && ketQua.Count > 0)
            {
                break;
            }

            var lat = tu.GetRange(batDau, soLay);
            var viTriDau = lat[0].ViTri;
            var viTriCuoi = lat[^1].ViTri + lat[^1].Tu.Length;
            var noiDung = chuanHoaNfc[viTriDau..viTriCuoi];

            var tuChuanHoa = VanBanTiengViet.TachTuBoStopword(noiDung);
            var noiDungChuanHoa = string.Join(' ', tuChuanHoa);

            ketQua.Add(new DoanVanCat(
                chiMuc++,
                noiDung,
                noiDungChuanHoa,
                soLay,
                viTriDau,
                viTriCuoi,
                SimHash.TinhTuDanhSachTu(tuChuanHoa)));

            if (batDau + soLay >= tu.Count)
            {
                break;
            }
        }

        return ketQua;
    }

    private readonly record struct TuKemViTri(string Tu, int ViTri);

    private static List<TuKemViTri> TachTuKemViTri(string vanBan)
    {
        var ketQua = new List<TuKemViTri>();
        var i = 0;

        while (i < vanBan.Length)
        {
            while (i < vanBan.Length && char.IsWhiteSpace(vanBan[i]))
            {
                i++;
            }

            if (i >= vanBan.Length)
            {
                break;
            }

            var batDau = i;
            while (i < vanBan.Length && !char.IsWhiteSpace(vanBan[i]))
            {
                i++;
            }

            ketQua.Add(new TuKemViTri(vanBan[batDau..i], batDau));
        }

        return ketQua;
    }
}
