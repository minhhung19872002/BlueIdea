using BlueIdea.Domain.Chung;

namespace BlueIdea.Domain.QuanTri;

/// <summary>Chuc nang 43 - Tai khoan nguoi dung.</summary>
public class NguoiDung : ThucThe
{
    public string TenDangNhap { get; set; } = string.Empty;

    public string? MatKhauHash { get; set; }

    public string? MatKhauSalt { get; set; }

    public string HoTen { get; set; } = string.Empty;

    public string HoTenKhongDau { get; set; } = string.Empty;

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    /// <summary>Ma hoa o tang ung dung (du lieu ca nhan nhay cam).</summary>
    public string? SoCccd { get; set; }

    public string? Email { get; set; }

    public string? DienThoai { get; set; }

    public Guid? DonViId { get; set; }

    public string? ChucVu { get; set; }

    public Guid? AnhDaiDienId { get; set; }

    /// <summary>NOI_BO | SSO</summary>
    public string LoaiTaiKhoan { get; set; } = "NOI_BO";

    public string? SsoSubjectId { get; set; }

    public string? SsoProvider { get; set; }

    /// <summary>HOAT_DONG | KHOA | CHO_KICH_HOAT</summary>
    public string TrangThaiTaiKhoan { get; set; } = TrangThaiNguoiDung.ChoKichHoat;

    public bool BuocDoiMatKhau { get; set; }

    public int SoLanDangNhapSai { get; set; }

    public DateTimeOffset? KhoaDen { get; set; }

    public DateTimeOffset? LanDangNhapCuoi { get; set; }

    public DateTimeOffset? NgayDoiMatKhauCuoi { get; set; }

    public bool MfaEnabled { get; set; }

    public string? MfaSecret { get; set; }

    public List<NguoiDungVaiTro> VaiTro { get; set; } = new List<NguoiDungVaiTro>();

    /// <summary>Tai khoan dang bi khoa tam thoi do dang nhap sai nhieu lan.</summary>
    public bool DangBiKhoaTam(DateTimeOffset thoiDiem) => KhoaDen.HasValue && KhoaDen.Value > thoiDiem;

    public bool ChoPhepDangNhap(DateTimeOffset thoiDiem)
        => TrangThaiTaiKhoan == TrangThaiNguoiDung.HoatDong && !DangBiKhoaTam(thoiDiem);
}

public static class TrangThaiNguoiDung
{
    public const string HoatDong = "HOAT_DONG";
    public const string Khoa = "KHOA";
    public const string ChoKichHoat = "CHO_KICH_HOAT";
}

public class VaiTro : ThucTheDanhMuc
{
    /// <summary>Vai tro he thong khong cho xoa.</summary>
    public bool LaHeThong { get; set; }

    public List<VaiTroQuyen> DanhSachQuyen { get; set; } = new List<VaiTroQuyen>();

    public List<PhamViDuLieu> PhamViDuLieu { get; set; } = new List<PhamViDuLieu>();
}

public class Quyen : ThucThe
{
    /// <summary>Vi du SANG_KIEN.XEM</summary>
    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string NhomChucNang { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public int ThuTu { get; set; }
}

public class VaiTroQuyen : ThucThe
{
    public Guid VaiTroId { get; set; }

    public VaiTro? VaiTro { get; set; }

    public Guid QuyenId { get; set; }

    public Quyen? Quyen { get; set; }
}

public class NguoiDungVaiTro : ThucThe
{
    public Guid NguoiDungId { get; set; }

    public NguoiDung? NguoiDung { get; set; }

    public Guid VaiTroId { get; set; }

    public VaiTro? VaiTro { get; set; }

    /// <summary>Pham vi don vi ap dung cua vai tro nay (null = theo don vi cua nguoi dung).</summary>
    public Guid? DonViId { get; set; }

    public DateOnly? TuNgay { get; set; }

    public DateOnly? DenNgay { get; set; }

    public bool DangHieuLuc(DateOnly ngay)
        => (TuNgay is null || TuNgay <= ngay) && (DenNgay is null || DenNgay >= ngay);
}

/// <summary>Pham vi du lieu ma vai tro duoc phep truy cap.</summary>
public class PhamViDuLieu : ThucThe
{
    public Guid VaiTroId { get; set; }

    public VaiTro? VaiTro { get; set; }

    /// <summary>TOAN_HE_THONG | DON_VI | DON_VI_VA_CAP_DUOI | CA_NHAN | TUY_CHINH</summary>
    public string LoaiPhamVi { get; set; } = LoaiPhamViDuLieu.CaNhan;

    public List<Guid> DonViIds { get; set; } = new();
}

public static class LoaiPhamViDuLieu
{
    public const string ToanHeThong = "TOAN_HE_THONG";
    public const string DonVi = "DON_VI";
    public const string DonViVaCapDuoi = "DON_VI_VA_CAP_DUOI";
    public const string CaNhan = "CA_NHAN";
    public const string TuyChinh = "TUY_CHINH";
}

/// <summary>Refresh token co xoay vong va thu hoi duoc.</summary>
public class RefreshToken : ThucThe
{
    public Guid NguoiDungId { get; set; }

    public string TokenHash { get; set; } = string.Empty;

    public DateTimeOffset HetHan { get; set; }

    public DateTimeOffset? ThoiGianThuHoi { get; set; }

    public string? ThayTheBoiTokenHash { get; set; }

    public string? DiaChiIp { get; set; }

    public string? UserAgent { get; set; }

    public bool ConHieuLuc(DateTimeOffset thoiDiem)
        => ThoiGianThuHoi is null && HetHan > thoiDiem;
}

/// <summary>Lich su mat khau - chan dat lai trung N mat khau gan nhat.</summary>
public class LichSuMatKhau : ThucThe
{
    public Guid NguoiDungId { get; set; }

    public string MatKhauHash { get; set; } = string.Empty;

    public string? MatKhauSalt { get; set; }

    public DateTimeOffset ThoiGian { get; set; } = DateTimeOffset.UtcNow;
}
