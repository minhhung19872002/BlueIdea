using BlueIdea.Shared.KetQua;

namespace BlueIdea.Application.SangKien;

/// <summary>Tac gia / dong tac gia gui len tu form wizard.</summary>
public sealed class TacGiaDto
{
    public Guid? Id { get; set; }

    public Guid? NguoiDungId { get; set; }

    public string HoTen { get; set; } = string.Empty;

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    public string? SoCccd { get; set; }

    public string? ChucVu { get; set; }

    public string? DonViCongTac { get; set; }

    public string? TrinhDoChuyenMon { get; set; }

    public string? Email { get; set; }

    public string? DienThoai { get; set; }

    public decimal TyLeDongGop { get; set; }

    public bool LaTacGiaChinh { get; set; }

    public int ThuTu { get; set; }
}

/// <summary>Du lieu noi dung ho so (dung chung cho tao moi va cap nhat).</summary>
public sealed class NoiDungHoSoDto
{
    public string TenSangKien { get; set; } = string.Empty;

    public Guid DotDeNghiId { get; set; }

    public Guid LinhVucId { get; set; }

    public Guid? DoiTuongId { get; set; }

    public Guid? LoaiTacGiaId { get; set; }

    public Guid? DonViId { get; set; }

    public string? MoTaGiaiPhap { get; set; }

    public string? TinhTrangTruocKhiApDung { get; set; }

    public string? NoiDungGiaiPhap { get; set; }

    public string? TinhMoi { get; set; }

    public string? KhaNangApDung { get; set; }

    public string? PhamViApDung { get; set; }

    public string? HieuQuaKinhTe { get; set; }

    public decimal? GiaTriLamLoiUocTinh { get; set; }

    public string? HieuQuaXaHoi { get; set; }

    public DateOnly? ThoiGianApDungTu { get; set; }

    public DateOnly? ThoiGianApDungDen { get; set; }

    /// <summary>Noi dung cac thanh phan ho so cau hinh dong: maThanhPhan -&gt; noi dung.</summary>
    public Dictionary<string, string> NoiDungDong { get; set; } = new();

    public List<TacGiaDto> DanhSachTacGia { get; set; } = new();
}

/// <summary>Dong du lieu trong bang danh sach ho so.</summary>
public sealed record SangKienTomTatDto(
    Guid Id,
    string MaHoSo,
    string TenSangKien,
    string? TenLinhVuc,
    string? TenDonVi,
    string? TenDot,
    string TrangThaiTong,
    string? TenBuocHienTai,
    string? TenTrangThaiHienTai,
    decimal? TongDiem,
    decimal? TyLeTrungLap,
    string? KetQua,
    DateTimeOffset? NgayNop,
    DateTimeOffset? HanXuLyHienTai,
    bool QuaHan,
    string TacGiaChinh,
    int PhienBan);

/// <summary>Chi tiet day du cua ho so (man hinh /sang-kien/:id).</summary>
public sealed class SangKienChiTietDto
{
    public Guid Id { get; set; }

    public string MaHoSo { get; set; } = string.Empty;

    public string TenSangKien { get; set; } = string.Empty;

    public Guid DotDeNghiId { get; set; }

    public string? TenDot { get; set; }

    public Guid LinhVucId { get; set; }

    public string? TenLinhVuc { get; set; }

    public Guid? DoiTuongId { get; set; }

    public string? TenDoiTuong { get; set; }

    public Guid? LoaiTacGiaId { get; set; }

    public Guid? DonViId { get; set; }

    public string? TenDonVi { get; set; }

    public string TrangThaiTong { get; set; } = string.Empty;

    public Guid? BuocHienTaiId { get; set; }

    public string? TenBuocHienTai { get; set; }

    public Guid? TrangThaiHienTaiId { get; set; }

    public string? TenTrangThaiHienTai { get; set; }

    public string? MauTrangThai { get; set; }

    public string? MoTaGiaiPhap { get; set; }

    public string? TinhTrangTruocKhiApDung { get; set; }

    public string? NoiDungGiaiPhap { get; set; }

    public string? TinhMoi { get; set; }

    public string? KhaNangApDung { get; set; }

    public string? PhamViApDung { get; set; }

    public string? HieuQuaKinhTe { get; set; }

    public decimal? GiaTriLamLoiUocTinh { get; set; }

    public string? HieuQuaXaHoi { get; set; }

    public DateOnly? ThoiGianApDungTu { get; set; }

    public DateOnly? ThoiGianApDungDen { get; set; }

    public Dictionary<string, string> NoiDungDong { get; set; } = new();

    public decimal? TyLeTrungLap { get; set; }

    public string TrangThaiKiemTraTrungLap { get; set; } = string.Empty;

    public decimal? TongDiem { get; set; }

    public decimal? DiemTrungBinh { get; set; }

    public string? KetQua { get; set; }

    public Guid? MucCongNhanId { get; set; }

    public string? TenMucCongNhan { get; set; }

    public DateOnly? NgayCongNhan { get; set; }

    public DateTimeOffset? NgayNop { get; set; }

    public DateTimeOffset? HanXuLyHienTai { get; set; }

    public bool DangKhoa { get; set; }

    public string? LyDoKhoa { get; set; }

    public bool CongKhai { get; set; }

    public int PhienBan { get; set; }

    public bool ChoPhepSua { get; set; }

    public bool ChoPhepRut { get; set; }

    public List<TacGiaDto> DanhSachTacGia { get; set; } = new();

    public List<TepDinhKemDto> TepDinhKem { get; set; } = new();

    public List<ThanhPhanHoSoDto> ThanhPhanHoSo { get; set; } = new();
}

public sealed record TepDinhKemDto(
    Guid Id,
    Guid TepTinId,
    string TenGoc,
    long KichThuoc,
    string? MimeType,
    string ThanhPhanHoSoMa,
    string? MoTa,
    DateTimeOffset NgayTaiLen);

/// <summary>Checklist thanh phan ho so (chuc nang 24).</summary>
public sealed record ThanhPhanHoSoDto(
    string Ma,
    string Ten,
    bool BatBuoc,
    string LoaiDuLieu,
    int SoKyTuToiThieu,
    int SoKyTuToiDa,
    int SoLuongToiDa,
    int DungLuongToiDaMb,
    IReadOnlyList<string> DinhDangChoPhep,
    string? MoTaHuongDan,
    string TrangThai,
    string? CanhBao);

/// <summary>Trang thai kiem tra mot thanh phan ho so.</summary>
public static class TrangThaiThanhPhan
{
    public const string Du = "DU";
    public const string Thieu = "THIEU";
    public const string ChuaDat = "CHUA_DAT";
    public const string KhongBatBuoc = "KHONG_BAT_BUOC";
}

/// <summary>Tham so loc danh sach ho so (chuc nang 28 - bang da bo loc).</summary>
public sealed class ThamSoLocSangKien : ThamSoPhanTrang
{
    public string? TuKhoa { get; set; }

    public Guid? DotDeNghiId { get; set; }

    public Guid? LinhVucId { get; set; }

    public Guid? DonViId { get; set; }

    public Guid? HoiDongId { get; set; }

    public string? TrangThaiTong { get; set; }

    public Guid? BuocHienTaiId { get; set; }

    public string? KetQua { get; set; }

    public int? Nam { get; set; }

    public DateOnly? NgayNopTu { get; set; }

    public DateOnly? NgayNopDen { get; set; }

    public decimal? DiemTu { get; set; }

    public decimal? DiemDen { get; set; }

    public decimal? TrungLapTu { get; set; }

    public decimal? TrungLapDen { get; set; }

    public bool? ChiCuaToi { get; set; }

    public bool? ChiQuaHan { get; set; }

    public bool? ChiCongKhai { get; set; }
}

/// <summary>Mot moc trong timeline tien do xu ly (chuc nang 30).</summary>
public sealed record MocTienDoDto(
    Guid Id,
    Guid BuocId,
    string TenBuoc,
    string? TenTrangThai,
    string? TenTruongHop,
    string? NguoiXuLy,
    string? YKien,
    DateTimeOffset ThoiGianNhan,
    DateTimeOffset? HanXuLy,
    DateTimeOffset? ThoiGianXuLy,
    decimal? SoNgayXuLy,
    bool QuaHan,
    IReadOnlyList<TepDinhKemDto> TepDinhKem);
