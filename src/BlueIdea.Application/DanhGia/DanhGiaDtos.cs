namespace BlueIdea.Application.DanhGia;

/// <summary>Mot dong diem trong phieu danh gia gui len tu UI.</summary>
public sealed class ChiTietChamDiemDto
{
    public Guid TieuChiId { get; set; }

    public decimal Diem { get; set; }

    public Guid? MucDiemId { get; set; }

    public string? NhanXet { get; set; }
}

/// <summary>Du lieu phieu cham gui len (luu nhap hoac gui chinh thuc).</summary>
public sealed class PhieuChamDto
{
    public Guid SangKienId { get; set; }

    public Guid HoiDongId { get; set; }

    public List<ChiTietChamDiemDto> ChiTiet { get; set; } = new();

    public string? NhanXetChung { get; set; }

    public string? UuDiem { get; set; }

    public string? HanChe { get; set; }

    public Guid? DeXuatMucCongNhanId { get; set; }
}

/// <summary>Phieu danh gia tra ve cho UI cham diem.</summary>
public sealed class PhieuDanhGiaDto
{
    public Guid Id { get; set; }

    public Guid SangKienId { get; set; }

    public string MaHoSo { get; set; } = string.Empty;

    public string TenSangKien { get; set; } = string.Empty;

    public Guid HoiDongId { get; set; }

    public Guid BoTieuChiId { get; set; }

    public string TrangThaiPhieu { get; set; } = string.Empty;

    public decimal TongDiem { get; set; }

    public string? NhanXetChung { get; set; }

    public string? UuDiem { get; set; }

    public string? HanChe { get; set; }

    public Guid? DeXuatMucCongNhanId { get; set; }

    public string? KetLuan { get; set; }

    public DateTimeOffset? NgayCham { get; set; }

    public DateTimeOffset? NgayGui { get; set; }

    public string? SoPhieu { get; set; }

    public bool ChoPhepSua { get; set; }

    public List<ChiTietChamDiemDto> ChiTiet { get; set; } = new();

    public BoTieuChiDto? BoTieuChi { get; set; }
}

/// <summary>Bo tieu chi render dong tren man hinh cham diem.</summary>
public sealed record BoTieuChiDto(
    Guid Id,
    string Ma,
    string Ten,
    decimal ThangDiemToiDa,
    decimal DiemDatToiThieu,
    string CachTinh,
    int LamTron,
    IReadOnlyList<NhomTieuChiDto> DanhSachNhom);

public sealed record NhomTieuChiDto(
    Guid Id,
    string Ma,
    string Ten,
    string? MoTa,
    decimal TrongSo,
    decimal DiemToiDa,
    int ThuTu,
    IReadOnlyList<TieuChiDto> DanhSachTieuChi);

public sealed record TieuChiDto(
    Guid Id,
    string Ma,
    string Ten,
    string? MoTa,
    decimal DiemToiDa,
    decimal DiemToiThieu,
    decimal TrongSo,
    string KieuNhap,
    decimal BuocNhay,
    bool BatBuocNhanXet,
    string? HuongDanCham,
    int ThuTu,
    IReadOnlyList<MucDiemDto> DanhSachMucDiem);

public sealed record MucDiemDto(Guid Id, string Ten, decimal Diem, string? MoTa, int ThuTu);

/// <summary>Dong trong man hinh "Viec cua toi" cua thanh vien hoi dong (chuc nang 33).</summary>
public sealed record PhanCongChamDto(
    Guid Id,
    Guid SangKienId,
    string MaHoSo,
    string TenSangKien,
    string? TenLinhVuc,
    Guid HoiDongId,
    string TenHoiDong,
    string TrangThaiPhanCong,
    DateTimeOffset NgayPhanCong,
    DateTimeOffset? HanHoanThanh,
    bool QuaHan,
    decimal? TongDiemDaCham,
    Guid? PhieuDanhGiaId);

/// <summary>O trong bang ma tran diem (hang = ho so, cot = thanh vien) - chuc nang 35.</summary>
/// <summary>
/// Mot o trong bang ma tran diem.
///
/// <c>PhieuId</c> co gia tri khi thanh vien da co phieu — thu ky can dinh danh phieu de mo lai
/// phieu da gui ngay tren bang, khong phai mo tung ho so ra tim.
/// </summary>
public sealed record ODiemMaTran(
    Guid ThanhVienId, string TenThanhVien, decimal? Diem, string TrangThai, Guid? PhieuId);

public sealed record DongMaTranDiem(
    Guid SangKienId,
    string MaHoSo,
    string TenSangKien,
    IReadOnlyList<ODiemMaTran> DiemThanhVien,
    decimal? DiemTrungBinh,
    decimal? DiemCaoNhat,
    decimal? DiemThapNhat,
    int SoPhieuDaCham,
    int SoPhieuPhanCong);

/// <summary>Ket qua tong hop diem cua hoi dong tra ve cho thu ky.</summary>
public sealed record KetQuaTongHopDto(
    Guid SangKienId,
    int SoPhieu,
    int SoPhieuSuDung,
    decimal DiemCaoNhat,
    decimal DiemThapNhat,
    decimal DiemTrungBinh,
    decimal DiemCuoiCung,
    bool Dat,
    Guid? MucCongNhanId,
    string? TenMucCongNhan,
    IReadOnlyList<string> CanhBao);
