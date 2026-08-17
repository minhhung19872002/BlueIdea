using BlueIdea.Application.TichHop;

namespace BlueIdea.UnitTests.TichHop;

/// <summary>
/// Kiem thu quy tac danh sach IP cua khoa API cap cho he thong ngoai.
///
/// Khop dai CIDR la cho de sai nhat trong ca chuc nang: sai mot bit mat na thi hoac chan nham
/// he thong doi tac hop le, hoac mo cua cho ca dai lon hon nhieu lan y dinh — va ca hai deu
/// khong lo ra khi thu bang mot dia chi don.
/// </summary>
public sealed class KhoaApiNgoaiTests
{
    [Fact]
    public void Danh_Sach_Rong_Thi_Khong_Chan_Theo_Ip()
    {
        DichVuKhoaApiNgoai.IpDuocPhep(Array.Empty<string>(), "203.0.113.7").Should().BeTrue();
    }

    [Theory]
    [InlineData("203.0.113.7", true)]
    [InlineData("203.0.113.8", false)]
    public void Khop_Dia_Chi_Don(string ip, bool mongDoi)
    {
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { "203.0.113.7" }, ip).Should().Be(mongDoi);
    }

    [Theory]
    // /24 — ba byte dau phai trung.
    [InlineData("192.168.1.0/24", "192.168.1.1", true)]
    [InlineData("192.168.1.0/24", "192.168.1.255", true)]
    [InlineData("192.168.1.0/24", "192.168.2.1", false)]
    // /16
    [InlineData("10.0.0.0/16", "10.0.255.254", true)]
    [InlineData("10.0.0.0/16", "10.1.0.1", false)]
    // Mat na le, khong tron byte — day la cho de cai sai nhat.
    [InlineData("192.168.1.0/25", "192.168.1.127", true)]
    [InlineData("192.168.1.0/25", "192.168.1.128", false)]
    [InlineData("192.168.1.128/25", "192.168.1.128", true)]
    [InlineData("192.168.1.128/25", "192.168.1.127", false)]
    // /32 la mot dia chi duy nhat.
    [InlineData("203.0.113.7/32", "203.0.113.7", true)]
    [InlineData("203.0.113.7/32", "203.0.113.8", false)]
    // /0 khop moi dia chi.
    [InlineData("0.0.0.0/0", "8.8.8.8", true)]
    public void Khop_Dai_Cidr(string dai, string ip, bool mongDoi)
    {
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { dai }, ip).Should().Be(mongDoi);
    }

    [Fact]
    public void Khop_Bat_Ky_Muc_Nao_Trong_Danh_Sach()
    {
        var danhSach = new[] { "203.0.113.7", "10.0.0.0/8", "192.168.1.5" };

        DichVuKhoaApiNgoai.IpDuocPhep(danhSach, "10.55.66.77").Should().BeTrue();
        DichVuKhoaApiNgoai.IpDuocPhep(danhSach, "192.168.1.5").Should().BeTrue();
        DichVuKhoaApiNgoai.IpDuocPhep(danhSach, "172.16.0.1").Should().BeFalse();
    }

    [Fact]
    public void Ipv4_Boc_Trong_Ipv6_Van_Khop_Dai_Ipv4()
    {
        // Kestrel lang nghe kieu kep tra ve dia chi dang ::ffff:10.0.0.1. Khong go bo lop boc
        // thi moi dai IPv4 quan tri vien khai bao deu khong bao gio khop.
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { "10.0.0.0/8" }, "::ffff:10.0.0.1").Should().BeTrue();
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { "10.0.0.1" }, "::ffff:10.0.0.1").Should().BeTrue();
    }

    [Fact]
    public void Ho_Tro_Dai_Ipv6()
    {
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { "2001:db8::/32" }, "2001:db8:1234::1")
            .Should().BeTrue();
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { "2001:db8::/32" }, "2001:db9::1")
            .Should().BeFalse();
    }

    [Fact]
    public void Khong_Tron_Lan_Hai_Ho_Dia_Chi()
    {
        // Dai IPv4 khong duoc phep khop mot dia chi IPv6 that va nguoc lai.
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { "0.0.0.0/0" }, "2001:db8::1").Should().BeFalse();
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { "::/0" }, "8.8.8.8").Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("khong-phai-ip")]
    [InlineData("999.1.1.1")]
    public void Ip_Goi_Den_Khong_Doc_Duoc_Thi_Tu_Choi(string? ip)
    {
        // Tu choi chu khong bo qua bo loc: khong xac dinh duoc nguon goi thi khong the khang
        // dinh no nam trong danh sach cho phep.
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { "203.0.113.7" }, ip).Should().BeFalse();
    }

    [Theory]
    [InlineData("192.168.1.0/33")]  // vuot so bit toi da cua IPv4
    [InlineData("192.168.1.0/-1")]
    [InlineData("khong-phai/24")]
    public void Dai_Cidr_Hong_Thi_Khong_Khop_Gi(string dai)
    {
        DichVuKhoaApiNgoai.IpDuocPhep(new[] { dai }, "192.168.1.1").Should().BeFalse();
    }
}
