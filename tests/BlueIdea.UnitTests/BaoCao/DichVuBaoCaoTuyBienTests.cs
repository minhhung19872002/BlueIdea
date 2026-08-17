using BlueIdea.Application.BaoCao;
using BlueIdea.Domain.DanhMuc;

namespace BlueIdea.UnitTests.BaoCao;

/// <summary>
/// Kiem thu phan KIEM TRA CAU HINH cua bao cao tuy bien.
///
/// Day la ranh gioi an toan quan trong nhat cua chuc nang nay: nguon du lieu do quan tri vien nhap
/// tren giao dien, neu khong chan bang bang trang thi cau hinh sai se vo luc chay - hoac te hon,
/// tro thanh duong dan de doc truong khong duoc phep.
/// </summary>
public sealed class DichVuBaoCaoTuyBienTests
{
    [Fact]
    public void Cau_Hinh_Hop_Le_Thi_Khong_Bao_Loi()
    {
        var cot = new List<CotBaoCao>
        {
            new() { Ma = "ma", TieuDe = "Mã hồ sơ", Nguon = "maHoSo", ThuTu = 1 },
            new() { Ma = "ten", TieuDe = "Tên sáng kiến", Nguon = "tenSangKien", ThuTu = 2 },
            new() { Ma = "diem", TieuDe = "Tổng điểm", Nguon = "tongDiem", HamTongHop = "TRUNG_BINH", ThuTu = 3 }
        };

        DichVuBaoCaoTuyBien.KiemTraCauHinh(cot).Should().BeEmpty();
    }

    [Fact]
    public void Bao_Cao_Khong_Co_Cot_Bi_Bao_Loi()
    {
        var loi = DichVuBaoCaoTuyBien.KiemTraCauHinh(new List<CotBaoCao>());

        loi.Should().ContainSingle()
            .Which.Should().Contain("ít nhất một cột");
    }

    [Theory]
    [InlineData("khongTonTai")]
    [InlineData("matKhauHash")]
    [InlineData("soCccd")]
    [InlineData("")]
    public void Nguon_Ngoai_Bang_Trang_Bi_Tu_Choi(string nguon)
    {
        var cot = new List<CotBaoCao>
        {
            new() { Ma = "x", TieuDe = "Cột thử", Nguon = nguon, ThuTu = 1 }
        };

        var loi = DichVuBaoCaoTuyBien.KiemTraCauHinh(cot);

        loi.Should().ContainSingle()
            .Which.Should().Contain("không nằm trong danh sách cho phép");
    }

    [Fact]
    public void Ham_Tong_Hop_La_Bi_Tu_Choi()
    {
        var cot = new List<CotBaoCao>
        {
            new() { Ma = "d", TieuDe = "Điểm", Nguon = "tongDiem", HamTongHop = "DROP_TABLE", ThuTu = 1 }
        };

        var loi = DichVuBaoCaoTuyBien.KiemTraCauHinh(cot);

        loi.Should().ContainSingle()
            .Which.Should().Contain("không hợp lệ");
    }

    [Fact]
    public void Ma_Cot_Trung_Bi_Bao_Loi()
    {
        var cot = new List<CotBaoCao>
        {
            new() { Ma = "x", TieuDe = "Mã hồ sơ", Nguon = "maHoSo", ThuTu = 1 },
            new() { Ma = "x", TieuDe = "Tên", Nguon = "tenSangKien", ThuTu = 2 }
        };

        DichVuBaoCaoTuyBien.KiemTraCauHinh(cot).Should()
            .ContainSingle(x => x.Contains("bị trùng"));
    }

    [Fact]
    public void Danh_Sach_Nguon_Kha_Dung_Khop_Voi_Nguon_Duoc_Chap_Nhan()
    {
        // Man hinh cau hinh doc danh sach nay; neu lech voi bang trang thi quan tri vien se chon
        // duoc mot truong ma luc chay lai bao loi.
        var khaDung = DichVuBaoCaoTuyBien.NguonDuLieuKhaDung();

        khaDung.Should().NotBeEmpty();

        var cot = khaDung
            .Select((x, i) => new CotBaoCao { Ma = x.Ma, TieuDe = x.TieuDeGoiY, Nguon = x.Ma, ThuTu = i })
            .ToList();

        DichVuBaoCaoTuyBien.KiemTraCauHinh(cot).Should().BeEmpty();
    }

    [Fact]
    public void Khong_Lo_Truong_Nhay_Cam_Ra_Bao_Cao()
    {
        var ma = DichVuBaoCaoTuyBien.NguonDuLieuKhaDung().Select(x => x.Ma).ToList();

        ma.Should().NotContain(x =>
            x.Contains("matKhau", StringComparison.OrdinalIgnoreCase)
            || x.Contains("cccd", StringComparison.OrdinalIgnoreCase)
            || x.Contains("token", StringComparison.OrdinalIgnoreCase)
            || x.Contains("secret", StringComparison.OrdinalIgnoreCase));
    }
}
