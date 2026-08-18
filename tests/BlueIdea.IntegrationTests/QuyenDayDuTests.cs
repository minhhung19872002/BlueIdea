using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.Domain.Chung;
using BlueIdea.Domain.QuanTri;
using BlueIdea.Infrastructure.Persistence;
using BlueIdea.Infrastructure.Seed;
using BlueIdea.IntegrationTests.HaTang;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Moi hang so <c>MaQuyen</c> phai co dong trong bang quyen VA duoc cap cho vai tro Quan tri
/// he thong.
///
/// Thieu mot trong hai la chuc nang tuong ung khoa cung: khong co dong trong bang thi quyen do
/// khong hien tren man hinh ma tran vai tro, nen khong ai cap duoc — ke ca quan tri vien — va
/// endpoint bao ve bang quyen do tra 403 cho tat ca moi nguoi, vinh vien.
///
/// Truoc day seed chi chay khi bang con rong nen loi nay chi phat sinh o he thong DA CAI DAT roi
/// nang cap, tuc dung noi khong ai kiem thu.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class QuyenDayDuTests
{
    private readonly UngDungKiemThu _ungDung;

    public QuyenDayDuTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Moi_Hang_So_MaQuyen_Deu_Co_Trong_Bang_Quyen()
    {
        var (quyen, _) = await LayMaTranAsync();

        var trongBang = quyen.Select(x => x.GetProperty("ma").GetString()).ToHashSet();

        foreach (var ma in TatCaMaQuyen())
        {
            trongBang.Should().Contain(ma,
                $"quyền '{ma}' được code kiểm tra nhưng không có dòng trong bảng, "
                + "nên không hiện trên màn hình vai trò và không ai cấp được");
        }
    }

    [Fact]
    public async Task Quan_Tri_He_Thong_Duoc_Cap_Toan_Bo_Quyen()
    {
        var (quyen, vaiTro) = await LayMaTranAsync();

        var quanTri = vaiTro.First(x => x.GetProperty("ma").GetString() == MaVaiTro.QuanTriHeThong);

        var daCap = quanTri.GetProperty("quyenIds").EnumerateArray()
            .Select(x => x.GetString())
            .ToHashSet();

        var thieu = quyen
            .Where(q => !daCap.Contains(q.GetProperty("id").GetString()))
            .Select(q => q.GetProperty("ma").GetString())
            .ToList();

        thieu.Should().BeEmpty(
            "đặc tả định nghĩa vai trò này là toàn quyền cấu hình; "
            + "thiếu quyền nào là chức năng tương ứng khoá cứng với mọi người");
    }

    /// <summary>
    /// Kich ban that su gay loi: he thong DA CAI DAT roi nang cap len ban co them quyen moi.
    ///
    /// Mo phong bang cach xoa mot quyen cung moi lien ket cua no roi chay lai seed. Truoc khi sua,
    /// seed thay bang quyen khong rong nen bo qua hoan toan va quyen do khong bao gio quay lai.
    /// </summary>
    [Fact]
    public async Task Nang_Cap_He_Thong_Da_Cai_Dat_Bo_Sung_Duoc_Quyen_Con_Thieu()
    {
        using var pham = _ungDung.Services.CreateScope();
        var db = pham.ServiceProvider.GetRequiredService<AppDbContext>();

        const string maThu = MaQuyen.BaoCaoXem;

        var quyen = await db.Quyen.FirstAsync(x => x.Ma == maThu);
        var lienKet = await db.Set<VaiTroQuyen>().Where(x => x.QuyenId == quyen.Id).ToListAsync();

        db.Set<VaiTroQuyen>().RemoveRange(lienKet);
        db.Quyen.Remove(quyen);
        await db.SaveChangesAsync();

        (await db.Quyen.AnyAsync(x => x.Ma == maThu)).Should().BeFalse("vừa xoá để mô phỏng");

        // Chay lai seed dung nhu luc ung dung khoi dong sau khi nang cap.
        var seed = pham.ServiceProvider.GetRequiredService<DuLieuMau>();
        await seed.ChayAsync();

        var quyenMoi = await db.Quyen.FirstOrDefaultAsync(x => x.Ma == maThu);

        quyenMoi.Should().NotBeNull("nâng cấp phải bổ sung được quyền còn thiếu");

        var quanTri = await db.VaiTro
            .Include(x => x.DanhSachQuyen)
            .FirstAsync(x => x.Ma == MaVaiTro.QuanTriHeThong);

        quanTri.DanhSachQuyen.Select(x => x.QuyenId).Should().Contain(quyenMoi!.Id,
            "quyền mới phải được cấp cho vai trò toàn quyền, nếu không thì không ai dùng được");
    }

    // ---------------------------------------------------------------------------------

    private async Task<(List<JsonElement> Quyen, List<JsonElement> VaiTro)> LayMaTranAsync()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/he-thong/vai-tro");
        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        return (
            duLieu.GetProperty("quyen").EnumerateArray().ToList(),
            duLieu.GetProperty("vaiTro").EnumerateArray().ToList());
    }

    private static IEnumerable<string> TatCaMaQuyen()
        => typeof(MaQuyen)
            .GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Static)
            .Where(f => f.IsLiteral && f.FieldType == typeof(string))
            .Select(f => (string)f.GetRawConstantValue()!)
            .Distinct();
}
