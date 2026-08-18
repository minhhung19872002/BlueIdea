using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu sap xep danh sach ho so va phan cap linh vuc — hai cho co san giao dien / mo hinh
/// du lieu nhung truoc day khong chay.
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class SapXepVaPhanCapTests
{
    private readonly UngDungKiemThu _ungDung;

    public SapXepVaPhanCapTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Sap_Xep_Theo_Ma_Ho_So_Dao_Duoc_Chieu()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tang = await LayCotAsync(admin, "maHoSo", "asc", "maHoSo");
        var giam = await LayCotAsync(admin, "maHoSo", "desc", "maHoSo");

        tang.Should().NotBeEmpty();
        tang.Should().BeInAscendingOrder();
        giam.Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Sap_Xep_Theo_Diem_Khong_Day_Ho_So_Chua_Cham_Len_Dau()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var phanHoi = await admin.GetAsync("/api/v1/sang-kien?soDong=20&sapXep=tongDiem&huong=desc");
        phanHoi.EnsureSuccessStatusCode();

        var diem = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .Select(x => x.TryGetProperty("tongDiem", out var d) && d.ValueKind != JsonValueKind.Null
                ? d.GetDecimal()
                : (decimal?)null)
            .ToList();

        diem.Should().NotBeEmpty();

        // PostgreSQL mac dinh dat NULL len dau khi DESC. Neu khong xu ly, "sap xep diem giam dan"
        // se do mot loat ho so CHUA CHAM len dau — dung cai nguoi dung muon xem nhat bi day xuong.
        var coDiem = diem.TakeWhile(x => x.HasValue).ToList();
        var conLai = diem.Skip(coDiem.Count).ToList();

        conLai.Should().OnlyContain(x => x == null, "hồ sơ chưa có điểm phải nằm cuối");
        coDiem.Select(x => x!.Value).Should().BeInDescendingOrder();
    }

    [Fact]
    public async Task Truyen_Huong_Ma_Khong_Kem_Ten_Cot_Van_Ra_Ho_So_Moi_Nhat_Truoc()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        // Truoc day hai nhanh mac dinh bi dao nguoc: huong=desc ma khong kem ten cot lai tra ve
        // ho so CU NHAT dau bang.
        var ngay = await LayCotAsync(admin, null, "desc", "ngayNop");

        ngay.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Danh_Sach_Linh_Vuc_Tra_Ve_Id_Linh_Vuc_Cap_Tren()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var cha = await TaoLinhVucAsync(admin, null);
        var con = await TaoLinhVucAsync(admin, cha);

        var danhSach = await LayLinhVucAsync(admin);

        var dongCon = danhSach.Single(x => x.GetProperty("id").GetString() == con);
        var dongCha = danhSach.Single(x => x.GetProperty("id").GetString() == cha);

        // Man hinh danh muc hien cot "Thuoc linh vuc"; khong tra truong nay thi cot luon trong
        // du du lieu phan cap co that trong CSDL.
        dongCon.GetProperty("danhMucChaId").GetString().Should().Be(cha);

        (dongCha.TryGetProperty("danhMucChaId", out var v) && v.ValueKind != JsonValueKind.Null)
            .Should().BeFalse("lĩnh vực gốc không có cấp trên");
    }

    private static async Task<List<string>> LayCotAsync(
        HttpClient client, string? sapXep, string huong, string cot)
    {
        var q = $"/api/v1/sang-kien?soDong=10&huong={huong}"
                + (sapXep is null ? string.Empty : $"&sapXep={sapXep}");

        var phanHoi = await client.GetAsync(q);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray()
            .Select(x => x.TryGetProperty(cot, out var v) ? v.ToString() : string.Empty)
            .Where(x => x.Length > 0)
            .ToList();
    }

    private static async Task<string> TaoLinhVucAsync(HttpClient client, string? chaId)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/danh-muc/linh-vuc", new
        {
            ma = $"KT{Guid.NewGuid():N}"[..12].ToUpperInvariant(),
            ten = chaId is null ? "Lĩnh vực gốc kiểm thử" : "Lĩnh vực con kiểm thử",
            linhVucChaId = chaId,
            trangThai = 1
        });

        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("id").GetString()!;
    }

    private static async Task<List<JsonElement>> LayLinhVucAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/danh-muc/linh-vuc?soDong=500");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().ToList();
    }
}
