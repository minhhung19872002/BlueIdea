using BlueIdea.Domain.Chung;
using BlueIdea.Domain.SangKien;
using BlueIdea.Workflow.MoHinh;

namespace BlueIdea.UnitTests.XuLy;

public class DichVuDieuPhaiHanhDongTests
{
    [Fact]
    public void HanhDongTuDong_Co_Du_10_Ma_Hanh_Dong()
    {
        var tat_ca = new[]
        {
            HanhDongTuDong.GuiEmail,
            HanhDongTuDong.GuiSms,
            HanhDongTuDong.TaoQuyetDinh,
            HanhDongTuDong.CapNhatKetQua,
            HanhDongTuDong.YeuCauKySo,
            HanhDongTuDong.DongBoLienThong,
            HanhDongTuDong.KiemTraTrungLap,
            HanhDongTuDong.TaoBienBan,
            HanhDongTuDong.PhanCongCham,
            HanhDongTuDong.CongBoKetQua
        };

        tat_ca.Should().HaveCount(10);
        tat_ca.Distinct().Should().HaveCount(10);
    }

    [Fact]
    public void HanhDongTuDong_DongBoLienThong_Dung_Gia_Tri()
    {
        HanhDongTuDong.DongBoLienThong.Should().Be("DONG_BO_LIEN_THONG");
    }

    [Fact]
    public void HanhDongTuDong_KiemTraTrungLap_Dung_Gia_Tri()
    {
        HanhDongTuDong.KiemTraTrungLap.Should().Be("KIEM_TRA_TRUNG_LAP");
    }

    [Fact]
    public void HanhDongTuDong_CapNhatKetQua_Dung_Gia_Tri()
    {
        HanhDongTuDong.CapNhatKetQua.Should().Be("CAP_NHAT_KET_QUA");
    }

    [Fact]
    public void HanhDongCanChay_Rong_Khong_Co_Gi_De_Dieu_Phai()
    {
        var ketQua = new KetQuaXuLy
        {
            ThanhCong = true,
            HanhDongCanChay = Array.Empty<string>()
        };

        ketQua.HanhDongCanChay.Should().BeEmpty();
    }

    [Fact]
    public void HanhDongCanChay_Chi_GuiEmail_GuiSms_Khong_Dieu_Phai_Qua_Day()
    {
        var ketQua = new KetQuaXuLy
        {
            ThanhCong = true,
            HanhDongCanChay = new[] { HanhDongTuDong.GuiEmail, HanhDongTuDong.GuiSms }
        };

        var canDieuPhai = ketQua.HanhDongCanChay
            .Where(h => h is not (HanhDongTuDong.GuiEmail or HanhDongTuDong.GuiSms));

        canDieuPhai.Should().BeEmpty();
    }

    [Fact]
    public void KetQuaXetDuyetGiaTri_Dat_Dung_Gia_Tri()
    {
        KetQuaXetDuyetGiaTri.Dat.Should().Be("DAT");
    }

    [Fact]
    public void KetQuaXetDuyetGiaTri_KhongDat_Dung_Gia_Tri()
    {
        KetQuaXetDuyetGiaTri.KhongDat.Should().Be("KHONG_DAT");
    }

    [Fact]
    public void HanhDongCanChay_Pha_Tron_Nhieu_Hanh_Dong_Deu_Duoc_Liet_Ke()
    {
        var ketQua = new KetQuaXuLy
        {
            ThanhCong = true,
            HanhDongCanChay = new[]
            {
                HanhDongTuDong.KiemTraTrungLap,
                HanhDongTuDong.CapNhatKetQua,
                HanhDongTuDong.DongBoLienThong,
                HanhDongTuDong.PhanCongCham
            }
        };

        ketQua.HanhDongCanChay.Should().HaveCount(4);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.KiemTraTrungLap);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.CapNhatKetQua);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.DongBoLienThong);
        ketQua.HanhDongCanChay.Should().Contain(HanhDongTuDong.PhanCongCham);
    }

    [Fact]
    public void HanhDongTuDong_Chua_Tat_Ca_5_Hanh_Dong_Chua_Trien_Khai()
    {
        var chuaTrienKhai = new[]
        {
            HanhDongTuDong.TaoQuyetDinh,
            HanhDongTuDong.YeuCauKySo,
            HanhDongTuDong.TaoBienBan,
            HanhDongTuDong.PhanCongCham,
            HanhDongTuDong.CongBoKetQua
        };

        chuaTrienKhai.Should().HaveCount(5);
        chuaTrienKhai.Should().OnlyHaveUniqueItems();
    }
}
