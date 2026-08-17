using BlueIdea.Domain.Chung;

namespace BlueIdea.Domain.Ai;

/// <summary>
/// Doan van (chunk) duoc cat tu noi dung sang kien / tep dinh kem,
/// phuc vu so khop trung lap bang SimHash + embedding.
/// </summary>
public class SangKienDoanVan : ThucThe
{
    public Guid SangKienId { get; set; }

    /// <summary>NOI_DUNG | TEP_DINH_KEM</summary>
    public string Nguon { get; set; } = "NOI_DUNG";

    public Guid? TepTinId { get; set; }

    public int ChiMuc { get; set; }

    public string NoiDung { get; set; } = string.Empty;

    /// <summary>Noi dung da bo dau, chu thuong, bo stopword - dung de tinh simhash/shingle.</summary>
    public string NoiDungChuanHoa { get; set; } = string.Empty;

    public int SoTu { get; set; }

    /// <summary>SimHash 64-bit luu duoi dang long (bigint).</summary>
    public long SimHash { get; set; }

    /// <summary>Vector embedding 768 chieu (pgvector). Null khi mo hinh chua chay.</summary>
    public float[]? Embedding { get; set; }
}

/// <summary>Chuc nang 26 - Mot lan chay kiem tra trung lap cho mot ho so.</summary>
public class KiemTraTrungLap : ThucThe
{
    public Guid SangKienId { get; set; }

    public DateTimeOffset NgayChay { get; set; } = DateTimeOffset.UtcNow;

    public string PhienBanThuatToan { get; set; } = "1.0";

    /// <summary>Pham vi doi chieu: nam, linh vuc, don vi.</summary>
    public Dictionary<string, object>? PhamVi { get; set; }

    public int TongSoDoiChieu { get; set; }

    public decimal TyLeCaoNhat { get; set; }

    /// <summary>AN_TOAN | CANH_BAO | NGHIEM_TRONG</summary>
    public string MucCanhBao { get; set; } = MucCanhBaoTrungLap.AnToan;

    /// <summary>DANG_CHAY | HOAN_THANH | LOI</summary>
    public string TrangThaiChay { get; set; } = "DANG_CHAY";

    public int ThoiGianXuLyMs { get; set; }

    public string? ThongBaoLoi { get; set; }

    public bool DaXemXet { get; set; }

    public string? YKienHoiDong { get; set; }

    public List<KiemTraTrungLapChiTiet> ChiTiet { get; set; } = new List<KiemTraTrungLapChiTiet>();
}

public static class TrangThaiKiemTraTrungLap
{
    public const string ChuaKiemTra = "CHUA_KIEM_TRA";
    public const string DangChay = "DANG_CHAY";
    public const string HoanThanh = "HOAN_THANH";
    public const string Loi = "LOI";
}

/// <summary>Chi tiet doi chieu voi mot sang kien khac.</summary>
public class KiemTraTrungLapChiTiet : ThucThe
{
    public Guid KiemTraId { get; set; }

    public KiemTraTrungLap? KiemTra { get; set; }

    public Guid SangKienDoiChieuId { get; set; }

    public decimal TyLeTuongDong { get; set; }

    public decimal TyLeTuVung { get; set; }

    public decimal TyLeNguNghia { get; set; }

    public int SoDoanTrung { get; set; }

    public List<DoanTrungLap> CacDoanTrung { get; set; } = new();
}

/// <summary>Mot cap doan van trung nhau (dung de highlight doi chieu 2 cot tren UI).</summary>
public class DoanTrungLap
{
    public string DoanNguon { get; set; } = string.Empty;

    public string DoanDich { get; set; } = string.Empty;

    public decimal TyLe { get; set; }

    public int ViTriBatDau { get; set; }

    public int ViTriKetThuc { get; set; }
}
