using BlueIdea.Domain.Chung;

namespace BlueIdea.Ai.TrungLap;

/// <summary>Mot doan van dua vao so khop trung lap.</summary>
public sealed class DoanVanSoSanh
{
    public int ChiMuc { get; init; }

    public string NoiDung { get; init; } = string.Empty;

    public string NoiDungChuanHoa { get; init; } = string.Empty;

    public long SimHash { get; init; }

    public float[]? Embedding { get; set; }

    public int ViTriBatDau { get; init; }

    public int ViTriKetThuc { get; init; }
}

/// <summary>Mot tai lieu (ho so sang kien) tham gia so khop.</summary>
public sealed class TaiLieuSoSanh
{
    public Guid SangKienId { get; init; }

    public string MaHoSo { get; init; } = string.Empty;

    public string TenSangKien { get; init; } = string.Empty;

    public int? Nam { get; init; }

    public Guid? LinhVucId { get; init; }

    public Guid? DonViId { get; init; }

    /// <summary>Toan van dung cho so khop muc tai lieu (TF-IDF, Jaccard).</summary>
    public string ToanVan { get; init; } = string.Empty;

    public IReadOnlyList<DoanVanSoSanh> CacDoan { get; init; } = Array.Empty<DoanVanSoSanh>();

    public float[]? EmbeddingTaiLieu { get; set; }
}

/// <summary>Tham so cau hinh thuat toan phat hien trung lap (doc tu cau_hinh_he_thong).</summary>
public sealed class ThamSoTrungLap
{
    /// <summary>Trong so thanh phan tu vung trong diem tong hop (mac dinh 0.4).</summary>
    public decimal HeSoTuVung { get; init; } = 0.4m;

    /// <summary>Trong so thanh phan ngu nghia trong diem tong hop (mac dinh 0.6).</summary>
    public decimal HeSoNguNghia { get; init; } = 0.6m;

    /// <summary>Nguong Hamming cho buoc loc tho bang SimHash.</summary>
    public int NguongHamming { get; init; } = ThuatToan.SimHash.NguongHammingMacDinh;

    /// <summary>Nguong tuong dong de coi 2 doan la "trung".</summary>
    public double NguongDoanTrung { get; init; } = 0.72;

    /// <summary>Nguong ty le (%) chuyen tu AN_TOAN sang CANH_BAO.</summary>
    public decimal NguongCanhBaoVang { get; init; } = 20m;

    /// <summary>Nguong ty le (%) chuyen sang NGHIEM_TRONG.</summary>
    public decimal NguongCanhBaoDo { get; init; } = 40m;

    /// <summary>So tai lieu doi chieu chi tiet toi da sau buoc loc tho.</summary>
    public int SoUngVienToiDa { get; init; } = 50;

    /// <summary>Chi luu chi tiet cac cap co ty le tu nguong nay tro len (%).</summary>
    public decimal NguongLuuChiTiet { get; init; } = 10m;

    public string PhienBanThuatToan { get; init; } = "1.0";

    public static ThamSoTrungLap MacDinh => new();
}

/// <summary>Cap doan van trung nhau.</summary>
public sealed record CapDoanTrung(
    int ChiMucNguon,
    string DoanNguon,
    int ChiMucDich,
    string DoanDich,
    decimal TyLe,
    int ViTriBatDau,
    int ViTriKetThuc);

/// <summary>Ket qua doi chieu voi MOT sang kien khac.</summary>
public sealed class ChiTietDoiChieu
{
    public Guid SangKienDoiChieuId { get; init; }

    public string MaHoSo { get; init; } = string.Empty;

    public string TenSangKien { get; init; } = string.Empty;

    public decimal TyLeTuongDong { get; init; }

    public decimal TyLeTuVung { get; init; }

    public decimal TyLeNguNghia { get; init; }

    public int SoDoanTrung { get; init; }

    public IReadOnlyList<CapDoanTrung> CacDoanTrung { get; init; } = Array.Empty<CapDoanTrung>();
}

/// <summary>Ket qua mot lan chay kiem tra trung lap.</summary>
public sealed class KetQuaPhanTichTrungLap
{
    public Guid SangKienId { get; init; }

    public int TongSoDoiChieu { get; init; }

    public decimal TyLeCaoNhat { get; init; }

    public string MucCanhBao { get; init; } = MucCanhBaoTrungLap.AnToan;

    public string PhienBanThuatToan { get; init; } = "1.0";

    public string TenMoHinhNhung { get; init; } = string.Empty;

    public long ThoiGianXuLyMs { get; init; }

    public IReadOnlyList<ChiTietDoiChieu> ChiTiet { get; init; } = Array.Empty<ChiTietDoiChieu>();

    /// <summary>Xac dinh muc canh bao theo nguong cau hinh.</summary>
    public static string TinhMucCanhBao(decimal tyLe, ThamSoTrungLap thamSo)
    {
        if (tyLe > thamSo.NguongCanhBaoDo)
        {
            return MucCanhBaoTrungLap.NghiemTrong;
        }

        return tyLe >= thamSo.NguongCanhBaoVang
            ? MucCanhBaoTrungLap.CanhBao
            : MucCanhBaoTrungLap.AnToan;
    }
}
