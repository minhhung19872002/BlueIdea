using System.Text.RegularExpressions;

namespace BlueIdea.UnitTests.Shared;

/// <summary>
/// Moi dia chi ghi trong BANG TRUY VET 51 CHUC NANG cua dac ta phai co route that trong
/// <c>web/src/app/router.tsx</c>.
///
/// Day la bang ma nguoi nghiem thu di theo. Truoc khi co phep kiem nay, ba dia chi trong bang
/// dan toi trang 404 hoac trang trang — khong phai vi chuc nang thieu, ma vi no nam o dia chi
/// khac. Nguoi kiem tra di dung theo tai lieu se ket luan la chua lam.
///
/// Doc thang tu tep dac ta chu khong chep tay danh sach: sua bang truy vet ma quen route thi
/// phep kiem tu phat hien.
/// </summary>
public class BangTruyVet51ChucNangTests
{
    [Fact]
    public void Moi_Dia_Chi_Trong_Bang_Truy_Vet_Deu_Co_Route()
    {
        var route = DocRouteTuRouterTsx();
        route.Should().NotBeEmpty("không đọc được router.tsx thì phép kiểm này vô nghĩa");

        var thieu = DocDiaChiTrongBangTruyVet()
            .Where(d => !route.Any(m => m.IsMatch(d)))
            .ToList();

        thieu.Should().BeEmpty(
            "đây là bảng người nghiệm thu đi theo; địa chỉ không có route nghĩa là họ mở ra "
            + "thấy trang trắng hoặc 404 rồi kết luận chức năng chưa làm");
    }

    [Fact]
    public void Bang_Truy_Vet_Van_Con_Du_51_Dong()
    {
        DocSoChucNang().Should().Be(51,
            "đặc tả ghi rõ 51 chức năng và không được bỏ sót dòng nào");
    }

    // ---------------------------------------------------------------------------------

    /// <summary>Cac dia chi dang <c>`/duong-dan`</c> trong bang truy vet (Muc 14).</summary>
    private static IEnumerable<string> DocDiaChiTrongBangTruyVet()
    {
        var bang = DocBangTruyVet();

        return Regex.Matches(bang, @"`(/[^`]*)`")
            .Select(m => m.Groups[1].Value)
            .Where(x => !x.Contains(' '))
            .Distinct(StringComparer.Ordinal)
            // Tham so trong dac ta viet la :id — doi thanh gia tri gia de so khop route.
            .Select(x => Regex.Replace(x, @":\w+", "gia-tri"));
    }

    private static int DocSoChucNang()
        => Regex.Matches(DocBangTruyVet(), @"^\|\s*\d+\s*\|", RegexOptions.Multiline).Count;

    private static string DocBangTruyVet()
    {
        var noiDung = DocTep(Path.Combine("docs", "00-MASTER-SPEC.md"))
            ?? throw new FileNotFoundException("Không tìm thấy docs/00-MASTER-SPEC.md");

        var dau = noiDung.IndexOf("## 14.", StringComparison.Ordinal);
        dau.Should().BeGreaterThan(0, "phải có Mục 14 — bảng truy vết");

        var cuoi = noiDung.IndexOf("## 15.", dau, StringComparison.Ordinal);

        return cuoi > dau ? noiDung[dau..cuoi] : noiDung[dau..];
    }

    private static List<Regex> DocRouteTuRouterTsx()
    {
        var noiDung = DocTep(Path.Combine("web", "src", "app", "router.tsx"));

        if (noiDung is null) return new List<Regex>();

        return Regex.Matches(noiDung, @"path:\s*'([^']+)'")
            .Select(m => m.Groups[1].Value)
            .Where(x => x != "*" && !string.IsNullOrWhiteSpace(x))
            .Select(x => x.StartsWith('/') ? x : "/" + x)
            .Select(x => new Regex("^" + Regex.Replace(Regex.Escape(x), @":\w+", "[^/]+") + "$"))
            .ToList();
    }

    private static string? DocTep(string duongDanTuongDoi)
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            var ungVien = Path.Combine(thuMuc.FullName, duongDanTuongDoi);
            if (File.Exists(ungVien)) return File.ReadAllText(ungVien);

            thuMuc = thuMuc.Parent;
        }

        return null;
    }
}
