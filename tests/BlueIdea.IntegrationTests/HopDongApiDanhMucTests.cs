using System.Net;
using System.Text.RegularExpressions;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Doi chieu hop dong giua giao dien va may chu cho cac danh muc dung chung.
///
/// Giao dien dung <c>taoApiDanhMuc('&lt;goc&gt;')</c> de sinh san sau loi goi chuan — trong do co
/// <c>&lt;goc&gt;/chon</c> de do vao o chon. Neu controller tuong ung khong khai <c>chon</c> thi o
/// chon trong tron ma giao dien khong bao gi ca: nguoi dung mo bo loc ra thay khong co lua chon
/// nao va tuong la he thong chua co du lieu.
///
/// Day chinh la chuyen da xay ra voi dot de nghi: 11 man hinh goi
/// <c>/api/v1/danh-muc/dot-de-nghi/chon</c>, endpoint khong ton tai, va loc theo dot khong dung
/// duoc o dau ca — tu trang chu, bao cao, tra cuu cho toi quyet dinh.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class HopDongApiDanhMucTests
{
    private readonly UngDungKiemThu _ungDung;

    public HopDongApiDanhMucTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Moi_Danh_Muc_Giao_Dien_Dung_Deu_Co_Endpoint_Chon()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var thieu = new List<string>();

        foreach (var goc in DocCacGocDanhMuc())
        {
            var phanHoi = await admin.GetAsync($"{goc}/chon");

            if (phanHoi.StatusCode == HttpStatusCode.NotFound)
            {
                thieu.Add($"{goc}/chon");
            }
        }

        thieu.Should().BeEmpty(
            "giao diện gọi các đường dẫn này để đổ vào ô chọn; thiếu endpoint thì ô chọn "
            + "trống trơn mà không có thông báo nào");
    }

    /// <summary>
    /// Doc cac goc khai bao qua <c>taoApiDanhMuc('...')</c> trong web/src/api/endpoints.ts.
    ///
    /// Doc tu ma nguon that thay vi liet ke tay: them mot danh muc moi ma quen endpoint thi phep
    /// kiem nay phai tu phat hien, khong doi ai nho cap nhat danh sach.
    /// </summary>
    private static IEnumerable<string> DocCacGocDanhMuc()
    {
        var tep = TimEndpointsTs();

        if (tep is null) yield break;

        var noiDung = File.ReadAllText(tep);

        var da = new HashSet<string>(StringComparer.Ordinal);

        foreach (Match m in Regex.Matches(noiDung, @"taoApiDanhMuc(?:<[^>]*>)?\(\s*'([^']+)'"))
        {
            var goc = m.Groups[1].Value;

            if (goc.StartsWith("/api/", StringComparison.Ordinal) && da.Add(goc))
            {
                yield return goc;
            }
        }
    }

    private static string? TimEndpointsTs()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            var ungVien = Path.Combine(thuMuc.FullName, "web", "src", "api", "endpoints.ts");
            if (File.Exists(ungVien)) return ungVien;

            thuMuc = thuMuc.Parent;
        }

        return null;
    }
}
