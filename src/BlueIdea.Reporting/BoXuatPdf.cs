using System.Globalization;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlueIdea.Reporting;

/// <summary>Mot dong thong tin dang nhan - gia tri trong phieu/bien ban.</summary>
public sealed record DongThongTin(string Nhan, string? GiaTri);

/// <summary>Mot bang du lieu trong tai lieu PDF.</summary>
public sealed record BangPdf(string TieuDe, IReadOnlyList<string> CotTieuDe, IReadOnlyList<string[]> CacDong);

/// <summary>
/// Xuat phieu/bien ban/bao cao ra PDF bang QuestPDF.
/// Dung font he thong ho tro tieng Viet; bo cuc theo mau van ban hanh chinh Viet Nam.
/// </summary>
public static class BoXuatPdf
{
    private const string TenFont = "Times New Roman";

    /// <summary>Cau hinh giay phep QuestPDF - goi mot lan luc khoi dong ung dung.</summary>
    public static void CauHinhGiayPhep() => QuestPDF.Settings.License = LicenseType.Community;

    public static byte[] XuatTaiLieu(
        string tenCoQuanChuQuan,
        string tenDonVi,
        string tieuDe,
        string? phuDe,
        IReadOnlyList<DongThongTin> thongTin,
        IReadOnlyList<BangPdf>? bang = null,
        string? noiDungThem = null,
        string? nguoiKy = null,
        string? chucVuNguoiKy = null)
    {
        var tep = Document.Create(tap =>
        {
            tap.Page(trang =>
            {
                trang.Size(PageSizes.A4);
                trang.Margin(2, Unit.Centimetre);
                trang.DefaultTextStyle(x => x.FontFamily(TenFont).FontSize(12));

                trang.Header().Element(e => VeTieuDeVanBan(e, tenCoQuanChuQuan, tenDonVi, tieuDe, phuDe));

                trang.Content().PaddingVertical(10).Column(cot =>
                {
                    cot.Spacing(8);

                    foreach (var d in thongTin)
                    {
                        cot.Item().Row(dong =>
                        {
                            dong.ConstantItem(180).Text($"{d.Nhan}:").SemiBold();
                            dong.RelativeItem().Text(d.GiaTri ?? string.Empty);
                        });
                    }

                    if (!string.IsNullOrWhiteSpace(noiDungThem))
                    {
                        cot.Item().PaddingTop(10).Text(noiDungThem).Justify();
                    }

                    if (bang is { Count: > 0 })
                    {
                        foreach (var b in bang)
                        {
                            cot.Item().PaddingTop(12).Text(b.TieuDe).Bold();
                            cot.Item().Element(e => VeBang(e, b));
                        }
                    }
                });

                trang.Footer().Column(cot =>
                {
                    if (!string.IsNullOrWhiteSpace(nguoiKy))
                    {
                        cot.Item().PaddingBottom(10).AlignRight().Column(ky =>
                        {
                            ky.Item().AlignCenter().Text(chucVuNguoiKy ?? string.Empty).SemiBold();
                            ky.Item().Height(50);
                            ky.Item().AlignCenter().Text(nguoiKy).Bold();
                        });
                    }

                    cot.Item().AlignCenter().Text(x =>
                    {
                        x.CurrentPageNumber();
                        x.Span(" / ");
                        x.TotalPages();
                    });
                });
            });
        });

        return tep.GeneratePdf();
    }

    private static void VeTieuDeVanBan(
        IContainer o, string tenCoQuanChuQuan, string tenDonVi, string tieuDe, string? phuDe)
    {
        o.Column(cot =>
        {
            cot.Item().Row(dong =>
            {
                dong.RelativeItem().Column(trai =>
                {
                    trai.Item().AlignCenter().Text(tenCoQuanChuQuan.ToUpperInvariant()).FontSize(11);
                    trai.Item().AlignCenter().Text(tenDonVi.ToUpperInvariant()).Bold().FontSize(11);
                    trai.Item().AlignCenter().PaddingHorizontal(30).LineHorizontal(1);
                });

                dong.RelativeItem().Column(phai =>
                {
                    phai.Item().AlignCenter().Text("CỘNG HÒA XÃ HỘI CHỦ NGHĨA VIỆT NAM").Bold().FontSize(11);
                    phai.Item().AlignCenter().Text("Độc lập - Tự do - Hạnh phúc").Bold().FontSize(11);
                    phai.Item().AlignCenter().PaddingHorizontal(40).LineHorizontal(1);
                });
            });

            cot.Item().PaddingTop(18).AlignCenter().Text(tieuDe.ToUpperInvariant()).Bold().FontSize(14);

            if (!string.IsNullOrWhiteSpace(phuDe))
            {
                cot.Item().AlignCenter().Text(phuDe).Italic();
            }

            cot.Item().PaddingTop(10).LineHorizontal(1);
        });
    }

    private static void VeBang(IContainer o, BangPdf bang)
    {
        o.Table(bangVe =>
        {
            bangVe.ColumnsDefinition(cot =>
            {
                cot.ConstantColumn(30);
                foreach (var _ in bang.CotTieuDe)
                {
                    cot.RelativeColumn();
                }
            });

            bangVe.Header(tieuDe =>
            {
                tieuDe.Cell().Element(OTieuDe).Text("STT");
                foreach (var c in bang.CotTieuDe)
                {
                    tieuDe.Cell().Element(OTieuDe).Text(c);
                }
            });

            var stt = 1;
            foreach (var dong in bang.CacDong)
            {
                bangVe.Cell().Element(ODuLieu).Text(stt++.ToString(CultureInfo.InvariantCulture));
                foreach (var giaTri in dong)
                {
                    bangVe.Cell().Element(ODuLieu).Text(giaTri);
                }
            }
        });

        static IContainer OTieuDe(IContainer o) => o
            .Border(0.5f)
            .Background(Colors.Grey.Lighten3)
            .Padding(4)
            .DefaultTextStyle(x => x.SemiBold());

        static IContainer ODuLieu(IContainer o) => o.Border(0.5f).Padding(4);
    }
}
