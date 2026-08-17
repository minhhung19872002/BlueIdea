using System.Globalization;
using ClosedXML.Excel;

namespace BlueIdea.Reporting;

/// <summary>Dinh nghia mot cot khi xuat Excel.</summary>
public sealed record CotXuat<T>(string TieuDe, Func<T, object?> LayGiaTri, double? DoRong = null);

/// <summary>
/// Xuat bao cao ra Excel bang ClosedXML. Moi bao cao deu co tieu de, thoi diem xuat
/// va dong tong so - phuc vu in an va luu tru ho so nghiem thu.
/// </summary>
public static class BoXuatExcel
{
    private const string MauTieuDe = "#1677FF";

    public static byte[] Xuat<T>(
        string tenBang,
        string tieuDeBaoCao,
        IReadOnlyList<T> duLieu,
        IReadOnlyList<CotXuat<T>> cacCot,
        IReadOnlyDictionary<string, string>? thongTinBoLoc = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(tenBang);
        ArgumentNullException.ThrowIfNull(duLieu);
        ArgumentNullException.ThrowIfNull(cacCot);

        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add(CatTenSheet(tenBang));

        var dong = 1;

        // Tieu de bao cao.
        sheet.Cell(dong, 1).Value = tieuDeBaoCao;
        sheet.Range(dong, 1, dong, Math.Max(cacCot.Count, 1)).Merge();
        sheet.Cell(dong, 1).Style.Font.Bold = true;
        sheet.Cell(dong, 1).Style.Font.FontSize = 14;
        sheet.Cell(dong, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        dong++;

        sheet.Cell(dong, 1).Value =
            $"Thời điểm xuất: {DateTimeOffset.UtcNow.ToOffset(TimeSpan.FromHours(7)):dd/MM/yyyy HH:mm}";
        sheet.Range(dong, 1, dong, Math.Max(cacCot.Count, 1)).Merge();
        sheet.Cell(dong, 1).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        sheet.Cell(dong, 1).Style.Font.Italic = true;
        dong++;

        if (thongTinBoLoc is { Count: > 0 })
        {
            foreach (var (khoa, giaTri) in thongTinBoLoc)
            {
                sheet.Cell(dong, 1).Value = $"{khoa}: {giaTri}";
                sheet.Range(dong, 1, dong, Math.Max(cacCot.Count, 1)).Merge();
                dong++;
            }
        }

        dong++;

        // Dong tieu de cot.
        var dongTieuDe = dong;
        sheet.Cell(dong, 1).Value = "STT";
        for (var i = 0; i < cacCot.Count; i++)
        {
            sheet.Cell(dong, i + 2).Value = cacCot[i].TieuDe;
        }

        var vungTieuDe = sheet.Range(dong, 1, dong, cacCot.Count + 1);
        vungTieuDe.Style.Font.Bold = true;
        vungTieuDe.Style.Font.FontColor = XLColor.White;
        vungTieuDe.Style.Fill.BackgroundColor = XLColor.FromHtml(MauTieuDe);
        vungTieuDe.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
        vungTieuDe.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
        vungTieuDe.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
        vungTieuDe.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
        dong++;

        // Du lieu.
        var stt = 1;
        foreach (var muc in duLieu)
        {
            sheet.Cell(dong, 1).Value = stt++;

            for (var i = 0; i < cacCot.Count; i++)
            {
                DatGiaTri(sheet.Cell(dong, i + 2), cacCot[i].LayGiaTri(muc));
            }

            dong++;
        }

        if (duLieu.Count > 0)
        {
            var vungDuLieu = sheet.Range(dongTieuDe, 1, dong - 1, cacCot.Count + 1);
            vungDuLieu.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            vungDuLieu.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            vungDuLieu.SetAutoFilter();
        }

        dong++;
        sheet.Cell(dong, 1).Value = $"Tổng số: {duLieu.Count} bản ghi";
        sheet.Cell(dong, 1).Style.Font.Bold = true;

        sheet.Column(1).Width = 6;
        for (var i = 0; i < cacCot.Count; i++)
        {
            if (cacCot[i].DoRong.HasValue)
            {
                sheet.Column(i + 2).Width = cacCot[i].DoRong!.Value;
            }
            else
            {
                sheet.Column(i + 2).AdjustToContents(dongTieuDe, dong, 10, 60);
            }
        }

        sheet.SheetView.FreezeRows(dongTieuDe);

        using var bo = new MemoryStream();
        workbook.SaveAs(bo);
        return bo.ToArray();
    }

    private static void DatGiaTri(IXLCell o, object? giaTri)
    {
        switch (giaTri)
        {
            case null:
                o.Value = string.Empty;
                break;
            case DateTimeOffset dto:
                o.Value = dto.ToOffset(TimeSpan.FromHours(7)).DateTime;
                o.Style.DateFormat.Format = "dd/mm/yyyy hh:mm";
                break;
            case DateTime dt:
                o.Value = dt;
                o.Style.DateFormat.Format = "dd/mm/yyyy";
                break;
            case DateOnly d:
                o.Value = d.ToDateTime(TimeOnly.MinValue);
                o.Style.DateFormat.Format = "dd/mm/yyyy";
                break;
            case decimal m:
                o.Value = m;
                o.Style.NumberFormat.Format = "#,##0.##";
                break;
            case double db:
                o.Value = db;
                o.Style.NumberFormat.Format = "#,##0.##";
                break;
            case int i:
                o.Value = i;
                break;
            case long l:
                o.Value = l;
                break;
            case bool b:
                o.Value = b ? "Có" : "Không";
                break;
            default:
                o.Value = Convert.ToString(giaTri, CultureInfo.GetCultureInfo("vi-VN")) ?? string.Empty;
                break;
        }
    }

    /// <summary>Ten sheet cua Excel gioi han 31 ky tu va khong chua ky tu dac biet.</summary>
    private static string CatTenSheet(string ten)
    {
        var kyTuCam = new[] { ':', '\\', '/', '?', '*', '[', ']' };
        var sach = new string(ten.Where(c => !kyTuCam.Contains(c)).ToArray());
        return sach.Length <= 31 ? sach : sach[..31];
    }
}
