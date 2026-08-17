using BlueIdea.Domain.Chung;

namespace BlueIdea.Domain.HoiDong;

/// <summary>Chuc nang 19 - Hoi dong sang kien.</summary>
public class HoiDongSangKien : ThucTheDanhMuc
{
    public string Cap { get; set; } = CapXetDuyet.CoSo;

    public Guid? DotDeNghiId { get; set; }

    public Guid? DonViId { get; set; }

    public string? SoQuyetDinhThanhLap { get; set; }

    public DateOnly? NgayQuyetDinh { get; set; }

    public Guid? TepQuyetDinhId { get; set; }

    public DateOnly? ThoiGianHoatDongTu { get; set; }

    public DateOnly? ThoiGianHoatDongDen { get; set; }

    public List<Guid> LinhVucPhuTrach { get; set; } = new();

    public int SoThanhVienToiThieu { get; set; } = 5;

    public decimal TyLeThongQua { get; set; } = 50m;

    /// <summary>DANG_HOAT_DONG | DA_KET_THUC</summary>
    public string TrangThaiHoatDong { get; set; } = "DANG_HOAT_DONG";

    public List<HoiDongThanhVien> ThanhVien { get; set; } = new List<HoiDongThanhVien>();

    public List<PhienHopHoiDong> PhienHop { get; set; } = new List<PhienHopHoiDong>();
}

/// <summary>Chuc nang 20 - Thanh vien hoi dong va quyen han theo chuc danh.</summary>
public class HoiDongThanhVien : ThucThe
{
    public Guid HoiDongId { get; set; }

    public HoiDongSangKien? HoiDong { get; set; }

    public Guid? NguoiDungId { get; set; }

    public string HoTenHienThi { get; set; } = string.Empty;

    public string? ChucVuCongTac { get; set; }

    public string? DonViCongTac { get; set; }

    /// <summary>CHU_TICH | PHO_CHU_TICH | UY_VIEN | UY_VIEN_THU_KY | THU_KY</summary>
    public string ChucDanh { get; set; } = Chung.ChucDanhHoiDong.UyVien;

    public bool QuyenChamDiem { get; set; } = true;

    public bool QuyenNhanXet { get; set; } = true;

    public bool QuyenBoPhieu { get; set; } = true;

    public bool QuyenKyBienBan { get; set; }

    public bool QuyenKetLuan { get; set; }

    public int ThuTu { get; set; }

    public short TrangThai { get; set; } = TrangThaiDanhMuc.HoatDong;
}

/// <summary>Phien hop hoi dong.</summary>
public class PhienHopHoiDong : ThucThe
{
    public Guid HoiDongId { get; set; }

    public HoiDongSangKien? HoiDong { get; set; }

    public string MaPhien { get; set; } = string.Empty;

    public string TenPhien { get; set; } = string.Empty;

    public DateTimeOffset ThoiGianBatDau { get; set; }

    public DateTimeOffset? ThoiGianKetThuc { get; set; }

    public string? DiaDiem { get; set; }

    /// <summary>TRUC_TIEP | TRUC_TUYEN | KET_HOP</summary>
    public string HinhThuc { get; set; } = "TRUC_TIEP";

    public Guid? ChuTriId { get; set; }

    public Guid? ThuKyId { get; set; }

    public string? NoiDung { get; set; }

    public string? KetLuan { get; set; }

    /// <summary>DU_KIEN | DANG_DIEN_RA | DA_KET_THUC | DA_HUY</summary>
    public string TrangThaiPhien { get; set; } = "DU_KIEN";

    public Guid? TepBienBanId { get; set; }

    public List<PhienHopHoSo> DanhSachHoSo { get; set; } = new List<PhienHopHoSo>();

    public List<PhienHopDiemDanh> DiemDanh { get; set; } = new List<PhienHopDiemDanh>();

    public List<PhieuBoPhieu> PhieuBoPhieu { get; set; } = new List<PhieuBoPhieu>();
}

public static class TrangThaiPhienHop
{
    public const string DuKien = "DU_KIEN";
    public const string DangDienRa = "DANG_DIEN_RA";
    public const string DaKetThuc = "DA_KET_THUC";
    public const string DaHuy = "DA_HUY";
}

/// <summary>Ho so duoc dua ra hop.</summary>
public class PhienHopHoSo : ThucThe
{
    public Guid PhienHopId { get; set; }

    public PhienHopHoiDong? PhienHop { get; set; }

    public Guid SangKienId { get; set; }

    public int ThuTu { get; set; }

    public string? KetLuanRieng { get; set; }

    /// <summary>DAT | KHONG_DAT | HOAN</summary>
    public string? KetQua { get; set; }
}

public class PhienHopDiemDanh : ThucThe
{
    public Guid PhienHopId { get; set; }

    public PhienHopHoiDong? PhienHop { get; set; }

    public Guid ThanhVienId { get; set; }

    public bool CoMat { get; set; }

    public string? LyDoVang { get; set; }

    public DateTimeOffset? ThoiGianDiemDanh { get; set; }
}

public class BienBanHop : ThucThe
{
    public Guid PhienHopId { get; set; }

    public PhienHopHoiDong? PhienHop { get; set; }

    public string SoBienBan { get; set; } = string.Empty;

    public Dictionary<string, object>? NoiDungJson { get; set; }

    public Guid? TepTinId { get; set; }

    /// <summary>NHAP | DA_LAP | DA_KY | DA_BAN_HANH</summary>
    public string TrangThaiBienBan { get; set; } = "NHAP";

    public DateOnly? NgayLap { get; set; }

    public List<BienBanChuKy> ChuKy { get; set; } = new List<BienBanChuKy>();
}

public class BienBanChuKy : ThucThe
{
    public Guid BienBanId { get; set; }

    public BienBanHop? BienBan { get; set; }

    public Guid ThanhVienId { get; set; }

    public bool DaKy { get; set; }

    public DateTimeOffset? ThoiGianKy { get; set; }

    public Guid? ChuKySoId { get; set; }

    public Guid? AnhChuKyId { get; set; }
}

/// <summary>Phieu bo phieu (kin hoac cong khai) cho tung ho so trong phien hop.</summary>
public class PhieuBoPhieu : ThucThe
{
    public Guid PhienHopId { get; set; }

    public PhienHopHoiDong? PhienHop { get; set; }

    public Guid SangKienId { get; set; }

    public Guid ThanhVienId { get; set; }

    /// <summary>DONG_Y | KHONG_DONG_Y | Y_KIEN_KHAC</summary>
    public string YKien { get; set; } = "DONG_Y";

    public Guid? MucDeXuatId { get; set; }

    public string? GhiChu { get; set; }

    public bool LaPhieuKin { get; set; }

    public DateTimeOffset ThoiGian { get; set; } = DateTimeOffset.UtcNow;
}

public static class YKienBoPhieu
{
    public const string DongY = "DONG_Y";
    public const string KhongDongY = "KHONG_DONG_Y";
    public const string YKienKhac = "Y_KIEN_KHAC";
}
