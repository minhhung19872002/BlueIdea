using System.Text.Json;
using BlueIdea.Application.DanhGia;
using BlueIdea.Domain.Chung;
using BlueIdea.UnitTests.TienIch;

namespace BlueIdea.UnitTests.Scoring;

public class BoTieuChiSnapshotTests
{
    [Fact]
    public void ChuyenDoiBoTieuChi_Bao_Gom_DanhSachMucCongNhan()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();

        var dto = DichVuDanhGia.ChuyenDoiBoTieuChi(bo);

        dto.DanhSachMucCongNhan.Should().HaveCount(3);
        dto.DanhSachMucCongNhan.Should().Contain(mc => mc.Ma == "KHONG_CN" && !mc.LaDat);
        dto.DanhSachMucCongNhan.Should().Contain(mc => mc.Ma == "CAP_CO_SO" && mc.LaDat);
        dto.DanhSachMucCongNhan.Should().Contain(mc => mc.Ma == "CAP_TP" && mc.LaDat);
    }

    [Fact]
    public void ChuyenDoiBoTieuChi_Loc_MucCongNhan_DaXoa()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var mucXoa = bo.DanhSachMucCongNhan.First(m => m.Ma == "KHONG_CN");
        mucXoa.DaXoa = true;

        var dto = DichVuDanhGia.ChuyenDoiBoTieuChi(bo);

        dto.DanhSachMucCongNhan.Should().HaveCount(2);
        dto.DanhSachMucCongNhan.Should().NotContain(mc => mc.Ma == "KHONG_CN");
    }

    [Fact]
    public void ChuyenDoiBoTieuChi_Loc_MucCongNhan_NgungHoatDong()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var mucNgung = bo.DanhSachMucCongNhan.First(m => m.Ma == "CAP_CO_SO");
        mucNgung.TrangThai = TrangThaiDanhMuc.NgungHoatDong;

        var dto = DichVuDanhGia.ChuyenDoiBoTieuChi(bo);

        dto.DanhSachMucCongNhan.Should().HaveCount(2);
        dto.DanhSachMucCongNhan.Should().NotContain(mc => mc.Ma == "CAP_CO_SO");
    }

    [Fact]
    public void ChuyenDoiBoTieuChi_SapXep_MucCongNhan_Theo_DiemTu()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        bo.DanhSachMucCongNhan.Reverse();

        var dto = DichVuDanhGia.ChuyenDoiBoTieuChi(bo);

        dto.DanhSachMucCongNhan![0].DiemTu.Should().Be(0m);
        dto.DanhSachMucCongNhan[1].DiemTu.Should().Be(50m);
        dto.DanhSachMucCongNhan[2].DiemTu.Should().Be(80m);
    }

    [Fact]
    public void Snapshot_Json_RoundTrip_Giu_Nguyen_MucCongNhan()
    {
        var bo = XuongDuLieuTest.TaoBoTieuChiMau();
        var dto = DichVuDanhGia.ChuyenDoiBoTieuChi(bo);

        var json = JsonSerializer.Serialize(dto);
        var restored = JsonSerializer.Deserialize<BoTieuChiDto>(json);

        restored.Should().NotBeNull();
        restored!.DanhSachMucCongNhan.Should().HaveCount(3);

        var capTp = restored.DanhSachMucCongNhan!.First(mc => mc.Ma == "CAP_TP");
        capTp.DiemTu.Should().Be(80m);
        capTp.DiemDen.Should().Be(100m);
        capTp.LaDat.Should().BeTrue();
    }

    [Fact]
    public void Snapshot_Cu_Thieu_MucCongNhan_Deserialize_Tra_Ve_Null()
    {
        var jsonCu = """{"Id":"00000000-0000-0000-0000-000000000001","Ma":"BTC","Ten":"Test","ThangDiemToiDa":100,"DiemDatToiThieu":50,"CachTinh":"TONG_DIEM","LamTron":2,"DanhSachNhom":[]}""";

        var restored = JsonSerializer.Deserialize<BoTieuChiDto>(jsonCu);

        restored.Should().NotBeNull();
        restored!.DanhSachMucCongNhan.Should().BeNull();
    }
}
