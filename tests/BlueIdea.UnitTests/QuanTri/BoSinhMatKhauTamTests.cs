using BlueIdea.Application.QuanTri;

namespace BlueIdea.UnitTests.QuanTri;

/// <summary>
/// Kiem thu bo sinh mat khau tam. Day la thong tin xac thuc that nen phai bao dam:
/// dat do dai, du 4 nhom ky tu, va KHONG lap lai giua cac lan goi.
/// </summary>
public sealed class BoSinhMatKhauTamTests
{
    [Theory]
    [InlineData(12)]
    [InlineData(16)]
    [InlineData(32)]
    public void Sinh_Dung_Do_Dai_Yeu_Cau(int doDai)
    {
        BoSinhMatKhauTam.Sinh(doDai).Should().HaveLength(doDai);
    }

    [Fact]
    public void Do_Dai_Duoi_4_Van_Du_Bon_Nhom_Ky_Tu()
    {
        // Khong the vua dat do dai 2 vua du 4 nhom -> uu tien du nhom.
        BoSinhMatKhauTam.Sinh(2).Should().HaveLength(4);
    }

    [Fact]
    public void Luon_Co_Du_Bon_Nhom_Ky_Tu()
    {
        for (var lan = 0; lan < 200; lan++)
        {
            var matKhau = BoSinhMatKhauTam.Sinh(12);

            matKhau.Any(char.IsAsciiLetterUpper).Should().BeTrue("phải có chữ hoa");
            matKhau.Any(char.IsAsciiLetterLower).Should().BeTrue("phải có chữ thường");
            matKhau.Any(char.IsAsciiDigit).Should().BeTrue("phải có chữ số");
            matKhau.Any(c => "@#$%&*!?".Contains(c)).Should().BeTrue("phải có ký tự đặc biệt");
        }
    }

    [Fact]
    public void Khong_Dung_Ky_Tu_De_Nham_Khi_Doc()
    {
        // I l 1 O 0 rat de doc nham khi quan tri vien ban giao mat khau bang giay hoac doc qua dien thoai.
        var gop = string.Concat(Enumerable.Range(0, 200).Select(_ => BoSinhMatKhauTam.Sinh(16)));

        gop.Should().NotContainAny("I", "l", "1", "O", "0");
    }

    [Fact]
    public void Khong_Trung_Nhau_Giua_Cac_Lan_Sinh()
    {
        var tap = Enumerable.Range(0, 500).Select(_ => BoSinhMatKhauTam.Sinh(12)).ToHashSet();

        tap.Should().HaveCount(500, "mật khẩu tạm phải ngẫu nhiên, không được lặp");
    }

    [Fact]
    public void Ky_Tu_Bat_Buoc_Khong_Luon_Nam_Dau_Chuoi()
    {
        // Neu khong xao tron, 4 ky tu dau se luon theo thu tu HOA-thuong-so-dacbiet
        // -> giam manh khong gian tim kiem thuc te.
        var kyTuDau = Enumerable.Range(0, 200)
            .Select(_ => BoSinhMatKhauTam.Sinh(12)[0])
            .ToHashSet();

        kyTuDau.Any(c => !char.IsAsciiLetterUpper(c)).Should()
            .BeTrue("vị trí đầu phải xuất hiện cả nhóm ký tự khác chữ hoa");
    }
}
