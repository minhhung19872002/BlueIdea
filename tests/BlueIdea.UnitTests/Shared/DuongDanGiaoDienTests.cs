using System.Text.RegularExpressions;
using BlueIdea.Domain.Chung;

namespace BlueIdea.UnitTests.Shared;

/// <summary>
/// Doi chieu duong dan may chu gan vao thong bao voi route khai trong <c>web/src/app/router.tsx</c>.
///
/// Day la kiem thu duy nhat noi hai ben lai voi nhau. Truoc khi co no, ca ba lien ket trong thong
/// bao deu tro sai va khong ai biet: duong dan la chuoi ghep o C#, route khai o TypeScript, khong
/// cong cu nao doi chieu giup. Nguoi dung bam vao thong bao thi ra trang 404 noi rang trang khong
/// ton tai hoac ho khong co quyen — trong khi ho so co that va ho co quyen xem.
/// </summary>
public class DuongDanGiaoDienTests
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");

    public static TheoryData<string> DuongDanMayChuSinhRa() => new()
    {
        DuongDanGiaoDien.ChiTietHoSo(Id),
        DuongDanGiaoDien.ChamDiem(Id),
        DuongDanGiaoDien.ChiTietHoiDong(Id),
        DuongDanGiaoDien.DanhSachViecDanhGia,
    };

    [Theory]
    [MemberData(nameof(DuongDanMayChuSinhRa))]
    public void Moi_Duong_Dan_Deu_Khop_Mot_Route_Cua_Web(string duongDan)
    {
        var route = DocRouteTuRouterTsx();

        route.Should().NotBeEmpty("không đọc được router.tsx thì phép kiểm này vô nghĩa");

        route.Should().Contain(
            m => m.IsMatch(duongDan),
            $"đường dẫn '{duongDan}' phải khớp một route khai trong web/src/app/router.tsx — "
            + "không khớp nghĩa là người dùng bấm vào thông báo sẽ ra trang 404");
    }

    [Fact]
    public void Duong_Dan_Luon_Bat_Dau_Bang_Dau_Gach_Cheo()
    {
        // React Router hieu duong dan khong co dau gach cheo dau la duong dan TUONG DOI so voi
        // trang hien tai, nen cung mot thong bao se mo ra cho khac nhau tuy nguoi dung dang o dau.
        foreach (var d in new[]
                 {
                     DuongDanGiaoDien.ChiTietHoSo(Id),
                     DuongDanGiaoDien.ChamDiem(Id),
                     DuongDanGiaoDien.ChiTietHoiDong(Id),
                     DuongDanGiaoDien.DanhSachViecDanhGia,
                 })
        {
            d.Should().StartWith("/");
        }
    }

    // ---------------------------------------------------------------------------------

    /// <summary>
    /// Doc cac route khai trong router.tsx roi doi thanh bieu thuc chinh quy.
    /// <c>sang-kien/:id</c> thanh <c>^/sang-kien/[^/]+$</c>.
    /// </summary>
    private static List<Regex> DocRouteTuRouterTsx()
    {
        var tep = TimRouterTsx();

        if (tep is null) return new List<Regex>();

        var noiDung = File.ReadAllText(tep);

        return Regex.Matches(noiDung, @"path:\s*'([^']+)'")
            .Select(m => m.Groups[1].Value)
            .Where(x => x != "*" && !string.IsNullOrWhiteSpace(x))
            .Select(x => x.StartsWith('/') ? x : "/" + x)
            .Select(x => new Regex("^" + Regex.Replace(Regex.Escape(x), @":\w+", "[^/]+") + "$"))
            .ToList();
    }

    private static string? TimRouterTsx()
    {
        var thuMuc = new DirectoryInfo(AppContext.BaseDirectory);

        while (thuMuc is not null)
        {
            var ungVien = Path.Combine(thuMuc.FullName, "web", "src", "app", "router.tsx");
            if (File.Exists(ungVien)) return ungVien;

            thuMuc = thuMuc.Parent;
        }

        return null;
    }
}
