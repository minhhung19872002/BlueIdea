using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlueIdea.Reporting;

/// <summary>Mot phieu cham can xuat ra PDF.</summary>
public sealed record PhieuChamPdf(
    string SoPhieu,
    string MaHoSo,
    string TenSangKien,
    string? TenTacGiaChinh,
    string? TenDonVi,
    string TenThanhVien,
    string? ChucDanh,
    string TenHoiDong,
    decimal TongDiem,
    string? KetLuan,
    string? TenMucCongNhanDeXuat,
    string? NhanXetChung,
    string? UuDiem,
    string? HanChe,
    DateTimeOffset? NgayGui,
    IReadOnlyList<DongPhieuChamPdf> ChiTiet);

public sealed record DongPhieuChamPdf(string TenTieuChi, decimal DiemToiDa, decimal Diem, string? NhanXet);

/// <summary>
/// Chuc nang 35 - Xuat phieu cham diem ra PDF, moi phieu MOT trang rieng.
///
/// Xuat hang loat vao mot tep thay vi nhieu tep roi: ho so nghiem thu can mot van ban lien mach,
/// va nguoi dung khong phai giai nen roi in tung tep.
/// </summary>
public static class BoXuatPhieuChamPdf
{
    private const string TenFont = "Times New Roman";

    public static byte[] Xuat(
        string tenCoQuanChuQuan,
        string tenDonVi,
        IReadOnlyList<PhieuChamPdf> danhSach)
    {
        ArgumentNullException.ThrowIfNull(danhSach);

        var tep = Document.Create(tap =>
        {
            foreach (var phieu in danhSach)
            {
                tap.Page(trang =>
                {
                    trang.Size(PageSizes.A4);
                    trang.Margin(2, Unit.Centimetre);
                    trang.DefaultTextStyle(x => x.FontFamily(TenFont).FontSize(11));

                    trang.Header().Element(e => VeTieuDe(e, tenCoQuanChuQuan, tenDonVi, phieu));
                    trang.Content().PaddingVertical(8).Element(e => VeNoiDung(e, phieu));

                    trang.Footer().Column(cot =>
                    {
                        cot.Item().PaddingBottom(6).AlignRight().Column(ky =>
                        {
                            ky.Item().AlignCenter().Text(phieu.ChucDanh ?? "Thành viên hội đồng")
                                .SemiBold().FontSize(10);
                            ky.Item().AlignCenter().Text("(Ký, ghi rõ họ tên)").Italic().FontSize(9);
                            ky.Item().Height(45);
                            ky.Item().AlignCenter().Text(phieu.TenThanhVien).Bold();
                        });

                        cot.Item().AlignCenter().Text(x =>
                        {
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                    });
                });
            }
        });

        return tep.GeneratePdf();
    }

    private static void VeTieuDe(
        IContainer o, string tenCoQuanChuQuan, string tenDonVi, PhieuChamPdf phieu)
    {
        o.Column(cot =>
        {
            cot.Item().Row(dong =>
            {
                dong.RelativeItem().Column(trai =>
                {
                    trai.Item().AlignCenter().Text(tenCoQuanChuQuan.ToUpperInvariant()).FontSize(10);
                    trai.Item().AlignCenter().Text(tenDonVi.ToUpperInvariant()).Bold().FontSize(10);
                    trai.Item().AlignCenter().PaddingHorizontal(25).LineHorizontal(1);
                });

                dong.RelativeItem().Column(phai =>
                {
                    phai.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM")
                        .Bold().FontSize(10);
                    phai.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").Bold().FontSize(10);
                    phai.Item().AlignCenter().PaddingHorizontal(35).LineHorizontal(1);
                });
            });

            cot.Item().PaddingTop(14).AlignCenter().Text("PHIẾU ĐÁNH GIÁ SÁNG KIẾN")
                .Bold().FontSize(14);

            cot.Item().AlignCenter().Text($"Số phiếu: {phieu.SoPhieu}").Italic().FontSize(10);
            cot.Item().PaddingTop(6).LineHorizontal(1);
        });
    }

    private static void VeNoiDung(IContainer o, PhieuChamPdf phieu)
    {
        o.Column(cot =>
        {
            cot.Spacing(4);

            Dong(cot, "Hội đồng", phieu.TenHoiDong);
            Dong(cot, "Thành viên chấm", $"{phieu.TenThanhVien}{MoTaChucDanh(phieu.ChucDanh)}");
            Dong(cot, "Mã hồ sơ", phieu.MaHoSo);
            Dong(cot, "Tên sáng kiến", phieu.TenSangKien);
            Dong(cot, "Tác giả chính", phieu.TenTacGiaChinh);
            Dong(cot, "Đơn vị", phieu.TenDonVi);
            Dong(cot, "Ngày chấm", phieu.NgayGui?.ToOffset(TimeSpan.FromHours(7))
                .ToString("dd/MM/yyyy HH:mm"));

            cot.Item().PaddingTop(10).Text("BẢNG ĐIỂM THEO TIÊU CHÍ").Bold();

            cot.Item().Table(bang =>
            {
                bang.ColumnsDefinition(c =>
                {
                    c.ConstantColumn(28);
                    c.RelativeColumn(4);
                    c.ConstantColumn(55);
                    c.ConstantColumn(50);
                    c.RelativeColumn(3);
                });

                bang.Header(dau =>
                {
                    O(dau.Cell(), "TT", true);
                    O(dau.Cell(), "Tiêu chí", true);
                    O(dau.Cell(), "Điểm tối đa", true);
                    O(dau.Cell(), "Điểm chấm", true);
                    O(dau.Cell(), "Nhận xét", true);
                });

                var stt = 1;
                foreach (var d in phieu.ChiTiet)
                {
                    O(bang.Cell(), (stt++).ToString());
                    O(bang.Cell(), d.TenTieuChi);
                    O(bang.Cell(), d.DiemToiDa.ToString("0.##"));
                    O(bang.Cell(), d.Diem.ToString("0.##"));
                    O(bang.Cell(), d.NhanXet ?? string.Empty);
                }

                O(bang.Cell().ColumnSpan(3), "TỔNG ĐIỂM", true);
                O(bang.Cell(), phieu.TongDiem.ToString("0.##"), true);
                O(bang.Cell(), string.Empty);
            });

            cot.Item().PaddingTop(10);

            Dong(cot, "Kết luận", phieu.KetLuan switch
            {
                "DAT" => "Đạt — đề nghị công nhận",
                "KHONG_DAT" => "Không đạt",
                _ => phieu.KetLuan
            });

            Dong(cot, "Mức đề xuất", phieu.TenMucCongNhanDeXuat);

            KhoiVanBan(cot, "Ưu điểm", phieu.UuDiem);
            KhoiVanBan(cot, "Hạn chế", phieu.HanChe);
            KhoiVanBan(cot, "Nhận xét chung", phieu.NhanXetChung);
        });
    }

    private static string MoTaChucDanh(string? chucDanh)
        => string.IsNullOrWhiteSpace(chucDanh) ? string.Empty : $" — {chucDanh}";

    private static void Dong(ColumnDescriptor cot, string nhan, string? giaTri)
    {
        if (string.IsNullOrWhiteSpace(giaTri))
        {
            return;
        }

        cot.Item().Row(dong =>
        {
            dong.ConstantItem(120).Text($"{nhan}:").SemiBold();
            dong.RelativeItem().Text(giaTri);
        });
    }

    private static void KhoiVanBan(ColumnDescriptor cot, string nhan, string? noiDung)
    {
        if (string.IsNullOrWhiteSpace(noiDung))
        {
            return;
        }

        cot.Item().PaddingTop(6).Text($"{nhan}:").SemiBold();
        cot.Item().Text(noiDung).Justify();
    }

    private static void O(IContainer o, string noiDung, bool dam = false)
    {
        var khoi = o.Border(0.5f).Padding(4);

        if (dam)
        {
            khoi.Text(noiDung).Bold();
        }
        else
        {
            khoi.Text(noiDung);
        }
    }
}
