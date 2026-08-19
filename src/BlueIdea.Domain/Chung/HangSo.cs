namespace BlueIdea.Domain.Chung;

/// <summary>Ma vai tro chuan (seed data - admin co the tao them vai tro moi).</summary>
public static class MaVaiTro
{
    public const string TacGia = "TAC_GIA";
    public const string CanBoTiepNhan = "CAN_BO_TIEP_NHAN";
    public const string ThuKyHoiDong = "THU_KY_HOI_DONG";
    public const string ThanhVienHoiDong = "THANH_VIEN_HOI_DONG";
    public const string ChuTichHoiDong = "CHU_TICH_HOI_DONG";
    public const string LanhDaoPheDuyet = "LANH_DAO_PHE_DUYET";
    public const string QuanTriDonVi = "QUAN_TRI_DON_VI";
    public const string QuanTriHeThong = "QUAN_TRI_HE_THONG";
    public const string LanhDaoXem = "LANH_DAO_XEM";

    public static readonly IReadOnlyList<string> TatCa = new[]
    {
        TacGia, CanBoTiepNhan, ThuKyHoiDong, ThanhVienHoiDong, ChuTichHoiDong,
        LanhDaoPheDuyet, QuanTriDonVi, QuanTriHeThong, LanhDaoXem
    };
}

/// <summary>
/// Ma quyen chi tiet dang MODULE.HANH_DONG. Duoc seed vao bang <c>quyen</c>
/// va kiem tra qua IPermissionService tren tung command.
/// </summary>
public static class MaQuyen
{
    // Danh muc
    public const string DanhMucXem = "DANH_MUC.XEM";
    public const string DanhMucThem = "DANH_MUC.THEM";
    public const string DanhMucSua = "DANH_MUC.SUA";
    public const string DanhMucXoa = "DANH_MUC.XOA";
    public const string DanhMucXuat = "DANH_MUC.XUAT";
    public const string DanhMucNhap = "DANH_MUC.NHAP";

    // Sang kien
    public const string SangKienXem = "SANG_KIEN.XEM";
    public const string SangKienThem = "SANG_KIEN.THEM";
    public const string SangKienSua = "SANG_KIEN.SUA";
    public const string SangKienXoa = "SANG_KIEN.XOA";
    public const string SangKienNop = "SANG_KIEN.NOP";
    public const string SangKienRut = "SANG_KIEN.RUT";
    public const string SangKienXuat = "SANG_KIEN.XUAT";
    public const string SangKienXemTatCa = "SANG_KIEN.XEM_TAT_CA";

    // Tiep nhan - Xu ly
    public const string TiepNhanXem = "TIEP_NHAN.XEM";
    public const string TiepNhanXuLy = "TIEP_NHAN.XU_LY";
    public const string XuLyXem = "XU_LY.XEM";
    public const string XuLyThucThi = "XU_LY.THUC_THI";
    public const string XuLyThuHoi = "XU_LY.THU_HOI";
    public const string XuLyUyQuyen = "XU_LY.UY_QUYEN";

    // Danh gia
    public const string DanhGiaXem = "DANH_GIA.XEM";
    public const string DanhGiaChamDiem = "DANH_GIA.CHAM_DIEM";
    public const string DanhGiaPhanCong = "DANH_GIA.PHAN_CONG";
    public const string DanhGiaTongHop = "DANH_GIA.TONG_HOP";
    public const string DanhGiaMoLaiPhieu = "DANH_GIA.MO_LAI_PHIEU";

    // Hoi dong
    public const string HoiDongXem = "HOI_DONG.XEM";
    public const string HoiDongCauHinh = "HOI_DONG.CAU_HINH";
    public const string HoiDongHopPhien = "HOI_DONG.HOP_PHIEN";
    public const string HoiDongBoPhieu = "HOI_DONG.BO_PHIEU";
    public const string HoiDongKetLuan = "HOI_DONG.KET_LUAN";

    // Quy trinh - Tieu chi
    public const string QuyTrinhXem = "QUY_TRINH.XEM";
    public const string QuyTrinhCauHinh = "QUY_TRINH.CAU_HINH";
    public const string TieuChiXem = "TIEU_CHI.XEM";
    public const string TieuChiCauHinh = "TIEU_CHI.CAU_HINH";

    // Quyet dinh
    public const string QuyetDinhXem = "QUYET_DINH.XEM";
    public const string QuyetDinhBanHanh = "QUYET_DINH.BAN_HANH";
    public const string QuyetDinhKySo = "QUYET_DINH.KY_SO";

    // Bao cao
    public const string BaoCaoXem = "BAO_CAO.XEM";
    public const string BaoCaoXuat = "BAO_CAO.XUAT";
    public const string BaoCaoCauHinh = "BAO_CAO.CAU_HINH";

    // Kiem tra trung lap
    public const string TrungLapXem = "TRUNG_LAP.XEM";
    public const string TrungLapChayLai = "TRUNG_LAP.CHAY_LAI";
    public const string TrungLapXemXet = "TRUNG_LAP.XEM_XET";

    // Quan tri
    public const string NguoiDungXem = "NGUOI_DUNG.XEM";
    public const string NguoiDungThem = "NGUOI_DUNG.THEM";
    public const string NguoiDungSua = "NGUOI_DUNG.SUA";
    public const string NguoiDungXoa = "NGUOI_DUNG.XOA";
    public const string NguoiDungDatLaiMatKhau = "NGUOI_DUNG.DAT_LAI_MAT_KHAU";
    public const string DonViXem = "DON_VI.XEM";
    public const string DonViCauHinh = "DON_VI.CAU_HINH";
    public const string VaiTroXem = "VAI_TRO.XEM";
    public const string VaiTroCauHinh = "VAI_TRO.CAU_HINH";
    public const string CauHinhXem = "CAU_HINH.XEM";
    public const string CauHinhSua = "CAU_HINH.SUA";
    public const string NhatKyXem = "NHAT_KY.XEM";
    public const string TichHopCauHinh = "TICH_HOP.CAU_HINH";
    public const string TichHopDongBo = "TICH_HOP.DONG_BO";
}

/// <summary>Khoa cau hinh he thong (bang cau_hinh_he_thong).</summary>
public static class KhoaCauHinh
{
    public const string TenHeThong = "TEN_HE_THONG";
    public const string LogoId = "LOGO_ID";
    public const string FaviconId = "FAVICON_ID";
    public const string MauChuDao = "MAU_CHU_DAO";
    public const string TenDonVi = "TEN_DON_VI";
    public const string DiaChi = "DIA_CHI";
    public const string EmailHoTro = "EMAIL_HO_TRO";
    public const string DienThoaiHoTro = "DIEN_THOAI_HO_TRO";
    public const string MauMaHoSo = "MAU_MA_HO_SO";
    public const string MucCanhBaoTrungLapVang = "MUC_CANH_BAO_TRUNG_LAP_VANG";
    public const string MucCanhBaoTrungLapDo = "MUC_CANH_BAO_TRUNG_LAP_DO";
    public const string SoNgayNhacTruocHan = "SO_NGAY_NHAC_TRUOC_HAN";
    public const string DungLuongTepToiDaMb = "DUNG_LUONG_TEP_TOI_DA_MB";
    public const string SoTepToiDa = "SO_TEP_TOI_DA";
    public const string HeSoTuVung = "HE_SO_TU_VUNG";
    public const string HeSoNguNghia = "HE_SO_NGU_NGHIA";
    public const string TuDongKiemTraTrungLap = "TU_DONG_KIEM_TRA_TRUNG_LAP";
    public const string ChinhSachMatKhauDoDaiToiThieu = "MAT_KHAU_DO_DAI_TOI_THIEU";
    public const string ChinhSachMatKhauSoNgayHetHan = "MAT_KHAU_SO_NGAY_HET_HAN";
    public const string ChinhSachMatKhauSoLanKhongTrung = "MAT_KHAU_SO_LAN_KHONG_TRUNG";
    public const string SoLanDangNhapSaiToiDa = "SO_LAN_DANG_NHAP_SAI_TOI_DA";

    /// <summary>So lan dang nhap sai truoc khi bat buoc nhap CAPTCHA. Dat 0 de tat CAPTCHA.</summary>
    public const string SoLanSaiCanCaptcha = "SO_LAN_SAI_CAN_CAPTCHA";
    public const string ThoiGianKhoaTaiKhoanPhut = "THOI_GIAN_KHOA_TAI_KHOAN_PHUT";

    /// <summary>
    /// Cho phep tu tao tai khoan khi nguoi dung dang nhap SSO lan dau.
    /// Mac dinh TAT: co quan nha nuoc thuong muon kiem soat ai duoc vao he thong.
    /// </summary>
    public const string SsoTuDongTaoTaiKhoan = "SSO_TU_DONG_TAO_TAI_KHOAN";

    /// <summary>
    /// Buoc vai tro quan tri he thong phai bat xac thuc hai lop moi dung duoc cac chuc nang khac.
    ///
    /// Mac dinh TAT. Bat len roi ma quan tri vien khong dung duoc ung dung xac thuc thi ho se kep
    /// o man hinh cai dat MFA — go bang cach doi lai khoa nay trong bang cau hinh.
    /// </summary>
    public const string MfaBatBuocQuanTri = "MFA_BAT_BUOC_QUAN_TRI";

    /// <summary>
    /// Danh sach IP/CIDR duoc phep dung tai khoan quan tri, ngan cach bang dau phay.
    /// De trong = khong gioi han (mac dinh).
    /// </summary>
    public const string IpChoPhepQuanTri = "IP_CHO_PHEP_QUAN_TRI";
}

/// <summary>Trang thai tong cua ho so sang kien.</summary>
public static class TrangThaiTongHoSo
{
    public const string Nhap = "NHAP";
    public const string DaNop = "DA_NOP";
    public const string DangXuLy = "DANG_XU_LY";
    public const string YeuCauBoSung = "YEU_CAU_BO_SUNG";
    public const string DaPheDuyet = "DA_PHE_DUYET";
    public const string KhongDat = "KHONG_DAT";
    public const string DaRut = "DA_RUT";
    public const string DaHuy = "DA_HUY";
}

/// <summary>Trang thai cua dot de nghi.</summary>
public static class TrangThaiDot
{
    public const string Nhap = "NHAP";
    public const string DangMo = "DANG_MO";
    public const string DaDong = "DA_DONG";
    public const string DaKhoa = "DA_KHOA";
}

/// <summary>Trang thai vong doi cua quy trinh.</summary>
public static class TrangThaiQuyTrinh
{
    public const string Nhap = "NHAP";
    public const string DangApDung = "DANG_AP_DUNG";
    public const string NgungApDung = "NGUNG_AP_DUNG";
}

/// <summary>Loai buoc trong quy trinh dong.</summary>
public static class LoaiBuoc
{
    public const string TiepNhan = "TIEP_NHAN";
    public const string ThamDinh = "THAM_DINH";
    public const string PhanCongCham = "PHAN_CONG_CHAM";
    public const string ChamDiem = "CHAM_DIEM";
    public const string HopHoiDong = "HOP_HOI_DONG";
    public const string BoPhieu = "BO_PHIEU";
    public const string PheDuyet = "PHE_DUYET";
    public const string BanHanhQuyetDinh = "BAN_HANH_QUYET_DINH";
    public const string CongBo = "CONG_BO";
    public const string KetThuc = "KET_THUC";

    public static readonly IReadOnlyList<string> TatCa = new[]
    {
        TiepNhan, ThamDinh, PhanCongCham, ChamDiem, HopHoiDong,
        BoPhieu, PheDuyet, BanHanhQuyetDinh, CongBo, KetThuc
    };
}

/// <summary>Loai tac nhan xu ly buoc.</summary>
public static class LoaiTacNhan
{
    public const string NguoiDung = "NGUOI_DUNG";
    public const string VaiTro = "VAI_TRO";
    public const string DonVi = "DON_VI";
    public const string HoiDong = "HOI_DONG";
    public const string ChucDanhHoiDong = "CHUC_DANH_HOI_DONG";
    public const string NguoiTaoHoSo = "NGUOI_TAO_HO_SO";
    public const string LanhDaoDonViTacGia = "LANH_DAO_DON_VI_TAC_GIA";
}

/// <summary>Quy tac xu ly khi buoc co nhieu tac nhan.</summary>
public static class QuyTacXuLy
{
    public const string MotNguoi = "MOT_NGUOI";
    public const string TatCa = "TAT_CA";
    public const string DaSo = "DA_SO";
    public const string ChuTichQuyetDinh = "CHU_TICH_QUYET_DINH";
}

/// <summary>Cach tinh diem cua bo tieu chi.</summary>
public static class CachTinhDiem
{
    public const string TongDiem = "TONG_DIEM";
    public const string TrungBinhCong = "TRUNG_BINH_CONG";
    public const string TrungBinhTrongSo = "TRUNG_BINH_TRONG_SO";
}

/// <summary>Kieu nhap lieu cua mot tieu chi cham diem.</summary>
public static class KieuNhapTieuChi
{
    public const string NhapSo = "NHAP_SO";
    public const string ThangDiem = "THANG_DIEM";
    public const string LuaChon = "LUA_CHON";
    public const string CoKhong = "CO_KHONG";
}

/// <summary>Chuc danh thanh vien hoi dong.</summary>
public static class ChucDanhHoiDong
{
    public const string ChuTich = "CHU_TICH";
    public const string PhoChuTich = "PHO_CHU_TICH";
    public const string UyVien = "UY_VIEN";
    public const string UyVienThuKy = "UY_VIEN_THU_KY";
    public const string ThuKy = "THU_KY";
}

/// <summary>Muc canh bao ket qua kiem tra trung lap.</summary>
public static class MucCanhBaoTrungLap
{
    public const string AnToan = "AN_TOAN";
    public const string CanhBao = "CANH_BAO";
    public const string NghiemTrong = "NGHIEM_TRONG";
}

/// <summary>Cap xet duyet.</summary>
public static class CapXetDuyet
{
    public const string CoSo = "CO_SO";
    public const string ThanhPho = "THANH_PHO";
    public const string Tinh = "TINH";
}

/// <summary>Ma truong hop chuyen tiep chuan (admin co the them ma khac).</summary>
public static class MaTruongHop
{
    public const string Dat = "DAT";
    public const string KhongDat = "KHONG_DAT";
    public const string BoSungHoSo = "BO_SUNG_HO_SO";
    public const string ChuyenCapCaoHon = "CHUYEN_CAP_CAO_HON";
    public const string TraLai = "TRA_LAI";
    public const string RutHoSo = "RUT_HO_SO";
}

/// <summary>Trang thai trich xuat van ban (OCR) cua mot tep dinh kem.</summary>
public static class TrangThaiOcrTep
{
    public const string ChuaXuLy = "CHUA_XU_LY";
    public const string DangXuLy = "DANG_XU_LY";
    public const string HoanThanh = "HOAN_THANH";

    /// <summary>Dinh dang khong rut duoc van ban (vi du .zip, .xlsx) - khong phai loi.</summary>
    public const string KhongCan = "KHONG_CAN";

    public const string Loi = "LOI";
}

/// <summary>Trang thai luot phan cong cham diem cho thanh vien hoi dong.</summary>
public static class TrangThaiPhanCongCham
{
    public const string ChuaCham = "CHUA_CHAM";
    public const string DangCham = "DANG_CHAM";
    public const string DaCham = "DA_CHAM";
    public const string QuaHan = "QUA_HAN";
}

/// <summary>Trang thai ban tin trong hang doi gui email/SMS.</summary>
public static class TrangThaiGuiTin
{
    public const string ChoGui = "CHO_GUI";
    public const string DangGui = "DANG_GUI";
    public const string DaGui = "DA_GUI";
    public const string Loi = "LOI";
}

/// <summary>Hanh dong tu dong chay khi di qua mot truong hop chuyen tiep.</summary>
public static class HanhDongTuDong
{
    public const string GuiEmail = "GUI_EMAIL";
    public const string GuiSms = "GUI_SMS";
    public const string TaoQuyetDinh = "TAO_QUYET_DINH";
    public const string CapNhatKetQua = "CAP_NHAT_KET_QUA";
    public const string YeuCauKySo = "YEU_CAU_KY_SO";
    public const string DongBoLienThong = "DONG_BO_LIEN_THONG";
    public const string KiemTraTrungLap = "KIEM_TRA_TRUNG_LAP";
    public const string TaoBienBan = "TAO_BIEN_BAN";
    public const string PhanCongCham = "PHAN_CONG_CHAM";
    public const string CongBoKetQua = "CONG_BO_KET_QUA";
}

/// <summary>Su kien lien thong — khi nao dong bo du lieu sang he thong ngoai (QuyTrinhLienThong.SuKien).</summary>
public static class SuKienLienThong
{
    public const string KhiVaoBuoc = "KHI_VAO_BUOC";
    public const string KhiHoanThanh = "KHI_HOAN_THANH";
    public const string KhiPheDuyet = "KHI_PHE_DUYET";
}
