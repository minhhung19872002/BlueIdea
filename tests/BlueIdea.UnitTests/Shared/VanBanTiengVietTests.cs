using System.Text;
using BlueIdea.Shared.TiengViet;

namespace BlueIdea.UnitTests.Shared;

/// <summary>Kiem thu tien ich tieng Viet - nen tang cua tim kiem khong dau va so khop trung lap.</summary>
public class VanBanTiengVietTests
{
    [Theory]
    [InlineData("Sáng kiến", "Sang kien")]
    [InlineData("Đường Đặng Thùy Trâm", "Duong Dang Thuy Tram")]
    [InlineData("Ủy ban nhân dân", "Uy ban nhan dan")]
    [InlineData("Nguyễn Thị Ánh Nguyệt", "Nguyen Thi Anh Nguyet")]
    [InlineData("ĐỖ VĂN HỢP", "DO VAN HOP")]
    [InlineData("Quản lý hồ sơ một cửa", "Quan ly ho so mot cua")]
    public void Bo_Dau_Dung_Cho_Toan_Bo_Bang_Chu_Tieng_Viet(string coDau, string mongDoi)
    {
        VanBanTiengViet.BoDau(coDau).Should().Be(mongDoi);
    }

    [Fact]
    public void Bo_Dau_Xu_Ly_Duoc_Ca_Chuoi_Dang_NFD()
    {
        // Van ban tu macOS thuong o dang NFD (ky tu + dau thanh tach roi).
        var nfd = "Sáng kiến".Normalize(NormalizationForm.FormD);
        nfd.Should().NotBe("Sáng kiến", "chuỗi NFD khác NFC ở mức code point");

        VanBanTiengViet.BoDau(nfd).Should().Be("Sang kien");
    }

    [Fact]
    public void Chuan_Hoa_NFC_Dua_Ve_Cung_Dang()
    {
        var nfd = "Sáng kiến".Normalize(NormalizationForm.FormD);

        VanBanTiengViet.ChuanHoaNfc(nfd).Should().Be("Sáng kiến");
        VanBanTiengViet.ChuanHoaNfc(nfd).Should().HaveLength("Sáng kiến".Length);
    }

    [Fact]
    public void Tao_Khong_Dau_Ve_Chu_Thuong_Va_Gom_Khoang_Trang()
    {
        VanBanTiengViet.TaoKhongDau("  Ứng   dụng  CÔNG nghệ  ")
            .Should().Be("ung dung cong nghe");
    }

    [Fact]
    public void Chuan_Hoa_De_So_Khop_Bo_Dau_Cau()
    {
        VanBanTiengViet.ChuanHoaDeSoKhop("Sáng kiến: \"Ứng dụng CNTT\" (năm 2026)!")
            .Should().Be("sang kien ung dung cntt nam 2026");
    }

    [Fact]
    public void Tim_Kiem_Khong_Dau_Ra_Ket_Qua_Co_Dau()
    {
        // Yeu cau UI bat buoc: go khong dau van ra ket qua co dau.
        VanBanTiengViet.ChuaKhongDau("Chuyển đổi số trong quản lý hành chính", "chuyen doi so")
            .Should().BeTrue();
        VanBanTiengViet.ChuaKhongDau("Chuyển đổi số", "y tế").Should().BeFalse();
    }

    [Fact]
    public void Tim_Kiem_Voi_Tu_Khoa_Rong_Luon_Khop()
    {
        VanBanTiengViet.ChuaKhongDau("bất kỳ", "   ").Should().BeTrue();
    }

    [Fact]
    public void Tach_Tu_Loai_Bo_Ky_Tu_Thua()
    {
        VanBanTiengViet.TachTu("Sáng kiến, cải tiến kỹ thuật!")
            .Should().Equal("sang", "kien", "cai", "tien", "ky", "thuat");
    }

    [Fact]
    public void Tach_Tu_Bo_Stopword_Loai_Tu_Dung()
    {
        var tu = VanBanTiengViet.TachTuBoStopword("Giải pháp của chúng tôi được áp dụng cho đơn vị");

        tu.Should().NotContain("cua").And.NotContain("duoc").And.NotContain("chung");
        tu.Should().Contain("giai").And.Contain("phap").And.Contain("don").And.Contain("vi");
    }

    [Fact]
    public void Stopword_Khong_Duoc_Pha_Huy_Thuat_Ngu_Nghiep_Vu()
    {
        // Sau khi bo dau, nhieu hu tu trung voi thuat ngu quan trong.
        // Danh sach stopword phai giu lai cac tu nay.
        var tu = VanBanTiengViet.TachTuBoStopword(
            "Hồ sơ đơn vị có trọng số theo văn bản đề nghị năm 2026, mã kết quả");

        tu.Should().Contain(new[] { "ho", "so", "don", "vi", "trong", "van", "de", "nam", "ma", "qua" });
    }

    [Fact]
    public void Tao_Slug_Dung_Cho_Duong_Dan()
    {
        VanBanTiengViet.TaoSlug("Đợt đề nghị công nhận sáng kiến năm 2026")
            .Should().Be("dot-de-nghi-cong-nhan-sang-kien-nam-2026");
    }

    [Fact]
    public void Cat_Ngan_Them_Dau_Ba_Cham()
    {
        VanBanTiengViet.CatNgan("Ứng dụng chuyển đổi số", 10).Should().Be("Ứng dụng c…");
        VanBanTiengViet.CatNgan("Ngắn", 10).Should().Be("Ngắn");
    }

    [Fact]
    public void Chuoi_Rong_Va_Null_Duoc_Xu_Ly_An_Toan()
    {
        VanBanTiengViet.BoDau(null).Should().BeEmpty();
        VanBanTiengViet.TaoKhongDau(null).Should().BeEmpty();
        VanBanTiengViet.ChuanHoaDeSoKhop(null).Should().BeEmpty();
        VanBanTiengViet.TachTu(null).Should().BeEmpty();
        VanBanTiengViet.TaoSlug(null).Should().BeEmpty();
    }
}
