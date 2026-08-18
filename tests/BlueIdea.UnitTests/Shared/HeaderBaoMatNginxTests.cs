using System.Text.RegularExpressions;

namespace BlueIdea.UnitTests.Shared;

/// <summary>
/// Doi chieu cau hinh Nginx: moi khoi <c>location</c> phai tu khai du bo header bao mat.
///
/// Nginx bo TOAN BO <c>add_header</c> ke thua tu cap ngoai ngay khi cap trong khai du mot
/// <c>add_header</c> cua rieng no. Bon header bao mat tung nam o cap server, con hai location
/// deu co <c>add_header Cache-Control</c>, nen ca bon bien mat tren MOI phan hoi — khai bao van
/// nam trong tep, van nam trong tai lieu, va chua bao gio di ra khoi may chu.
///
/// Kiem tra o day chu khong phai bang <c>curl</c> vi loi nay khong lam gi vo ca: khong ai phat
/// hien cho toi luc co nguoi doc ky header, hoac toi luc danh gia an toan thong tin.
/// </summary>
public class HeaderBaoMatNginxTests
{
    private static readonly string[] HeaderBatBuoc =
    {
        "X-Content-Type-Options",
        "X-Frame-Options",
        "Referrer-Policy",
        "Permissions-Policy",
    };

    [Fact]
    public void Moi_Location_Khai_Add_Header_Deu_Phai_Khai_Du_Header_Bao_Mat()
    {
        var noiDung = DocCauHinh("web.conf");

        foreach (var (ten, than) in TachLocation(noiDung))
        {
            if (!than.Contains("add_header", StringComparison.Ordinal)) continue;

            foreach (var header in HeaderBatBuoc)
            {
                than.Should().Contain(header,
                    $"location '{ten}' có add_header riêng nên Nginx bỏ hết header kế thừa; "
                    + $"thiếu {header} là phản hồi của location này đi ra mà không có nó");
            }
        }
    }

    [Fact]
    public void Cau_Hinh_May_Chu_Co_Csp_Va_Hsts()
    {
        var noiDung = DocCauHinh("blueidea.conf");

        noiDung.Should().Contain("Content-Security-Policy",
            "trang HTML là nơi CSP thực sự ràng buộc việc chạy script của một ứng dụng SPA");
        noiDung.Should().Contain("Strict-Transport-Security",
            "TLS kết thúc ở máy chủ này nên HSTS phải phát từ đây");
    }

    /// <summary>
    /// CSP khong duoc mo 'unsafe-inline' cho script — do chinh la duong chen ma ma CSP sinh ra
    /// de chan. Rieng style thi buoc phai mo vi Ant Design chen the style luc chay.
    /// </summary>
    [Fact]
    public void Csp_Khong_Cho_Phep_Script_Noi_Tuyen()
    {
        var noiDung = DocCauHinh("blueidea.conf");

        var csp = Regex.Match(noiDung, @"Content-Security-Policy\s+""([^""]+)""").Groups[1].Value;

        csp.Should().NotBeNullOrWhiteSpace();

        var scriptSrc = csp.Split(';')
            .Select(x => x.Trim())
            .FirstOrDefault(x => x.StartsWith("script-src", StringComparison.Ordinal));

        scriptSrc.Should().NotBeNull();
        scriptSrc.Should().NotContain("unsafe-inline");
        scriptSrc.Should().NotContain("unsafe-eval");
    }

    // ---------------------------------------------------------------------------------

    /// <summary>Tach cac khoi <c>location ... { ... }</c> o cap mot.</summary>
    private static IEnumerable<(string Ten, string Than)> TachLocation(string noiDung)
    {
        foreach (Match m in Regex.Matches(noiDung, @"location\s+([^\s{]+)\s*\{"))
        {
            var batDau = m.Index + m.Length;
            var sau = 1;
            var i = batDau;

            while (i < noiDung.Length && sau > 0)
            {
                if (noiDung[i] == '{') sau++;
                else if (noiDung[i] == '}') sau--;
                i++;
            }

            yield return (m.Groups[1].Value, noiDung[batDau..(i - 1)]);
        }
    }

    private static string DocCauHinh(string ten)
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            var ungVien = Path.Combine(thuMuc.FullName, "deploy", "nginx", ten);
            if (File.Exists(ungVien)) return File.ReadAllText(ungVien);

            thuMuc = thuMuc.Parent;
        }

        throw new FileNotFoundException($"Không tìm thấy deploy/nginx/{ten}");
    }
}
