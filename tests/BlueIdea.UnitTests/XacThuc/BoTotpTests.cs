using System.Text;
using BlueIdea.Application.XacThuc;

namespace BlueIdea.UnitTests.XacThuc;

/// <summary>
/// Kiem thu bo sinh ma TOTP.
///
/// Bo vector mau lay tu Phu luc B cua RFC 6238. Day la phep kiem quan trong nhat cua ca lop:
/// neu thuat toan lech du chi mot bit, he thong van chay tron tru voi chinh no nhung ung dung
/// xac thuc tren dien thoai nguoi dung se KHONG BAO GIO khop — va loi chi lo ra khi quan tri
/// vien da bat MFA va tu khoa minh ra ngoai.
/// </summary>
public sealed class BoTotpTests
{
    /// <summary>Bi mat mau cua RFC 6238: chuoi ASCII "12345678901234567890" ma hoa Base32.</summary>
    private static readonly string BiMatMau =
        BoTotp.MaHoaBase32(Encoding.ASCII.GetBytes("12345678901234567890"));

    [Theory]
    // (thoi diem Unix, ma mong doi) — cot SHA1 trong bang Phu luc B, cat lay 6 chu so cuoi.
    [InlineData(59L, "287082")]
    [InlineData(1111111109L, "081804")]
    [InlineData(1111111111L, "050471")]
    [InlineData(1234567890L, "005924")]
    [InlineData(2000000000L, "279037")]
    [InlineData(20000000000L, "353130")]
    public void Sinh_Ma_Khop_Vector_Mau_RFC_6238(long giayUnix, string maMongDoi)
    {
        var buoc = BoTotp.TinhBuoc(DateTimeOffset.FromUnixTimeSeconds(giayUnix));

        BoTotp.SinhMa(BiMatMau, buoc).Should().Be(maMongDoi);
    }

    [Fact]
    public void Base32_Ma_Hoa_Roi_Giai_Ma_Ra_Chinh_No()
    {
        var goc = Encoding.ASCII.GetBytes("12345678901234567890");

        BoTotp.GiaiMaBase32(BoTotp.MaHoaBase32(goc)).Should().Equal(goc);
    }

    [Fact]
    public void Chap_Nhan_Ma_Lech_Mot_Buoc_Ve_Hai_Phia()
    {
        var bayGio = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var buoc = BoTotp.TinhBuoc(bayGio);

        // Dong ho dien thoai cham/nhanh 30 giay la chuyen binh thuong; tu choi thang thi nguoi
        // dung bi chan dang nhap ma khong hieu vi sao.
        BoTotp.KiemTra(BiMatMau, BoTotp.SinhMa(BiMatMau, buoc - 1), bayGio).Should().Be(buoc - 1);
        BoTotp.KiemTra(BiMatMau, BoTotp.SinhMa(BiMatMau, buoc), bayGio).Should().Be(buoc);
        BoTotp.KiemTra(BiMatMau, BoTotp.SinhMa(BiMatMau, buoc + 1), bayGio).Should().Be(buoc + 1);
    }

    [Fact]
    public void Tu_Choi_Ma_Lech_Qua_Xa()
    {
        var bayGio = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var buoc = BoTotp.TinhBuoc(bayGio);

        BoTotp.KiemTra(BiMatMau, BoTotp.SinhMa(BiMatMau, buoc - 2), bayGio).Should().BeNull();
        BoTotp.KiemTra(BiMatMau, BoTotp.SinhMa(BiMatMau, buoc + 2), bayGio).Should().BeNull();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("12345")]      // thieu chu so
    [InlineData("1234567")]    // thua chu so
    [InlineData("12345a")]     // co chu cai
    public void Tu_Choi_Ma_Sai_Dinh_Dang(string? ma)
    {
        BoTotp.KiemTra(BiMatMau, ma, DateTimeOffset.UtcNow).Should().BeNull();
    }

    [Fact]
    public void Chap_Nhan_Ma_Co_Khoang_Trang_Nguoi_Dung_Go_Vao()
    {
        var bayGio = DateTimeOffset.FromUnixTimeSeconds(1_700_000_000);
        var ma = BoTotp.SinhMa(BiMatMau, BoTotp.TinhBuoc(bayGio));

        // Ung dung xac thuc hien ma dang "123 456"; nguoi dung sao chep ca khoang trang.
        var coKhoangTrang = ma[..3] + " " + ma[3..];

        BoTotp.KiemTra(BiMatMau, coKhoangTrang, bayGio).Should().NotBeNull();
    }

    [Fact]
    public void Moi_Bi_Mat_Sinh_Ra_Deu_Khac_Nhau_Va_Giai_Ma_Duoc()
    {
        var bo = Enumerable.Range(0, 50).Select(_ => BoTotp.TaoBiMat()).ToList();

        bo.Distinct().Should().HaveCount(50);
        bo.Should().AllSatisfy(x => BoTotp.GiaiMaBase32(x).Should().HaveCount(20));
    }

    [Fact]
    public void Uri_Ghi_Danh_Ma_Hoa_Ky_Tu_Dac_Biet()
    {
        var uri = BoTotp.TaoUriGhiDanh("nguyen.van.a", "Nền tảng Sáng kiến", "ABCDEFGH");

        uri.Should().StartWith("otpauth://totp/");
        uri.Should().Contain("secret=ABCDEFGH");
        uri.Should().Contain("digits=6").And.Contain("period=30").And.Contain("algorithm=SHA1");

        // Ten he thong co dau va khoang trang: khong ma hoa thi ung dung xac thuc doc sai nhan.
        uri.Should().NotContain(" ");
    }
}
