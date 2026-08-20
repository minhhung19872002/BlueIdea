using System.Reflection;
using BlueIdea.Api.Controllers;
using BlueIdea.Domain.Chung;
using Microsoft.AspNetCore.Authorization;

namespace BlueIdea.UnitTests.Shared;

public class ChinhSachPhanQuyenControllerTests
{
    [Theory]
    [InlineData(typeof(LinhVucController), "LayDanhSachAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(LinhVucController), "LayCayAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(LinhVucController), "LayTheoIdAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(LinhVucController), "ThemAsync", MaQuyen.DanhMucThem)]
    [InlineData(typeof(LinhVucController), "SuaAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(LinhVucController), "XoaAsync", MaQuyen.DanhMucXoa)]
    [InlineData(typeof(LinhVucController), "DoiTrangThaiAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(LinhVucController), "SapXepAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(LinhVucController), "XuatExcelAsync", MaQuyen.DanhMucXuat)]
    [InlineData(typeof(DoiTuongController), "LayDanhSachAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(DoiTuongController), "LayTheoIdAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(DoiTuongController), "ThemAsync", MaQuyen.DanhMucThem)]
    [InlineData(typeof(DoiTuongController), "SuaAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(DoiTuongController), "XoaAsync", MaQuyen.DanhMucXoa)]
    [InlineData(typeof(LoaiTacGiaController), "LayDanhSachAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(LoaiTacGiaController), "LayTheoIdAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(LoaiTacGiaController), "ThemAsync", MaQuyen.DanhMucThem)]
    [InlineData(typeof(LoaiTacGiaController), "SuaAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(LoaiTacGiaController), "XoaAsync", MaQuyen.DanhMucXoa)]
    public void DanhMuc_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(DotDeNghiController), "LayDanhSachAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(DotDeNghiController), "LayDanhSachQuanLyAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(DotDeNghiController), "LayTheoIdAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(DotDeNghiController), "LayTongQuanAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(DotDeNghiController), "ThemAsync", MaQuyen.DanhMucThem)]
    [InlineData(typeof(DotDeNghiController), "SuaAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(DotDeNghiController), "XoaAsync", MaQuyen.DanhMucXoa)]
    [InlineData(typeof(DotDeNghiController), "MoDotAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(DotDeNghiController), "DongDotAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(DotDeNghiController), "KhoaDotAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(DotDeNghiController), "SaoChepAsync", MaQuyen.DanhMucThem)]
    public void DotDeNghi_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(DonViController), "LayDanhSachAsync", MaQuyen.DonViXem)]
    [InlineData(typeof(DonViController), "LayTheoIdAsync", MaQuyen.DonViXem)]
    [InlineData(typeof(DonViController), "ThemAsync", MaQuyen.DonViCauHinh)]
    [InlineData(typeof(DonViController), "SuaAsync", MaQuyen.DonViCauHinh)]
    [InlineData(typeof(DonViController), "XoaAsync", MaQuyen.DonViCauHinh)]
    [InlineData(typeof(DonViController), "ChuyenChaAsync", MaQuyen.DonViCauHinh)]
    [InlineData(typeof(DonViController), "GopAsync", MaQuyen.DonViCauHinh)]
    public void DonVi_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(BieuMauXuatController), "LayDanhSachAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(BieuMauXuatController), "LayTheoIdAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(BieuMauXuatController), "ThemAsync", MaQuyen.DanhMucThem)]
    [InlineData(typeof(BieuMauXuatController), "SuaAsync", MaQuyen.DanhMucSua)]
    [InlineData(typeof(BieuMauXuatController), "XoaAsync", MaQuyen.DanhMucXoa)]
    [InlineData(typeof(BieuMauXuatController), "LayTruongKhaDung", MaQuyen.DanhMucXem)]
    [InlineData(typeof(BieuMauXuatController), "XemTruocAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(BieuMauXuatController), "XemTruocPdfAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(BieuMauThongKeController), "LayDanhSachAsync", MaQuyen.DanhMucXem)]
    [InlineData(typeof(BieuMauThongKeController), "LayTheoIdAsync", MaQuyen.DanhMucXem)]
    // Bieu mau thong ke la cau hinh BAO CAO chu khong phai mot danh muc thong thuong: no quyet dinh
    // so lieu bao cao trong ra sao. Sua no bang quyen danh muc chung dong nghia bat ky ai them sua
    // duoc mot danh muc cung doi duoc dinh dang bao cao gui len cap tren.
    [InlineData(typeof(BieuMauThongKeController), "ThemAsync", MaQuyen.BaoCaoCauHinh)]
    [InlineData(typeof(BieuMauThongKeController), "SuaAsync", MaQuyen.BaoCaoCauHinh)]
    [InlineData(typeof(BieuMauThongKeController), "XoaAsync", MaQuyen.BaoCaoCauHinh)]
    public void BieuMau_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(BaoCaoController), "TongQuanAsync", MaQuyen.BaoCaoXem)]
    [InlineData(typeof(BaoCaoController), "SangKienDatAsync", MaQuyen.BaoCaoXem)]
    [InlineData(typeof(BaoCaoController), "SangKienChuaDatAsync", MaQuyen.BaoCaoXem)]
    [InlineData(typeof(BaoCaoController), "TheoDonViAsync", MaQuyen.BaoCaoXem)]
    [InlineData(typeof(BaoCaoController), "TheoTacGiaAsync", MaQuyen.BaoCaoXem)]
    [InlineData(typeof(BaoCaoController), "ThoiGianXuLyAsync", MaQuyen.BaoCaoXem)]
    [InlineData(typeof(BaoCaoController), "TongHopNamAsync", MaQuyen.BaoCaoXem)]
    [InlineData(typeof(BaoCaoController), "XuatSangKienDatAsync", MaQuyen.BaoCaoXuat)]
    [InlineData(typeof(BaoCaoController), "XuatSangKienChuaDatAsync", MaQuyen.BaoCaoXuat)]
    [InlineData(typeof(BaoCaoController), "XuatTheoDonViAsync", MaQuyen.BaoCaoXuat)]
    [InlineData(typeof(BaoCaoController), "XuatTheoTacGiaAsync", MaQuyen.BaoCaoXuat)]
    [InlineData(typeof(BaoCaoController), "XuatThoiGianXuLyAsync", MaQuyen.BaoCaoXuat)]
    [InlineData(typeof(BaoCaoController), "XuatTongHopNamAsync", MaQuyen.BaoCaoXuat)]
    [InlineData(typeof(BaoCaoController), "XuatPdfSangKienDatAsync", MaQuyen.BaoCaoXuat)]
    public void BaoCao_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(SangKienController), "LayDanhSachAsync", MaQuyen.SangKienXem)]
    [InlineData(typeof(SangKienController), "GoiYAsync", MaQuyen.SangKienXem)]
    [InlineData(typeof(SangKienController), "TimNguNghiaAsync", MaQuyen.SangKienXem)]
    [InlineData(typeof(SangKienController), "LayHoSoCuaToiAsync", MaQuyen.SangKienXem)]
    [InlineData(typeof(SangKienController), "LayChiTietAsync", MaQuyen.SangKienXem)]
    [InlineData(typeof(SangKienController), "LayTienDoAsync", MaQuyen.SangKienXem)]
    [InlineData(typeof(SangKienController), "LayLichSuAsync", MaQuyen.SangKienXem)]
    [InlineData(typeof(SangKienController), "LayHanhDongAsync", MaQuyen.SangKienXem)]
    [InlineData(typeof(SangKienController), "LayTrungLapAsync", MaQuyen.TrungLapXem)]
    [InlineData(typeof(SangKienController), "ChayLaiTrungLapAsync", MaQuyen.TrungLapChayLai)]
    [InlineData(typeof(SangKienController), "TaoAsync", MaQuyen.SangKienThem)]
    [InlineData(typeof(SangKienController), "CapNhatAsync", MaQuyen.SangKienSua)]
    [InlineData(typeof(SangKienController), "NopAsync", MaQuyen.SangKienNop)]
    [InlineData(typeof(SangKienController), "PhieuTiepNhanAsync", MaQuyen.SangKienXem)]
    [InlineData(typeof(SangKienController), "RutAsync", MaQuyen.SangKienRut)]
    [InlineData(typeof(SangKienController), "XoaAsync", MaQuyen.SangKienXoa)]
    // Hang cho tiep nhan phai la TIEP_NHAN.XEM: dat SANG_KIEN.XEM thi quyen tiep nhan lai tro
    // thanh mot cai ten khong chan gi, dung tinh trang ma dot ra soat nay di sua.
    [InlineData(typeof(SangKienController), "LayDanhSachChoTiepNhanAsync", MaQuyen.TiepNhanXem)]
    [InlineData(typeof(SangKienController), "XuatExcelAsync", MaQuyen.SangKienXuat)]
    public void SangKien_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(QuyTrinhController), "LayDanhSachAsync", MaQuyen.QuyTrinhXem)]
    [InlineData(typeof(QuyTrinhController), "LayTheoIdAsync", MaQuyen.QuyTrinhXem)]
    [InlineData(typeof(QuyTrinhController), "LaySoDoAsync", MaQuyen.QuyTrinhXem)]
    [InlineData(typeof(QuyTrinhController), "LayThanhPhanAsync", MaQuyen.QuyTrinhXem)]
    [InlineData(typeof(QuyTrinhController), "KiemTraAsync", MaQuyen.QuyTrinhXem)]
    [InlineData(typeof(QuyTrinhController), "LuuSoDoAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "ThemThanhPhanAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "SuaThanhPhanAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "XoaThanhPhanAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "SapXepThanhPhanAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "KichHoatAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "NgungApDungAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "SaoChepAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "TaoPhienBanMoiAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "ThemAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "SuaAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(QuyTrinhController), "XoaAsync", MaQuyen.QuyTrinhCauHinh)]
    [InlineData(typeof(TieuChiController), "LayDanhSachAsync", MaQuyen.TieuChiXem)]
    [InlineData(typeof(TieuChiController), "LayChiTietAsync", MaQuyen.TieuChiXem)]
    [InlineData(typeof(TieuChiController), "KiemTraAsync", MaQuyen.TieuChiXem)]
    [InlineData(typeof(TieuChiController), "LuuCayAsync", MaQuyen.TieuChiCauHinh)]
    [InlineData(typeof(TieuChiController), "LuuMucCongNhanAsync", MaQuyen.TieuChiCauHinh)]
    [InlineData(typeof(TieuChiController), "SaoChepAsync", MaQuyen.TieuChiCauHinh)]
    [InlineData(typeof(TieuChiController), "ThemAsync", MaQuyen.TieuChiCauHinh)]
    [InlineData(typeof(TieuChiController), "SuaAsync", MaQuyen.TieuChiCauHinh)]
    [InlineData(typeof(TieuChiController), "XoaAsync", MaQuyen.TieuChiCauHinh)]
    public void QuyTrinhVaTieuChi_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(HoiDongController), "LayDanhSachAsync", MaQuyen.HoiDongXem)]
    [InlineData(typeof(HoiDongController), "LayTheoIdAsync", MaQuyen.HoiDongXem)]
    [InlineData(typeof(HoiDongController), "LayPhienHopAsync", MaQuyen.HoiDongXem)]
    [InlineData(typeof(HoiDongController), "LayKetQuaBoPhieuAsync", MaQuyen.HoiDongXem)]
    [InlineData(typeof(HoiDongController), "ThemAsync", MaQuyen.HoiDongCauHinh)]
    [InlineData(typeof(HoiDongController), "SuaAsync", MaQuyen.HoiDongCauHinh)]
    [InlineData(typeof(HoiDongController), "XoaAsync", MaQuyen.HoiDongCauHinh)]
    [InlineData(typeof(HoiDongController), "LuuThanhVienAsync", MaQuyen.HoiDongCauHinh)]
    [InlineData(typeof(HoiDongController), "TaoPhienHopAsync", MaQuyen.HoiDongHopPhien)]
    [InlineData(typeof(HoiDongController), "DiemDanhAsync", MaQuyen.HoiDongHopPhien)]
    [InlineData(typeof(HoiDongController), "GhiYKienHoSoAsync", MaQuyen.HoiDongHopPhien)]
    [InlineData(typeof(HoiDongController), "BoPhieuAsync", MaQuyen.HoiDongBoPhieu)]
    [InlineData(typeof(HoiDongController), "KetThucPhienHopAsync", MaQuyen.HoiDongKetLuan)]
    [InlineData(typeof(BienBanHopController), "LapAsync", MaQuyen.HoiDongHopPhien)]
    [InlineData(typeof(BienBanHopController), "LayTheoPhienAsync", MaQuyen.HoiDongXem)]
    [InlineData(typeof(BienBanHopController), "LayAsync", MaQuyen.HoiDongXem)]
    [InlineData(typeof(BienBanHopController), "KyAsync", MaQuyen.HoiDongXem)]
    [InlineData(typeof(BienBanHopController), "XuatPdfAsync", MaQuyen.HoiDongXem)]
    [InlineData(typeof(BienBanHopController), "KySoAsync", MaQuyen.QuyetDinhKySo)]
    [InlineData(typeof(BienBanHopController), "LayLichSuKySoAsync", MaQuyen.HoiDongXem)]
    public void HoiDong_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(DanhGiaController), "LayViecCuaToiAsync", MaQuyen.DanhGiaChamDiem)]
    [InlineData(typeof(DanhGiaController), "PhanCongAsync", MaQuyen.DanhGiaPhanCong)]
    [InlineData(typeof(DanhGiaController), "LayPhieuAsync", MaQuyen.DanhGiaChamDiem)]
    [InlineData(typeof(DanhGiaController), "LuuNhapAsync", MaQuyen.DanhGiaChamDiem)]
    [InlineData(typeof(DanhGiaController), "GuiPhieuAsync", MaQuyen.DanhGiaChamDiem)]
    [InlineData(typeof(DanhGiaController), "MoLaiPhieuAsync", MaQuyen.DanhGiaMoLaiPhieu)]
    [InlineData(typeof(DanhGiaController), "TongHopAsync", MaQuyen.DanhGiaTongHop)]
    [InlineData(typeof(DanhGiaController), "LayMaTranDiemAsync", MaQuyen.DanhGiaTongHop)]
    [InlineData(typeof(DanhGiaController), "KySoPhieuAsync", MaQuyen.DanhGiaXem)]
    [InlineData(typeof(DanhGiaController), "LichSuKySoPhieuAsync", MaQuyen.DanhGiaXem)]
    public void DanhGia_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(XuLyController), "ThucThiAsync", MaQuyen.XuLyThucThi)]
    [InlineData(typeof(XuLyController), "ThucThiHangLoatAsync", MaQuyen.XuLyThucThi)]
    [InlineData(typeof(XuLyController), "ThuHoiAsync", MaQuyen.XuLyThuHoi)]
    public void XuLy_Action_Co_Authorize_Policy_Dung(Type loaiController, string tenAction, string maQuyen)
    {
        BatBuocCoPolicy(loaiController, tenAction, maQuyen);
    }

    [Theory]
    [InlineData(typeof(LinhVucController), "LayDanhSachChonAsync")]
    [InlineData(typeof(DoiTuongController), "LayDanhSachChonAsync")]
    [InlineData(typeof(LoaiTacGiaController), "LayDanhSachChonAsync")]
    [InlineData(typeof(DotDeNghiController), "LayDanhSachChonAsync")]
    [InlineData(typeof(DotDeNghiController), "LayDotDangMoAsync")]
    [InlineData(typeof(DonViController), "LayCayAsync")]
    [InlineData(typeof(DonViController), "LayDanhSachChonAsync")]
    [InlineData(typeof(DonViController), "LayLogoAsync")]
    [InlineData(typeof(BieuMauXuatController), "LayDanhSachChonAsync")]
    [InlineData(typeof(BieuMauThongKeController), "LayDanhSachChonAsync")]
    [InlineData(typeof(QuyTrinhController), "LayDanhSachChonAsync")]
    [InlineData(typeof(TieuChiController), "LayDanhSachChonAsync")]
    [InlineData(typeof(HoiDongController), "LayDanhSachChonAsync")]
    public void Dropdown_Va_Cong_Khai_Chi_Can_Authorize_Khong_Can_Policy(Type loaiController, string tenAction)
    {
        var classAttrs = loaiController.GetCustomAttributes<AuthorizeAttribute>().ToList();
        classAttrs.Should().NotBeEmpty(
            $"{loaiController.Name} phải có class-level [Authorize]");

        var method = loaiController.GetMethod(tenAction);
        method.Should().NotBeNull($"{loaiController.Name}.{tenAction} phải tồn tại");

        var attrs = method!.GetCustomAttributes<AuthorizeAttribute>().ToList();

        attrs.Should().BeEmpty(
            $"{loaiController.Name}.{tenAction} là endpoint dropdown/công khai — "
            + "chỉ cần class-level [Authorize], không cần Policy riêng");
    }

    private static void BatBuocCoPolicy(Type loaiController, string tenAction, string maQuyen)
    {
        var method = loaiController.GetMethod(tenAction);
        method.Should().NotBeNull($"{loaiController.Name}.{tenAction} phải tồn tại");

        var attr = method!.GetCustomAttributes<AuthorizeAttribute>()
            .FirstOrDefault(a => a.Policy == maQuyen);

        attr.Should().NotBeNull(
            $"{loaiController.Name}.{tenAction} phải có [Authorize(Policy = \"{maQuyen}\")]");
    }
}
