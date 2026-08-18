using BlueIdea.Application.XuLy;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;

namespace BlueIdea.UnitTests.Workflow;

/// <summary>
/// Kiem thu anh xa trang thai ho so -> su kien thong bao.
///
/// Truoc day ho so KHONG_DAT dung chung su kien CO_KET_QUA voi moi truong hop khac, nen mau
/// MTB_TU_CHOI (neu ro ly do khong duoc tiep nhan) nam trong he thong ma khong bao gio duoc gui:
/// tac gia bi loai chi nhan mot dong "da co ket qua" khong noi vi sao.
/// </summary>
public class SuKienThongBaoTheoTrangThaiTests
{
    [Fact]
    public void Ho_So_Khong_Dat_Phat_Su_Kien_Bi_Tu_Choi()
        => ThucThiBuocCommandHandler.SuKienTheoTrangThai(TrangThaiTongHoSo.KhongDat)
            .Should().Be(SuKienThongBao.HoSoBiTuChoi);

    [Fact]
    public void Ho_So_Duoc_Duyet_Phat_Su_Kien_Da_Phe_Duyet()
        => ThucThiBuocCommandHandler.SuKienTheoTrangThai(TrangThaiTongHoSo.DaPheDuyet)
            .Should().Be(SuKienThongBao.DaPheDuyet);

    [Fact]
    public void Ho_So_Can_Bo_Sung_Phat_Su_Kien_Yeu_Cau_Bo_Sung()
        => ThucThiBuocCommandHandler.SuKienTheoTrangThai(TrangThaiTongHoSo.YeuCauBoSung)
            .Should().Be(SuKienThongBao.YeuCauBoSung);

    [Theory]
    [InlineData(TrangThaiTongHoSo.DangXuLy)]
    [InlineData(TrangThaiTongHoSo.DaNop)]
    [InlineData(null)]
    public void Cac_Trang_Thai_Con_Lai_Bao_Da_Tiep_Nhan(string? trangThai)
        => ThucThiBuocCommandHandler.SuKienTheoTrangThai(trangThai)
            .Should().Be(SuKienThongBao.HoSoDuocTiepNhan);

    /// <summary>
    /// Moi su kien ma anh xa nay tra ve deu phai nam trong danh sach su kien he thong cong bo:
    /// tra ve mot ma khong co trong danh sach thi mau thong bao khong bao gio khop, va thong bao
    /// im lang bien mat thay vi bao loi.
    /// </summary>
    [Fact]
    public void Moi_Su_Kien_Tra_Ve_Deu_Duoc_He_Thong_Khai_Bao()
    {
        string?[] trangThai =
        {
            TrangThaiTongHoSo.KhongDat, TrangThaiTongHoSo.DaPheDuyet,
            TrangThaiTongHoSo.YeuCauBoSung, TrangThaiTongHoSo.DangXuLy,
            TrangThaiTongHoSo.DaNop, TrangThaiTongHoSo.DaRut, null
        };

        foreach (var tt in trangThai)
        {
            SuKienThongBao.TatCa.Should()
                .Contain(ThucThiBuocCommandHandler.SuKienTheoTrangThai(tt));
        }
    }
}
