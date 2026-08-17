using BlueIdea.Domain.Chung;

namespace BlueIdea.Domain.DanhMuc;

/// <summary>Chuc nang 1 - Linh vuc ap dung sang kien (co phan cap cha/con).</summary>
public class LinhVuc : ThucTheDanhMuc
{
    public Guid? LinhVucChaId { get; set; }

    public LinhVuc? LinhVucCha { get; set; }

    public List<LinhVuc> LinhVucCon { get; set; } = new List<LinhVuc>();
}

/// <summary>Chuc nang 2 - Doi tuong ap dung / huong loi.</summary>
public class DoiTuong : ThucTheDanhMuc
{
}

/// <summary>Chuc nang 4 - Loai tac gia (ca nhan / nhom / don vi).</summary>
public class LoaiTacGia : ThucTheDanhMuc
{
    public bool ChoPhepNhieuTacGia { get; set; }

    public int SoTacGiaToiDa { get; set; } = 1;
}

/// <summary>Chuc nang 5, 44 - Co cau to chuc dang cay.</summary>
public class DonVi : ThucTheDanhMuc
{
    public string? TenVietTat { get; set; }

    public Guid? DonViChaId { get; set; }

    public DonVi? DonViCha { get; set; }

    public List<DonVi> DonViCon { get; set; } = new List<DonVi>();

    /// <summary>Cap trong cay, goc = 1.</summary>
    public int Cap { get; set; } = 1;

    /// <summary>DON_VI | PHONG_BAN | HOI_DONG | UBND</summary>
    public string Loai { get; set; } = LoaiDonVi.DonVi;

    /// <summary>Duong dan cay dang /goc/con/chau - phuc vu truy van cap duoi nhanh.</summary>
    public string Path { get; set; } = "/";

    public string? DiaChi { get; set; }

    public string? DienThoai { get; set; }

    public string? Email { get; set; }

    public string? NguoiDaiDien { get; set; }

    public string? ChucVuNguoiDaiDien { get; set; }

    public bool LaDonViPheDuyet { get; set; }

    public string? CapPheDuyet { get; set; }

    // Chuc nang 47 - cau hinh rieng cua don vi
    public Guid? LogoId { get; set; }

    public string? TieuDeVanBan { get; set; }

    public string? NguoiKyMacDinh { get; set; }

    public string? ChucVuNguoiKyMacDinh { get; set; }
}

public static class LoaiDonVi
{
    public const string DonVi = "DON_VI";
    public const string PhongBan = "PHONG_BAN";
    public const string HoiDong = "HOI_DONG";
    public const string Ubnd = "UBND";
}

/// <summary>Chuc nang 3 - Dot de nghi (dot tiep nhan va xet duyet).</summary>
public class DotDeNghi : ThucTheDanhMuc
{
    public int Nam { get; set; }

    /// <summary>QUY_1..QUY_4 | NAM | DOT_1...</summary>
    public string? Ky { get; set; }

    public DateOnly? TuNgay { get; set; }

    public DateOnly? DenNgay { get; set; }

    public DateTimeOffset? HanNopHoSo { get; set; }

    public DateTimeOffset? HanChamDiem { get; set; }

    /// <summary>CO_SO | THANH_PHO | TINH</summary>
    public string CapXetDuyet { get; set; } = Chung.CapXetDuyet.CoSo;

    public Guid? QuyTrinhId { get; set; }

    public Guid? BoTieuChiId { get; set; }

    /// <summary>Danh sach Id don vi ap dung (jsonb).</summary>
    public List<Guid> DonViApDungIds { get; set; } = new();

    /// <summary>NHAP | DANG_MO | DA_DONG | DA_KHOA</summary>
    public string TrangThaiDot { get; set; } = Chung.TrangThaiDot.Nhap;

    public bool TuDongKhoa { get; set; } = true;

    public string? GhiChu { get; set; }

    /// <summary>
    /// Nghiep vu: chi cho nop/sua ho so khi dot dang mo VA chua qua han nop.
    /// </summary>
    public bool ChoPhepNopHoSo(DateTimeOffset thoiDiem)
        => TrangThaiDot == Chung.TrangThaiDot.DangMo
           && (HanNopHoSo is null || thoiDiem <= HanNopHoSo.Value);
}

/// <summary>Chuc nang 5 - Cau hinh cap phe duyet theo dot / linh vuc.</summary>
public class CauHinhCapPheDuyet : ThucThe
{
    public Guid? DotDeNghiId { get; set; }

    public Guid? LinhVucId { get; set; }

    public Guid DonViPheDuyetId { get; set; }

    public DonVi? DonViPheDuyet { get; set; }

    public int ThuTuCap { get; set; }

    public string? GhiChu { get; set; }
}

/// <summary>Chuc nang 6 - Mau bieu xuat du lieu (.docx/.xlsx/.pdf).</summary>
public class BieuMauXuat : ThucTheDanhMuc
{
    /// <summary>PHIEU_TIEP_NHAN | PHIEU_DANH_GIA | BIEN_BAN_HOP | QUYET_DINH | TONG_HOP | KHAC</summary>
    public string Loai { get; set; } = LoaiBieuMau.Khac;

    /// <summary>DOCX | XLSX | PDF</summary>
    public string DinhDang { get; set; } = "DOCX";

    public Guid? FileTemplateId { get; set; }

    /// <summary>Mapping placeholder -&gt; nguon du lieu (jsonb).</summary>
    public List<CauHinhTruongBieuMau> CauHinhTruong { get; set; } = new();

    /// <summary>Pham vi ap dung (dot, linh vuc, cap...).</summary>
    public Dictionary<string, object>? PhamViApDung { get; set; }
}

public static class LoaiBieuMau
{
    public const string PhieuTiepNhan = "PHIEU_TIEP_NHAN";
    public const string PhieuDanhGia = "PHIEU_DANH_GIA";
    public const string BienBanHop = "BIEN_BAN_HOP";
    public const string QuyetDinh = "QUYET_DINH";
    public const string TongHop = "TONG_HOP";
    public const string Khac = "KHAC";
}

/// <summary>Mot dong mapping placeholder trong bieu mau.</summary>
public class CauHinhTruongBieuMau
{
    public string Placeholder { get; set; } = string.Empty;

    public string Nguon { get; set; } = string.Empty;

    /// <summary>text | number | date | table</summary>
    public string Kieu { get; set; } = "text";

    public List<string>? Cot { get; set; }

    public string? DinhDangHienThi { get; set; }
}

/// <summary>Chuc nang 7 - Mau bao cao thong ke cau hinh dong.</summary>
public class BieuMauThongKe : ThucTheDanhMuc
{
    public string? LoaiBaoCao { get; set; }

    public Dictionary<string, object>? CauHinhTieuChi { get; set; }

    public List<CotBaoCao> CauHinhCot { get; set; } = new();

    public Dictionary<string, object>? CauHinhBoLoc { get; set; }

    public List<string> DinhDangXuat { get; set; } = new() { "XLSX", "PDF" };
}

public class CotBaoCao
{
    public string Ma { get; set; } = string.Empty;

    public string TieuDe { get; set; } = string.Empty;

    public string? Nguon { get; set; }

    public string? HamTongHop { get; set; }

    public int ThuTu { get; set; }
}

/// <summary>Chuc nang 8, 31, 36 - Quyet dinh cong nhan sang kien.</summary>
public class QuyetDinh : ThucThe
{
    public string SoQuyetDinh { get; set; } = string.Empty;

    public DateOnly NgayBanHanh { get; set; }

    /// <summary>CAP_CO_SO | CAP_THANH_PHO | CAP_TINH</summary>
    public string Loai { get; set; } = "CAP_CO_SO";

    public string? TrichYeu { get; set; }

    public string? NguoiKy { get; set; }

    public string? ChucVuNguoiKy { get; set; }

    public Guid? DonViBanHanhId { get; set; }

    public Guid? DotDeNghiId { get; set; }

    public Guid? TepTinId { get; set; }

    public bool DaKySo { get; set; }

    public short TrangThai { get; set; } = TrangThaiDanhMuc.HoatDong;

    public List<QuyetDinhSangKien> DanhSachSangKien { get; set; } = new List<QuyetDinhSangKien>();
}

/// <summary>Bang noi N-N giua quyet dinh va sang kien duoc cong nhan.</summary>
public class QuyetDinhSangKien : ThucThe
{
    public Guid QuyetDinhId { get; set; }

    public QuyetDinh? QuyetDinh { get; set; }

    public Guid SangKienId { get; set; }

    public Guid? MucCongNhanId { get; set; }

    public string? GhiChu { get; set; }
}

/// <summary>Ngay nghi le - dung khi tinh han xu ly theo ngay lam viec.</summary>
public class NgayNghiLe : ThucThe
{
    public DateOnly Ngay { get; set; }

    public string Ten { get; set; } = string.Empty;

    /// <summary>Lap lai hang nam (vi du 2/9, 30/4) - khi do chi xet thang/ngay.</summary>
    public bool LapLaiHangNam { get; set; }

    public short TrangThai { get; set; } = TrangThaiDanhMuc.HoatDong;
}
