using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BlueIdea.IntegrationTests.HaTang;

namespace BlueIdea.IntegrationTests;

/// <summary>
/// Kiem thu dot 9: CRUD rieng cho thanh phan ho so, bo nho dem bao cao va bo loc nam
/// tren danh sach ho so (truoc day khai bao nhung khong duoc ap dung).
/// </summary>
[Collection(BoKiemThuTichHop.Ten)]
public sealed class ThanhPhanVaBoNhoDemTests
{
    private readonly UngDungKiemThu _ungDung;

    public ThanhPhanVaBoNhoDemTests(UngDungKiemThu ungDung) => _ungDung = ungDung;

    [Fact]
    public async Task Them_Sua_Xoa_Thanh_Phan_Ho_So_Khong_Can_Gui_Ca_So_Do()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var quyTrinhId = await TaoQuyTrinhNhapAsync(admin);
        var ma = $"KT{Guid.NewGuid():N}"[..10].ToUpperInvariant();

        var them = await admin.PostAsJsonAsync(
            $"/api/v1/quy-trinh/{quyTrinhId}/thanh-phan-ho-so",
            new { ma, ten = "Thành phần kiểm thử", batBuoc = true, loaiDuLieu = "CA_HAI",
                  dungLuongToiDaMb = 20, soLuongToiDa = 3, thuTu = 1 });

        them.EnsureSuccessStatusCode();
        var id = (await them.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetString()!;

        var trung = await admin.PostAsJsonAsync(
            $"/api/v1/quy-trinh/{quyTrinhId}/thanh-phan-ho-so",
            new { ma, ten = "Trùng mã", batBuoc = false, loaiDuLieu = "TEP",
                  dungLuongToiDaMb = 20, soLuongToiDa = 1, thuTu = 2 });

        trung.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var sua = await admin.PutAsJsonAsync(
            $"/api/v1/quy-trinh/{quyTrinhId}/thanh-phan-ho-so/{id}",
            new { ma, ten = "Tên đã sửa", batBuoc = false, loaiDuLieu = "VAN_BAN",
                  dungLuongToiDaMb = 20, soLuongToiDa = 1, soKyTuToiThieu = 100,
                  soKyTuToiDa = 5000, thuTu = 1 });

        sua.EnsureSuccessStatusCode();

        var danhSach = await LayThanhPhanAsync(admin, quyTrinhId);
        danhSach.Single(x => x.GetProperty("id").GetString() == id)
            .GetProperty("ten").GetString().Should().Be("Tên đã sửa");

        var xoa = await admin.DeleteAsync($"/api/v1/quy-trinh/{quyTrinhId}/thanh-phan-ho-so/{id}");
        xoa.EnsureSuccessStatusCode();

        (await LayThanhPhanAsync(admin, quyTrinhId))
            .Should().NotContain(x => x.GetProperty("id").GetString() == id);
    }

    [Fact]
    public async Task So_Ky_Tu_Toi_Thieu_Lon_Hon_Toi_Da_Thi_Bi_Chan()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var quyTrinhId = await TaoQuyTrinhNhapAsync(admin);

        // Cau hinh nay lam nguoi nop khong bao gio nhap hop le duoc — phai chan ngay luc luu.
        var phanHoi = await admin.PostAsJsonAsync(
            $"/api/v1/quy-trinh/{quyTrinhId}/thanh-phan-ho-so",
            new { ma = "KT_KHOANG", ten = "Khoảng ký tự sai", batBuoc = true,
                  loaiDuLieu = "VAN_BAN", dungLuongToiDaMb = 20, soLuongToiDa = 1,
                  soKyTuToiThieu = 5000, soKyTuToiDa = 100, thuTu = 1 });

        phanHoi.StatusCode.Should().Be(HttpStatusCode.UnprocessableEntity);
    }

    [Fact]
    public async Task Bao_Cao_Tra_Ve_Ket_Qua_On_Dinh_Trong_Thoi_Gian_Dem()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var lan1 = await LayTongQuanAsync(admin);
        var lan2 = await LayTongQuanAsync(admin);

        lan2.Should().Be(lan1);
    }

    [Fact]
    public async Task Moi_Tai_Khoan_Co_Muc_Dem_Rieng()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");
        var canBo = await _ungDung.TaoClientDaDangNhapAsync("tiepnhan");

        var adminLan1 = await LayTongQuanAsync(admin);

        var phanHoi = await canBo.GetAsync("/api/v1/bao-cao/tong-quan");

        if (phanHoi.StatusCode == HttpStatusCode.Forbidden)
        {
            return; // Tai khoan nay khong co quyen bao cao — khong co gi de so sanh.
        }

        phanHoi.EnsureSuccessStatusCode();

        var canBoLan1 = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("tongHoSo").GetInt32();
        var canBoLan2 = await LayTongQuanAsync(canBo);
        var adminLan2 = await LayTongQuanAsync(admin);

        // Moi tai khoan on dinh tren muc dem CUA RIENG NO. Lan goi cua can bo khong duoc ghi de
        // muc dem cua admin va nguoc lai — neu khoa dem khong gom danh tinh, mot trong hai con
        // so nay se nhay sang gia tri cua nguoi kia.
        canBoLan2.Should().Be(canBoLan1);
        adminLan2.Should().Be(adminLan1);
    }

    [Fact]
    public async Task Loc_Theo_Nam_Tren_Danh_Sach_Ho_So_Thuc_Su_Chay()
    {
        var admin = await _ungDung.TaoClientDaDangNhapAsync("admin");

        var tatCa = await DemHoSoAsync(admin, "/api/v1/sang-kien?soDong=1");
        var namTrong = await DemHoSoAsync(admin, "/api/v1/sang-kien?soDong=1&nam=1990");

        tatCa.Should().BeGreaterThan(0);
        namTrong.Should().Be(0);
    }

    private static async Task<int> DemHoSoAsync(HttpClient client, string duongDan)
    {
        var phanHoi = await client.GetAsync(duongDan);
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("tongSo").GetInt32();
    }

    private static async Task<int> LayTongQuanAsync(HttpClient client)
    {
        var phanHoi = await client.GetAsync("/api/v1/bao-cao/tong-quan");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").GetProperty("tongHoSo").GetInt32();
    }

    private static async Task<List<JsonElement>> LayThanhPhanAsync(HttpClient client, string id)
    {
        var phanHoi = await client.GetAsync($"/api/v1/quy-trinh/{id}/thanh-phan-ho-so");
        phanHoi.EnsureSuccessStatusCode();

        return (await phanHoi.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("duLieu").EnumerateArray().ToList();
    }

    /// <summary>Quy trinh dang ap dung khong sua cau hinh duoc, nen kiem thu tao ban nhap rieng.</summary>
    private static async Task<string> TaoQuyTrinhNhapAsync(HttpClient client)
    {
        var phanHoi = await client.PostAsJsonAsync("/api/v1/quy-trinh", new
        {
            ma = $"KT_QT_{Guid.NewGuid():N}"[..20].ToUpperInvariant(),
            ten = "Quy trình kiểm thử thành phần",
            cap = "CO_SO",
            trangThai = 1,
            thuTu = 99
        });

        phanHoi.EnsureSuccessStatusCode();

        var duLieu = (await phanHoi.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("duLieu");

        return duLieu.ValueKind == JsonValueKind.String
            ? duLieu.GetString()!
            : duLieu.GetProperty("id").GetString()!;
    }
}
