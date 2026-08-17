using BlueIdea.Reporting;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BlueIdea.UnitTests.Reporting;

/// <summary>
/// Kiem thu bo quet placeholder .docx (chuc nang 6).
///
/// Trong tam la truong hop Word CAT MOT PLACEHOLDER thanh nhieu run — day la tinh huong xay ra
/// thuong xuyen ngoai doi (khi nguoi soan sua chinh ta hoac doi dinh dang giua chung) va la ly do
/// khong the quet tung run rieng le.
/// </summary>
public sealed class BoQuetPlaceholderDocxTests
{
    [Fact]
    public void Doc_Duoc_Placeholder_Trong_Mot_Run()
    {
        var tep = TaoDocx(new[] { new[] { "Kính gửi {{ tenDonVi }}, hồ sơ {{maHoSo}} đã được duyệt." } });

        var ketQua = BoQuetPlaceholderDocx.Quet(tep);

        ketQua.Placeholder.Should().Equal("tenDonVi", "maHoSo");
        ketQua.CanhBao.Should().BeNull();
    }

    [Fact]
    public void Doc_Duoc_Placeholder_Bi_Word_Cat_Thanh_Nhieu_Run()
    {
        // Word thuong tach "{{maHoSo}}" thanh "{{", "maHo", "So}}" sau khi nguoi dung sua giua chung.
        var tep = TaoDocx(new[] { new[] { "Hồ sơ ", "{{", "maHo", "So", "}}", " đã tiếp nhận." } });

        var ketQua = BoQuetPlaceholderDocx.Quet(tep);

        ketQua.Placeholder.Should().ContainSingle().Which.Should().Be("maHoSo");
    }

    [Fact]
    public void Placeholder_Trung_Chi_Tra_Ve_Mot_Lan_Va_Giu_Thu_Tu()
    {
        var tep = TaoDocx(new[]
        {
            new[] { "{{tenSangKien}} — {{maHoSo}}" },
            new[] { "Nhắc lại: {{maHoSo}} và {{tacGiaChinh}}" }
        });

        var ketQua = BoQuetPlaceholderDocx.Quet(tep);

        ketQua.Placeholder.Should().Equal("tenSangKien", "maHoSo", "tacGiaChinh");
    }

    [Fact]
    public void Tep_Khong_Co_Placeholder_Thi_Bao_Canh_Bao()
    {
        var tep = TaoDocx(new[] { new[] { "Văn bản thường, không có biến nào." } });

        var ketQua = BoQuetPlaceholderDocx.Quet(tep);

        ketQua.Placeholder.Should().BeEmpty();
        ketQua.CanhBao.Should().Contain("{{ tenBien }}");
    }

    [Fact]
    public void Dem_Dung_So_Doan_Van()
    {
        var tep = TaoDocx(new[]
        {
            new[] { "Đoạn một {{a}}" },
            new[] { "Đoạn hai" },
            new[] { "Đoạn ba {{b}}" }
        });

        var ketQua = BoQuetPlaceholderDocx.Quet(tep);

        ketQua.SoDoanVan.Should().Be(3);
        ketQua.Placeholder.Should().Equal("a", "b");
    }

    [Fact]
    public void Placeholder_Khong_Dong_Ngoac_Thi_Bo_Qua()
    {
        var tep = TaoDocx(new[] { new[] { "Thiếu ngoặc {{ maHoSo và {{tenDonVi}}" } });

        var ketQua = BoQuetPlaceholderDocx.Quet(tep);

        ketQua.Placeholder.Should().ContainSingle().Which.Should().Be("tenDonVi");
    }

    // ------------------------------------------------------------------------------------

    /// <summary>Tao mot tep .docx trong bo nho: moi phan tu ngoai la mot doan, moi chuoi la mot run.</summary>
    private static MemoryStream TaoDocx(IReadOnlyList<string[]> cacDoan)
    {
        var bo = new MemoryStream();

        using (var tep = WordprocessingDocument.Create(bo, WordprocessingDocumentType.Document, true))
        {
            var phan = tep.AddMainDocumentPart();
            phan.Document = new Document();
            var than = phan.Document.AppendChild(new Body());

            foreach (var cacRun in cacDoan)
            {
                var doan = than.AppendChild(new Paragraph());

                foreach (var noiDung in cacRun)
                {
                    doan.AppendChild(new Run(new Text(noiDung) { Space = SpaceProcessingModeValues.Preserve }));
                }
            }

            phan.Document.Save();
        }

        bo.Position = 0;
        return bo;
    }
}
