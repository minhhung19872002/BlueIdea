namespace BlueIdea.Shared.KetQua;

/// <summary>
/// Ket qua phan trang chuan cua he thong (Muc 8 dac ta):
/// { duLieu, tongSo, trang, soDong, tongTrang }
/// </summary>
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> duLieu, int tongSo, int trang, int soDong)
    {
        DuLieu = duLieu;
        TongSo = tongSo;
        Trang = trang <= 0 ? 1 : trang;
        SoDong = soDong <= 0 ? ThamSoPhanTrang.SoDongMacDinh : soDong;
    }

    public IReadOnlyList<T> DuLieu { get; }

    public int TongSo { get; }

    public int Trang { get; }

    public int SoDong { get; }

    public int TongTrang => SoDong == 0 ? 0 : (int)Math.Ceiling(TongSo / (double)SoDong);

    public bool CoTrangTruoc => Trang > 1;

    public bool CoTrangSau => Trang < TongTrang;

    public static PagedResult<T> Rong(int trang = 1, int soDong = ThamSoPhanTrang.SoDongMacDinh)
        => new(Array.Empty<T>(), 0, trang, soDong);

    public PagedResult<TKhac> ChuyenDoi<TKhac>(Func<T, TKhac> anhXa)
        => new(DuLieu.Select(anhXa).ToList(), TongSo, Trang, SoDong);
}

/// <summary>
/// Tham so phan trang chuan: ?trang=1&amp;soDong=20&amp;sapXep=ngayTao&amp;huong=desc
/// </summary>
public class ThamSoPhanTrang
{
    public const int SoDongMacDinh = 20;
    public const int SoDongToiDa = 200;

    private int _trang = 1;
    private int _soDong = SoDongMacDinh;

    public int Trang
    {
        get => _trang;
        set => _trang = value <= 0 ? 1 : value;
    }

    public int SoDong
    {
        get => _soDong;
        set => _soDong = value switch
        {
            <= 0 => SoDongMacDinh,
            > SoDongToiDa => SoDongToiDa,
            _ => value
        };
    }

    /// <summary>Ten truong sap xep (camelCase theo hop dong API).</summary>
    public string? SapXep { get; set; }

    /// <summary>asc | desc</summary>
    public string? Huong { get; set; }

    public bool GiamDan => string.Equals(Huong, "desc", StringComparison.OrdinalIgnoreCase);

    public int BoQua => (Trang - 1) * SoDong;
}
