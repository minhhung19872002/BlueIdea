using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace BlueIdea.Reporting;

/// <summary>
/// Chuc nang 35 - Xuat phieu cham diem ra tep Word (.docx).
///
/// Ban PDF de in va ky, con ban Word de thu ky con SUA duoc truoc khi dong vao ho so hoi dong:
/// nhieu don vi phai chen them phan mo dau, so quyet dinh thanh lap hoi dong hoac ghi chu rieng
/// theo mau van ban cua ho. Dac ta Muc 5 nhom VII yeu cau ca hai dinh dang.
/// </summary>
public static class BoXuatPhieuChamWord
{
    private const string TenFont = "Times New Roman";

    /// <summary>Co chu chuan van ban hanh chinh: 13pt = 26 half-point.</summary>
    private const string CoChuThuong = "26";

    private const string CoChuTieuDe = "30";

    private const string CoChuNho = "22";

    public static byte[] Xuat(
        string tenCoQuanChuQuan, string tenDonVi, IReadOnlyList<PhieuChamPdf> danhSach)
    {
        ArgumentNullException.ThrowIfNull(danhSach);

        using var bo = new MemoryStream();

        using (var tep = WordprocessingDocument.Create(bo, WordprocessingDocumentType.Document, true))
        {
            var phanChinh = tep.AddMainDocumentPart();
            phanChinh.Document = new Document();
            var than = phanChinh.Document.AppendChild(new Body());

            for (var i = 0; i < danhSach.Count; i++)
            {
                VePhieu(than, tenCoQuanChuQuan, tenDonVi, danhSach[i]);

                // Moi phieu mot trang rieng, giong ban PDF - tru phieu cuoi cung.
                if (i < danhSach.Count - 1)
                {
                    than.AppendChild(new Paragraph(new Run(new Break { Type = BreakValues.Page })));
                }
            }

            phanChinh.Document.Save();
        }

        return bo.ToArray();
    }

    private static void VePhieu(Body than, string tenCoQuanChuQuan, string tenDonVi, PhieuChamPdf p)
    {
        than.AppendChild(Doan(tenCoQuanChuQuan.ToUpperInvariant(), canGiua: true, coChu: CoChuNho));
        than.AppendChild(Doan(tenDonVi.ToUpperInvariant(), canGiua: true, dam: true, coChu: CoChuNho));
        than.AppendChild(Doan(string.Empty));
        than.AppendChild(Doan("PHIẾU ĐÁNH GIÁ SÁNG KIẾN", canGiua: true, dam: true, coChu: CoChuTieuDe));

        if (!string.IsNullOrWhiteSpace(p.SoPhieu))
        {
            than.AppendChild(Doan($"Số phiếu: {p.SoPhieu}", canGiua: true, coChu: CoChuNho));
        }

        than.AppendChild(Doan(string.Empty));
        than.AppendChild(DoanNhanGiaTri("Mã hồ sơ", p.MaHoSo));
        than.AppendChild(DoanNhanGiaTri("Tên sáng kiến", p.TenSangKien));

        if (!string.IsNullOrWhiteSpace(p.TenTacGiaChinh))
        {
            than.AppendChild(DoanNhanGiaTri("Tác giả chính", p.TenTacGiaChinh));
        }

        if (!string.IsNullOrWhiteSpace(p.TenDonVi))
        {
            than.AppendChild(DoanNhanGiaTri("Đơn vị", p.TenDonVi));
        }

        than.AppendChild(DoanNhanGiaTri("Hội đồng", p.TenHoiDong));
        than.AppendChild(DoanNhanGiaTri(
            "Thành viên chấm",
            string.IsNullOrWhiteSpace(p.ChucDanh) ? p.TenThanhVien : $"{p.TenThanhVien} ({p.ChucDanh})"));

        if (p.NgayGui.HasValue)
        {
            than.AppendChild(DoanNhanGiaTri(
                "Ngày gửi phiếu", p.NgayGui.Value.ToLocalTime().ToString("dd/MM/yyyy HH:mm")));
        }

        than.AppendChild(Doan(string.Empty));
        than.AppendChild(BangDiem(p));
        than.AppendChild(Doan(string.Empty));
        than.AppendChild(DoanNhanGiaTri("Tổng điểm", p.TongDiem.ToString("0.##")));

        if (!string.IsNullOrWhiteSpace(p.TenMucCongNhanDeXuat))
        {
            than.AppendChild(DoanNhanGiaTri("Mức công nhận đề xuất", p.TenMucCongNhanDeXuat));
        }

        if (!string.IsNullOrWhiteSpace(p.KetLuan))
        {
            than.AppendChild(DoanNhanGiaTri("Kết luận", p.KetLuan));
        }

        ThemNhanXet(than, "Ưu điểm", p.UuDiem);
        ThemNhanXet(than, "Hạn chế", p.HanChe);
        ThemNhanXet(than, "Nhận xét chung", p.NhanXetChung);

        than.AppendChild(Doan(string.Empty));
        than.AppendChild(Doan(p.ChucDanh ?? "Thành viên hội đồng", canPhai: true, dam: true));
        than.AppendChild(Doan("(Ký, ghi rõ họ tên)", canPhai: true, nghieng: true, coChu: CoChuNho));
        than.AppendChild(Doan(string.Empty));
        than.AppendChild(Doan(string.Empty));
        than.AppendChild(Doan(p.TenThanhVien, canPhai: true, dam: true));
    }

    private static void ThemNhanXet(Body than, string nhan, string? noiDung)
    {
        if (string.IsNullOrWhiteSpace(noiDung)) return;

        than.AppendChild(Doan($"{nhan}:", dam: true));
        than.AppendChild(Doan(noiDung));
    }

    private static Table BangDiem(PhieuChamPdf p)
    {
        var bang = new Table();

        bang.AppendChild(new TableProperties(
            new TableBorders(
                new TopBorder { Val = BorderValues.Single, Size = 4 },
                new BottomBorder { Val = BorderValues.Single, Size = 4 },
                new LeftBorder { Val = BorderValues.Single, Size = 4 },
                new RightBorder { Val = BorderValues.Single, Size = 4 },
                new InsideHorizontalBorder { Val = BorderValues.Single, Size = 4 },
                new InsideVerticalBorder { Val = BorderValues.Single, Size = 4 }),
            new TableWidth { Width = "5000", Type = TableWidthUnitValues.Pct }));

        bang.AppendChild(DongBang(new[] { "Tiêu chí", "Điểm tối đa", "Điểm chấm", "Nhận xét" }, dam: true));

        foreach (var dong in p.ChiTiet)
        {
            bang.AppendChild(DongBang(new[]
            {
                dong.TenTieuChi,
                dong.DiemToiDa.ToString("0.##"),
                dong.Diem.ToString("0.##"),
                dong.NhanXet ?? string.Empty
            }));
        }

        return bang;
    }

    private static TableRow DongBang(IReadOnlyList<string> oData, bool dam = false)
    {
        var dong = new TableRow();

        foreach (var o in oData)
        {
            dong.AppendChild(new TableCell(Doan(o, dam: dam)));
        }

        return dong;
    }

    private static Paragraph DoanNhanGiaTri(string nhan, string? giaTri)
    {
        var doan = new Paragraph();
        doan.AppendChild(ThuocTinhDoan());

        doan.AppendChild(new Run(
            new RunProperties(
                new RunFonts { Ascii = TenFont, HighAnsi = TenFont },
                new Bold(),
                new FontSize { Val = CoChuThuong }),
            new Text($"{nhan}: ") { Space = SpaceProcessingModeValues.Preserve }));

        doan.AppendChild(new Run(
            new RunProperties(
                new RunFonts { Ascii = TenFont, HighAnsi = TenFont },
                new FontSize { Val = CoChuThuong }),
            new Text(giaTri ?? string.Empty) { Space = SpaceProcessingModeValues.Preserve }));

        return doan;
    }

    private static Paragraph Doan(
        string noiDung,
        bool canGiua = false,
        bool canPhai = false,
        bool dam = false,
        bool nghieng = false,
        string coChu = CoChuThuong)
    {
        var doan = new Paragraph();

        var canLe = canGiua
            ? JustificationValues.Center
            : canPhai
                ? JustificationValues.Right
                : JustificationValues.Left;

        doan.AppendChild(ThuocTinhDoan(canLe));

        var thuocTinh = new RunProperties(
            new RunFonts { Ascii = TenFont, HighAnsi = TenFont },
            new FontSize { Val = coChu });

        if (dam) thuocTinh.AppendChild(new Bold());
        if (nghieng) thuocTinh.AppendChild(new Italic());

        doan.AppendChild(new Run(
            thuocTinh,
            new Text(noiDung) { Space = SpaceProcessingModeValues.Preserve }));

        return doan;
    }

    private static ParagraphProperties ThuocTinhDoan(
        JustificationValues canLe = default)
        => new(
            new Justification { Val = canLe == default ? JustificationValues.Left : canLe },
            new SpacingBetweenLines { After = "60" });
}
