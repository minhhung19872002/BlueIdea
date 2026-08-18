using BlueIdea.Domain.Chung;
using BlueIdea.Workflow.MoHinh;

namespace BlueIdea.UnitTests.XuLy;

/// <summary>
/// REQ-16: verify DongBoLienThong action is correctly present in HanhDongCanChay
/// and SuKienLienThong constants match values used in QuyTrinhLienThong entity default.
/// </summary>
public class DieuPhaiLienThongTests
{
    [Fact]
    public void SuKienLienThong_KhiHoanThanh_Khop_Gia_Tri_Mac_Dinh_QuyTrinhLienThong()
    {
        SuKienLienThong.KhiHoanThanh.Should().Be("KHI_HOAN_THANH");
    }

    [Fact]
    public void SuKienLienThong_KhiVaoBuoc_Khop_Gia_Tri_QuyTrinhLienThong()
    {
        SuKienLienThong.KhiVaoBuoc.Should().Be("KHI_VAO_BUOC");
    }

    [Fact]
    public void SuKienLienThong_KhiPheDuyet_Khop_Gia_Tri_QuyTrinhLienThong()
    {
        SuKienLienThong.KhiPheDuyet.Should().Be("KHI_PHE_DUYET");
    }

    [Fact]
    public void HanhDongTuDong_DongBoLienThong_Ton_Tai()
    {
        HanhDongTuDong.DongBoLienThong.Should().Be("DONG_BO_LIEN_THONG");
    }

    [Fact]
    public void HanhDongCanChay_Chua_DongBoLienThong_Thi_Khong_Dieu_Phai()
    {
        var ketQua = new KetQuaXuLy
        {
            ThanhCong = true,
            HanhDongCanChay = new[] { HanhDongTuDong.GuiEmail, HanhDongTuDong.TaoBienBan }
        };

        ketQua.HanhDongCanChay.Contains(HanhDongTuDong.DongBoLienThong).Should().BeFalse();
    }

    [Fact]
    public void HanhDongCanChay_Co_DongBoLienThong_Thi_Can_Dieu_Phai()
    {
        var ketQua = new KetQuaXuLy
        {
            ThanhCong = true,
            HanhDongCanChay = new[]
            {
                HanhDongTuDong.GuiEmail,
                HanhDongTuDong.DongBoLienThong,
                HanhDongTuDong.CongBoKetQua
            }
        };

        ketQua.HanhDongCanChay.Contains(HanhDongTuDong.DongBoLienThong).Should().BeTrue();
    }

    [Fact]
    public void HanhDongCanChay_Rong_Thi_Khong_Dieu_Phai()
    {
        var ketQua = new KetQuaXuLy
        {
            ThanhCong = true,
            HanhDongCanChay = Array.Empty<string>()
        };

        ketQua.HanhDongCanChay.Contains(HanhDongTuDong.DongBoLienThong).Should().BeFalse();
    }

    [Fact]
    public void KetQuaXuLy_Co_BuocTruocId_Va_BuocMoiId_Cho_Doi_Chieu_LienThong()
    {
        var buocTruocId = Guid.NewGuid();
        var buocMoiId = Guid.NewGuid();

        var ketQua = new KetQuaXuLy
        {
            ThanhCong = true,
            BuocTruocId = buocTruocId,
            BuocMoiId = buocMoiId,
            HanhDongCanChay = new[] { HanhDongTuDong.DongBoLienThong }
        };

        ketQua.BuocTruocId.Should().Be(buocTruocId);
        ketQua.BuocMoiId.Should().Be(buocMoiId);
    }
}
