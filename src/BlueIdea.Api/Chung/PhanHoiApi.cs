using BlueIdea.Shared.KetQua;

namespace BlueIdea.Api.Chung;

/// <summary>
/// Cau truc phan hoi chuan cua toan he thong (Muc 8 dac ta):
/// { thanhCong, duLieu, thongBao, maLoi, chiTietLoi }
/// </summary>
public sealed class PhanHoiApi<T>
{
    public bool ThanhCong { get; init; }

    public T? DuLieu { get; init; }

    public string ThongBao { get; init; } = string.Empty;

    public string? MaLoi { get; init; }

    public IReadOnlyList<ChiTietLoiApi> ChiTietLoi { get; init; } = Array.Empty<ChiTietLoiApi>();

    public static PhanHoiApi<T> Ok(T duLieu, string thongBao = "")
        => new() { ThanhCong = true, DuLieu = duLieu, ThongBao = thongBao };

    public static PhanHoiApi<T> Loi(
        string maLoi, string thongBao, IReadOnlyList<ChiTietLoiApi>? chiTiet = null)
        => new()
        {
            ThanhCong = false,
            MaLoi = maLoi,
            ThongBao = thongBao,
            ChiTietLoi = chiTiet ?? Array.Empty<ChiTietLoiApi>()
        };
}

/// <summary>Phan hoi khong kem du lieu.</summary>
public sealed class PhanHoiApi
{
    public bool ThanhCong { get; init; }

    public string ThongBao { get; init; } = string.Empty;

    public string? MaLoi { get; init; }

    public IReadOnlyList<ChiTietLoiApi> ChiTietLoi { get; init; } = Array.Empty<ChiTietLoiApi>();

    public static PhanHoiApi Ok(string thongBao = "Thành công")
        => new() { ThanhCong = true, ThongBao = thongBao };

    public static PhanHoiApi Loi(
        string maLoi, string thongBao, IReadOnlyList<ChiTietLoiApi>? chiTiet = null)
        => new()
        {
            ThanhCong = false,
            MaLoi = maLoi,
            ThongBao = thongBao,
            ChiTietLoi = chiTiet ?? Array.Empty<ChiTietLoiApi>()
        };
}

public sealed record ChiTietLoiApi(string Truong, string ThongBao);

/// <summary>Phan hoi phan trang chuan: { duLieu, tongSo, trang, soDong, tongTrang }.</summary>
public sealed class PhanHoiPhanTrang<T>
{
    public required IReadOnlyList<T> DuLieu { get; init; }

    public required int TongSo { get; init; }

    public required int Trang { get; init; }

    public required int SoDong { get; init; }

    public int TongTrang => SoDong == 0 ? 0 : (int)Math.Ceiling(TongSo / (double)SoDong);

    public static PhanHoiPhanTrang<T> Tu(PagedResult<T> nguon) => new()
    {
        DuLieu = nguon.DuLieu,
        TongSo = nguon.TongSo,
        Trang = nguon.Trang,
        SoDong = nguon.SoDong
    };
}
