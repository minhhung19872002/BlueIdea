using BlueIdea.Application.XuLy;
using BlueIdea.Domain.QuyTrinh;

namespace BlueIdea.UnitTests.XuLy;

public class LayKenhChoPhepTests
{
    [Fact]
    public void Null_ChucNangBat_Chi_Tra_Ve_APP()
    {
        var kenh = ThucThiBuocCommandHandler.LayKenhChoPhep(null);

        kenh.Should().ContainSingle().Which.Should().Be("APP");
    }

    [Fact]
    public void Rong_ChucNangBat_Chi_Tra_Ve_APP()
    {
        var kenh = ThucThiBuocCommandHandler.LayKenhChoPhep(Array.Empty<string>());

        kenh.Should().ContainSingle().Which.Should().Be("APP");
    }

    [Fact]
    public void GuiEmail_Bat_Them_Kenh_EMAIL()
    {
        var kenh = ThucThiBuocCommandHandler.LayKenhChoPhep(
            new[] { MaChucNangBoSung.GuiEmail });

        kenh.Should().BeEquivalentTo(new[] { "APP", "EMAIL" });
    }

    [Fact]
    public void GuiSms_Bat_Them_Kenh_SMS()
    {
        var kenh = ThucThiBuocCommandHandler.LayKenhChoPhep(
            new[] { MaChucNangBoSung.GuiSms });

        kenh.Should().BeEquivalentTo(new[] { "APP", "SMS" });
    }

    [Fact]
    public void Ca_Hai_Bat_Them_EMAIL_Va_SMS()
    {
        var kenh = ThucThiBuocCommandHandler.LayKenhChoPhep(
            new[] { MaChucNangBoSung.GuiEmail, MaChucNangBoSung.GuiSms });

        kenh.Should().BeEquivalentTo(new[] { "APP", "EMAIL", "SMS" });
    }

    [Fact]
    public void ChucNang_Khac_Khong_Anh_Huong_Kenh()
    {
        var kenh = ThucThiBuocCommandHandler.LayKenhChoPhep(
            new[] { MaChucNangBoSung.KySo, MaChucNangBoSung.TaoBienBan });

        kenh.Should().ContainSingle().Which.Should().Be("APP");
    }
}
