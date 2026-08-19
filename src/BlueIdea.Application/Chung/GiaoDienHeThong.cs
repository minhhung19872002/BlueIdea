using BlueIdea.Domain.Ai;
using BlueIdea.Domain.DanhMuc;
using BlueIdea.Domain.HoiDong;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Domain.QuyTrinh;
using BlueIdea.Domain.SangKien;
using BlueIdea.Domain.TieuChi;
using Microsoft.EntityFrameworkCore;

namespace BlueIdea.Application.Chung;

/// <summary>
/// Hop dong truy cap CSDL cho tang Application (tang Infrastructure cai dat bang EF Core).
/// Nho abstraction nay, handler khong phu thuoc truc tiep vao DbContext cu the.
/// </summary>
public interface IAppDbContext
{
    // Danh muc
    DbSet<LinhVuc> LinhVuc { get; }
    DbSet<DoiTuong> DoiTuong { get; }
    DbSet<LoaiTacGia> LoaiTacGia { get; }
    DbSet<DonVi> DonVi { get; }
    DbSet<DotDeNghi> DotDeNghi { get; }
    DbSet<CauHinhCapPheDuyet> CauHinhCapPheDuyet { get; }
    DbSet<BieuMauXuat> BieuMauXuat { get; }
    DbSet<BieuMauThongKe> BieuMauThongKe { get; }
    DbSet<QuyetDinh> QuyetDinh { get; }
    DbSet<QuyetDinhSangKien> QuyetDinhSangKien { get; }
    DbSet<NgayNghiLe> NgayNghiLe { get; }

    // Quy trinh
    DbSet<QuyTrinh> QuyTrinh { get; }
    DbSet<QuyTrinhBuoc> QuyTrinhBuoc { get; }
    DbSet<QuyTrinhBuocTacNhan> QuyTrinhBuocTacNhan { get; }
    DbSet<QuyTrinhTrangThai> QuyTrinhTrangThai { get; }
    DbSet<QuyTrinhTruongHop> QuyTrinhTruongHop { get; }
    DbSet<QuyTrinhThanhPhanHoSo> QuyTrinhThanhPhanHoSo { get; }
    DbSet<QuyTrinhChucNangBoSung> QuyTrinhChucNangBoSung { get; }
    DbSet<QuyTrinhLienThong> QuyTrinhLienThong { get; }

    // Tieu chi
    DbSet<BoTieuChi> BoTieuChi { get; }
    DbSet<NhomTieuChi> NhomTieuChi { get; }
    DbSet<TieuChiChamDiem> TieuChi { get; }
    DbSet<TieuChiMucDiem> TieuChiMucDiem { get; }
    DbSet<MucCongNhan> MucCongNhan { get; }

    // Hoi dong
    DbSet<HoiDongSangKien> HoiDong { get; }
    DbSet<HoiDongThanhVien> HoiDongThanhVien { get; }
    DbSet<PhienHopHoiDong> PhienHop { get; }
    DbSet<PhienHopHoSo> PhienHopHoSo { get; }
    DbSet<PhienHopDiemDanh> PhienHopDiemDanh { get; }
    DbSet<BienBanHop> BienBanHop { get; }
    DbSet<BienBanChuKy> BienBanChuKy { get; }
    DbSet<PhieuBoPhieu> PhieuBoPhieu { get; }

    // Sang kien
    DbSet<HoSoSangKien> SangKien { get; }
    DbSet<SangKienTacGia> SangKienTacGia { get; }
    DbSet<TepTin> TepTin { get; }
    DbSet<SangKienTepDinhKem> SangKienTepDinhKem { get; }
    DbSet<SangKienLichSu> SangKienLichSu { get; }
    DbSet<SangKienXuLy> SangKienXuLy { get; }
    DbSet<SangKienPhanCong> SangKienPhanCong { get; }
    DbSet<PhieuDanhGia> PhieuDanhGia { get; }
    DbSet<PhieuDanhGiaChiTiet> PhieuDanhGiaChiTiet { get; }
    DbSet<KetQuaXetDuyet> KetQuaXetDuyet { get; }

    // AI
    DbSet<SangKienDoanVan> SangKienDoanVan { get; }
    DbSet<KiemTraTrungLap> KiemTraTrungLap { get; }
    DbSet<KiemTraTrungLapChiTiet> KiemTraTrungLapChiTiet { get; }

    // Quan tri
    DbSet<NguoiDung> NguoiDung { get; }
    DbSet<VaiTro> VaiTro { get; }
    DbSet<Quyen> Quyen { get; }
    DbSet<VaiTroQuyen> VaiTroQuyen { get; }
    DbSet<NguoiDungVaiTro> NguoiDungVaiTro { get; }
    DbSet<PhamViDuLieu> PhamViDuLieu { get; }
    DbSet<RefreshToken> RefreshToken { get; }
    DbSet<LichSuMatKhau> LichSuMatKhau { get; }
    DbSet<BoLocYeuThich> BoLocYeuThich { get; }
    DbSet<MaXacThucTam> MaXacThucTam { get; }
    DbSet<KhoaApiNgoai> KhoaApiNgoai { get; }
    DbSet<CauHinhHeThong> CauHinhHeThong { get; }
    DbSet<CauHinhMenu> CauHinhMenu { get; }
    DbSet<CauHinhEmailSms> CauHinhEmailSms { get; }
    DbSet<MauThongBao> MauThongBao { get; }
    DbSet<ThongBao> ThongBao { get; }
    DbSet<HangDoiGuiTin> HangDoiGuiTin { get; }
    DbSet<CauHinhChuKySo> CauHinhChuKySo { get; }
    DbSet<NhatKyKySo> NhatKyKySo { get; }
    DbSet<HeThongTichHop> HeThongTichHop { get; }
    DbSet<NhatKyDongBo> NhatKyDongBo { get; }
    DbSet<NhatKyHeThong> NhatKyHeThong { get; }
    DbSet<NhatKyLoi> NhatKyLoi { get; }
    DbSet<NhatKyDangNhap> NhatKyDangNhap { get; }

    Task<int> SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Xoa toan bo thuc the dang duoc theo doi. Dung khi mot thao tac that bai va can
    /// ghi trang thai loi bang mot context "sach" (tranh loi day chuyen tu thay doi hong).
    /// </summary>
    void XoaTheoDoi();
}

/// <summary>Thong tin nguoi dung dang thuc hien request hien tai.</summary>
public interface INguoiDungHienTai
{
    Guid? Id { get; }

    string? TenDangNhap { get; }

    string? HoTen { get; }

    Guid? DonViId { get; }

    IReadOnlySet<string> VaiTro { get; }

    IReadOnlySet<string> Quyen { get; }

    string? DiaChiIp { get; }

    string? UserAgent { get; }

    bool DaXacThuc { get; }
}

/// <summary>
/// Kiem tra quyen tren tung chuc nang (yeu cau Muc 6 dac ta:
/// "moi command deu di qua IPermissionService").
/// </summary>
public interface IDichVuPhanQuyen
{
    Task<bool> KiemTraQuyenAsync(Guid nguoiDungId, string maQuyen, CancellationToken ct = default);

    /// <summary>Nem <see cref="KhongCoQuyenException"/> neu khong du quyen.</summary>
    Task BatBuocCoQuyenAsync(string maQuyen, CancellationToken ct = default);

    /// <summary>Danh sach don vi ma nguoi dung duoc phep xem du lieu (theo pham vi vai tro).</summary>
    Task<PhamViTruyCap> LayPhamViTruyCapAsync(Guid nguoiDungId, CancellationToken ct = default);
}

/// <summary>Pham vi du lieu ma nguoi dung duoc truy cap.</summary>
public sealed class PhamViTruyCap
{
    public bool ToanHeThong { get; init; }

    public bool ChiCaNhan { get; init; }

    public IReadOnlySet<Guid> DonViIds { get; init; } = new HashSet<Guid>();

    public static PhamViTruyCap Tat_Ca => new() { ToanHeThong = true };

    public static PhamViTruyCap CaNhan => new() { ChiCaNhan = true };
}

public sealed class KhongCoQuyenException : Exception
{
    public KhongCoQuyenException(string maQuyen)
        : base($"Bạn không có quyền thực hiện chức năng này ({maQuyen}).")
        => MaQuyen = maQuyen;

    public string MaQuyen { get; }
}

/// <summary>Loi nghiep vu co ma loi - middleware chuyen thanh response chuan.</summary>
public sealed class NghiepVuException : Exception
{
    public NghiepVuException(string maLoi, string thongBao) : base(thongBao) => MaLoi = maLoi;

    public string MaLoi { get; }
}

public sealed class KhongTimThayException : Exception
{
    public KhongTimThayException(string doiTuong, object id)
        : base($"Không tìm thấy {doiTuong} với định danh '{id}'.")
    {
    }
}

/// <summary>Nguon thoi gian - tach ra de test tat dinh.</summary>
public interface IDongHoHeThong
{
    DateTimeOffset BayGio { get; }

    DateOnly HomNay { get; }
}

/// <summary>Luu tru tep - abstraction cho MinIO / dia cuc bo.</summary>
public interface ILuuTruTep
{
    Task<string> TaiLenAsync(Stream noiDung, string tenLuuTru, string? mimeType, string bucket,
        CancellationToken ct = default);

    Task<Stream> TaiXuongAsync(string bucket, string duongDan, CancellationToken ct = default);

    Task XoaAsync(string bucket, string duongDan, CancellationToken ct = default);

    /// <summary>Sinh URL co thoi han (khong expose duong dan truc tiep - yeu cau Muc 5 chuc nang 25).</summary>
    Task<string> TaoUrlCoThoiHanAsync(string bucket, string duongDan, TimeSpan thoiHan,
        CancellationToken ct = default);

    Task<bool> TonTaiAsync(string bucket, string duongDan, CancellationToken ct = default);
}

/// <summary>Ma hoa / giai ma du lieu nhay cam (so CCCD, secret tich hop) bang AES-256-GCM.</summary>
public interface IDichVuMaHoa
{
    string MaHoa(string? banRo);

    string? GiaiMa(string? banMa);
}

/// <summary>Bam va kiem tra mat khau (Argon2id).</summary>
public interface IDichVuMatKhau
{
    (string Hash, string Salt) BamMatKhau(string matKhau);

    bool KiemTra(string matKhau, string hash, string salt);

    /// <summary>Kiem tra mat khau co dat chinh sach hay khong, tra ve danh sach loi.</summary>
    IReadOnlyList<string> KiemTraChinhSach(string matKhau, ChinhSachMatKhau chinhSach);
}

public sealed class ChinhSachMatKhau
{
    public int DoDaiToiThieu { get; init; } = 8;

    public bool YeuCauChuHoa { get; init; } = true;

    public bool YeuCauChuThuong { get; init; } = true;

    public bool YeuCauSo { get; init; } = true;

    public bool YeuCauKyTuDacBiet { get; init; } = true;

    public int SoLanKhongTrung { get; init; } = 3;

    public int SoNgayHetHan { get; init; } = 90;
}

/// <summary>Gui thong bao qua cac kenh (app, email, SMS) theo mau cau hinh.</summary>
public interface IDichVuThongBao
{
    Task GuiTheoSuKienAsync(string maSuKien, IEnumerable<Guid> nguoiNhanIds,
        IDictionary<string, object?> bien, CancellationToken ct = default);

    /// <summary>
    /// Gui thong bao chi qua cac kenh duoc cho phep ("APP", "EMAIL", "SMS").
    /// Workflow feature toggles (GUI_EMAIL, GUI_SMS) quyet dinh kenh nao duoc phep.
    /// </summary>
    Task GuiTheoSuKienAsync(string maSuKien, IEnumerable<Guid> nguoiNhanIds,
        IDictionary<string, object?> bien, IReadOnlyCollection<string> kenhChoPhep,
        CancellationToken ct = default);

    Task GuiTrongUngDungAsync(Guid nguoiNhanId, string tieuDe, string noiDung,
        string? duongDan = null, string? mucDo = null, CancellationToken ct = default);
}

/// <summary>
/// Day su kien realtime xuong trinh duyet.
///
/// Tach thanh giao dien de tang Application/Infrastructure khong phai biet den SignalR: doi
/// cong nghe realtime ve sau chi thay ban cai dat o tang Api.
/// </summary>
public interface IBoDayRealtime
{
    /// <summary>Bao cho mot nguoi dung rang ho co thong bao moi.</summary>
    Task ThongBaoMoiAsync(Guid nguoiNhanId, string tieuDe, CancellationToken ct = default);

    /// <summary>Bao cho nhung ai dang mo mot ho so rang trang thai ho so vua doi.</summary>
    Task CapNhatHoSoAsync(Guid sangKienId, CancellationToken ct = default);

    /// <summary>Bao cho phong hop rang bang diem / ket qua bo phieu vua doi.</summary>
    Task CapNhatPhienHopAsync(Guid phienHopId, CancellationToken ct = default);

    /// <summary>
    /// Bao cho nhung ai dang mo mot ho so rang kiem tra trung lap vua chay xong.
    ///
    /// Kiem tra trung lap chay nen (job quet bu moi 15 phut hoac ngay khi nop), nen nguoi dung
    /// dang mo tab Trung lap khong co cach nao biet ket qua da ve neu khong co tin hieu nay.
    /// </summary>
    Task KetQuaTrungLapAsync(Guid sangKienId, CancellationToken ct = default);
}

/// <summary>Sinh ma ho so theo mau cau hinh (vi du SK-{NAM}-{STT:0000}).</summary>
public interface ISinhMaHoSo
{
    Task<string> SinhAsync(int nam, CancellationToken ct = default);
}

/// <summary>Ghi nhat ky he thong (audit log).</summary>
public interface IDichVuNhatKy
{
    Task GhiAsync(string hanhDong, string module, string? doiTuong = null, Guid? doiTuongId = null,
        string? moTa = null, object? duLieuTruoc = null, object? duLieuSau = null,
        string ketQua = "THANH_CONG", CancellationToken ct = default);
}

/// <summary>
/// Day cong viec sang tien trinh nen.
///
/// Tang Application chi biet "viec gi can chay nen", khong biet Hangfire. Nho vay handler
/// van unit-test duoc bang mot cai dat gia, va co the doi bo dieu phoi ma khong sua nghiep vu.
/// </summary>
public interface IHangDoiCongViecNen
{
    /// <summary>Trich xuat van ban (text-layer hoac OCR) cho mot tep vua tai len.</summary>
    void XepLichTrichXuatVanBan(Guid tepTinId);

    /// <summary>Chay kiem tra trung lap cho mot ho so vua nop.</summary>
    void XepLichKiemTraTrungLap(Guid sangKienId);

    /// <summary>Gui ngay cac tin dang cho trong hang doi (khong doi den luot dinh ky).</summary>
    void XepLichGuiHangDoi();
}

/// <summary>
/// Trich xuat van ban tu tep. Cai dat goi dich vu OCR NOI BO (Tesseract) qua mang rieng
/// cua container - tuyet doi khong goi API AI ben thu ba (rang buoc Muc 1 dac ta).
/// </summary>
public interface IDichVuOcr
{
    Task<KetQuaTrichXuatVanBan> TrichXuatAsync(Stream noiDung, string tenTep, string? mimeType,
        CancellationToken ct = default);

    /// <summary>Dinh dang co the trich xuat duoc van ban hay khong (bo qua anh chup man hinh, zip...).</summary>
    bool HoTro(string? phanMoRong);
}

public sealed class KetQuaTrichXuatVanBan
{
    public bool ThanhCong { get; init; }

    public string VanBan { get; init; } = string.Empty;

    public int SoTrang { get; init; }

    /// <summary>TEXT_LAYER | OCR | ANH</summary>
    public string PhuongPhap { get; init; } = "TEXT_LAYER";

    public string? ThongBaoLoi { get; init; }
}

/// <summary>
/// Quet ma doc cho tep tai len (chuc nang 25).
///
/// Quet TRUOC khi ghi xuong kho luu tru, khong phai sau: tep nhiem khong duoc phep cham vao
/// noi luu tru du chi trong choc lat.
/// </summary>
public interface IDichVuQuetVirus
{
    Task<KetQuaQuetVirus> QuetAsync(Stream noiDung, CancellationToken ct = default);
}

public static class TrangThaiQuetVirus
{
    public const string Sach = "SACH";
    public const string Nhiem = "NHIEM";

    /// <summary>Bo quet khong san sang hoac tra ve loi — KHONG dong nghia voi sach.</summary>
    public const string KhongQuetDuoc = "KHONG_QUET_DUOC";
}

public sealed class KetQuaQuetVirus
{
    public string TrangThai { get; init; } = TrangThaiQuetVirus.KhongQuetDuoc;

    public string? TenMaDoc { get; init; }

    public string? ThongBao { get; init; }

    public bool Sach => TrangThai == TrangThaiQuetVirus.Sach;

    public bool Nhiem => TrangThai == TrangThaiQuetVirus.Nhiem;
}

/// <summary>Gui email / SMS that su. Tach khoi hang doi de test duoc phan dieu phoi.</summary>
public interface IDichVuGuiTin
{
    Task GuiEmailAsync(string nguoiNhan, string? tieuDe, string noiDung, CancellationToken ct = default);

    Task GuiSmsAsync(string soDienThoai, string noiDung, CancellationToken ct = default);
}

/// <summary>Doc cau hinh he thong co cache.</summary>
public interface IDichVuCauHinh
{
    Task<string?> LayAsync(string khoa, CancellationToken ct = default);

    Task<T> LayAsync<T>(string khoa, T macDinh, CancellationToken ct = default);

    Task DatAsync(string khoa, string? giaTri, CancellationToken ct = default);

    Task XoaCacheAsync(CancellationToken ct = default);
}
