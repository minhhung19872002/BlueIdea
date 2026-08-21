using BlueIdea.Domain.Chung;

namespace BlueIdea.Domain.QuyTrinh;

/// <summary>Chuc nang 9 - Quy trinh xu ly ho so (cau hinh dong, co phien ban).</summary>
public class QuyTrinh : ThucTheDanhMuc
{
    /// <summary>CO_SO | THANH_PHO | TINH</summary>
    public string Cap { get; set; } = CapXetDuyet.CoSo;

    public int PhienBan { get; set; } = 1;

    /// <summary>Quy trinh goc khi sao chep / nang phien ban.</summary>
    public Guid? QuyTrinhGocId { get; set; }

    public PhamViApDungQuyTrinh PhamViApDung { get; set; } = new();

    public bool LaMacDinh { get; set; }

    /// <summary>NHAP | DANG_AP_DUNG | NGUNG_AP_DUNG</summary>
    public string TrangThaiQuyTrinh { get; set; } = Chung.TrangThaiQuyTrinh.Nhap;

    /// <summary>Toa do node phuc vu ReactFlow (jsonb).</summary>
    public Dictionary<string, object>? SoDoLayout { get; set; }

    public List<QuyTrinhBuoc> DanhSachBuoc { get; set; } = new List<QuyTrinhBuoc>();

    public List<QuyTrinhThanhPhanHoSo> ThanhPhanHoSo { get; set; } = new List<QuyTrinhThanhPhanHoSo>();

    public List<QuyTrinhChucNangBoSung> ChucNangBoSung { get; set; } = new List<QuyTrinhChucNangBoSung>();

    public List<QuyTrinhLienThong> LienThong { get; set; } = new List<QuyTrinhLienThong>();

    public List<QuyTrinhTrangThai> TrangThaiToanCuc { get; set; } = new List<QuyTrinhTrangThai>();
}

public class PhamViApDungQuyTrinh
{
    public List<Guid> DotDeNghiIds { get; set; } = new();

    public List<Guid> LinhVucIds { get; set; } = new();

    public List<Guid> LoaiSangKienIds { get; set; } = new();
}

/// <summary>Chuc nang 11 - Buoc xu ly trong quy trinh.</summary>
public class QuyTrinhBuoc : ThucThe
{
    public Guid QuyTrinhId { get; set; }

    public QuyTrinh? QuyTrinh { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public int ThuTu { get; set; }

    /// <summary>TIEP_NHAN | THAM_DINH | ... | KET_THUC</summary>
    public string LoaiBuoc { get; set; } = Chung.LoaiBuoc.TiepNhan;

    public int SoNgayXuLy { get; set; } = 3;

    public bool TinhTheoNgayLamViec { get; set; } = true;

    public bool BatBuocDinhKem { get; set; }

    public List<string> DanhSachTepBatBuoc { get; set; } = new();

    public bool BatBuocNhapYKien { get; set; }

    public bool ChoPhepUyQuyen { get; set; }

    public bool ChoPhepThuHoi { get; set; }

    public bool LaBuocBatDau { get; set; }

    public bool LaBuocKetThuc { get; set; }

    /// <summary>So gio canh bao truoc han xu ly.</summary>
    public int CanhBaoTruocHanGio { get; set; } = 24;

    public string? MoTaHuongDan { get; set; }

    /// <summary>Buoc CHAM_DIEM bat buoc gan hoi dong + bo tieu chi (rule validator so 6).</summary>
    public Guid? HoiDongId { get; set; }

    public Guid? BoTieuChiId { get; set; }

    public List<QuyTrinhBuocTacNhan> TacNhan { get; set; } = new List<QuyTrinhBuocTacNhan>();

    public List<QuyTrinhTruongHop> TruongHop { get; set; } = new List<QuyTrinhTruongHop>();

    public List<QuyTrinhTrangThai> TrangThai { get; set; } = new List<QuyTrinhTrangThai>();
}

/// <summary>Chuc nang 15 - Tac nhan xu ly cua mot buoc.</summary>
public class QuyTrinhBuocTacNhan : ThucThe
{
    public Guid BuocId { get; set; }

    public QuyTrinhBuoc? Buoc { get; set; }

    /// <summary>NGUOI_DUNG | VAI_TRO | DON_VI | HOI_DONG | CHUC_DANH_HOI_DONG | NGUOI_TAO_HO_SO | LANH_DAO_DON_VI_TAC_GIA</summary>
    public string LoaiTacNhan { get; set; } = Chung.LoaiTacNhan.VaiTro;

    public Guid? ThamChieuId { get; set; }

    /// <summary>Ma tham chieu khi tac nhan la vai tro / chuc danh (thay cho Id).</summary>
    public string? ThamChieuMa { get; set; }

    /// <summary>MOT_NGUOI | TAT_CA | DA_SO | CHU_TICH_QUYET_DINH</summary>
    public string QuyTacXuLy { get; set; } = Chung.QuyTacXuLy.MotNguoi;

    public decimal? TyLeDongThuan { get; set; }

    public int ThuTu { get; set; }
}

/// <summary>Chuc nang 14 - Trang thai cua buoc (hoac trang thai toan cuc khi BuocId = null).</summary>
public class QuyTrinhTrangThai : ThucThe
{
    public Guid QuyTrinhId { get; set; }

    public Guid? BuocId { get; set; }

    public QuyTrinhBuoc? Buoc { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public string? MauSac { get; set; }

    public string? Icon { get; set; }

    public bool LaTrangThaiKetThuc { get; set; }

    public bool HienThiChoTacGia { get; set; } = true;

    public int ThuTu { get; set; }
}

/// <summary>Chuc nang 10 - Truong hop chuyen tiep (nhanh re) tu mot buoc.</summary>
public class QuyTrinhTruongHop : ThucThe
{
    public Guid BuocId { get; set; }

    public QuyTrinhBuoc? Buoc { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    /// <summary>null = ket thuc quy trinh.</summary>
    public Guid? BuocTiepTheoId { get; set; }

    public Guid? TrangThaiGanId { get; set; }

    /// <summary>Bieu thuc dieu kien dang jsonb, danh gia boi rule evaluator tu viet.</summary>
    public BieuThucDieuKien? DieuKien { get; set; }

    public List<string> HanhDong { get; set; } = new();

    public Guid? MauThongBaoId { get; set; }

    public string? MauNut { get; set; }

    public int ThuTu { get; set; }

    /// <summary>Truong hop mac dinh khi khong co truong hop nao khop dieu kien.</summary>
    public bool LaMacDinh { get; set; }
}

/// <summary>
/// Bieu thuc dieu kien dang cay: hoac la mot phep so sanh don, hoac la mot nhom AND/OR/NOT.
/// Vi du: {"truong":"tong_diem","toan_tu":">=","gia_tri":80}
/// hoac  {"phep":"AND","cac_dieu_kien":[...]}
/// </summary>
public class BieuThucDieuKien
{
    /// <summary>AND | OR | NOT - chi co khi la nhom.</summary>
    public string? Phep { get; set; }

    public List<BieuThucDieuKien>? CacDieuKien { get; set; }

    /// <summary>Ten bien lay tu context, vi du tong_diem, ty_le_trung_lap.</summary>
    public string? Truong { get; set; }

    /// <summary>= != &gt; &gt;= &lt; &lt;= IN CONTAINS BETWEEN</summary>
    public string? ToanTu { get; set; }

    public object? GiaTri { get; set; }

    public bool LaNhom => !string.IsNullOrEmpty(Phep);
}

/// <summary>Chuc nang 13 - Thanh phan ho so bat buoc/tuy chon cua quy trinh.</summary>
public class QuyTrinhThanhPhanHoSo : ThucThe
{
    public Guid QuyTrinhId { get; set; }

    public QuyTrinh? QuyTrinh { get; set; }

    public string Ma { get; set; } = string.Empty;

    public string Ten { get; set; } = string.Empty;

    public bool BatBuoc { get; set; }

    /// <summary>VAN_BAN | TEP | CA_HAI</summary>
    public string LoaiDuLieu { get; set; } = "CA_HAI";

    public List<string> DinhDangChoPhep { get; set; } = new();

    public int DungLuongToiDaMb { get; set; } = 20;

    public int SoLuongToiDa { get; set; } = 5;

    public int SoKyTuToiThieu { get; set; }

    public int SoKyTuToiDa { get; set; }

    public bool DungDeKiemTraTrungLap { get; set; }

    public int ThuTu { get; set; }

    public string? MoTaHuongDan { get; set; }
}

public static class LoaiDuLieuThanhPhan
{
    public const string VanBan = "VAN_BAN";
    public const string Tep = "TEP";
    public const string CaHai = "CA_HAI";
}

/// <summary>Chuc nang 12 - Chuc nang bo sung bat/tat theo quy trinh hoac theo buoc.</summary>
public class QuyTrinhChucNangBoSung : ThucThe
{
    public Guid QuyTrinhId { get; set; }

    public QuyTrinh? QuyTrinh { get; set; }

    public Guid? BuocId { get; set; }

    /// <summary>KY_SO | GUI_EMAIL | GUI_SMS | XUAT_BIEU_MAU | BO_PHIEU_KIN | TAO_BIEN_BAN | KIEM_TRA_TRUNG_LAP | CHAM_DIEM_DOC_LAP | CONG_KHAI_KET_QUA</summary>
    public string MaChucNang { get; set; } = string.Empty;

    public bool BatBuoc { get; set; }

    public Dictionary<string, object>? CauHinh { get; set; }
}

public static class MaChucNangBoSung
{
    public const string KySo = "KY_SO";
    public const string GuiEmail = "GUI_EMAIL";
    public const string GuiSms = "GUI_SMS";
    public const string XuatBieuMau = "XUAT_BIEU_MAU";
    public const string BoPhieuKin = "BO_PHIEU_KIN";
    public const string TaoBienBan = "TAO_BIEN_BAN";
    public const string KiemTraTrungLap = "KIEM_TRA_TRUNG_LAP";
    public const string ChamDiemDocLap = "CHAM_DIEM_DOC_LAP";
    public const string CongKhaiKetQua = "CONG_KHAI_KET_QUA";
}

/// <summary>Chuc nang 16 - Cau hinh lien thong sang he thong ngoai tai mot buoc.</summary>
public class QuyTrinhLienThong : ThucThe
{
    public Guid QuyTrinhId { get; set; }

    public QuyTrinh? QuyTrinh { get; set; }

    public Guid? BuocId { get; set; }

    public Guid HeThongTichHopId { get; set; }

    /// <summary>KHI_VAO_BUOC | KHI_HOAN_THANH | KHI_PHE_DUYET</summary>
    public string SuKien { get; set; } = "KHI_HOAN_THANH";

    public string? LoaiDuLieu { get; set; }

    public Dictionary<string, string>? CauHinhMapping { get; set; }

    /// <summary>
    /// KHONG DUOC DOC O DAU va API khong con nhan gia tri nay.
    ///
    /// Dong bo hai chieu doi mot duong GHI tu he thong ngoai vao he thong nay. API cong khai
    /// (/api/public/v1) co y chi cho DOC. Mo duong ghi phai co dac ta that cua IOC / Thi dua khen
    /// thuong (dinh dang goi tin, cach doi chieu ho so, quyen ghi den dau) — REQ-41 dang cho ben
    /// thanh pho. Giu cot lai de khong phai viet migration xoa tren CSDL dang chay.
    /// Xem docs/audit/technical-debt.md muc TD-011.
    /// </summary>
    public bool DongBoHaiChieu { get; set; }

    public short TrangThai { get; set; } = TrangThaiDanhMuc.HoatDong;
}
