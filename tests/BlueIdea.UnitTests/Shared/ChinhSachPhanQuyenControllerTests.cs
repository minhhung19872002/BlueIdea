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
    [InlineData(typeof(LinhVucController), "LayDanhSachChonAsync")]
    [InlineData(typeof(DoiTuongController), "LayDanhSachChonAsync")]
    [InlineData(typeof(LoaiTacGiaController), "LayDanhSachChonAsync")]
    [InlineData(typeof(DotDeNghiController), "LayDanhSachChonAsync")]
    [InlineData(typeof(DotDeNghiController), "LayDotDangMoAsync")]
    [InlineData(typeof(DonViController), "LayCayAsync")]
    [InlineData(typeof(DonViController), "LayDanhSachChonAsync")]
    [InlineData(typeof(DonViController), "LayLogoAsync")]
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
