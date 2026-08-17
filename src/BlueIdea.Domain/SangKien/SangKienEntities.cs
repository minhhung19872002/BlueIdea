using BlueIdea.Domain.Chung;

namespace BlueIdea.Domain.SangKien;

/// <summary>Bang trung tam - ho so sang kien.</summary>
public class HoSoSangKien : ThucThe
{
    /// <summary>Ma ho so sinh theo mau cau hinh, vi du SK-2026-0001.</summary>
    public string MaHoSo { get; set; } = string.Empty;

    public string TenSangKien { get; set; } = string.Empty;

    public string TenKhongDau { get; set; } = string.Empty;

    public Guid DotDeNghiId { get; set; }

    public Guid LinhVucId { get; set; }

    public Guid? DoiTuongId { get; set; }

    public Guid? LoaiTacGiaId { get; set; }

    public Guid? DonViId { get; set; }

    public Guid? QuyTrinhId { get; set; }

    /// <summary>Snapshot quy trinh tai thoi diem nop - engine chay theo snapshot nay.</summary>
    public string? QuyTrinhSnapshot { get; set; }

    public Guid? BuocHienTaiId { get; set; }

    public Guid? TrangThaiHienTaiId { get; set; }

    /// <summary>NHAP | DA_NOP | DANG_XU_LY | YEU_CAU_BO_SUNG | DA_PHE_DUYET | KHONG_DAT | DA_RUT | DA_HUY</summary>
    public string TrangThaiTong { get; set; } = TrangThaiTongHoSo.Nhap;

    // --- Noi dung nghiep vu co dinh ---
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

    /// <summary>Du lieu cac thanh phan ho so cau hinh dong (jsonb: ma thanh phan -&gt; noi dung).</summary>
    public Dictionary<string, string> NoiDungDong { get; set; } = new();

    // --- Ket qua ---
    public decimal? TyLeTrungLap { get; set; }

    /// <summary>CHUA_KIEM_TRA | DANG_CHAY | HOAN_THANH | LOI</summary>
    public string TrangThaiKiemTraTrungLap { get; set; } = "CHUA_KIEM_TRA";

    public decimal? TongDiem { get; set; }

    public decimal? DiemTrungBinh { get; set; }

    public Guid? MucCongNhanId { get; set; }

    /// <summary>DAT | KHONG_DAT</summary>
    public string? KetQua { get; set; }

    public Guid? QuyetDinhId { get; set; }

    /// <summary>
    /// Ket qua da duoc CONG BO chinh thuc hay chua (chuc nang 32).
    ///
    /// Co tinh dat tren ho so chu khong chi tren <c>ket_qua_xet_duyet</c>: khong phai ho so nao
    /// cung co ban ghi ket qua hoi dong (vi du ho so ket luan o cap lanh dao), nen neu chi dua vao
    /// bang do thi trang thai cong bo se bi mat.
    /// </summary>
    public bool DaCongBoKetQua { get; set; }

    public DateTimeOffset? NgayCongBoKetQua { get; set; }

    public DateOnly? NgayCongNhan { get; set; }

    // --- Thoi gian va khoa ---
    public DateTimeOffset? NgayNop { get; set; }

    public DateTimeOffset? HanXuLyHienTai { get; set; }

    public DateTimeOffset? NgayHoanThanh { get; set; }

    public bool DangKhoa { get; set; }

    public string? LyDoKhoa { get; set; }

    public bool CongKhai { get; set; }

    public int SoLuotXem { get; set; }

    /// <summary>Optimistic concurrency - tang moi lan cap nhat.</summary>
    public int PhienBan { get; set; } = 1;

    public List<SangKienTacGia> DanhSachTacGia { get; set; } = new List<SangKienTacGia>();

    public List<SangKienTepDinhKem> TepDinhKem { get; set; } = new List<SangKienTepDinhKem>();

    public List<SangKienXuLy> LichSuXuLy { get; set; } = new List<SangKienXuLy>();

    public List<SangKienLichSu> LichSuChinhSua { get; set; } = new List<SangKienLichSu>();

    /// <summary>Ho so chi sua duoc khi o trang thai nhap hoac dang yeu cau bo sung.</summary>
    public bool ChoPhepSua()
        => !DangKhoa
           && TrangThaiTong is TrangThaiTongHoSo.Nhap or TrangThaiTongHoSo.YeuCauBoSung;

    /// <summary>Chi cho rut ho so khi chua co ket qua cuoi cung.</summary>
    public bool ChoPhepRut()
        => TrangThaiTong is TrangThaiTongHoSo.DaNop or TrangThaiTongHoSo.DangXuLy
            or TrangThaiTongHoSo.YeuCauBoSung;
}

/// <summary>Tac gia / dong tac gia cua mot ho so.</summary>
public class SangKienTacGia : ThucThe
{
    public Guid SangKienId { get; set; }

    public HoSoSangKien? SangKien { get; set; }

    /// <summary>Cho phep null khi tac gia khong co tai khoan trong he thong.</summary>
    public Guid? NguoiDungId { get; set; }

    public string HoTen { get; set; } = string.Empty;

    public DateOnly? NgaySinh { get; set; }

    public string? GioiTinh { get; set; }

    /// <summary>Ma hoa AES-256-GCM o tang ung dung (du lieu ca nhan).</summary>
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

/// <summary>Bang tep tin dung chung toan he thong.</summary>
public class TepTin : ThucThe
{
    public string TenGoc { get; set; } = string.Empty;

    public string TenLuuTru { get; set; } = string.Empty;

    public string DuongDan { get; set; } = string.Empty;

    public string Bucket { get; set; } = "sangkien";

    public long KichThuoc { get; set; }

    public string? MimeType { get; set; }

    public string? PhanMoRong { get; set; }

    public string? HashSha256 { get; set; }

    public Guid? NguoiTaiLenId { get; set; }

    public DateTimeOffset NgayTaiLen { get; set; } = DateTimeOffset.UtcNow;

    public bool DaQuetVirus { get; set; }

    /// <summary>Noi dung van ban trich xuat tu OCR / parse - dung cho kiem tra trung lap.</summary>
    public string? NoiDungTrichXuat { get; set; }

    /// <summary>CHUA_XU_LY | DANG_XU_LY | HOAN_THANH | KHONG_CAN | LOI</summary>
    public string TrangThaiOcr { get; set; } = "CHUA_XU_LY";
}

public class SangKienTepDinhKem : ThucThe
{
    public Guid SangKienId { get; set; }

    public HoSoSangKien? SangKien { get; set; }

    public Guid TepTinId { get; set; }

    public TepTin? TepTin { get; set; }

    /// <summary>Ma thanh phan ho so ma tep nay thuoc ve.</summary>
    public string ThanhPhanHoSoMa { get; set; } = string.Empty;

    public string? MoTa { get; set; }

    public int ThuTu { get; set; }

    public int PhienBan { get; set; } = 1;
}

/// <summary>Chuc nang 23 - Lich su chinh sua ho so (diff truoc/sau).</summary>
public class SangKienLichSu : ThucThe
{
    public Guid SangKienId { get; set; }

    public HoSoSangKien? SangKien { get; set; }

    /// <summary>TAO | SUA | NOP | RUT | BO_SUNG | XOA_TEP | THEM_TEP</summary>
    public string HanhDong { get; set; } = string.Empty;

    public List<string> TruongThayDoi { get; set; } = new();

    public Dictionary<string, string?>? GiaTriTruoc { get; set; }

    public Dictionary<string, string?>? GiaTriSau { get; set; }

    public Guid? NguoiThucHienId { get; set; }

    public DateTimeOffset ThoiGian { get; set; } = DateTimeOffset.UtcNow;

    public string? DiaChiIp { get; set; }

    public string? UserAgent { get; set; }

    public string? GhiChu { get; set; }
}

public static class HanhDongLichSuHoSo
{
    public const string Tao = "TAO";
    public const string Sua = "SUA";
    public const string Nop = "NOP";
    public const string Rut = "RUT";
    public const string BoSung = "BO_SUNG";
    public const string XoaTep = "XOA_TEP";
    public const string ThemTep = "THEM_TEP";
}

/// <summary>Chuc nang 29, 30 - Instance thuc thi cua workflow tren mot ho so.</summary>
public class SangKienXuLy : ThucThe
{
    public Guid SangKienId { get; set; }

    public HoSoSangKien? SangKien { get; set; }

    public Guid BuocId { get; set; }

    public string TenBuocSnapshot { get; set; } = string.Empty;

    public Guid? TrangThaiId { get; set; }

    public Guid? TruongHopId { get; set; }

    public string? TenTruongHopSnapshot { get; set; }

    public Guid? NguoiXuLyId { get; set; }

    public Guid? NguoiUyQuyenId { get; set; }

    public string? YKien { get; set; }

    public List<Guid> TepDinhKemIds { get; set; } = new();

    public DateTimeOffset ThoiGianNhan { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? HanXuLy { get; set; }

    public DateTimeOffset? ThoiGianXuLy { get; set; }

    public decimal? SoNgayXuLy { get; set; }

    public bool QuaHan { get; set; }

    public int ThuTu { get; set; }

    public bool DaHoanThanh => ThoiGianXuLy.HasValue;
}

/// <summary>Chuc nang 33 - Phan cong thanh vien hoi dong cham mot ho so.</summary>
public class SangKienPhanCong : ThucThe
{
    public Guid SangKienId { get; set; }

    public Guid HoiDongId { get; set; }

    public Guid ThanhVienId { get; set; }

    public Guid? NguoiPhanCongId { get; set; }

    public DateTimeOffset NgayPhanCong { get; set; } = DateTimeOffset.UtcNow;

    public DateTimeOffset? HanHoanThanh { get; set; }

    /// <summary>CHUA_CHAM | DANG_CHAM | DA_CHAM | QUA_HAN</summary>
    public string TrangThaiPhanCong { get; set; } = "CHUA_CHAM";

    public string? GhiChu { get; set; }
}

public static class TrangThaiPhanCong
{
    public const string ChuaCham = "CHUA_CHAM";
    public const string DangCham = "DANG_CHAM";
    public const string DaCham = "DA_CHAM";
    public const string QuaHan = "QUA_HAN";
}

/// <summary>Chuc nang 34, 35 - Phieu danh gia cua mot thanh vien hoi dong.</summary>
public class PhieuDanhGia : ThucThe
{
    public Guid SangKienId { get; set; }

    public Guid HoiDongId { get; set; }

    public Guid ThanhVienId { get; set; }

    public Guid BoTieuChiId { get; set; }

    /// <summary>Snapshot bo tieu chi luc cham (json) de phieu cu khong bi anh huong khi sua tieu chi.</summary>
    public string? BoTieuChiSnapshot { get; set; }

    public decimal TongDiem { get; set; }

    /// <summary>Diem theo tung nhom tieu chi (jsonb: nhomId -&gt; diem).</summary>
    public Dictionary<string, decimal> DiemTheoNhom { get; set; } = new();

    public string? NhanXetChung { get; set; }

    public string? UuDiem { get; set; }

    public string? HanChe { get; set; }

    public Guid? DeXuatMucCongNhanId { get; set; }

    /// <summary>DAT | KHONG_DAT</summary>
    public string? KetLuan { get; set; }

    /// <summary>NHAP | DA_GUI | DA_KY</summary>
    public string TrangThaiPhieu { get; set; } = "NHAP";

    public DateTimeOffset? NgayCham { get; set; }

    public DateTimeOffset? NgayGui { get; set; }

    public Guid? ChuKySoId { get; set; }

    public string? SoPhieu { get; set; }

    public List<PhieuDanhGiaChiTiet> ChiTiet { get; set; } = new List<PhieuDanhGiaChiTiet>();

    public bool DaGui => TrangThaiPhieu is "DA_GUI" or "DA_KY";
}

public static class TrangThaiPhieuDanhGia
{
    public const string Nhap = "NHAP";
    public const string DaGui = "DA_GUI";
    public const string DaKy = "DA_KY";
}

public class PhieuDanhGiaChiTiet : ThucThe
{
    public Guid PhieuDanhGiaId { get; set; }

    public PhieuDanhGia? PhieuDanhGia { get; set; }

    public Guid TieuChiId { get; set; }

    public string TenTieuChiSnapshot { get; set; } = string.Empty;

    public decimal DiemToiDaSnapshot { get; set; }

    public decimal Diem { get; set; }

    public Guid? MucDiemId { get; set; }

    public string? NhanXet { get; set; }
}

/// <summary>Chuc nang 32 - Ket qua xet duyet tong hop cua hoi dong tren mot ho so.</summary>
public class KetQuaXetDuyet : ThucThe
{
    public Guid SangKienId { get; set; }

    public Guid HoiDongId { get; set; }

    public Guid? PhienHopId { get; set; }

    public int SoPhieuCham { get; set; }

    public decimal? DiemCaoNhat { get; set; }

    public decimal? DiemThapNhat { get; set; }

    public decimal DiemTrungBinh { get; set; }

    public decimal TongDiemTrongSo { get; set; }

    public int SoPhieuDongY { get; set; }

    public int SoPhieuKhongDongY { get; set; }

    /// <summary>DAT | KHONG_DAT</summary>
    public string? KetQua { get; set; }

    public Guid? MucCongNhanId { get; set; }

    public string? LyDo { get; set; }

    public Guid? NguoiKetLuanId { get; set; }

    public DateTimeOffset? NgayKetLuan { get; set; }

    public bool DaCongBo { get; set; }

    public DateTimeOffset? NgayCongBo { get; set; }
}

public static class KetQuaXetDuyetGiaTri
{
    public const string Dat = "DAT";
    public const string KhongDat = "KHONG_DAT";
}
