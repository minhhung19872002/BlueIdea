using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;

namespace BlueIdea.Workflow.MoHinh;

/// <summary>Trang thai runtime cua mot ho so trong quy trinh.</summary>
public sealed class WorkflowInstance
{
    public Guid SangKienId { get; init; }

    public Guid QuyTrinhId { get; init; }

    public int PhienBanQuyTrinh { get; init; }

    public Guid? BuocHienTaiId { get; init; }

    public string? TenBuocHienTai { get; init; }

    public Guid? TrangThaiHienTaiId { get; init; }

    public string? TenTrangThaiHienTai { get; init; }

    public DateTimeOffset? HanXuLy { get; init; }

    public bool DaKetThuc { get; init; }

    public string TrangThaiTong { get; init; } = string.Empty;

    public IReadOnlyList<SangKienXuLy> LichSu { get; init; } = Array.Empty<SangKienXuLy>();
}

/// <summary>
/// Mot hanh dong ma nguoi dung hien tai duoc phep thuc hien tren ho so.
/// Frontend render nut bam tu danh sach nay - KHONG hardcode nut.
/// </summary>
public sealed class HanhDongKhaDung
{
    public Guid TruongHopId { get; init; }

    public string Ma { get; init; } = string.Empty;

    public string Ten { get; init; } = string.Empty;

    public Guid BuocId { get; init; }

    public string TenBuoc { get; init; } = string.Empty;

    /// <summary>Kieu nut hien thi: primary | default | danger | dashed.</summary>
    public string MauNut { get; init; } = "default";

    public bool BatBuocNhapYKien { get; init; }

    public bool BatBuocDinhKem { get; init; }

    public IReadOnlyList<string> TepBatBuoc { get; init; } = Array.Empty<string>();

    /// <summary>True khi dieu kien chuyen tiep hien khong thoa (nut bi mo/disable kem ly do).</summary>
    public bool BiChan { get; init; }

    public string? LyDoChan { get; init; }

    public Guid? BuocTiepTheoId { get; init; }

    public string? TenBuocTiepTheo { get; init; }

    public IReadOnlyList<string> HanhDongTuDong { get; init; } = Array.Empty<string>();

    /// <summary>Chuc nang bo sung dang bat cho buoc nay (tu ChucNangBoSung cau hinh).</summary>
    public IReadOnlyList<string> ChucNangBuoc { get; init; } = Array.Empty<string>();
}

/// <summary>Yeu cau thuc thi mot buoc.</summary>
public sealed class XuLyBuocRequest
{
    public Guid SangKienId { get; init; }

    public Guid NguoiDungId { get; init; }

    /// <summary>Truong hop chuyen tiep duoc chon (Dat / Khong dat / Bo sung...).</summary>
    public Guid TruongHopId { get; init; }

    public string? YKien { get; init; }

    public IReadOnlyList<Guid> TepDinhKemIds { get; init; } = Array.Empty<Guid>();

    /// <summary>Nguoi duoc uy quyen xu ly thay (neu buoc cho phep uy quyen).</summary>
    public Guid? NguoiUyQuyenId { get; init; }

    /// <summary>
    /// Hanh dong nguoi xu ly chu dong chon, dua vao bien ngu canh <c>hanh_dong_nguoi_dung</c>.
    ///
    /// Danh cho nhung nhanh mo theo QUYET DINH CUA NGUOI thay vi theo du lieu — vi du quy trinh
    /// rieng cua don vi khai mot nhanh "xu ly khan" chi mo khi nguoi xu ly chon muc khan.
    /// </summary>
    public string? HanhDongNguoiDung { get; init; }

    /// <summary>Chong double-submit (dac ta yeu cau Idempotency-Key).</summary>
    public string? IdempotencyKey { get; init; }

    /// <summary>Phien ban ho so client dang giu - dung cho optimistic concurrency.</summary>
    public int? PhienBanHoSo { get; init; }

    public string? DiaChiIp { get; init; }

    public string? UserAgent { get; init; }
}

/// <summary>Ket qua sau khi thuc thi mot buoc.</summary>
public sealed class KetQuaXuLy
{
    public bool ThanhCong { get; init; }

    public string? MaLoi { get; init; }

    public string ThongBao { get; init; } = string.Empty;

    public Guid? BuocTruocId { get; init; }

    public Guid? BuocMoiId { get; init; }

    public string? TenBuocMoi { get; init; }

    public Guid? TrangThaiMoiId { get; init; }

    public string TrangThaiTongMoi { get; init; } = string.Empty;

    public DateTimeOffset? HanXuLyMoi { get; init; }

    public bool DaKetThucQuyTrinh { get; init; }

    /// <summary>Chua du so tac nhan hoan thanh (quy tac TAT_CA / DA_SO) - ho so van o buoc cu.</summary>
    public bool ChoThemTacNhan { get; init; }

    public int SoTacNhanDaXuLy { get; init; }

    public int SoTacNhanCanThiet { get; init; }

    public IReadOnlyList<string> HanhDongCanChay { get; init; } = Array.Empty<string>();

    /// <summary>Chuc nang bo sung dang bat cho buoc vua xu ly (tu ChucNangBoSung cau hinh).</summary>
    public IReadOnlyList<string> ChucNangBat { get; init; } = Array.Empty<string>();

    public static KetQuaXuLy Loi(string maLoi, string thongBao)
        => new() { ThanhCong = false, MaLoi = maLoi, ThongBao = thongBao };
}

/// <summary>Thong tin nguoi xu ly dung de doi chieu voi tac nhan cua buoc.</summary>
public sealed class NguCanhNguoiXuLy
{
    public Guid NguoiDungId { get; init; }

    public Guid? DonViId { get; init; }

    /// <summary>Danh sach ma vai tro cua nguoi dung (TAC_GIA, CAN_BO_TIEP_NHAN...).</summary>
    public IReadOnlySet<string> MaVaiTro { get; init; } = new HashSet<string>();

    public IReadOnlySet<Guid> VaiTroIds { get; init; } = new HashSet<Guid>();

    /// <summary>Cac don vi thuoc pham vi quan ly (don vi cua minh + cap duoi).</summary>
    public IReadOnlySet<Guid> DonViTrongPhamVi { get; init; } = new HashSet<Guid>();

    /// <summary>Hoi dong ma nguoi dung la thanh vien -&gt; chuc danh trong hoi dong do.</summary>
    public IReadOnlyDictionary<Guid, string> ChucDanhTheoHoiDong { get; init; }
        = new Dictionary<Guid, string>();

    public bool LaNguoiTaoHoSo { get; init; }

    public bool LaLanhDaoDonViTacGia { get; init; }
}

/// <summary>Du lieu can thiet de engine chay - do tang Infrastructure nap tu DB.</summary>
public sealed class NguCanhThucThi
{
    public required HoSoSangKien HoSo { get; init; }

    public required QuyTrinh QuyTrinh { get; init; }

    public required NguCanhNguoiXuLy NguoiXuLy { get; init; }

    public IReadOnlyList<SangKienXuLy> LichSuXuLy { get; init; } = Array.Empty<SangKienXuLy>();

    /// <summary>Cac bien bo sung dua vao bo danh gia dieu kien (diem, so phieu...).</summary>
    public IReadOnlyDictionary<string, object?> BienBoSung { get; init; }
        = new Dictionary<string, object?>();

    /// <summary>
    /// Tong so tac nhan du kien phai xu ly buoc hien tai (do Infrastructure dem tu vai tro/hoi dong).
    /// Dung cho quy tac TAT_CA / DA_SO.
    /// </summary>
    public int SoTacNhanDuKien { get; init; } = 1;

    /// <summary>Thoi diem hien tai - tach ra de test tat dinh.</summary>
    public DateTimeOffset ThoiDiem { get; init; } = DateTimeOffset.UtcNow;
}
