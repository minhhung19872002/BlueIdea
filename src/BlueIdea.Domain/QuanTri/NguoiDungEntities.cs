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

    /// <summary>Bi mat TOTP dang Base32, MA HOA o tang ung dung truoc khi luu.</summary>
    public string? MfaSecret { get; set; }

    /// <summary>
    /// Buoc thoi gian cua ma TOTP da dung gan nhat.
    ///
    /// Mot ma TOTP con hieu luc trong ca cua so 90 giay (buoc hien tai ±1). Neu khong ghi lai
    /// buoc da dung thi ke nhin trom man hinh dien thoai van kip dung lai chinh ma do de dang
    /// nhap phien thu hai. Ghi lai roi tu choi buoc <= gia tri nay chinh la chong dung lai ma.
    /// </summary>
    public long? MfaBuocDaDung { get; set; }

    /// <summary>
    /// Bam cua cac ma khoi phuc dung mot lan, luu dang JSON mang chuoi.
    ///
    /// Luu BAM chu khong luu ma goc: mat CSDL thi ma khoi phuc cung la chia khoa vuot qua MFA.
    /// </summary>
    public string? MfaMaKhoiPhuc { get; set; }

    public DateTimeOffset? MfaNgayBat { get; set; }

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

/// <summary>
/// Chuc nang 28 — Bo loc nguoi dung tu luu lai tren mot man hinh danh sach.
///
/// Tham so luu dang JSON tu do chu khong tach thanh cot: moi man hinh co bo tieu chi loc rieng
/// va danh sach do con thay doi theo cau hinh, tach cot thi them mot bo loc la phai di chuyen
/// CSDL.
/// </summary>
public class BoLocYeuThich : ThucThe
{
    public Guid NguoiDungId { get; set; }

    /// <summary>Ma man hinh ap dung, vi du SANG_KIEN, TIEP_NHAN, TRA_CUU.</summary>
    public string ManHinh { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    /// <summary>Tham so loc dang JSON (jsonb).</summary>
    public string ThamSo { get; set; } = "{}";

    /// <summary>Bo loc tu dong ap dung khi mo man hinh.</summary>
    public bool MacDinh { get; set; }
}

/// <summary>
/// Chuc nang 21 — Ma xac thuc song ngan: OTP dat lai mat khau va loi giai CAPTCHA.
///
/// Luu trong CSDL chu KHONG luu trong bo nho tien trinh: he thong trien khai nhieu ban sau
/// can bang tai, ma sinh o ban nay ma nguoi dung gui len ban kia se khong tim thay.
///
/// Hai loai dung chung mot bang vi vong doi giong het nhau (song ngan, dung mot lan, can don
/// dep dinh ky) — tach hai bang chi de phai viet hai cong viec don dep y het nhau.
/// </summary>
public class MaXacThucTam : ThucThe
{
    /// <summary>OTP_DAT_LAI_MAT_KHAU | CAPTCHA</summary>
    public string Loai { get; set; } = string.Empty;

    /// <summary>Khoa tra cuu: ten dang nhap voi OTP, dinh danh phien voi CAPTCHA.</summary>
    public string Khoa { get; set; } = string.Empty;

    /// <summary>Bam SHA-256 cua ma — khong luu ma goc de dump CSDL khong doc duoc.</summary>
    public string MaBam { get; set; } = string.Empty;

    public DateTimeOffset HetHan { get; set; }

    public int SoLanThuSai { get; set; }

    public bool DaDung { get; set; }

    public string? DiaChiIp { get; set; }

    public bool ConHieuLuc(DateTimeOffset thoiDiem) => !DaDung && HetHan > thoiDiem;
}

public static class LoaiMaXacThucTam
{
    public const string OtpDatLaiMatKhau = "OTP_DAT_LAI_MAT_KHAU";
    public const string Captcha = "CAPTCHA";
}
