using BlueIdea.Reporting;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BlueIdea.UnitTests.Reporting;

/// <summary>
/// Kiem thu ban Word cua phieu cham (chuc nang 35).
///
/// Kiem bang cach MO LAI tep vua sinh bang OpenXml chu khong chi doi do dai mang byte: mot tep
/// .docx hong van co do dai lon, Word moi la ben phat hien ra khi thu ky mo len giua cuoc hop.
/// </summary>
public sealed class BoXuatPhieuChamWordTests
{
    [Fact]
    public void Sinh_Duoc_Tep_Docx_Doc_Lai_Duoc()
    {
        var tep = BoXuatPhieuChamWord.Xuat("UBND THÀNH PHỐ", "HỘI ĐỒNG SÁNG KIẾN", new[] { TaoPhieu() });

        using var luong = new MemoryStream(tep);
        using var taiLieu = WordprocessingDocument.Open(luong, false);

        var vanBan = taiLieu.MainDocumentPart!.Document.Body!.InnerText;

        vanBan.Should().Contain("PHIẾU ĐÁNH GIÁ SÁNG KIẾN");
        vanBan.Should().Contain("SK-2026-0001");
        vanBan.Should().Contain("Số hoá quy trình tiếp nhận hồ sơ");
        vanBan.Should().Contain("Nguyễn Văn A");
        vanBan.Should().Contain("Tính mới");
        vanBan.Should().Contain("87");
    }

    [Fact]
    public void Bang_Diem_Co_Du_Dong_Tieu_Chi_Kem_Dong_Tieu_De()
    {
        var tep = BoXuatPhieuChamWord.Xuat("UBND", "HỘI ĐỒNG", new[] { TaoPhieu() });

        using var luong = new MemoryStream(tep);
        using var taiLieu = WordprocessingDocument.Open(luong, false);

        var bang = taiLieu.MainDocumentPart!.Document.Body!.Elements<Table>().Single();

        // 1 dong tieu de + 2 tieu chi.
        bang.Elements<TableRow>().Count().Should().Be(3);
    }

    [Fact]
    public void Nhieu_Phieu_Thi_Moi_Phieu_Mot_Trang()
    {
        var tep = BoXuatPhieuChamWord.Xuat(
            "UBND", "HỘI ĐỒNG", new[] { TaoPhieu(), TaoPhieu("Trần Thị B") });

        using var luong = new MemoryStream(tep);
        using var taiLieu = WordprocessingDocument.Open(luong, false);

        var soNgatTrang = taiLieu.MainDocumentPart!.Document.Body!
            .Descendants<Break>()
            .Count(x => x.Type is not null && x.Type == BreakValues.Page);

        soNgatTrang.Should().Be(1, "hai phiếu thì chỉ cần một dấu ngắt trang ở giữa");
    }

    private static PhieuChamPdf TaoPhieu(string tenThanhVien = "Lê Văn C") => new(
        SoPhieu: "PC-001",
        MaHoSo: "SK-2026-0001",
        TenSangKien: "Số hoá quy trình tiếp nhận hồ sơ",
        TenTacGiaChinh: "Nguyễn Văn A",
        TenDonVi: "Phòng Kế hoạch",
        TenThanhVien: tenThanhVien,
        ChucDanh: "Uỷ viên",
        TenHoiDong: "Hội đồng sáng kiến cấp cơ sở",
        TongDiem: 87.5m,
        KetLuan: "Đạt",
        TenMucCongNhanDeXuat: "Cấp cơ sở",
        NhanXetChung: "Giải pháp rõ ràng, khả thi.",
        UuDiem: "Tiết kiệm thời gian tiếp nhận.",
        HanChe: "Cần bổ sung số liệu đo lường.",
        NgayGui: DateTimeOffset.Now,
        ChiTiet: new[]
        {
            new DongPhieuChamPdf("Tính mới", 30m, 26m, "Có cải tiến rõ"),
            new DongPhieuChamPdf("Khả năng áp dụng", 25m, 22m, null)
        });
}
