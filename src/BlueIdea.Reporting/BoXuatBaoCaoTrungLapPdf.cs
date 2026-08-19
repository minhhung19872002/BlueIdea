using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace BlueIdea.Reporting;

/// <summary>Mot ho so bi doi chieu trung, kem cac doan trung cu the.</summary>
public sealed record DongDoiChieuTrungLapPdf(
    string MaHoSo,
    string TenSangKien,
    string? TenDonVi,
    decimal TyLeTuongDong,
    decimal TyLeTuVung,
    decimal TyLeNguNghia,
    int SoDoanTrung,
    IReadOnlyList<CapDoanTrungPdf> CacDoanTrung);

/// <summary>Mot cap doan van trung nhau (nguon - doi chieu).</summary>
public sealed record CapDoanTrungPdf(string DoanNguon, string DoanDich, decimal TyLe);

/// <summary>Du lieu day du cua mot ban bao cao trung lap.</summary>
public sealed record BaoCaoTrungLapPdf(
    string MaHoSo,
    string TenSangKien,
    string? TenTacGiaChinh,
    string? TenDonVi,
    DateTimeOffset NgayChay,
    string PhienBanThuatToan,
    string? TenMoHinhNhung,
    int TongSoDoiChieu,
    decimal TyLeCaoNhat,
    string MucCanhBao,
    bool DaXemXet,
    string? YKienHoiDong,
    IReadOnlyList<DongDoiChieuTrungLapPdf> ChiTiet);

/// <summary>
/// Chuc nang 26 - Xuat bao cao kiem tra trung lap ra PDF de dinh kem ho so hoi dong.
///
/// Bao cao in ca doan van trung cu the chu khong chi ty le: hoi dong phai doc duoc CAI GI trung
/// moi ket luan duoc, con mot con so phan tram khong tu bao ve duoc truoc tac gia bi ket luan.
/// </summary>
public static class BoXuatBaoCaoTrungLapPdf
{
    private const string TenFont = "Times New Roman";

    /// <summary>So doan trung in ra cho moi ho so doi chieu — du de doc, khong lam bao cao dai vo han.</summary>
    private const int SoDoanToiDaMoiHoSo = 5;

    /// <summary>Do dai toi da cua mot doan trich dan trong bao cao.</summary>
    private const int DoDaiTrichDanToiDa = 600;

    public static byte[] Xuat(
        string tenCoQuanChuQuan, string tenDonVi, BaoCaoTrungLapPdf baoCao)
    {
        ArgumentNullException.ThrowIfNull(baoCao);

        var tep = Document.Create(tap =>
        {
            tap.Page(trang =>
            {
                trang.Size(PageSizes.A4);
                trang.Margin(2, Unit.Centimetre);
                trang.DefaultTextStyle(x => x.FontFamily(TenFont).FontSize(11));

                trang.Header().Element(e => VeTieuDe(e, tenCoQuanChuQuan, tenDonVi));
                trang.Content().PaddingVertical(8).Element(e => VeNoiDung(e, baoCao));

                trang.Footer().AlignCenter().Text(x =>
                {
                    x.DefaultTextStyle(s => s.FontSize(9).Italic());
                    x.Span("Trang ");
                    x.CurrentPageNumber();
                    x.Span(" / ");
                    x.TotalPages();
                });
            });
        });

        return tep.GeneratePdf();
    }

    private static void VeTieuDe(IContainer khung, string tenCoQuanChuQuan, string tenDonVi)
        => khung.Column(cot =>
        {
            cot.Item().AlignCenter().Text(tenCoQuanChuQuan.ToUpperInvariant()).FontSize(10);
            cot.Item().AlignCenter().Text(tenDonVi.ToUpperInvariant()).Bold().FontSize(10);
            cot.Item().PaddingTop(10).AlignCenter()
                .Text("BÁO CÁO KIỂM TRA TRÙNG LẶP SÁNG KIẾN").Bold().FontSize(14);
        });

    private static void VeNoiDung(IContainer khung, BaoCaoTrungLapPdf bc)
        => khung.Column(cot =>
        {
            cot.Spacing(8);

            cot.Item().Text(t =>
            {
                t.Span("Mã hồ sơ: ").SemiBold();
                t.Span(bc.MaHoSo);
            });
            cot.Item().Text(t =>
            {
                t.Span("Tên sáng kiến: ").SemiBold();
                t.Span(bc.TenSangKien);
            });

            if (!string.IsNullOrWhiteSpace(bc.TenTacGiaChinh))
            {
                cot.Item().Text(t =>
                {
                    t.Span("Tác giả chính: ").SemiBold();
                    t.Span(bc.TenTacGiaChinh);
                });
            }

            if (!string.IsNullOrWhiteSpace(bc.TenDonVi))
            {
                cot.Item().Text(t =>
                {
                    t.Span("Đơn vị: ").SemiBold();
                    t.Span(bc.TenDonVi);
                });
            }

            cot.Item().Text(t =>
            {
                t.Span("Thời điểm kiểm tra: ").SemiBold();
                t.Span(bc.NgayChay.ToLocalTime().ToString("dd/MM/yyyy HH:mm"));
            });

            cot.Item().Text(t =>
            {
                t.Span("Phiên bản thuật toán: ").SemiBold();
                t.Span(bc.PhienBanThuatToan);

                if (!string.IsNullOrWhiteSpace(bc.TenMoHinhNhung))
                {
                    t.Span($" — mô hình nhúng: {bc.TenMoHinhNhung}");
                }
            });

            cot.Item().PaddingTop(6).Text(t =>
            {
                t.Span("Số hồ sơ đã đối chiếu: ").SemiBold();
                t.Span(bc.TongSoDoiChieu.ToString());
            });

            cot.Item().Text(t =>
            {
                t.Span("Tỷ lệ trùng lặp cao nhất: ").SemiBold();
                t.Span($"{bc.TyLeCaoNhat:0.##}%").Bold();
                t.Span($" — mức cảnh báo: {MoTaMucCanhBao(bc.MucCanhBao)}");
            });

            if (bc.ChiTiet.Count == 0)
            {
                cot.Item().PaddingTop(10).Text(
                    "Không phát hiện hồ sơ nào có nội dung tương đồng đáng kể.").Italic();
            }
            else
            {
                cot.Item().PaddingTop(10).Text("CHI TIẾT ĐỐI CHIẾU").Bold();

                foreach (var (dong, chiMuc) in bc.ChiTiet.Select((d, i) => (d, i + 1)))
                {
                    cot.Item().Element(e => VeMotHoSoDoiChieu(e, dong, chiMuc));
                }
            }

            cot.Item().PaddingTop(12).Text("Ý KIẾN CỦA HỘI ĐỒNG").Bold();
            cot.Item().Text(bc.DaXemXet
                ? bc.YKienHoiDong ?? "(Đã xem xét, không ghi ý kiến)"
                : "(Chưa xem xét)");

            cot.Item().PaddingTop(10).Text(
                    "Kết quả kiểm tra trùng lặp mang tính tham khảo, cảnh báo. "
                    + "Kết luận cuối cùng thuộc thẩm quyền của hội đồng sáng kiến.")
                .Italic().FontSize(9);
        });

    private static void VeMotHoSoDoiChieu(IContainer khung, DongDoiChieuTrungLapPdf dong, int chiMuc)
        => khung.PaddingTop(8).Column(cot =>
        {
            cot.Item().Text(t =>
            {
                t.Span($"{chiMuc}. {dong.MaHoSo} — {dong.TenSangKien}").SemiBold();
            });

            if (!string.IsNullOrWhiteSpace(dong.TenDonVi))
            {
                cot.Item().Text($"Đơn vị: {dong.TenDonVi}").FontSize(10);
            }

            cot.Item().Text(
                    $"Tương đồng tổng hợp {dong.TyLeTuongDong:0.##}% "
                    + $"(từ vựng {dong.TyLeTuVung:0.##}%, ngữ nghĩa {dong.TyLeNguNghia:0.##}%) — "
                    + $"{dong.SoDoanTrung} đoạn trùng")
                .FontSize(10);

            foreach (var doan in dong.CacDoanTrung.Take(SoDoanToiDaMoiHoSo))
            {
                cot.Item().PaddingLeft(12).PaddingTop(4).Column(khoi =>
                {
                    khoi.Item().Text($"• Đoạn trùng {doan.TyLe:0.##}%").FontSize(9).SemiBold();
                    khoi.Item().Text($"Hồ sơ này: {CatBot(doan.DoanNguon)}").FontSize(9).Italic();
                    khoi.Item().Text($"Hồ sơ đối chiếu: {CatBot(doan.DoanDich)}").FontSize(9).Italic();
                });
            }

            if (dong.CacDoanTrung.Count > SoDoanToiDaMoiHoSo)
            {
                cot.Item().PaddingLeft(12).Text(
                        $"… và {dong.CacDoanTrung.Count - SoDoanToiDaMoiHoSo} đoạn trùng khác "
                        + "(xem đầy đủ trên giao diện đối chiếu 2 cột).")
                    .FontSize(9).Italic();
            }
        });

    private static string CatBot(string noiDung)
        => noiDung.Length <= DoDaiTrichDanToiDa
            ? noiDung
            : noiDung[..DoDaiTrichDanToiDa] + "…";

    private static string MoTaMucCanhBao(string ma) => ma switch
    {
        "NGHIEM_TRONG" => "Nghiêm trọng",
        "CANH_BAO" => "Cảnh báo",
        _ => "An toàn"
    };
}
