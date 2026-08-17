using BlueIdea.Domain.Chung;

namespace BlueIdea.Domain.QuanTri;

/// <summary>Chuc nang 46, 51 - Cau hinh he thong dang key-value.</summary>
public class CauHinhHeThong : ThucThe
{
    public string Nhom { get; set; } = "CHUNG";

    public string Khoa { get; set; } = string.Empty;

    public string? GiaTri { get; set; }

    public Dictionary<string, object>? GiaTriJson { get; set; }

    /// <summary>TEXT | NUMBER | BOOLEAN | JSON | COLOR | FILE</summary>
    public string KieuDuLieu { get; set; } = "TEXT";

    public string TenHienThi { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public bool ChoPhepSua { get; set; } = true;

    public int ThuTu { get; set; }
}

/// <summary>Chuc nang 48 - Cau hinh menu dong theo quyen, tach rieng Web/Mobile.</summary>
public class CauHinhMenu : ThucThe
{
    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string? Icon { get; set; }

    public string? DuongDan { get; set; }

    public Guid? MenuChaId { get; set; }

    public CauHinhMenu? MenuCha { get; set; }

    public List<CauHinhMenu> MenuCon { get; set; } = new List<CauHinhMenu>();

    public int ThuTu { get; set; }

    /// <summary>Ma quyen can co de nhin thay menu (null = ai cung thay).</summary>
    public string? QuyenMa { get; set; }

    /// <summary>WEB | MOBILE</summary>
    public string Loai { get; set; } = "WEB";

    public bool HienThi { get; set; } = true;

    public bool MoTabMoi { get; set; }
}

/// <summary>Chuc nang 50 - Cau hinh may chu email / nha cung cap SMS.</summary>
public class CauHinhEmailSms : ThucThe
{
    /// <summary>EMAIL | SMS</summary>
    public string Loai { get; set; } = "EMAIL";

    public string? NhaCungCap { get; set; }

    public string? Host { get; set; }

    public int? Port { get; set; }

    public string? TenDangNhap { get; set; }

    /// <summary>Ma hoa AES-256-GCM truoc khi luu.</summary>
    public string? MatKhauMaHoa { get; set; }

    public bool SuDungSsl { get; set; } = true;

    public string? EmailGuiDi { get; set; }

    public string? TenHienThi { get; set; }

    public string? ApiEndpoint { get; set; }

    public string? ApiKeyMaHoa { get; set; }

    public string? Brandname { get; set; }

    public short TrangThai { get; set; } = TrangThaiDanhMuc.HoatDong;

    public bool LaMacDinh { get; set; }
}

/// <summary>Mau thong bao theo su kien, noi dung dung placeholder Scriban.</summary>
public class MauThongBao : ThucTheDanhMuc
{
    /// <summary>EMAIL | SMS | APP | TAT_CA</summary>
    public string Kenh { get; set; } = "TAT_CA";

    /// <summary>HO_SO_DUOC_TIEP_NHAN | YEU_CAU_BO_SUNG | ...</summary>
    public string SuKien { get; set; } = string.Empty;

    public string TieuDe { get; set; } = string.Empty;

    public string NoiDung { get; set; } = string.Empty;

    public List<string> DanhSachBien { get; set; } = new();
}

public static class SuKienThongBao
{
    public const string HoSoDuocTiepNhan = "HO_SO_DUOC_TIEP_NHAN";
    public const string YeuCauBoSung = "YEU_CAU_BO_SUNG";
    public const string DuocPhanCongCham = "DUOC_PHAN_CONG_CHAM";
    public const string SapHetHan = "SAP_HET_HAN";
    public const string CoKetQua = "CO_KET_QUA";
    public const string DaPheDuyet = "DA_PHE_DUYET";
    public const string HoSoBiTuChoi = "HO_SO_BI_TU_CHOI";
    public const string MoiHopHoiDong = "MOI_HOP_HOI_DONG";

    public static readonly IReadOnlyList<string> TatCa = new[]
    {
        HoSoDuocTiepNhan, YeuCauBoSung, DuocPhanCongCham, SapHetHan,
        CoKetQua, DaPheDuyet, HoSoBiTuChoi, MoiHopHoiDong
    };
}

/// <summary>Thong bao trong ung dung (chuong thong bao).</summary>
public class ThongBao : ThucThe
{
    public Guid NguoiNhanId { get; set; }

    public string TieuDe { get; set; } = string.Empty;

    public string NoiDung { get; set; } = string.Empty;

    public string? LoaiSuKien { get; set; }

    public string? DoiTuongLienQuan { get; set; }

    public Guid? DoiTuongId { get; set; }

    public string? DuongDan { get; set; }

    /// <summary>THAP | BINH_THUONG | CAO | KHAN</summary>
    public string MucDo { get; set; } = "BINH_THUONG";

    public bool DaDoc { get; set; }

    public DateTimeOffset? NgayDoc { get; set; }

    public DateTimeOffset ThoiGian { get; set; } = DateTimeOffset.UtcNow;
}

/// <summary>Hang doi gui email/SMS - Hangfire xu ly va retry.</summary>
public class HangDoiGuiTin : ThucThe
{
    public string Kenh { get; set; } = "EMAIL";

    public string NguoiNhan { get; set; } = string.Empty;

    public string? TieuDe { get; set; }

    public string NoiDung { get; set; } = string.Empty;

    public int SoLanThu { get; set; }

    /// <summary>CHO_GUI | DANG_GUI | DA_GUI | LOI</summary>
    public string TrangThaiGui { get; set; } = "CHO_GUI";

    public string? ThongBaoLoi { get; set; }

    public DateTimeOffset? ThoiGianGui { get; set; }
}

/// <summary>Chuc nang 49 - Cau hinh tich hop chu ky so.</summary>
public class CauHinhChuKySo : ThucThe
{
    /// <summary>BAN_CO_YEU_CHINH_PHU | VNPT_CA | VIETTEL_CA | FPT_CA | MISA | KHAC</summary>
    public string NhaCungCap { get; set; } = "KHAC";

    /// <summary>USB_TOKEN | HSM | REMOTE_SIGNING | SMART_CA</summary>
    public string LoaiKy { get; set; } = "REMOTE_SIGNING";

    public string? Endpoint { get; set; }

    public string? ClientId { get; set; }

    public string? ClientSecretMaHoa { get; set; }

    public string? ChungThuSo { get; set; }

    public string ThuatToan { get; set; } = "SHA256withRSA";

    public string? TichHopPluginUrl { get; set; }

    public short TrangThai { get; set; } = TrangThaiDanhMuc.HoatDong;

    public bool LaMacDinh { get; set; }
}

public class NhatKyKySo : ThucThe
{
    public string DoiTuong { get; set; } = string.Empty;

    public Guid DoiTuongId { get; set; }

    public Guid? NguoiKyId { get; set; }

    public DateTimeOffset ThoiGianKy { get; set; } = DateTimeOffset.UtcNow;

    public string? SerialChungThu { get; set; }

    public string? NguoiCapChungThu { get; set; }

    public DateTimeOffset? HieuLucTu { get; set; }

    public DateTimeOffset? HieuLucDen { get; set; }

    public Guid? TepGocId { get; set; }

    public Guid? TepDaKyId { get; set; }

    public string TrangThaiKy { get; set; } = "THANH_CONG";

    public Dictionary<string, object>? ThongTinXacThuc { get; set; }
}

/// <summary>Chuc nang 41 - He thong ngoai duoc tich hop.</summary>
public class HeThongTichHop : ThucTheDanhMuc
{
    public string? EndpointBase { get; set; }

    /// <summary>OAUTH2 | OIDC | API_KEY | CHUNG_THU_SO | HMAC</summary>
    public string LoaiXacThuc { get; set; } = "API_KEY";

    public string? ClientId { get; set; }

    public string? ClientSecretMaHoa { get; set; }

    public string? Scope { get; set; }

    public Dictionary<string, string>? CauHinhMapping { get; set; }

    /// <summary>THU_CONG | MOI_GIO | HANG_NGAY | HANG_TUAN</summary>
    public string TanSuatDongBo { get; set; } = "THU_CONG";

    public DateTimeOffset? LanDongBoCuoi { get; set; }
}

public static class MaHeThongTichHop
{
    public const string SsoThanhPho = "SSO_THANH_PHO";
    public const string Ioc = "IOC";
    public const string ThiDuaKhenThuong = "THI_DUA_KHEN_THUONG";
}

public class NhatKyDongBo : ThucThe
{
    public Guid HeThongTichHopId { get; set; }

    /// <summary>GUI | NHAN</summary>
    public string Chieu { get; set; } = "GUI";

    public string? LoaiDuLieu { get; set; }

    public int TongBanGhi { get; set; }

    public int ThanhCong { get; set; }

    public int ThatBai { get; set; }

    public string? DuLieuGui { get; set; }

    public string? PhanHoi { get; set; }

    public string TrangThaiDongBo { get; set; } = "DANG_CHAY";

    public string? ThongBaoLoi { get; set; }

    public DateTimeOffset ThoiGianBatDau { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? ThoiGianKetThuc { get; set; }
}

/// <summary>
/// Chuc nang 41 — Khoa API cap cho he thong ngoai GOI VAO BlueIdea.
///
/// Khac han <see cref="HeThongTichHop"/>: ban ghi do la thong tin de BlueIdea goi ra ngoai,
/// ban ghi nay la thong tin de ben ngoai goi vao. Hai chieu co vong doi va rui ro khac nhau
/// nen tach bang — gop lai thi mot lan sua cau hinh chieu ra co the vo tinh mo chieu vao.
/// </summary>
public class KhoaApiNgoai : ThucThe
{
    public string Ten { get; set; } = string.Empty;

    /// <summary>
    /// Tam ky tu dau cua khoa, luu RO de tra cuu duoc ban ghi ma khong phai bam thu tung dong.
    /// </summary>
    public string TienTo { get; set; } = string.Empty;

    /// <summary>Bam SHA-256 cua khoa day du. Khoa goc chi hien duy nhat mot lan luc cap.</summary>
    public string KhoaBam { get; set; } = string.Empty;

    /// <summary>
    /// Danh sach IP hoac dai CIDR duoc phep goi. De rong nghia la KHONG chan theo IP —
    /// phai la lua chon co y thuc cua quan tri vien chu khong phai mac dinh.
    /// </summary>
    public List<string> DanhSachIp { get; set; } = new();

    public bool DangHoatDong { get; set; } = true;

    public DateTimeOffset? NgayHetHan { get; set; }

    public DateTimeOffset? LanGoiCuoi { get; set; }

    public long SoLanGoi { get; set; }

    public string? GhiChu { get; set; }

    public bool ConHieuLuc(DateTimeOffset thoiDiem)
        => DangHoatDong && (NgayHetHan is null || NgayHetHan > thoiDiem);
}
